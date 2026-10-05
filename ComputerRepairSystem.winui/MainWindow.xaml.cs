using ComputerRepairSystem.company.Services;
using ComputerRepairSystem_winui.Pages;
using ComputerRepairSystem_winui.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ComputerRepairSystem.infrastructure.data;
using ComputerRepairSystem.winui.Pages;

namespace ComputerRepairSystem_winui;

public sealed partial class MainWindow : Window
{
    private readonly SubscriptionAccessService
        _subscriptionAccessService;
    private readonly SuperAdminSystemSettingsService
    _superAdminSystemSettingsService;

    // ==========================================
    // CONSTRUCTOR
    // ==========================================

    public MainWindow(SuperAdminSystemSettingsService
    superAdminSystemSettingsService,
        SubscriptionAccessService subscriptionAccessService)
    {
        InitializeComponent();

        _subscriptionAccessService =
            subscriptionAccessService;

        _superAdminSystemSettingsService =
            superAdminSystemSettingsService;

        ShowLogin();

        ExtendsContentIntoTitleBar = true;

        SetTitleBar(AppTitleBar);

        AppWindow.TitleBar.PreferredHeightOption =
            TitleBarHeightOption.Tall;

        AppWindow.SetIcon(
            "Assets/AppIcon.ico");
    }

    private async Task<bool>
        CheckMaintenanceModeAsync()
    {
        try
        {
            var isMaintenanceMode =
                await _superAdminSystemSettingsService
                    .IsMaintenanceModeEnabledAsync();

            if (!isMaintenanceMode)
            {
                return false;
            }

            // Super Admin is allowed to access
            // the application during maintenance.
            if (CurrentUser.Role == "Super Admin")
            {
                return false;
            }

            await ShowMaintenanceMessageAsync();

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            // Fail open if the master database
            // cannot be checked.
            return false;
        }
    }

    private async Task
        ShowMaintenanceMessageAsync()
    {
        var dialog =
            new ContentDialog
            {
                Title = "System Maintenance",
                Content =
                    "The system is currently under maintenance. " +
                    "Please try again later.",
                CloseButtonText = "OK",
                XamlRoot = RootGrid.XamlRoot
            };

        await dialog.ShowAsync();
    }
    // ==========================================
    // TITLE BAR
    // ==========================================

    private void TitleBar_PaneToggleRequested(
        object sender,
        RoutedEventArgs e)
    {
        NavView.IsPaneOpen =
            !NavView.IsPaneOpen;
    }


    // ==========================================
    // SHOW APPLICATION
    // ==========================================

    public void ShowApplication()
    {
        UpdateUserHeader();

        // ==========================================
        // HIDE EVERYTHING FIRST
        // ==========================================

        HomeItem.Visibility =
            Visibility.Collapsed;

        ServiceManagementItem.Visibility =
            Visibility.Collapsed;

        InventoryItem.Visibility =
            Visibility.Collapsed;

        RepairManagementItem.Visibility =
            Visibility.Collapsed;

        BillingItem.Visibility =
            Visibility.Collapsed;

        UserManagementItem.Visibility =
            Visibility.Collapsed;

        CompanyManagementItem.Visibility =
            Visibility.Collapsed;

        SystemSettingsItem.Visibility =
            Visibility.Collapsed;

        CustomerManagementItem.Visibility =
            Visibility.Collapsed;

        EmployeeManagementItem.Visibility =
            Visibility.Collapsed;

        AttendanceManagementItem.Visibility =
            Visibility.Collapsed;

        PayrollManagementItem.Visibility =  
            Visibility.Collapsed;

        FinanceManagementItem.Visibility =
            Visibility.Collapsed;

        SupplierManagementItem.Visibility =
            Visibility.Collapsed;

        TermsAndConditionItem.Visibility =
            Visibility.Collapsed;

        SubscriptionManagementItem.Visibility =
            Visibility.Collapsed;

        PurchaseManagementItem.Visibility =
            Visibility.Collapsed;

        MySubscriptionItem.Visibility =
            Visibility.Collapsed;

        BranchManagementItem.Visibility =
            Visibility.Collapsed;

        DepartmentManagementItem.Visibility =
            Visibility.Collapsed;

        SuperAdminSystemSettingsItem.Visibility =
            Visibility.Collapsed;


        var role =
            CurrentUser.Role;


        // ==========================================
        // SUPER ADMIN
        // ==========================================

        if (role == "Super Admin")
        {
            HomeItem.Visibility =
                Visibility.Visible;

            UserManagementItem.Visibility =
                Visibility.Visible;

            CompanyManagementItem.Visibility =
                Visibility.Visible;

            SubscriptionManagementItem.Visibility =
                Visibility.Visible;

            TermsAndConditionItem.Visibility =
                Visibility.Visible;

            SuperAdminSystemSettingsItem.Visibility =
                Visibility.Visible;

            HomeItem.IsSelected =
                true;

            NavFrame.Content =
                App.Services
                    .GetRequiredService<
                        HomePage>();

            return;
        }


        // ==========================================
        // ADMIN
        // ==========================================

        if (role == "Admin")
        {
            HomeItem.Visibility =
                Visibility.Visible;

            ServiceManagementItem.Visibility =
                Visibility.Visible;

            RepairManagementItem.Visibility =
                Visibility.Visible;

            BillingItem.Visibility =
                Visibility.Visible;

            UserManagementItem.Visibility =
                Visibility.Visible;

            InventoryItem.Visibility =
                Visibility.Visible;

            SystemSettingsItem.Visibility =
                Visibility.Visible;

            CustomerManagementItem.Visibility =
                Visibility.Visible;

            EmployeeManagementItem.Visibility =
                Visibility.Visible;

            AttendanceManagementItem.Visibility =
                Visibility.Visible;

            PayrollManagementItem.Visibility =
                Visibility.Visible;

            FinanceManagementItem.Visibility =
                Visibility.Visible;

            SupplierManagementItem.Visibility =
                Visibility.Visible;

            TermsAndConditionItem.Visibility =
                Visibility.Visible;

            PurchaseManagementItem.Visibility =
                Visibility.Visible;

            MySubscriptionItem.Visibility =
                Visibility.Visible;


            HomeItem.IsSelected =
                true;

            NavFrame.Content =
                App.Services
                    .GetRequiredService<HomePage>();

            _ = ApplySubscriptionAccessAsync();

            return;
        }


        // ==========================================
        // TECHNICIAN
        // ==========================================

        if (role == "Technician")
        {
            RepairManagementItem.Visibility =
                Visibility.Visible;

            InventoryItem.Visibility =
                Visibility.Visible;

            RepairManagementItem.IsSelected =
                true;

            NavFrame.Content =
                App.Services
                    .GetRequiredService<
                        RepairManagementPage>();

            _ = ApplySubscriptionAccessAsync();

            return;
        }


        // ==========================================
        // RECEPTIONIST
        // ==========================================

        if (role == "Receptionist")
        {
            ServiceManagementItem.Visibility =
                Visibility.Visible;

            BillingItem.Visibility =
                Visibility.Visible;

            CustomerManagementItem.Visibility =
                Visibility.Visible;

            ServiceManagementItem.IsSelected =
                true;

            NavFrame.Content =
                App.Services
                    .GetRequiredService<
                        ServiceManagementPage>();

            _ = ApplySubscriptionAccessAsync();

            return;
        }


        // ==========================================
        // HR STAFF
        // ==========================================

        if (role == "HR Staff")
        {
            EmployeeManagementItem.Visibility =
                Visibility.Visible;

            AttendanceManagementItem.Visibility =
                Visibility.Visible;

            PayrollManagementItem.Visibility =
                Visibility.Visible;

            EmployeeManagementItem.IsSelected =
                true;

            NavFrame.Content =
                App.Services
                    .GetRequiredService<
                        EmployeeManagementPage>();

            _ = ApplySubscriptionAccessAsync();

            return;
        }


        // ==========================================
        // FINANCE STAFF
        // ==========================================

        if (role == "Finance Staff")
        {
            BillingItem.Visibility =
                Visibility.Visible;

            FinanceManagementItem.Visibility =
                Visibility.Visible;

            FinanceManagementItem.IsSelected =
                true;

            NavFrame.Content =
                App.Services
                    .GetRequiredService<
                        FinanceManagementPage>();

            _ = ApplySubscriptionAccessAsync();

            return;
        }
    }


    // ==========================================
    // APPLY SUBSCRIPTION ACCESS
    // ==========================================

    private async Task
        ApplySubscriptionAccessAsync()
    {
        if (CurrentUser.Role ==
            "Super Admin")
        {
            return;
        }

        var modules =
            await _subscriptionAccessService
                .GetAccessibleModuleCodesAsync();


        // ==========================================
        // ADMIN
        // ==========================================

        if (CurrentUser.Role == "Admin")
        {
            // DASHBOARD
            HomeItem.Visibility =
                modules.Contains("DASHBOARD")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // SERVICE MANAGEMENT
            ServiceManagementItem.Visibility =
                modules.Contains("CUSTOMER")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // CUSTOMER MANAGEMENT
            CustomerManagementItem.Visibility =
                modules.Contains("CUSTOMER")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // REPAIR MANAGEMENT
            RepairManagementItem.Visibility =
                modules.Contains("REPAIR")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // BILLING
            BillingItem.Visibility =
                modules.Contains("FINANCE")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // INVENTORY
            InventoryItem.Visibility =
                modules.Contains("INVENTORY")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // EMPLOYEE
            EmployeeManagementItem.Visibility =
                modules.Contains("EMPLOYEE")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // ATTENDANCE
            AttendanceManagementItem.Visibility =
                modules.Contains("ATTENDANCE")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // PAYROLL
            PayrollManagementItem.Visibility =
                modules.Contains("PAYROLL")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // FINANCE
            FinanceManagementItem.Visibility =
                modules.Contains("FINANCE")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // SUPPLIER
            SupplierManagementItem.Visibility =
                modules.Contains("SUPPLIER")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // PURCHASE
            PurchaseManagementItem.Visibility =
                modules.Contains("INVENTORY")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // ADMIN-ONLY ITEMS
            UserManagementItem.Visibility =
                Visibility.Visible;

            SystemSettingsItem.Visibility =
                Visibility.Visible;

            TermsAndConditionItem.Visibility =
                Visibility.Visible;

            // BRANCH MANAGEMENT
            BranchManagementItem.Visibility =
                modules.Contains("BRANCH")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // DEPARTMENT MANAGEMENT
            DepartmentManagementItem.Visibility =
                modules.Contains("DEPARTMENT")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if (NavView.SelectedItem
                is NavigationViewItem selectedAdminItem
                && selectedAdminItem.Visibility ==
                    Visibility.Collapsed)
            {
                NavigateToFirstAvailableModule(
                    modules);
            }

            return;
        }


        // ==========================================
        // TECHNICIAN
        // ==========================================

        if (CurrentUser.Role == "Technician")
        {
            RepairManagementItem.Visibility =
                modules.Contains("REPAIR")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            InventoryItem.Visibility =
                modules.Contains("INVENTORY")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if (NavView.SelectedItem
                is NavigationViewItem selectedTechnicianItem
                && selectedTechnicianItem.Visibility ==
                    Visibility.Collapsed)
            {
                NavigateToFirstAvailableModule(
                    modules);
            }

            return;
        }


        // ==========================================
        // RECEPTIONIST
        // ==========================================

        if (CurrentUser.Role == "Receptionist")
        {
            ServiceManagementItem.Visibility =
                modules.Contains("CUSTOMER")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            BillingItem.Visibility =
                modules.Contains("FINANCE")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            CustomerManagementItem.Visibility =
                modules.Contains("CUSTOMER")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if (NavView.SelectedItem
                is NavigationViewItem selectedReceptionistItem
                && selectedReceptionistItem.Visibility ==
                    Visibility.Collapsed)
            {
                NavigateToFirstAvailableModule(
                    modules);
            }

            return;
        }


        // ==========================================
        // HR STAFF
        // ==========================================

        if (CurrentUser.Role == "HR Staff")
        {
            EmployeeManagementItem.Visibility =
                modules.Contains("EMPLOYEE")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            AttendanceManagementItem.Visibility =
                modules.Contains("ATTENDANCE")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            PayrollManagementItem.Visibility =
                modules.Contains("PAYROLL")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if (NavView.SelectedItem
                is NavigationViewItem selectedHrItem
                && selectedHrItem.Visibility ==
                    Visibility.Collapsed)
            {
                NavigateToFirstAvailableModule(
                    modules);
            }

            return;
        }


        // ==========================================
        // FINANCE STAFF
        // ==========================================

        if (CurrentUser.Role == "Finance Staff")
        {
            BillingItem.Visibility =
                modules.Contains("FINANCE")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            FinanceManagementItem.Visibility =
                modules.Contains("FINANCE")
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if (NavView.SelectedItem
                is NavigationViewItem selectedFinanceItem
                && selectedFinanceItem.Visibility ==
                    Visibility.Collapsed)
            {
                NavigateToFirstAvailableModule(
                    modules);
            }

            return;
        }
    }


    // ==========================================
    // FIRST AVAILABLE MODULE
    // ==========================================

    private void NavigateToFirstAvailableModule(
        HashSet<string> modules)
    {
        // ==========================================
        // HR STAFF
        // ==========================================

        if (CurrentUser.Role == "HR Staff")
        {
            if (modules.Contains("EMPLOYEE"))
            {
                EmployeeManagementItem.IsSelected =
                    true;

                return;
            }

            if (modules.Contains("ATTENDANCE"))
            {
                AttendanceManagementItem.IsSelected =
                    true;

                return;
            }

            if (modules.Contains("PAYROLL"))
            {
                PayrollManagementItem.IsSelected =
                    true;

                return;
            }

            return;
        }


        // ==========================================
        // FINANCE STAFF
        // ==========================================

        if (CurrentUser.Role == "Finance Staff")
        {
            if (modules.Contains("FINANCE"))
            {
                FinanceManagementItem.IsSelected =
                    true;

                return;
            }

            return;
        }


        // ==========================================
        // OTHER ROLES
        // ==========================================

        if (modules.Contains("DASHBOARD"))
        {
            HomeItem.IsSelected = true;
            return;
        }

        if (modules.Contains("CUSTOMER"))
        {
            ServiceManagementItem.IsSelected =
                true;

            return;
        }

        if (modules.Contains("REPAIR"))
        {
            RepairManagementItem.IsSelected =
                true;

            return;
        }

        if (modules.Contains("FINANCE"))
        {
            BillingItem.IsSelected =
                true;

            return;
        }

        if (modules.Contains("INVENTORY"))
        {
            InventoryItem.IsSelected =
                true;

            return;
        }

        if (modules.Contains("EMPLOYEE"))
        {
            EmployeeManagementItem.IsSelected =
                true;

            return;
        }
    }


    // ==========================================
    // SHOW LOGIN
    // ==========================================

    public void ShowLogin()
    {
        HomeItem.Visibility =
            Visibility.Collapsed;

        ServiceManagementItem.Visibility =
            Visibility.Collapsed;

        RepairManagementItem.Visibility =
            Visibility.Collapsed;

        BillingItem.Visibility =
            Visibility.Collapsed;

        UserManagementItem.Visibility =
            Visibility.Collapsed;

        InventoryItem.Visibility =
            Visibility.Collapsed;

        SystemSettingsItem.Visibility =
            Visibility.Collapsed;

        CustomerManagementItem.Visibility =
            Visibility.Collapsed;

        CompanyManagementItem.Visibility =
            Visibility.Collapsed;

        EmployeeManagementItem.Visibility =
            Visibility.Collapsed;

        AttendanceManagementItem.Visibility =
            Visibility.Collapsed;

        PayrollManagementItem.Visibility =
            Visibility.Collapsed;

        FinanceManagementItem.Visibility =
            Visibility.Collapsed;

        SupplierManagementItem.Visibility =
            Visibility.Collapsed;

        TermsAndConditionItem.Visibility =
            Visibility.Collapsed;

        SubscriptionManagementItem.Visibility =
            Visibility.Collapsed;

        PurchaseManagementItem.Visibility =
            Visibility.Collapsed;

        MySubscriptionItem.Visibility =
            Visibility.Collapsed;

        BranchManagementItem.Visibility =
            Visibility.Collapsed;

        DepartmentManagementItem.Visibility =
            Visibility.Collapsed;

        SuperAdminSystemSettingsItem.Visibility =
            Visibility.Collapsed;

        HomeItem.IsSelected = false;

        NavFrame.Content =
            App.Services
                .GetRequiredService<LoginPage>();
    }


    // ==========================================
    // NAVIGATION
    // ==========================================

    private void NavView_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (!CurrentUser.IsLoggedIn)
        {
            return;
        }

        if (args.SelectedItem
            is NavigationViewItem item)
        {
            switch (item.Tag)
            {
                case "home":
                    NavigateToHomePage();
                    break;

                case "customers":
                    NavigateToServiceManagementPage();
                    break;

                case "repairs":
                    NavigateToRepairManagementPage();
                    break;

                case "billing":
                    NavigateToBillingPage();
                    break;

                case "customer-management":
                    NavigateToCustomerManagementPage();
                    break;

                case "users":
                    NavigateToUserManagementPage();
                    break;

                case "companies":
                    NavigateToCompanyManagementPage();
                    break;

                case "employee-management":
                    NavigateToEmployeeManagement();
                    break;

                case "attendance-management":
                    NavigateToAttendanceManagementPage();
                    break;

                case "payroll-management":
                    NavigateToPayrollManagementPage();
                    break;

                case "finance-management":
                    NavigateToFinanceManagementPage();
                    break;

                case "inventory":
                    NavigateToInventoryPage();
                    break;

                case "supplier-management":
                    NavigateToSupplierManagementPage();
                    break;

                case "my-subscription":
                    NavigateToMySubscriptionPage();
                    break;

                case "settings":
                    NavigateToSettingsPage();
                    break;

                case "terms-and-conditions":
                    NavigateToTermsAndConditionsPage();
                    break;

                case "subscription-management":
                    NavigateToSubscriptionManagementPage();
                    break;

                case "purchase-management":
                    NavigateToPurchaseManagementPage();
                    break;

                case "branch-management":
                    NavigateToBranchManagementPage();
                    break;

                case "department-management":
                    NavigateToDepartmentManagementPage();
                    break;

                case "super-admin-system-settings":
                    NavigateToSuperAdminSystemSettingsPage();
                    break;
            }
        }
    }


    // ==========================================
    // PAGE NAVIGATION
    // ==========================================

    private void NavigateToServiceManagementPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    ServiceManagementPage>();

        NavFrame.Content = page;
    }


    public void NavigateToUserManagementPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    UserManagementPage>();

        NavFrame.Content = page;
    }


    private void NavigateToRepairManagementPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    RepairManagementPage>();

        NavFrame.Content = page;
    }


    private void NavigateToBillingPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    BillingPage>();

        NavFrame.Content = page;
    }


    private void NavigateToHomePage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    HomePage>();

        NavFrame.Content = page;
    }


    private void NavigateToInventoryPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    InventoryManagementPage>();

        NavFrame.Content = page;
    }


    private void UpdateUserHeader()
    {
        UserNameText.Text =
            CurrentUser.UserName;
    }


    // ==========================================
    // GLOBAL SEARCH
    // ==========================================

    private async void GlobalSearchButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Search",

            Content = new TextBox
            {
                PlaceholderText =
                    "Search customers, repairs, devices..."
            },

            CloseButtonText = "Close",

            XamlRoot =
                Content.XamlRoot
        };

        await dialog.ShowAsync();
    }


    // ==========================================
    // NOTIFICATIONS
    // ==========================================

    private async void NotificationButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Notifications",

            Content = new TextBlock
            {
                Text =
                    "No new notifications.",

                TextWrapping =
                    TextWrapping.Wrap
            },

            CloseButtonText = "Close",

            XamlRoot =
                Content.XamlRoot
        };

        await dialog.ShowAsync();
    }


    // ==========================================
    // PROFILE
    // ==========================================

    private async void ProfileButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title =
                CurrentUser.UserName,

            Content =
                new StackPanel
                {
                    Spacing = 8,

                    Children =
                    {
                        new TextBlock
                        {
                            Text =
                                $"Username: {CurrentUser.UserName}"
                        },

                        new TextBlock
                        {
                            Text =
                                $"Role: {CurrentUser.Role}"
                        }
                    }
                },

            PrimaryButtonText = "Logout",

            CloseButtonText = "Close",

            XamlRoot =
                Content.XamlRoot
        };

        var result =
            await dialog.ShowAsync();

        if (result ==
            ContentDialogResult.Primary)
        {
            CurrentUser.Logout();

            ShowLogin();
        }
    }


    // ==========================================
    // SETTINGS
    // ==========================================

    private void NavigateToSettingsPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    SettingsPage>();

        NavFrame.Content = page;
    }


    // ==========================================
    // CUSTOMER MANAGEMENT
    // ==========================================

    private void NavigateToCustomerManagementPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    CustomerManagementPage>();

        NavFrame.Content = page;
    }


    // ==========================================
    // COMPANY MANAGEMENT
    // ==========================================

    public void NavigateToCompanyManagementPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    CompanyManagementPage>();

        NavFrame.Content = page;
    }


    // ==========================================
    // EMPLOYEE MANAGEMENT
    // ==========================================

    private void NavigateToEmployeeManagement()
    {
        var page =
            App.Services
                .GetRequiredService<
                    EmployeeManagementPage>();

        NavFrame.Content = page;
    }


    // ==========================================
    // ATTENDANCE MANAGEMENT
    // ==========================================

    private void NavigateToAttendanceManagementPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    AttendanceManagementPage>();

        NavFrame.Content = page;
    }


    // ==========================================
    // PAYROLL MANAGEMENT
    // ==========================================

    private void NavigateToPayrollManagementPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    PayrollManagementPage>();

        NavFrame.Content = page;
    }


    // ==========================================
    // FINANCE MANAGEMENT
    // ==========================================

    private void NavigateToFinanceManagementPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    FinanceManagementPage>();

        NavFrame.Content = page;
    }


    // ==========================================
    // SUPPLIER MANAGEMENT
    // ==========================================

    private void NavigateToSupplierManagementPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    SupplierManagementPage>();

        NavFrame.Content = page;
    }


    // ==========================================
    // TERMS AND CONDITIONS
    // ==========================================

    public void NavigateToTermsAndConditionsPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    TermsAndConditionsPage>();

        NavFrame.Content = page;
    }


    // ==========================================
    // SUBSCRIPTION MANAGEMENT
    // ==========================================

    public void NavigateToSubscriptionManagementPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    SubscriptionManagementPage>();

        NavFrame.Content = page;
    }


    // ==========================================
    // PURCHASE MANAGEMENT
    // ==========================================

    private void NavigateToPurchaseManagementPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    PurchaseManagementPage>();

        NavFrame.Content = page;
    }
    // ==========================================
    // MY SUBSCRIPTION
    // ==========================================

    private void NavigateToMySubscriptionPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    MySubscriptionPage>();

        NavFrame.Content = page;
    }

    // ==========================================
    // MY SUBSCRIPTION
    // ==========================================

    private void NavigateToBranchManagementPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    BranchManagementPage>();

        NavFrame.Content = page;
    }

    private void NavigateToDepartmentManagementPage()
    {
        var page =
            App.Services
                .GetRequiredService<
                    DepartmentManagementPage>();

        NavFrame.Content = page;
    }

    private void NavigateToSuperAdminSystemSettingsPage()
    {
        NavFrame.Content =
            App.Services
                .GetRequiredService<
                    SuperAdminSystemSettingsPage>();
    }
}

