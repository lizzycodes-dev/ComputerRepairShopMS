using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Entities;
using ComputerRepairSystem.infrastructure.data;
using ComputerRepairSystem_winui.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace ComputerRepairSystem_winui.Pages;

public sealed partial class EmployeeManagementPage : Page
{
    private readonly TenantDbContextFactory _tenantDbFactory;
    private readonly SubscriptionAccessService
    _subscriptionAccessService;

    private bool _hasBranchModule;
    private List<EmployeeRow> _allEmployees = new();

    public EmployeeManagementPage(
        TenantDbContextFactory tenantDbFactory,
        SubscriptionAccessService subscriptionAccessService)
    {
        InitializeComponent();

        _tenantDbFactory = tenantDbFactory;

        _subscriptionAccessService =
            subscriptionAccessService;

        Loaded += EmployeeManagementPage_Loaded;
    }


    // ==========================================
    // EMPLOYEE ROW
    // ==========================================

    private sealed class EmployeeRow
    {
        public Employee Employee { get; set; } = null!;

        public string FullName =>
            string.Join(
                " ",
                new[]
                {
                Employee.FirstName,
                Employee.MiddleName,
                Employee.LastName
                }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

        public string Position =>
            Employee.Position;

        public string DepartmentName =>
            Employee.Department?.DepartmentName
            ?? "—";

        public string BranchName =>
            Employee.Branch?.BranchName
            ?? "—";

        public string HireDateText =>
            Employee.HireDate.ToString("MMM dd, yyyy");

        public string StatusText =>
            Employee.IsActive
                ? "Active"
                : "Inactive";
    }

    // ==========================================
    // PAGE LOADED
    // ==========================================

    private async void EmployeeManagementPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        var modules =
            await _subscriptionAccessService
                .GetAccessibleModuleCodesAsync();

        _hasBranchModule =
            modules.Contains("BRANCH");

        await LoadEmployeesAsync();
    }


    // ==========================================
    // LOAD EMPLOYEES
    // ==========================================

    private async Task LoadEmployeesAsync()
    {
        try
        {
            if (CurrentUser.CompanyId == null)
            {
                EmployeesListView.ItemsSource = null;
                return;
            }

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var employees =
                await db.Employees
                    .AsNoTracking()
                    .Include(x => x.Department)
                    .Include(x => x.Branch)
                    .OrderBy(x => x.LastName)
                    .ThenBy(x => x.FirstName)
                    .ToListAsync();

            _allEmployees =
                employees
                    .Select(x => new EmployeeRow
                    {
                        Employee = x
                    })
                    .ToList();

            ApplySearch();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Loading Employees",
                ex.Message);
        }
    }


    // ==========================================
    // SEARCH
    // ==========================================

    private void SearchBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        ApplySearch();
    }


    private void ApplySearch()
    {
        var search =
            SearchBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(search))
        {
            EmployeesListView.ItemsSource =
                _allEmployees;

            return;
        }

        var filtered =
            _allEmployees
                .Where(x =>
                    x.FullName.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    x.Position.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    (
                        x.Employee.Department?
                            .DepartmentName
                        ?? string.Empty
                    ).Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    (
                        x.Employee.Branch?
                            .BranchName
                        ?? string.Empty
                    ).Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

        EmployeesListView.ItemsSource =
            filtered;
    }


    // ==========================================
    // SELECTION
    // ==========================================

    private void EmployeesListView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        var hasSelection =
            EmployeesListView.SelectedItem != null;

        EditEmployeeButton.IsEnabled =
            hasSelection;

        DeleteEmployeeButton.IsEnabled =
            hasSelection;
    }


    // ==========================================
    // ADD EMPLOYEE
    // ==========================================

    private async void AddEmployeeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            if (CurrentUser.CompanyId == null)
            {
                await ShowMessageAsync(
                    "Access Error",
                    "A company is required to manage employees.");

                return;
            }


            var firstNameBox = new TextBox
            {
                Header = "First Name",
                PlaceholderText = "Enter first name"
            };

            var middleNameBox = new TextBox
            {
                Header = "Middle Name",
                PlaceholderText = "Optional"
            };

            var lastNameBox = new TextBox
            {
                Header = "Last Name",
                PlaceholderText = "Enter last name"
            };

            var phoneBox = new TextBox
            {
                Header = "Phone",
                PlaceholderText = "Enter phone number"
            };

            var emailBox = new TextBox
            {
                Header = "Email",
                PlaceholderText = "Enter email"
            };

            var addressBox = new TextBox
            {
                Header = "Address",
                PlaceholderText = "Enter address"
            };

            var positionBox = new TextBox
            {
                Header = "Job Position",
                PlaceholderText = "Enter job position"
            };

            var hireDatePicker = new DatePicker
            {
                Header = "Hire Date",
                Date = DateTimeOffset.Now
            };


            // ==========================================
            // LOAD DEPARTMENTS
            // ==========================================

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var modules =
                await _subscriptionAccessService
                    .GetAccessibleModuleCodesAsync();

            var hasBranchModule =
                modules.Contains("BRANCH");

            var departments =
                await db.Departments
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.DepartmentName)
                    .ToListAsync();


            var departmentBox = new ComboBox
            {
                Header = "Department",
                PlaceholderText = "Optional",
                DisplayMemberPath = "DepartmentName"
            };

            foreach (var department in departments)
            {
                departmentBox.Items.Add(department);
            }


            // ==========================================
            // LOAD BRANCHES
            // ==========================================

            ComboBox? branchBox = null;

            if (hasBranchModule)
            {
                var branches =
                    await db.Branches
                        .AsNoTracking()
                        .Where(x => x.IsActive)
                        .OrderBy(x => x.BranchName)
                        .ToListAsync();

                branchBox = new ComboBox
                {
                    Header = "Branch",
                    PlaceholderText = "Optional",
                    DisplayMemberPath = "BranchName"
                };

                foreach (var branch in branches)
                {
                    branchBox.Items.Add(branch);
                }
            }


            // ==========================================
            // ACTIVE
            // ==========================================

            var activeCheckBox = new CheckBox
            {
                Content = "Active",
                IsChecked = true
            };


            // ==========================================
            // CONTENT
            // ==========================================

            var panel = new StackPanel
            {
                Spacing = 12
            };

            panel.Children.Add(firstNameBox);
            panel.Children.Add(middleNameBox);
            panel.Children.Add(lastNameBox);

            panel.Children.Add(phoneBox);
            panel.Children.Add(emailBox);
            panel.Children.Add(addressBox);

            panel.Children.Add(positionBox);

            panel.Children.Add(departmentBox);

            if (branchBox != null)
            {
                panel.Children.Add(branchBox);
            }

            panel.Children.Add(hireDatePicker);

            panel.Children.Add(activeCheckBox);


            var scrollViewer = new ScrollViewer
            {
                Content = panel,
                VerticalScrollBarVisibility =
                    ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility =
                    ScrollBarVisibility.Disabled,
                MaxHeight = 550
            };


            var dialog = new ContentDialog
            {
                Title = "Add Employee",

                Content = scrollViewer,

                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",

                DefaultButton =
                    ContentDialogButton.Primary,

                XamlRoot = XamlRoot
            };


            var result =
                await dialog.ShowAsync();

            if (result !=
                ContentDialogResult.Primary)
            {
                return;
            }


            // ==========================================
            // VALIDATION
            // ==========================================

            if (string.IsNullOrWhiteSpace(
                    firstNameBox.Text))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "First name is required.");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                    lastNameBox.Text))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Last name is required.");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                    positionBox.Text))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Job position is required.");

                return;
            }


            // ==========================================
            // CREATE EMPLOYEE
            // ==========================================

            var employee = new Employee
            {
                MasterUserId = null,

                FirstName =
                    firstNameBox.Text.Trim(),

                MiddleName =
                    string.IsNullOrWhiteSpace(
                        middleNameBox.Text)
                        ? null
                        : middleNameBox.Text.Trim(),

                LastName =
                    lastNameBox.Text.Trim(),

                Phone =
                    string.IsNullOrWhiteSpace(
                        phoneBox.Text)
                        ? null
                        : phoneBox.Text.Trim(),

                Email =
                    string.IsNullOrWhiteSpace(
                        emailBox.Text)
                        ? null
                        : emailBox.Text.Trim(),

                Address =
                    string.IsNullOrWhiteSpace(
                        addressBox.Text)
                        ? null
                        : addressBox.Text.Trim(),

                Position =
                    positionBox.Text.Trim(),

                            DepartmentId =
                departmentBox.SelectedItem
                    is Department selectedDepartment
                    ? selectedDepartment.DepartmentId
                    : null,

                BranchId =
                    branchBox?.SelectedItem
                        is Branch selectedBranch
                        ? selectedBranch.BranchId
                        : null,

                HireDate =
                    hireDatePicker.Date.Date,

                IsActive =
                    activeCheckBox.IsChecked == true
            };


            db.Employees.Add(employee);

            await db.SaveChangesAsync();


            await LoadEmployeesAsync();


            await ShowMessageAsync(
                "Employee Added",
                $"Employee '{employee.FirstName} {employee.LastName}' was added successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Add Employee Error",
                ex.ToString());
        }
    }


    // ==========================================
    // EDIT EMPLOYEE
    // ==========================================

    private async void EditEmployeeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (EmployeesListView.SelectedItem
            is not EmployeeRow row)
        {
            return;
        }

        try
        {
            if (CurrentUser.CompanyId == null)
            {
                await ShowMessageAsync(
                    "Access Error",
                    "A company is required.");

                return;
            }


            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var modules =
                await _subscriptionAccessService
                    .GetAccessibleModuleCodesAsync();

            var hasBranchModule =
                modules.Contains("BRANCH");

            var employee =
                await db.Employees
                    .FirstOrDefaultAsync(
                        x =>
                            x.EmployeeId ==
                            row.Employee.EmployeeId);

            if (employee == null)
            {
                await ShowMessageAsync(
                    "Employee Not Found",
                    "The employee record could not be found.");

                return;
            }


            var firstNameBox = new TextBox
            {
                Header = "First Name",
                Text = employee.FirstName
            };

            var middleNameBox = new TextBox
            {
                Header = "Middle Name",
                Text = employee.MiddleName ?? string.Empty
            };

            var lastNameBox = new TextBox
            {
                Header = "Last Name",
                Text = employee.LastName
            };

            var phoneBox = new TextBox
            {
                Header = "Phone",
                Text = employee.Phone ?? string.Empty
            };

            var emailBox = new TextBox
            {
                Header = "Email",
                Text = employee.Email ?? string.Empty
            };

            var addressBox = new TextBox
            {
                Header = "Address",
                Text = employee.Address ?? string.Empty
            };

            var positionBox = new TextBox
            {
                Header = "Job Position",
                Text = employee.Position
            };

            var hireDatePicker = new DatePicker
            {
                Header = "Hire Date",
                Date = employee.HireDate
            };


            var departments =
                await db.Departments
                    .AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.DepartmentName)
                    .ToListAsync();

            var departmentBox = new ComboBox
            {
                Header = "Department",
                PlaceholderText = "Optional",
                DisplayMemberPath = "DepartmentName"
            };

            foreach (var department in departments)
            {
                departmentBox.Items.Add(department);
            }


            if (employee.DepartmentId.HasValue)
            {
                departmentBox.SelectedItem =
                    departments.FirstOrDefault(
                        x =>
                            x.DepartmentId ==
                            employee.DepartmentId.Value);
            }


            ComboBox? branchBox = null;

            if (hasBranchModule)
            {
                var branches =
                    await db.Branches
                        .AsNoTracking()
                        .Where(x => x.IsActive)
                        .OrderBy(x => x.BranchName)
                        .ToListAsync();

                branchBox = new ComboBox
                {
                    Header = "Branch",
                    PlaceholderText = "Optional",
                    DisplayMemberPath = "BranchName"
                };

                foreach (var branch in branches)
                {
                    branchBox.Items.Add(branch);
                }

                if (employee.BranchId.HasValue)
                {
                    branchBox.SelectedItem =
                        branches.FirstOrDefault(
                            x =>
                                x.BranchId ==
                                employee.BranchId.Value);
                }
            }


            var activeCheckBox = new CheckBox
            {
                Content = "Active",
                IsChecked = employee.IsActive
            };


            var panel = new StackPanel
            {
                Spacing = 12
            };

            panel.Children.Add(firstNameBox);
            panel.Children.Add(middleNameBox);
            panel.Children.Add(lastNameBox);

            panel.Children.Add(phoneBox);
            panel.Children.Add(emailBox);
            panel.Children.Add(addressBox);

            panel.Children.Add(positionBox);

            panel.Children.Add(
                departmentBox);

            if (branchBox != null)
            {
                panel.Children.Add(branchBox);
            }

            panel.Children.Add(
                hireDatePicker);

            panel.Children.Add(
                activeCheckBox);


            var scrollViewer = new ScrollViewer
            {
                Content = panel,
                VerticalScrollBarVisibility =
                    ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility =
                    ScrollBarVisibility.Disabled,
                MaxHeight = 550
            };


            var dialog = new ContentDialog
            {
                Title = "Edit Employee",

                Content = scrollViewer,

                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",

                DefaultButton =
                    ContentDialogButton.Primary,

                XamlRoot = XamlRoot
            };


            var result =
                await dialog.ShowAsync();

            if (result !=
                ContentDialogResult.Primary)
            {
                return;
            }


            // ==========================================
            // VALIDATION
            // ==========================================

            if (string.IsNullOrWhiteSpace(
                    firstNameBox.Text))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "First name is required.");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                    lastNameBox.Text))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Last name is required.");

                return;
            }

            if (string.IsNullOrWhiteSpace(
                    positionBox.Text))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Job position is required.");

                return;
            }


            // ==========================================
            // UPDATE
            // ==========================================

            employee.FirstName =
                firstNameBox.Text.Trim();

            employee.MiddleName =
                string.IsNullOrWhiteSpace(
                    middleNameBox.Text)
                    ? null
                    : middleNameBox.Text.Trim();

            employee.LastName =
                lastNameBox.Text.Trim();

            employee.Phone =
                string.IsNullOrWhiteSpace(
                    phoneBox.Text)
                    ? null
                    : phoneBox.Text.Trim();

            employee.Email =
                string.IsNullOrWhiteSpace(
                    emailBox.Text)
                    ? null
                    : emailBox.Text.Trim();

            employee.Address =
                string.IsNullOrWhiteSpace(
                    addressBox.Text)
                    ? null
                    : addressBox.Text.Trim();

            employee.Position =
                positionBox.Text.Trim();

            employee.DepartmentId =
                departmentBox.SelectedItem
                    is Department selectedDepartment
                    ? selectedDepartment.DepartmentId
                    : null;

            employee.BranchId =
                branchBox?.SelectedItem
                    is Branch selectedBranch
                    ? selectedBranch.BranchId
                    : null;

            employee.HireDate =
                hireDatePicker.Date.Date;

            employee.IsActive =
                activeCheckBox.IsChecked == true;


            await db.SaveChangesAsync();


            await LoadEmployeesAsync();


            await ShowMessageAsync(
                "Employee Updated",
                "The employee record was updated successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Edit Employee Error",
                ex.ToString());
        }
    }


    // ==========================================
    // DELETE EMPLOYEE
    // ==========================================

    private async void DeleteEmployeeButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (EmployeesListView.SelectedItem
            is not EmployeeRow row)
        {
            return;
        }


        var confirmDialog = new ContentDialog
        {
            Title = "Delete Employee",

            Content =
                $"Are you sure you want to delete '{row.FullName}'?",

            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",

            DefaultButton =
                ContentDialogButton.Close,

            XamlRoot = XamlRoot
        };


        var result =
            await confirmDialog.ShowAsync();

        if (result !=
            ContentDialogResult.Primary)
        {
            return;
        }


        try
        {
            if (CurrentUser.CompanyId == null)
            {
                await ShowMessageAsync(
                    "Access Error",
                    "A company is required.");

                return;
            }


            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var modules =
                await _subscriptionAccessService
                    .GetAccessibleModuleCodesAsync();

            var hasBranchModule =
                modules.Contains("BRANCH");

            var employee =
                await db.Employees
                    .FirstOrDefaultAsync(
                        x =>
                            x.EmployeeId ==
                            row.Employee.EmployeeId);

            if (employee == null)
            {
                return;
            }


            db.Employees.Remove(employee);

            await db.SaveChangesAsync();


            await LoadEmployeesAsync();


            await ShowMessageAsync(
                "Employee Deleted",
                "The employee record was deleted successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Delete Employee Error",
                ex.ToString());
        }
    }


    // ==========================================
    // MESSAGE HELPER
    // ==========================================

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
}