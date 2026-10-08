using System.Net.Http;
using System.Net.Http.Json;
using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Entities;
using ComputerRepairSystem.company.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;

namespace ComputerRepairSystem.customer.Services;

public class SyncService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ICompanyContext _companyContext;

    public SyncService(
        IHttpClientFactory httpClientFactory,
        ITenantDbContextFactory tenantDbContextFactory,
        ICompanyContext companyContext)
    {
        _httpClientFactory = httpClientFactory;
        _tenantDbContextFactory = tenantDbContextFactory;
        _companyContext = companyContext;
    }

    public async Task SyncPendingAsync()
    {
        if (!_companyContext.CompanyId.HasValue)
            return;

        int companyId = _companyContext.CompanyId.Value;

        await using var db =
            await _tenantDbContextFactory.CreateAsync(companyId);

        var pendingItems = await db.SyncQueues
            .Where(x => !x.IsSynced)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        if (pendingItems.Count == 0)
            return;

        var client =
            _httpClientFactory.CreateClient("FixFlowApi");

        foreach (var queueItem in pendingItems)
        {
            try
            {
                bool success = false;

                switch (queueItem.TableName)
                {
                    case "Customers":
                        success = await SyncCustomerAsync(
                            db,
                            client,
                            companyId,
                            queueItem);

                        break;

                    default:
                        break;
                }

                if (success)
                {
                    queueItem.IsSynced = true;
                    queueItem.SyncedAt = DateTime.UtcNow;

                    await db.SaveChangesAsync();
                }
            }
            catch
            {
                // Leave the queue item unsynced.
                // It will be retried later.
            }
        }
    }

    private async Task<bool> SyncCustomerAsync(
        TenantDbContext db,
        HttpClient client,
        int companyId,
        SyncQueue queueItem)
    {
        Customer? customer = null;

        if (queueItem.Operation != "DELETE")
        {
            customer = await db.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.CustomerId == queueItem.RecordId);

            if (customer == null)
                return false;
        }

        var request = new
        {
            CompanyId = companyId,
            Operation = queueItem.Operation,
            Customer = customer,
            CustomerId =
                queueItem.Operation == "DELETE"
                    ? queueItem.RecordId
                    : (int?)null
        };

        var response = await client.PostAsJsonAsync(
            "api/Customers/sync",
            request);

        return response.IsSuccessStatusCode;
    }
}