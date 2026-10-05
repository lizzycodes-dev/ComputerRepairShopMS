namespace ComputerRepairSystem.domain.Entities;

public class SuperAdminSystemSettings
{
    public int SuperAdminSystemSettingsId { get; set; }

    public string PlatformName { get; set; }
        = "Computer Repair Shop Management System";

    public string? PlatformDescription { get; set; }

    public string? SupportEmail { get; set; }

    public string? SupportPhone { get; set; }

    public bool MaintenanceMode { get; set; }

    public bool AllowNewCompanyRegistration { get; set; } = true;

    public int DefaultTrialDurationInDays { get; set; } = 30;

    public DateTime UpdatedAt { get; set; }
        = DateTime.UtcNow;
}