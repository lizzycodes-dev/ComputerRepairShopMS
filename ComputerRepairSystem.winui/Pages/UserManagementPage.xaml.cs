using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Entities;
using ComputerRepairSystem.domain.Entities;
using ComputerRepairSystem.infrastructure.data;
using ComputerRepairSystem.infrastructure.Entities;
using ComputerRepairSystem_winui.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComputerRepairSystem_winui.Pages;

public sealed partial class UserManagementPage : Page
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MasterErpDbContext _masterDb;
    private readonly TenantDbContextFactory _tenantDbFactory;
    private readonly SubscriptionAccessService _subscriptionAccessService;

    // ==========================================
    // PAGINATION STATE
    // ==========================================

    private List<UserRow> _allUserRows = new();
    private int _currentPage = 1;
    private const int _pageSize = 10;


    public UserManagementPage(
        UserManager<ApplicationUser> userManager,
        MasterErpDbContext masterDb,
        TenantDbContextFactory tenantDbFactory,
        SubscriptionAccessService subscriptionAccessService)
    {
        InitializeComponent();

        _userManager = userManager;
        _masterDb = masterDb;
        _tenantDbFactory = tenantDbFactory;
        _subscriptionAccessService = subscriptionAccessService;

        Loaded += UserManagementPage_Loaded;
    }


    // ==========================================
    // PAGE LOADED
    // ==========================================

    private async void UserManagementPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await LoadUsersAsync();
    }


    // ==========================================
    // LOAD USERS
    // ==========================================

    private async Task LoadUsersAsync()
    {
        try
        {
            var usersQuery =
                _userManager.Users
                    .AsNoTracking();

            // ==========================================
            // USER VISIBILITY BY ROLE
            // ==========================================

            if (CurrentUser.Role == "Super Admin")
            {
                // Super Admin can see all users.
            }
            else if (CurrentUser.Role == "Admin")
            {
                usersQuery =
                    usersQuery.Where(
                        u => u.CompanyId == CurrentUser.CompanyId);
            }
            else
            {
                usersQuery =
                    usersQuery.Where(u => false);
            }

            var users =
                await usersQuery.ToListAsync();


            // ==========================================
            // LOAD COMPANIES
            // ==========================================

            var companies =
                await _masterDb.Companies
                    .AsNoTracking()
                    .ToListAsync();


            // ==========================================
            // BUILD USER ROWS
            // ==========================================

            _allUserRows = new List<UserRow>();

            foreach (var user in users)
            {
                var company =
                    companies.FirstOrDefault(
                        c => c.CompanyId == user.CompanyId);

                var roles =
                    await _userManager.GetRolesAsync(user);

                var roleName =
                    roles.FirstOrDefault()
                    ?? "No Role";

                _allUserRows.Add(
                    new UserRow
                    {
                        User = user,

                        CompanyName =
                            company?.CompanyName
                            ?? "System / No Company",

                        Role = roleName
                    });
            }

            // Newest first — sort by Id descending.
            // ApplicationUser.Id is a GUID string, so this
            // gives a stable, deterministic order for paging.
            _allUserRows =
                _allUserRows
                    .OrderByDescending(r => r.User.Id)
                    .ToList();

            _currentPage = 1;

            UpdatePagination();

            EditUserButton.IsEnabled =
                UsersListView.SelectedItem != null;

            DeleteUserButton.IsEnabled =
                UsersListView.SelectedItem != null;
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Loading Users",
                ex.Message);
        }
    }


    // ==========================================
    // PAGINATION
    // ==========================================

    private void UpdatePagination()
    {
        var totalPages =
            Math.Max(
                1,
                (int)Math.Ceiling(
                    (double)_allUserRows.Count /
                    _pageSize));

        if (_currentPage > totalPages)
            _currentPage = totalPages;

        if (_currentPage < 1)
            _currentPage = 1;

        var paged =
            _allUserRows
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

        UsersListView.ItemsSource = paged;

        PageInfoText.Text =
            $"Page {_currentPage} of {totalPages}  •  " +
            $"{_allUserRows.Count} total";

        PreviousPageButton.IsEnabled = _currentPage > 1;
        NextPageButton.IsEnabled = _currentPage < totalPages;
    }


    private void PreviousPageButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_currentPage <= 1)
            return;

        _currentPage--;

        UpdatePagination();
    }


    private void NextPageButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var totalPages =
            Math.Max(
                1,
                (int)Math.Ceiling(
                    (double)_allUserRows.Count /
                    _pageSize));

        if (_currentPage >= totalPages)
            return;

        _currentPage++;

        UpdatePagination();
    }


    // ==========================================
    // SELECTION CHANGED
    // ==========================================

    private void UsersListView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        bool hasSelection =
            UsersListView.SelectedItem != null;

        EditUserButton.IsEnabled = hasSelection;
        DeleteUserButton.IsEnabled = hasSelection;
    }


    // ==========================================
    // ADD USER
    // ==========================================

    private async void AddUserButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            var usernameBox = new TextBox
            {
                Header = "Username",
                PlaceholderText = "Enter username"
            };

            var emailBox = new TextBox
            {
                Header = "Email",
                PlaceholderText = "Enter email"
            };

            var passwordBox = new PasswordBox
            {
                Header = "Password",
                PlaceholderText = "Enter password"
            };

            var roleBox = new ComboBox
            {
                Header = "Role",
                PlaceholderText = "Select a role"
            };

            roleBox.Items.Add("Admin");
            roleBox.Items.Add("Technician");
            roleBox.Items.Add("Receptionist");

            if (CurrentUser.Role == "Super Admin")
            {
                roleBox.Items.Insert(0, "Super Admin");
            }

            roleBox.SelectedIndex = 0;


            var firstNameBox = new TextBox
            {
                Header = "First Name",
                PlaceholderText = "Enter first name"
            };

            var middleNameBox = new TextBox
            {
                Header = "Middle Name",
                PlaceholderText = "Enter middle name"
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


            roleBox.SelectionChanged += (_, _) =>
            {
                if (roleBox.SelectedItem is not string selectedRole)
                    return;

                positionBox.Text =
                    selectedRole switch
                    {
                        "Admin" => "Administrator",
                        "Technician" => "Computer Technician",
                        "Receptionist" => "Receptionist",
                        "HR Staff" => "HR Staff",
                        "Finance Staff" => "Finance Staff",
                        "Super Admin" => "",
                        _ => ""
                    };
            };

            var hireDatePicker = new DatePicker
            {
                Header = "Hire Date",
                Date = DateTimeOffset.Now
            };


            var companies =
                await _masterDb.Companies
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CompanyName)
                    .ToListAsync();

            var companyBox = new ComboBox
            {
                Header = "Company",
                PlaceholderText = "Select a company",
                DisplayMemberPath = "CompanyName"
            };

            if (CurrentUser.Role == "Admin")
            {
                companyBox.IsEnabled = false;
            }

            foreach (var company in companies)
            {
                companyBox.Items.Add(company);
            }

            if (CurrentUser.Role == "Admin")
            {
                var myCompany =
                    companies.FirstOrDefault(
                        c => c.CompanyId == CurrentUser.CompanyId);

                companyBox.SelectedItem = myCompany;
            }
            else
            {
                if (companies.Count > 0)
                {
                    companyBox.SelectedIndex = 0;
                }
            }


            if (companyBox.SelectedItem is Company selectedCompany)
            {
                var accessibleModules =
                    await _subscriptionAccessService
                        .GetAccessibleModuleCodesAsync(
                            selectedCompany.CompanyId);

                if (accessibleModules.Contains("EMPLOYEE") &&
                    accessibleModules.Contains("ATTENDANCE") &&
                    accessibleModules.Contains("PAYROLL"))
                {
                    roleBox.Items.Add("HR Staff");
                }

                if (accessibleModules.Contains("FINANCE"))
                {
                    roleBox.Items.Add("Finance Staff");
                }
            }


            companyBox.SelectionChanged += async (_, _) =>
            {
                if (companyBox.SelectedItem is not Company changedCompany)
                    return;

                roleBox.Items.Clear();

                if (CurrentUser.Role == "Super Admin")
                {
                    roleBox.Items.Add("Super Admin");
                }

                roleBox.Items.Add("Admin");
                roleBox.Items.Add("Technician");
                roleBox.Items.Add("Receptionist");

                var accessibleModules =
                    await _subscriptionAccessService
                        .GetAccessibleModuleCodesAsync(
                            changedCompany.CompanyId);

                if (accessibleModules.Contains("EMPLOYEE") &&
                    accessibleModules.Contains("ATTENDANCE") &&
                    accessibleModules.Contains("PAYROLL"))
                {
                    roleBox.Items.Add("HR Staff");
                }

                if (accessibleModules.Contains("FINANCE"))
                {
                    roleBox.Items.Add("Finance Staff");
                }

                roleBox.SelectedIndex = 0;
            };


            var activeCheckBox = new CheckBox
            {
                Content = "Active",
                IsChecked = true
            };


            var panel = new StackPanel
            {
                Spacing = 12
            };

            panel.Children.Add(
                new TextBlock
                {
                    Text = "Account Information",
                    FontSize = 18,
                    FontWeight =
                        Microsoft.UI.Text.FontWeights.SemiBold
                });

            panel.Children.Add(usernameBox);
            panel.Children.Add(emailBox);
            panel.Children.Add(passwordBox);
            panel.Children.Add(roleBox);
            panel.Children.Add(companyBox);

            panel.Children.Add(
                new TextBlock
                {
                    Text = "Employee Information",
                    FontSize = 18,
                    FontWeight =
                        Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 12, 0, 0)
                });

            panel.Children.Add(firstNameBox);
            panel.Children.Add(middleNameBox);
            panel.Children.Add(lastNameBox);
            panel.Children.Add(phoneBox);
            panel.Children.Add(addressBox);
            panel.Children.Add(positionBox);
            panel.Children.Add(hireDatePicker);
            panel.Children.Add(activeCheckBox);


            var scrollViewer = new ScrollViewer
            {
                Content = panel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = 550
            };

            var dialog = new ContentDialog
            {
                Title = "Add User",
                Content = scrollViewer,

                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",

                DefaultButton = ContentDialogButton.Primary,

                XamlRoot = XamlRoot
            };

            var result =
                await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;


            // ==========================================
            // VALIDATION
            // ==========================================

            if (string.IsNullOrWhiteSpace(usernameBox.Text))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Username is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(emailBox.Text))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Email is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Password is required.");
                return;
            }

            if (roleBox.SelectedItem == null)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select a role.");
                return;
            }

            if (string.IsNullOrWhiteSpace(firstNameBox.Text))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "First name is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(lastNameBox.Text))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Last name is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(positionBox.Text))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Position is required.");
                return;
            }


            var selectedRole =
                roleBox.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(selectedRole))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select a role.");
                return;
            }


            int? companyId = null;

            if (selectedRole == "Super Admin")
            {
                companyId = null;
            }
            else if (CurrentUser.Role == "Admin")
            {
                if (CurrentUser.CompanyId == null)
                {
                    await ShowMessageAsync(
                        "Validation Error",
                        "Unable to determine your company.");
                    return;
                }

                companyId =
                    CurrentUser.CompanyId.Value;
            }
            else
            {
                if (companyBox.SelectedItem is not Company subscriptionCompany)
                {
                    await ShowMessageAsync(
                        "Validation Error",
                        "Please select a company.");
                    return;
                }

                companyId =
                    subscriptionCompany.CompanyId;
            }


            if (selectedRole == "HR Staff" ||
                selectedRole == "Finance Staff")
            {
                if (companyId is not int selectedCompanyId)
                {
                    await ShowMessageAsync(
                        "Validation Error",
                        "A company is required for this role.");
                    return;
                }

                var accessibleModules =
                    await _subscriptionAccessService
                        .GetAccessibleModuleCodesAsync(
                            selectedCompanyId);

                if (selectedRole == "HR Staff" &&
                    (!accessibleModules.Contains("EMPLOYEE") ||
                     !accessibleModules.Contains("ATTENDANCE") ||
                     !accessibleModules.Contains("PAYROLL")))
                {
                    await ShowMessageAsync(
                        "Modules Not Available",
                        "The selected company does not have all required HR modules.");
                    return;
                }

                if (selectedRole == "Finance Staff" &&
                    !accessibleModules.Contains("FINANCE"))
                {
                    await ShowMessageAsync(
                        "Module Not Available",
                        "The selected company does not have the Finance module.");
                    return;
                }
            }


            var existingUsername =
                await _userManager.FindByNameAsync(
                    usernameBox.Text.Trim());

            if (existingUsername != null)
            {
                await ShowMessageAsync(
                    "User Already Exists",
                    "That username is already being used.");
                return;
            }


            var existingEmail =
                await _userManager.FindByEmailAsync(
                    emailBox.Text.Trim());

            if (existingEmail != null)
            {
                await ShowMessageAsync(
                    "Email Already Exists",
                    "That email is already being used.");
                return;
            }


            var user = new ApplicationUser
            {
                UserName = usernameBox.Text.Trim(),
                Email = emailBox.Text.Trim(),
                CompanyId = companyId,
                IsActive = activeCheckBox.IsChecked == true
            };


            var createResult =
                await _userManager.CreateAsync(
                    user,
                    passwordBox.Password);

            if (!createResult.Succeeded)
            {
                await ShowIdentityErrorsAsync(
                    "Unable to Add User",
                    createResult);
                return;
            }


            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    selectedRole);

            if (!roleResult.Succeeded)
            {
                await ShowIdentityErrorsAsync(
                    "Unable to Assign Role",
                    roleResult);

                await _userManager.DeleteAsync(user);
                return;
            }


            if (selectedRole == "Super Admin")
            {
                await LoadUsersAsync();

                await ShowMessageAsync(
                    "User Added",
                    $"Super Admin '{user.UserName}' was created successfully.");
                return;
            }


            if (!companyId.HasValue)
            {
                await ShowMessageAsync(
                    "Employee Creation Error",
                    "A company is required for employee users.");

                await _userManager.RemoveFromRoleAsync(
                    user,
                    selectedRole);

                await _userManager.DeleteAsync(user);
                return;
            }

            try
            {
                await using var tenantDb =
                    await _tenantDbFactory.CreateAsync(
                        companyId.Value);

                var employee = new Employee
                {
                    MasterUserId = user.Id,

                    BranchId = null,
                    DepartmentId = null,

                    FirstName = firstNameBox.Text.Trim(),

                    MiddleName =
                        string.IsNullOrWhiteSpace(middleNameBox.Text)
                            ? null
                            : middleNameBox.Text.Trim(),

                    LastName = lastNameBox.Text.Trim(),

                    Phone =
                        string.IsNullOrWhiteSpace(phoneBox.Text)
                            ? null
                            : phoneBox.Text.Trim(),

                    Email = emailBox.Text.Trim(),

                    Address =
                        string.IsNullOrWhiteSpace(addressBox.Text)
                            ? null
                            : addressBox.Text.Trim(),

                    Position = positionBox.Text.Trim(),

                    HireDate = hireDatePicker.Date.Date,

                    IsActive = activeCheckBox.IsChecked == true
                };

                tenantDb.Employees.Add(employee);

                await tenantDb.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await _userManager.RemoveFromRoleAsync(
                    user,
                    selectedRole);

                await _userManager.DeleteAsync(user);

                await ShowMessageAsync(
                    "Employee Creation Error",
                    $"The user could not be created because the employee record failed.\n\n{ex.Message}");

                return;
            }


            await LoadUsersAsync();

            await ShowMessageAsync(
                "User Added",
                $"User '{user.UserName}' and the employee record were created successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Add User Error",
                ex.ToString());

            System.Diagnostics.Debug.WriteLine(ex);
        }
    }


    // ==========================================
    // EDIT USER
    // ==========================================

    private async void EditUserButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (UsersListView.SelectedItem
            is not UserRow selectedRow)
        {
            await ShowMessageAsync(
                "No User Selected",
                "Please select a user first.");
            return;
        }

        var selectedUser = selectedRow.User;

        try
        {
            var user =
                await _userManager.FindByIdAsync(
                    selectedUser.Id);

            if (user == null)
            {
                await ShowMessageAsync(
                    "User Not Found",
                    "The selected user could not be found.");

                await LoadUsersAsync();
                return;
            }

            var currentRoles =
                await _userManager.GetRolesAsync(user);

            var currentRole =
                currentRoles.FirstOrDefault();


            if (CurrentUser.Role == "Admin" &&
                user.CompanyId != CurrentUser.CompanyId)
            {
                await ShowMessageAsync(
                    "Access Denied",
                    "You can only edit users from your own company.");
                return;
            }

            var isSuperAdmin =
                await _userManager.IsInRoleAsync(
                    user,
                    "Super Admin");

            if (CurrentUser.Role == "Admin" &&
                isSuperAdmin)
            {
                await ShowMessageAsync(
                    "Access Denied",
                    "You cannot edit a Super Admin.");
                return;
            }


            var usernameBox = new TextBox
            {
                Header = "Username",
                Text = user.UserName ?? string.Empty,
                PlaceholderText = "Enter username"
            };

            var emailBox = new TextBox
            {
                Header = "Email",
                Text = user.Email ?? string.Empty,
                PlaceholderText = "Enter email"
            };

            var roleBox = new ComboBox
            {
                Header = "Role",
                PlaceholderText = "Select a role"
            };

            var companies =
                await _masterDb.Companies
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CompanyName)
                    .ToListAsync();

            var companyBox = new ComboBox
            {
                Header = "Company",
                PlaceholderText = "Select a company",
                DisplayMemberPath = "CompanyName"
            };

            foreach (var company in companies)
            {
                companyBox.Items.Add(company);
            }

            var currentCompany =
                companies.FirstOrDefault(
                    c => c.CompanyId == user.CompanyId);

            if (CurrentUser.Role == "Admin")
            {
                var myCompany =
                    companies.FirstOrDefault(
                        c => c.CompanyId == CurrentUser.CompanyId);

                companyBox.SelectedItem = myCompany;
                companyBox.IsEnabled = false;
            }
            else
            {
                companyBox.SelectedItem = currentCompany;
            }


            async Task LoadRolesForCompanyAsync(
                Company? selectedCompanyForRoles,
                string? roleToSelect = null)
            {
                roleBox.Items.Clear();

                roleBox.Items.Add("Admin");
                roleBox.Items.Add("Technician");
                roleBox.Items.Add("Receptionist");

                if (CurrentUser.Role == "Super Admin")
                {
                    roleBox.Items.Insert(0, "Super Admin");
                }

                if (selectedCompanyForRoles != null)
                {
                    var accessibleModules =
                        await _subscriptionAccessService
                            .GetAccessibleModuleCodesAsync(
                                selectedCompanyForRoles.CompanyId);

                    if (accessibleModules.Contains("EMPLOYEE") &&
                        accessibleModules.Contains("ATTENDANCE") &&
                        accessibleModules.Contains("PAYROLL"))
                    {
                        roleBox.Items.Add("HR Staff");
                    }

                    if (accessibleModules.Contains("FINANCE"))
                    {
                        roleBox.Items.Add("Finance Staff");
                    }
                }

                if (!string.IsNullOrWhiteSpace(roleToSelect) &&
                    roleBox.Items.Contains(roleToSelect))
                {
                    roleBox.SelectedItem = roleToSelect;
                }
                else
                {
                    roleBox.SelectedIndex = 0;
                }
            }

            await LoadRolesForCompanyAsync(
                companyBox.SelectedItem as Company,
                currentRole);


            companyBox.SelectionChanged += async (_, _) =>
            {
                if (companyBox.SelectedItem
                    is not Company changedCompany)
                {
                    return;
                }

                await LoadRolesForCompanyAsync(
                    changedCompany);
            };

            var activeCheckBox = new CheckBox
            {
                Content = "Active",
                IsChecked = user.IsActive
            };


            var panel = new StackPanel
            {
                Spacing = 12
            };

            panel.Children.Add(usernameBox);
            panel.Children.Add(emailBox);
            panel.Children.Add(roleBox);
            panel.Children.Add(companyBox);
            panel.Children.Add(activeCheckBox);

            var dialog = new ContentDialog
            {
                Title = "Edit User",
                Content = panel,

                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",

                DefaultButton = ContentDialogButton.Primary,

                XamlRoot = XamlRoot
            };

            var dialogResult =
                await dialog.ShowAsync();

            if (dialogResult != ContentDialogResult.Primary)
                return;


            var username = usernameBox.Text.Trim();
            var email = emailBox.Text.Trim();

            Company? selectedCompany = null;

            if (CurrentUser.Role == "Admin")
            {
                selectedCompany =
                    companies.FirstOrDefault(
                        c => c.CompanyId == CurrentUser.CompanyId);

                if (selectedCompany == null)
                {
                    await ShowMessageAsync(
                        "Validation Error",
                        "Your company could not be found.");
                    return;
                }
            }
            else
            {
                if (companyBox.SelectedItem
                    is not Company company)
                {
                    await ShowMessageAsync(
                        "Validation Error",
                        "Please select a company.");
                    return;
                }

                selectedCompany = company;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Username is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Email is required.");
                return;
            }

            var selectedRole =
                roleBox.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(selectedRole))
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select a role.");
                return;
            }


            if (selectedRole == "HR Staff" ||
                selectedRole == "Finance Staff")
            {
                if (selectedCompany == null)
                {
                    await ShowMessageAsync(
                        "Validation Error",
                        "A company is required for this role.");
                    return;
                }

                var accessibleModules =
                    await _subscriptionAccessService
                        .GetAccessibleModuleCodesAsync(
                            selectedCompany.CompanyId);

                if (selectedRole == "HR Staff" &&
                    (!accessibleModules.Contains("EMPLOYEE") ||
                     !accessibleModules.Contains("ATTENDANCE") ||
                     !accessibleModules.Contains("PAYROLL")))
                {
                    await ShowMessageAsync(
                        "Modules Not Available",
                        "The selected company does not have all required HR modules.");
                    return;
                }

                if (selectedRole == "Finance Staff" &&
                    !accessibleModules.Contains("FINANCE"))
                {
                    await ShowMessageAsync(
                        "Module Not Available",
                        "The selected company does not have the Finance module.");
                    return;
                }
            }


            var usernameExists =
                await _userManager.FindByNameAsync(
                    username);

            if (usernameExists != null &&
                usernameExists.Id != user.Id)
            {
                await ShowMessageAsync(
                    "Username Already Exists",
                    "That username is already being used.");
                return;
            }

            var emailExists =
                await _userManager.FindByEmailAsync(
                    email);

            if (emailExists != null &&
                emailExists.Id != user.Id)
            {
                await ShowMessageAsync(
                    "Email Already Exists",
                    "That email is already being used.");
                return;
            }


            if (!string.Equals(
                    currentRole,
                    selectedRole,
                    StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(currentRole))
                {
                    var removeRoleResult =
                        await _userManager.RemoveFromRoleAsync(
                            user,
                            currentRole);

                    if (!removeRoleResult.Succeeded)
                    {
                        await ShowIdentityErrorsAsync(
                            "Unable to Remove Previous Role",
                            removeRoleResult);
                        return;
                    }
                }

                var addRoleResult =
                    await _userManager.AddToRoleAsync(
                        user,
                        selectedRole);

                if (!addRoleResult.Succeeded)
                {
                    await ShowIdentityErrorsAsync(
                        "Unable to Assign New Role",
                        addRoleResult);

                    if (!string.IsNullOrWhiteSpace(currentRole))
                    {
                        await _userManager.AddToRoleAsync(
                            user,
                            currentRole);
                    }

                    return;
                }
            }


            if (!string.Equals(
                    user.UserName,
                    username,
                    StringComparison.OrdinalIgnoreCase))
            {
                var usernameResult =
                    await _userManager.SetUserNameAsync(
                        user,
                        username);

                if (!usernameResult.Succeeded)
                {
                    await ShowIdentityErrorsAsync(
                        "Unable to Update Username",
                        usernameResult);
                    return;
                }
            }


            if (!string.Equals(
                    user.Email,
                    email,
                    StringComparison.OrdinalIgnoreCase))
            {
                var emailResult =
                    await _userManager.SetEmailAsync(
                        user,
                        email);

                if (!emailResult.Succeeded)
                {
                    await ShowIdentityErrorsAsync(
                        "Unable to Update Email",
                        emailResult);
                    return;
                }
            }


            user.CompanyId = selectedCompany.CompanyId;
            user.IsActive = activeCheckBox.IsChecked == true;


            var updateResult =
                await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                await ShowIdentityErrorsAsync(
                    "Unable to Update User",
                    updateResult);
                return;
            }


            await LoadUsersAsync();

            await ShowMessageAsync(
                "User Updated",
                $"User '{user.UserName}' was updated successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Edit User Error",
                ex.ToString());
        }
    }


    // ==========================================
    // DELETE USER
    // ==========================================

    private async void DeleteUserButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (UsersListView.SelectedItem
            is not UserRow selectedRow)
        {
            await ShowMessageAsync(
                "No User Selected",
                "Please select a user first.");
            return;
        }

        var selectedUser = selectedRow.User;

        var username =
            selectedUser.UserName ?? "this user";

        var dialog = new ContentDialog
        {
            Title = "Delete User",

            Content =
                $"Are you sure you want to delete '{username}'?",

            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",

            DefaultButton = ContentDialogButton.Close,

            XamlRoot = XamlRoot
        };

        var result =
            await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;

        try
        {
            var user =
                await _userManager.FindByIdAsync(
                    selectedUser.Id);

            if (user == null)
            {
                await ShowMessageAsync(
                    "User Not Found",
                    "The selected user no longer exists.");

                await LoadUsersAsync();
                return;
            }

            var isSuperAdmin =
                await _userManager.IsInRoleAsync(
                    user,
                    "Super Admin");

            if (CurrentUser.Role == "Admin" && isSuperAdmin)
            {
                await ShowMessageAsync(
                    "Access Denied",
                    "You cannot delete a Super Admin.");
                return;
            }

            if (CurrentUser.Role == "Admin" &&
                user.CompanyId != CurrentUser.CompanyId)
            {
                await ShowMessageAsync(
                    "Access Denied",
                    "You can only delete users from your own company.");
                return;
            }

            // ==========================================
            // ⬇⬇⬇ FIX: delete the tenant employee record first
            // ==========================================

            if (user.CompanyId.HasValue)
            {
                try
                {
                    await using var tenantDb =
                        await _tenantDbFactory.CreateAsync(
                            user.CompanyId.Value);

                    var employee =
                        await tenantDb.Employees
                            .FirstOrDefaultAsync(e =>
                                e.MasterUserId == user.Id);

                    if (employee != null)
                    {
                        tenantDb.Employees.Remove(employee);
                        await tenantDb.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    await ShowMessageAsync(
                        "Cleanup Error",
                        "The user could not be deleted because the linked employee record could not be removed.\n\n" +
                        ex.Message);

                    return;
                }
            }

            var deleteResult =
                await _userManager.DeleteAsync(user);

            if (!deleteResult.Succeeded)
            {
                await ShowIdentityErrorsAsync(
                    "Unable to Delete User",
                    deleteResult);
                return;
            }

            await LoadUsersAsync();

            await ShowMessageAsync(
                "User Deleted",
                $"User '{username}' was deleted successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Delete User Error",
                ex.Message);
        }
    }


    // ==========================================
    // REFRESH
    // ==========================================

    private async void RefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await LoadUsersAsync();
    }


    // ==========================================
    // SHOW MESSAGE
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


    // ==========================================
    // IDENTITY ERRORS
    // ==========================================

    private async Task ShowIdentityErrorsAsync(
        string title,
        IdentityResult result)
    {
        var errors =
            string.Join(
                "\n",
                result.Errors.Select(
                    error =>
                        $"• {error.Description}"));

        await ShowMessageAsync(
            title,
            errors);
    }


    // ==========================================
    // USER ROW
    // ==========================================

    private sealed class UserRow
    {
        public ApplicationUser User { get; set; } = null!;

        public string CompanyName { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string StatusText =>
            User.IsActive ? "Active" : "Inactive";
    }
}