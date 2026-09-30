using ComputerRepairSystem.infrastructure.Entities;

namespace ComputerRepairSystem_winui.Pages;

public class UserRow
{
    public ApplicationUser User { get; set; } = null!;

    public string UserName =>
        User.UserName ?? string.Empty;

    public string Email =>
        User.Email ?? string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsActive =>
        User.IsActive;
}