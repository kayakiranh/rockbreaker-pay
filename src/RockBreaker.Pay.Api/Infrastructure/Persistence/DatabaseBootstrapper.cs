using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;

namespace RockBreaker.Pay.Infrastructure.Persistence;

/// <summary>
/// TR: Development/local ortamda MSSQL veritabanını oluşturur ve sürümlü SQL scriptlerini sırayla uygular.
/// EN: Creates the MSSQL database and applies versioned SQL scripts in order for development/local environments.
/// Architecture: Lightweight Database Bootstrapper; production migration ownership remains external.
/// </summary>
internal sealed partial class DatabaseBootstrapper
{
    private const int ConnectionRetryCount = 15;
    private static readonly TimeSpan ConnectionRetryDelay = TimeSpan.FromSeconds(2);

    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseBootstrapper> _logger;

    public DatabaseBootstrapper(
        IConfiguration configuration,
        ILogger<DatabaseBootstrapper> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        var applicationBuilder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = applicationBuilder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("DefaultConnection must include a database name.");
        }

        await EnsureDatabaseExistsAsync(applicationBuilder, databaseName, cancellationToken);
        await ApplyScriptsAsync(connectionString, cancellationToken);
    }

    private async Task EnsureDatabaseExistsAsync(
        SqlConnectionStringBuilder applicationBuilder,
        string databaseName,
        CancellationToken cancellationToken)
    {
        var masterBuilder = new SqlConnectionStringBuilder(applicationBuilder.ConnectionString)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(masterBuilder.ConnectionString);
        await OpenWithRetryAsync(connection, cancellationToken);

        var exists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT CASE WHEN DB_ID(@DatabaseName) IS NULL THEN 0 ELSE 1 END;",
                new { DatabaseName = databaseName },
                cancellationToken: cancellationToken));

        if (exists == 1)
        {
            return;
        }

        var safeDatabaseName = databaseName.Replace("]", "]]", StringComparison.Ordinal);
        await connection.ExecuteAsync(
            new CommandDefinition(
                $"CREATE DATABASE [{safeDatabaseName}];",
                cancellationToken: cancellationToken));

        _logger.LogInformation(
            "Created local database {DatabaseName}.",
            databaseName);
    }

    private async Task ApplyScriptsAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        var scriptDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "DatabaseScripts");

        if (!Directory.Exists(scriptDirectory))
        {
            throw new DirectoryNotFoundException(
                $"Database script directory was not found: {scriptDirectory}");
        }

        var scripts = Directory
            .EnumerateFiles(scriptDirectory, "*.sql", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        await using var connection = new SqlConnection(connectionString);
        await OpenWithRetryAsync(connection, cancellationToken);

        const string createVersionTableSql = """
            IF OBJECT_ID('dbo.SchemaVersions', 'U') IS NULL
            BEGIN
                CREATE TABLE dbo.SchemaVersions
                (
                    ScriptName NVARCHAR(260) NOT NULL CONSTRAINT PK_SchemaVersions PRIMARY KEY,
                    AppliedAtUtc DATETIME2(3) NOT NULL
                );
            END;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                createVersionTableSql,
                cancellationToken: cancellationToken));

        foreach (var scriptPath in scripts)
        {
            var scriptName = Path.GetFileName(scriptPath);

            var alreadyApplied = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    SELECT CASE WHEN EXISTS
                    (
                        SELECT 1
                        FROM dbo.SchemaVersions
                        WHERE ScriptName = @ScriptName
                    )
                    THEN 1 ELSE 0 END;
                    """,
                    new { ScriptName = scriptName },
                    cancellationToken: cancellationToken));

            if (alreadyApplied == 1)
            {
                continue;
            }

            var script = await File.ReadAllTextAsync(scriptPath, cancellationToken);
            var batches = GoBatchRegex()
                .Split(script)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var batch in batches)
                {
                    await connection.ExecuteAsync(
                        new CommandDefinition(
                            batch,
                            transaction: transaction,
                            cancellationToken: cancellationToken));
                }

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO dbo.SchemaVersions(ScriptName, AppliedAtUtc)
                        VALUES (@ScriptName, SYSUTCDATETIME());
                        """,
                        new { ScriptName = scriptName },
                        transaction,
                        cancellationToken: cancellationToken));

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Applied database script {ScriptName}.",
                    scriptName);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    private static async Task OpenWithRetryAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= ConnectionRetryCount; attempt++)
        {
            try
            {
                await connection.OpenAsync(cancellationToken);
                return;
            }
            catch (Exception exception) when (attempt < ConnectionRetryCount)
            {
                lastException = exception;
                await Task.Delay(ConnectionRetryDelay, cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "MSSQL could not be reached after the configured bootstrap retries.",
            lastException);
    }

    [GeneratedRegex("""(?im)^\s*GO\s*;?\s*$""")]
    private static partial Regex GoBatchRegex();
}
