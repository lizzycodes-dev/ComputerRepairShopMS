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

public sealed partial class DepartmentManagementPage : Page
{
    private readonly ITenantDbContextFactory _tenantDbFactory;

    private List<DepartmentDisplayItem> _allDepartments = new();

    private DepartmentDisplayItem? _selectedDepartment;

    public DepartmentManagementPage(
        ITenantDbContextFactory tenantDbFactory)
    {
        InitializeComponent();

        _tenantDbFactory = tenantDbFactory;

        Loaded += DepartmentManagementPage_Loaded;
    }

    private async void DepartmentManagementPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        Loaded -= DepartmentManagementPage_Loaded;

        await LoadDepartmentsAsync();
    }

    // ============================================================
    // LOAD DEPARTMENTS
    // ============================================================

    private async Task LoadDepartmentsAsync()
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

            var departments =
                await db.Departments
                    .AsNoTracking()
                    .OrderBy(x => x.DepartmentName)
                    .ToListAsync();

            _allDepartments =
                departments
                    .Select(x => new DepartmentDisplayItem
                    {
                        DepartmentId = x.DepartmentId,
                        DepartmentName = x.DepartmentName,
                        Description = x.Description,
                        IsActive = x.IsActive
                    })
                    .ToList();

            ApplyDepartmentFilter();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error",
                $"Unable to load departments.\n\n{ex.Message}");
        }
    }

    // ============================================================
    // SEARCH
    // ============================================================

    private void DepartmentSearchBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        ApplyDepartmentFilter();
    }

    private void ApplyDepartmentFilter()
    {
        var search =
            DepartmentSearchBox?.Text?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            DepartmentsListView.ItemsSource =
                _allDepartments
                    .Where(x =>
                        x.DepartmentName.Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase)
                        ||
                        (x.Description ?? string.Empty).Contains(
                            search,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
        }
        else
        {
            DepartmentsListView.ItemsSource =
                _allDepartments.ToList();
        }

        _selectedDepartment = null;

        EditDepartmentButton.IsEnabled = false;
        DeleteDepartmentButton.IsEnabled = false;
    }

    // ============================================================
    // SELECTION
    // ============================================================

    private void DepartmentsListView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        _selectedDepartment =
            DepartmentsListView.SelectedItem
                as DepartmentDisplayItem;

        var hasSelection =
            _selectedDepartment != null;

        EditDepartmentButton.IsEnabled = hasSelection;
        DeleteDepartmentButton.IsEnabled = hasSelection;
    }

    // ============================================================
    // ADD DEPARTMENT
    // ============================================================

    private async void AddDepartmentButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ShowDepartmentDialogAsync(null);
    }

    // ============================================================
    // EDIT DEPARTMENT
    // ============================================================

    private async void EditDepartmentButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_selectedDepartment == null)
            return;

        await ShowDepartmentDialogAsync(
            _selectedDepartment.DepartmentId);
    }

    // ============================================================
    // ADD / EDIT DIALOG
    // ============================================================

    private async Task ShowDepartmentDialogAsync(
        int? departmentId)
    {
        if (!CurrentUser.CompanyId.HasValue)
        {
            await ShowMessageAsync(
                "Error",
                "No company is currently selected.");

            return;
        }

        // ============================================================
        // LOAD EXISTING DEPARTMENT FOR EDITING
        // ============================================================

        Department? department = null;

        if (departmentId.HasValue)
        {
            await using var loadDb =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            department =
                await loadDb.Departments
                    .FirstOrDefaultAsync(
                        x =>
                            x.DepartmentId ==
                            departmentId.Value);

            if (department == null)
            {
                await ShowMessageAsync(
                    "Error",
                    "The selected department could not be found.");

                return;
            }
        }

        // ============================================================
        // INPUT FIELDS
        // ============================================================

        var departmentNameTextBox = new TextBox
        {
            Header = "Department Name",
            PlaceholderText = "Enter department name",
            Text = department?.DepartmentName ?? string.Empty
        };

        var descriptionTextBox = new TextBox
        {
            Header = "Description",
            PlaceholderText = "Enter department description",
            Text = department?.Description ?? string.Empty,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            Height = 100
        };

        var activeCheckBox = new CheckBox
        {
            Content = "Department is active",
            IsChecked = department?.IsActive ?? true
        };

        var panel = new StackPanel
        {
            Spacing = 12
        };

        panel.Children.Add(departmentNameTextBox);
        panel.Children.Add(descriptionTextBox);
        panel.Children.Add(activeCheckBox);

        // ============================================================
        // DIALOG
        // ============================================================

        var dialog = new ContentDialog
        {
            Title = department == null
                ? "Add Department"
                : "Edit Department",

            Content = panel,

            PrimaryButtonText = department == null
                ? "Add"
                : "Save",

            CloseButtonText = "Cancel",

            DefaultButton =
                ContentDialogButton.Primary,

            XamlRoot = XamlRoot
        };

        var result =
            await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        // ============================================================
        // GET VALUES
        // ============================================================

        var departmentName =
            departmentNameTextBox.Text.Trim();

        var description =
            string.IsNullOrWhiteSpace(
                descriptionTextBox.Text)
                ? null
                : descriptionTextBox.Text.Trim();

        var isActive =
            activeCheckBox.IsChecked == true;

        // ============================================================
        // VALIDATION
        // ============================================================

        if (string.IsNullOrWhiteSpace(departmentName))
        {
            await ShowMessageAsync(
                "Validation",
                "Department name is required.");

            return;
        }

        try
        {
            // ========================================================
            // CREATE ONE DB CONTEXT FOR THE SAVE OPERATION
            // ========================================================

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            // ========================================================
            // CHECK DUPLICATE NAME
            // ========================================================

            var duplicateExists =
                await db.Departments
                    .AnyAsync(x =>
                        x.DepartmentName == departmentName
                        &&
                        (!departmentId.HasValue
                         ||
                         x.DepartmentId !=
                         departmentId.Value));

            if (duplicateExists)
            {
                await ShowMessageAsync(
                    "Duplicate Department",
                    "A department with this name already exists.");

                return;
            }

            // ========================================================
            // ADD
            // ========================================================

            if (!departmentId.HasValue)
            {
                var newDepartment = new Department
                {
                    DepartmentName = departmentName,
                    Description = description,
                    IsActive = isActive,
                    CreatedAt = DateTime.UtcNow
                };

                db.Departments.Add(newDepartment);
            }
            // ========================================================
            // EDIT
            // ========================================================
            else
            {
                // IMPORTANT:
                // Load the department using THIS DbContext so EF Core
                // tracks the entity correctly.

                var existingDepartment =
                    await db.Departments
                        .FirstOrDefaultAsync(
                            x =>
                                x.DepartmentId ==
                                departmentId.Value);

                if (existingDepartment == null)
                {
                    await ShowMessageAsync(
                        "Error",
                        "The selected department could not be found.");

                    return;
                }

                existingDepartment.DepartmentName =
                    departmentName;

                existingDepartment.Description =
                    description;

                existingDepartment.IsActive =
                    isActive;
            }

            // ========================================================
            // SAVE
            // ========================================================

            await db.SaveChangesAsync();

            // ========================================================
            // REFRESH LIST
            // ========================================================

            await LoadDepartmentsAsync();

            await ShowMessageAsync(
                "Success",
                departmentId.HasValue
                    ? "Department updated successfully."
                    : "Department added successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error",
                $"Unable to save the department.\n\n{ex.Message}");
        }
    }

    // ============================================================
    // DELETE DEPARTMENT
    // ============================================================

    private async void DeleteDepartmentButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_selectedDepartment == null)
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

            var department =
                await db.Departments
                    .FirstOrDefaultAsync(
                        x =>
                            x.DepartmentId ==
                            _selectedDepartment.DepartmentId);

            if (department == null)
            {
                await ShowMessageAsync(
                    "Error",
                    "The selected department could not be found.");

                return;
            }

            var employeeCount =
                await db.Employees
                    .CountAsync(x =>
                        x.DepartmentId.HasValue &&
                        x.DepartmentId.Value ==
                        department.DepartmentId);

            if (employeeCount > 0)
            {
                await ShowMessageAsync(
                    "Cannot Delete Department",
                    $"This department currently has {employeeCount} employee(s) assigned to it.\n\n" +
                    "Please reassign the employees before deleting the department.");

                return;
            }

            var confirmDialog = new ContentDialog
            {
                Title = "Delete Department",

                Content =
                    $"Are you sure you want to delete '{department.DepartmentName}'?",

                PrimaryButtonText = "Delete",

                CloseButtonText = "Cancel",

                DefaultButton =
                    ContentDialogButton.Close,

                XamlRoot = XamlRoot
            };

            var result =
                await confirmDialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;

            db.Departments.Remove(department);

            await db.SaveChangesAsync();

            await LoadDepartmentsAsync();

            await ShowMessageAsync(
                "Success",
                "Department deleted successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error",
                $"Unable to delete the department.\n\n{ex.Message}");
        }
    }

    // ============================================================
    // MESSAGE
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
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }

    // ============================================================
    // DISPLAY MODEL
    // ============================================================

    private sealed class DepartmentDisplayItem
    {
        public int DepartmentId { get; set; }

        public string DepartmentName { get; set; }
            = string.Empty;

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public string Status =>
            IsActive
                ? "Active"
                : "Inactive";
    }
}