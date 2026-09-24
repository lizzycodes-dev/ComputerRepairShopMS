using ComputerRepairSystem.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ComputerRepairSystem_winui.Services;

public class SubscriptionAccessService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SubscriptionAccessService(
        IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }


    // ==========================================
    // GET ACCESSIBLE MODULES
    // ==========================================

    public async Task<HashSet<string>>
        GetAccessibleModuleCodesAsync()
    {
        // Super Admin is not restricted
        // by company subscriptions.

        if (CurrentUser.Role == "Super Admin")
        {
            return new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
        }


        if (CurrentUser.CompanyId is not int companyId)
        {
            return new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);
        }


        using var scope =
            _scopeFactory.CreateScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<MasterErpDbContext>();


        var now = DateTime.UtcNow;


        var moduleCodes =
            await (
                from subscription
                    in db.Subscriptions.AsNoTracking()

                join planModule
                    in db.SubscriptionPlanModules.AsNoTracking()
                    on subscription.SubscriptionPlanId
                    equals planModule.SubscriptionPlanId

                join module
                    in db.ModuleDefinitions.AsNoTracking()
                    on planModule.ModuleDefinitionId
                    equals module.ModuleDefinitionId

                where
                    subscription.CompanyId == companyId
                    && subscription.Status == "Active"
                    && subscription.StartDate <= now
                    && (
                        subscription.EndDate == null
                        || subscription.EndDate > now
                    )
                    && module.IsActive

                select module.ModuleCode
            )
            .Distinct()
            .ToListAsync();


        return new HashSet<string>(
            moduleCodes,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<HashSet<string>>
        GetAccessibleModuleCodesAsync(int companyId)
    {
        // ==========================================
        // GET MODULES FOR SPECIFIC COMPANY
        // ==========================================

        using var scope =
            _scopeFactory.CreateScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<MasterErpDbContext>();

        var now = DateTime.UtcNow;

        var moduleCodes =
            await (
                from subscription
                    in db.Subscriptions.AsNoTracking()

                join planModule
                    in db.SubscriptionPlanModules.AsNoTracking()
                    on subscription.SubscriptionPlanId
                    equals planModule.SubscriptionPlanId

                join module
                    in db.ModuleDefinitions.AsNoTracking()
                    on planModule.ModuleDefinitionId
                    equals module.ModuleDefinitionId

                where
                    subscription.CompanyId == companyId
                    && subscription.Status == "Active"
                    && subscription.StartDate <= now
                    && (
                        subscription.EndDate == null
                        || subscription.EndDate > now
                    )
                    && module.IsActive

                select module.ModuleCode
            )
            .Distinct()
            .ToListAsync();

        return new HashSet<string>(
            moduleCodes,
            StringComparer.OrdinalIgnoreCase);
    }
}