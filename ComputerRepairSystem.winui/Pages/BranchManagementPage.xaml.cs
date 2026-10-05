using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Entities;
using ComputerRepairSystem.infrastructure.data;
using ComputerRepairSystem_winui.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ComputerRepairSystem_winui.Pages;

public sealed partial class BranchManagementPage : Page
{
    private readonly ITenantDbContextFactory _tenantDbFactory;

    private List<BranchDisplayItem> _allBranches = new();

    private BranchDisplayItem? _selectedBranch;

    public BranchManagementPage(
        ITenantDbContextFactory tenantDbFactory)
    {
        this.InitializeComponent();

        _tenantDbFactory = tenantDbFactory;

        Loaded += BranchManagementPage_Loaded;
    }

    private async void BranchManagementPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        Loaded -= BranchManagementPage_Loaded;

        await LoadBranchesAsync();
    }

    // ============================================================
    // LOAD BRANCHES
    // ============================================================

    private async Task LoadBranchesAsync()
    {
        try
        {
            if (!CurrentUser.CompanyId.HasValue)
            {
                await ShowMessageAsync(
                    "Error",
                    "No company is currently selected.");

                return;
            }

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var branches =
                await db.Branches
                    .AsNoTracking()
                    .OrderBy(x => x.BranchName)
                    .ToListAsync();

            _allBranches =
                branches
                    .Select(x => new BranchDisplayItem
                    {
                        BranchId = x.BranchId,
                        BranchCode = x.BranchCode,
                        BranchName = x.BranchName,
                        Address = x.Address,
                        Phone = x.Phone,
                        Email = x.Email,
                        IsActive = x.IsActive
                    })
                    .ToList();

            ApplyBranchFilter();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error",
                $"Unable to load branches.\n\n{ex.Message}");
        }
    }

    // ============================================================
    // SEARCH
    // ============================================================

    private void BranchSearchBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        ApplyBranchFilter();
    }

    private void ApplyBranchFilter()
    {
        if (!string.IsNullOrWhiteSpace(
                BranchSearchBox?.Text))
        {
            var search =
                BranchSearchBox.Text
                    .Trim()
                    .ToLower();

            BranchesListView.ItemsSource =
                _allBranches
                    .Where(x =>
                        x.BranchCode
                            .Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)
                        ||
                        x.BranchName
                            .Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)
                        ||
                        x.Address
                            .Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)
                        ||
                        (x.Phone ?? string.Empty)
                            .Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase)
                        ||
                        (x.Email ?? string.Empty)
                            .Contains(
                                search,
                                StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }
        else
        {
            BranchesListView.ItemsSource =
                _allBranches.ToList();
        }

        _selectedBranch = null;

        EditBranchButton.IsEnabled = false;
        DeleteBranchButton.IsEnabled = false;
    }

    // ============================================================
    // SELECTION
    // ============================================================

    private void BranchesListView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        _selectedBranch =
            BranchesListView.SelectedItem
                as BranchDisplayItem;

        var hasSelection =
            _selectedBranch != null;

        EditBranchButton.IsEnabled = hasSelection;
        DeleteBranchButton.IsEnabled = hasSelection;
    }

    // ============================================================
    // ADD BRANCH
    // ============================================================

    private async void AddBranchButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ShowBranchDialogAsync(null);
    }

    // ============================================================
    // EDIT BRANCH
    // ============================================================

    private async void EditBranchButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_selectedBranch == null)
            return;

        await ShowBranchDialogAsync(
            _selectedBranch.BranchId);
    }

    // ============================================================
    // ADD / EDIT DIALOG
    // ============================================================

    private async Task ShowBranchDialogAsync(
        int? branchId)
    {
        Branch? branch = null;

        if (branchId.HasValue)
        {
            if (!CurrentUser.CompanyId.HasValue)
            {
                await ShowMessageAsync(
                    "Error",
                    "No company is currently selected.");

                return;
            }

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            branch =
                await db.Branches
                    .FirstOrDefaultAsync(
                        x =>
                            x.BranchId ==
                            branchId.Value);

            if (branch == null)
            {
                await ShowMessageAsync(
                    "Error",
                    "The selected branch could not be found.");

                return;
            }
        }

        var branchCodeTextBox = new TextBox
        {
            Header = "Branch Code",
            PlaceholderText = "Enter branch code",
            Text = branch?.BranchCode ?? string.Empty
        };

        var branchNameTextBox = new TextBox
        {
            Header = "Branch Name",
            PlaceholderText = "Enter branch name",
            Text = branch?.BranchName ?? string.Empty
        };

        var addressTextBox = new TextBox
        {
            Header = "Address",
            PlaceholderText = "Enter branch address",
            Text = branch?.Address ?? string.Empty,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap
        };

        var phoneTextBox = new TextBox
        {
            Header = "Phone",
            PlaceholderText = "Enter phone number",
            Text = branch?.Phone ?? string.Empty
        };

        var emailTextBox = new TextBox
        {
            Header = "Email",
            PlaceholderText = "Enter email address",
            Text = branch?.Email ?? string.Empty
        };

        var activeCheckBox = new CheckBox
        {
            Content = "Branch is active",
            IsChecked = branch?.IsActive ?? true,
            Tag = "IsActive"
        };

        var panel = new StackPanel
        {
            Spacing = 12
        };

        panel.Children.Add(branchCodeTextBox);
        panel.Children.Add(branchNameTextBox);
        panel.Children.Add(addressTextBox);
        panel.Children.Add(phoneTextBox);
        panel.Children.Add(emailTextBox);
        panel.Children.Add(activeCheckBox);

        var dialog = new ContentDialog
        {
            Title = branch == null
                ? "Add Branch"
                : "Edit Branch",

            Content = panel,

            PrimaryButtonText = branch == null
                ? "Add"
                : "Save",

            CloseButtonText = "Cancel",

            DefaultButton = ContentDialogButton.Primary,

            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        var branchCode =
            branchCodeTextBox.Text.Trim();

        var branchName =
            branchNameTextBox.Text.Trim();

        var address =
            addressTextBox.Text.Trim();

        var phone =
            string.IsNullOrWhiteSpace(
                phoneTextBox.Text)
                ? null
                : phoneTextBox.Text.Trim();

        var email =
            string.IsNullOrWhiteSpace(
                emailTextBox.Text)
                ? null
                : emailTextBox.Text.Trim();

        var isActive =
            activeCheckBox.IsChecked == true;

        if (string.IsNullOrWhiteSpace(branchCode))
        {
            await ShowMessageAsync(
                "Validation",
                "Branch code is required.");

            return;
        }

        if (string.IsNullOrWhiteSpace(branchName))
        {
            await ShowMessageAsync(
                "Validation",
                "Branch name is required.");

            return;
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            await ShowMessageAsync(
                "Validation",
                "Branch address is required.");

            return;
        }

        if (!CurrentUser.CompanyId.HasValue)
        {
            await ShowMessageAsync(
                "Error",
                "No company is currently selected.");

            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var duplicateExists =
                await db.Branches
                    .AnyAsync(x =>
                        x.BranchCode == branchCode &&
                        (!branchId.HasValue ||
                         x.BranchId != branchId.Value));

            if (duplicateExists)
            {
                await ShowMessageAsync(
                    "Duplicate Branch Code",
                    "A branch with this branch code already exists.");

                return;
            }

            if (branch == null)
            {
                var newBranch = new Branch
                {
                    BranchCode = branchCode,
                    BranchName = branchName,
                    Address = address,
                    Phone = phone,
                    Email = email,
                    IsActive = isActive,
                    CreatedAt = DateTime.UtcNow
                };

                db.Branches.Add(newBranch);
            }
            else
            {
                branch.BranchCode = branchCode;
                branch.BranchName = branchName;
                branch.Address = address;
                branch.Phone = phone;
                branch.Email = email;
                branch.IsActive = isActive;
            }

            await db.SaveChangesAsync();

            await LoadBranchesAsync();

            await ShowMessageAsync(
                "Success",
                branch == null
                    ? "Branch added successfully."
                    : "Branch updated successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error",
                $"Unable to save the branch.\n\n{ex.Message}");
        }
    }

    // ============================================================
    // DELETE BRANCH
    // ============================================================

    private async void DeleteBranchButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_selectedBranch == null)
            return;

        if (!CurrentUser.CompanyId.HasValue)
        {
            await ShowMessageAsync(
                "Error",
                "No company is currently selected.");

            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var branch =
                await db.Branches
                    .FirstOrDefaultAsync(
                        x =>
                            x.BranchId ==
                            _selectedBranch.BranchId);

            if (branch == null)
            {
                await ShowMessageAsync(
                    "Error",
                    "The selected branch could not be found.");

                return;
            }

            var employeeCount =
                await db.Employees
                    .CountAsync(x =>
                        x.BranchId.HasValue &&
                        x.BranchId.Value ==
                        branch.BranchId);

            if (employeeCount > 0)
            {
                await ShowMessageAsync(
                    "Cannot Delete Branch",
                    $"This branch currently has {employeeCount} employee(s) assigned to it.\n\n" +
                    "Please reassign the employees before deleting the branch.");

                return;
            }

            var confirmDialog = new ContentDialog
            {
                Title = "Delete Branch",
                Content =
                    $"Are you sure you want to delete '{branch.BranchName}'?",

                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",

                DefaultButton = ContentDialogButton.Close,

                XamlRoot = this.XamlRoot
            };

            var result =
                await confirmDialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;

            db.Branches.Remove(branch);

            await db.SaveChangesAsync();

            await LoadBranchesAsync();

            await ShowMessageAsync(
                "Success",
                "Branch deleted successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error",
                $"Unable to delete the branch.\n\n{ex.Message}");
        }
    }

    // ============================================================
    // MESSAGE DIALOG
    // ============================================================

    private async Task ShowMessageAsync(
        string title,
        string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
    }

    // ============================================================
    // DISPLAY MODEL
    // ============================================================

    private sealed class BranchDisplayItem
    {
        public int BranchId { get; set; }

        public string BranchCode { get; set; } = string.Empty;

        public string BranchName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public bool IsActive { get; set; }

        public string Status =>
            IsActive ? "Active" : "Inactive";
    }
}