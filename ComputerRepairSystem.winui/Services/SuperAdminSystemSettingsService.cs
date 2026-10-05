using ComputerRepairSystem.domain.Entities;
using ComputerRepairSystem.infrastructure.data;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairSystem_winui.Services;

public class SuperAdminSystemSettingsService
{
    private readonly MasterErpDbContext _masterDb;

    public SuperAdminSystemSettingsService(
        MasterErpDbContext masterDb)
    {
        _masterDb = masterDb;
    }

    public async Task<SuperAdminSystemSettings>
        GetSettingsAsync()
    {
        var settings =
            await _masterDb
                .SuperAdminSystemSettings
                .FirstOrDefaultAsync();

        if (settings == null)
        {
            settings =
                new SuperAdminSystemSettings();

            _masterDb
                .SuperAdminSystemSettings
                .Add(settings);

            await _masterDb.SaveChangesAsync();
        }

        return settings;
    }

    public async Task<bool>
        IsMaintenanceModeEnabledAsync()
    {
        var settings =
            await GetSettingsAsync();

        return settings.MaintenanceMode;
    }
}