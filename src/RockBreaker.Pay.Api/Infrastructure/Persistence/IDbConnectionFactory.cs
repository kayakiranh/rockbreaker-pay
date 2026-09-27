using System.Data;

namespace RockBreaker.Pay.Infrastructure.Persistence;

/// <summary>
/// TR: Dapper kullanan repository'ler için veritabanı bağlantısı üretir.
/// EN: Creates database connections for repositories using Dapper.
/// Architecture: Factory Pattern + Dependency Inversion.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// TR: Çağıranın yöneteceği yeni bir MSSQL bağlantısı oluşturur.
    /// EN: Creates a new MSSQL connection whose lifetime is managed by the caller.
    /// Architecture: Factory Method.
    /// </summary>
    /// <returns>TR: Veritabanı bağlantısı. EN: Database connection.</returns>
    IDbConnection CreateConnection();
}
