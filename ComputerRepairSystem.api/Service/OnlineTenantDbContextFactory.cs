using ComputerRepairSystem.company.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
namespace ComputerRepairSystem.api.Services;

public class OnlineTenantDbContextFactory
{
    private readonly IConfiguration _configuration;

    public OnlineTenantDbContextFactory(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TenantDbContext Create(int companyId)
    {
        string? connectionString = companyId switch
        {
            1 => _configuration.GetConnectionString("Tenant1Online"),

            2 => _configuration.GetConnectionString("Tenant2Online"),

            3 => _configuration.GetConnectionString("Tenant3Online"),

            _ => null
        };

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No online tenant database is configured for CompanyId {companyId}.");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                $"No online tenant database is configured for CompanyId {companyId}.");

        var sqlBuilder = new SqlConnectionStringBuilder(connectionString);

        Console.WriteLine(
            $"ONLINE SQL TEST -> Server: {sqlBuilder.DataSource}");

        Console.WriteLine(
            $"ONLINE SQL TEST -> Database: {sqlBuilder.InitialCatalog}");

        Console.WriteLine(
            $"ONLINE SQL TEST -> Encrypt: {sqlBuilder.Encrypt}");
        var options =
            new DbContextOptionsBuilder<TenantDbContext>()
                .UseSqlServer(connectionString)
                .Options;

        return new TenantDbContext(options);
    }
}