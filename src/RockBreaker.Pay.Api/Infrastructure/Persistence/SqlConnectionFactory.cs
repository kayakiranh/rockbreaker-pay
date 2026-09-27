using System.Data;
using Microsoft.Data.SqlClient;

namespace RockBreaker.Pay.Infrastructure.Persistence;

/// <summary>
/// TR: IConfiguration içindeki connection string ile SqlConnection üretir.
/// EN: Creates SqlConnection instances from the connection string in IConfiguration.
/// Architecture: Infrastructure Adapter + Factory Pattern.
/// </summary>
public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// TR: SQL bağlantı fabrikasını oluşturur.
    /// EN: Creates the SQL connection factory.
    /// Architecture: Constructor Injection.
    /// </summary>
    /// <param name="configuration">TR: Uygulama ayarları. EN: Application configuration.</param>
    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
    }

    /// <summary>
    /// TR: Yeni bir SqlConnection döndürür.
    /// EN: Returns a new SqlConnection.
    /// Architecture: Factory Method.
    /// </summary>
    /// <returns>TR: SQL bağlantısı. EN: SQL connection.</returns>
    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
