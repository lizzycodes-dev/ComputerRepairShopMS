using ComputerRepairSystem.company.Context;
using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Entities;
using ComputerRepairSystem.infrastructure.data;
using ComputerRepairSystem_winui.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace ComputerRepairSystem_winui.Pages;

public sealed partial class HomePage : Page
{
    private readonly TenantDbContextFactory
        _tenantDbFactory;

    private readonly MasterErpDbContext
        _masterDb;

    private readonly CurrentBranchContext _currentBranchContext;

    private int? _selectedDashboardBranchId;
    private bool _branchManagementEnabled;
    public HomePage(
        TenantDbContextFactory tenantDbFactory,
        MasterErpDbContext masterDb,
        CurrentBranchContext currentBranchContext)
    {
        InitializeComponent();

        _tenantDbFactory =
            tenantDbFactory;

        _masterDb =
            masterDb;

        _currentBranchContext =
            currentBranchContext;

        Loaded += HomePage_Loaded;
    }

    private async Task<bool> HasBranchManagementAsync()
    {
        if (CurrentUser.CompanyId == null)
            return false;

        var subscription =
            await _masterDb.Subscriptions
                .AsNoTracking()
                .Where(s =>
                    s.CompanyId == CurrentUser.CompanyId.Value &&
                    s.Status == "Active" &&
                    (!s.EndDate.HasValue ||
                     s.EndDate.Value > DateTime.UtcNow))
                .OrderByDescending(s => s.EndDate)
                .FirstOrDefaultAsync();

        if (subscription == null)
            return false;

        return await _masterDb.SubscriptionPlanModules
            .AsNoTracking()
            .AnyAsync(x =>
                x.SubscriptionPlanId ==
                    subscription.SubscriptionPlanId &&
                x.ModuleDefinitionId == 11);
    }

    private async Task LoadDashboardBranchesAsync(
        TenantDbContext db)
    {
        _branchManagementEnabled =
            await HasBranchManagementAsync();

        // No Branch Management module
        if (!_branchManagementEnabled)
        {
            _selectedDashboardBranchId = null;
            return;
        }

        // Use the global branch selector
        _selectedDashboardBranchId =
            _currentBranchContext.BranchId;
    }
    private async Task LoadProfitVsExpensesAsync(TenantDbContext db)
    {
        var startOfMonth =
            new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month,
                1);

        var startOfNextMonth =
            startOfMonth.AddMonths(1);

        // Completed sales
        var revenueQuery =
            db.Payments
                .AsNoTracking()
                .Where(p =>
                    p.Status == "Completed" &&
                    p.PaymentDate >= startOfMonth &&
                    p.PaymentDate < startOfNextMonth);

        if (_branchManagementEnabled &&
            _selectedDashboardBranchId.HasValue)
        {
            var branchId =
                _selectedDashboardBranchId.Value;

            revenueQuery =
                revenueQuery.Where(p =>
                    p.Invoice!.Repair!.ServiceRequest!.BranchId ==
                    branchId);
        }

        var revenue =
            await revenueQuery
                .Select(p => (decimal?)p.Amount)
                .SumAsync() ?? 0;

        // Expenses
        var expensesQuery =
            db.Expenses
                .AsNoTracking()
                .Where(e =>
                    e.ExpenseDate >= startOfMonth &&
                    e.ExpenseDate < startOfNextMonth);

        if (_branchManagementEnabled &&
            _selectedDashboardBranchId.HasValue)
        {
            var branchId =
                _selectedDashboardBranchId.Value;

            expensesQuery =
                expensesQuery.Where(e =>
                    e.BranchId == branchId);
        }

        var expenses =
            await expensesQuery
                .Select(e => (decimal?)e.Amount)
                .SumAsync() ?? 0;

        var profit = revenue - expenses;

        ProfitTextBlock.Text = $"₱{profit:N2}";
        RevenueTextBlock.Text = $"₱{revenue:N2}";
        ExpensesTextBlock.Text = $"₱{expenses:N2}";
    }
    private async void SalesTrendPeriodComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        if (CurrentUser.Role == "Super Admin")
        {
            return;
        }

        if (CurrentUser.CompanyId == null)
        {
            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            await LoadSalesTrendsAsync(db);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "Failed to load sales trends: " + ex);
        }
    }
    // ==========================================
    // PAGE LOADED
    // ==========================================

    private async void HomePage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await LoadDashboardAsync();
    }
    private void ManageCompaniesButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var mainWindow =
            App.Services
                .GetRequiredService<MainWindow>();

        mainWindow.NavigateToCompanyManagementPage();
    }


    private void ManageUsersButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var mainWindow =
            App.Services
                .GetRequiredService<MainWindow>();

        mainWindow.NavigateToUserManagementPage();
    }


    private void ManageSubscriptionPlansButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var mainWindow =
            App.Services
                .GetRequiredService<MainWindow>();

        mainWindow.NavigateToSubscriptionManagementPage();
    }


    private void ManageTermsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        var mainWindow =
            App.Services
                .GetRequiredService<MainWindow>();

        mainWindow.NavigateToTermsAndConditionsPage();
    }



    // ==========================================
    // LOAD DASHBOARD
    // ==========================================

    private async Task LoadDashboardAsync()
    {
        try
        {
            if (CurrentUser.Role == "Super Admin")
            {
                await LoadSuperAdminDashboardAsync();
                return;
            }

            if (CurrentUser.CompanyId == null)
            {
                return;
            }

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);
            await LoadDashboardBranchesAsync(db);

            // ==========================================
            // SYSTEM SETTINGS
            // ==========================================

            var settings =
                await db.SystemSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

            ShopNameText.Text =
                settings?.ShopName ??
                "Computer Repair Shop";


            // ==========================================
            // KPI
            // ==========================================
            var totalRepairsQuery =
                db.Repairs
                    .AsNoTracking()
                    .AsQueryable();

            if (_branchManagementEnabled &&
                _selectedDashboardBranchId.HasValue)
            {
                totalRepairsQuery =
                    totalRepairsQuery.Where(r =>
                        r.ServiceRequest.BranchId ==
                        _selectedDashboardBranchId.Value);
            }

            var totalRepairs =
                await totalRepairsQuery.CountAsync();

            var pendingRepairsQuery =
                db.Repairs
                    .AsNoTracking()
                    .Where(r =>
                        r.Status == "Pending");

            if (_branchManagementEnabled &&
                _selectedDashboardBranchId.HasValue)
            {
                pendingRepairsQuery =
                    pendingRepairsQuery.Where(r =>
                        r.ServiceRequest.BranchId ==
                        _selectedDashboardBranchId.Value);
            }

            var pendingRepairs =
                await pendingRepairsQuery.CountAsync();

            var completedRepairsQuery =
                db.Repairs
                    .AsNoTracking()
                    .Where(r =>
                        r.Status == "Completed");

            if (_branchManagementEnabled &&
                _selectedDashboardBranchId.HasValue)
            {
                completedRepairsQuery =
                    completedRepairsQuery.Where(r =>
                        r.ServiceRequest.BranchId ==
                        _selectedDashboardBranchId.Value);
            }

            var completedRepairs =
                await completedRepairsQuery.CountAsync();


            // ==========================================
            // TOTAL REVENUE
            // ==========================================
            //
            // Uses completed payments, so partial
            // payments are included in actual revenue.
            // ==========================================

            var totalRevenueQuery =
                db.Payments
                    .AsNoTracking()
                    .Where(p =>
                        p.Status == "Completed");

            if (_branchManagementEnabled &&
                _selectedDashboardBranchId.HasValue)
            {
                totalRevenueQuery =
                    totalRevenueQuery.Where(p =>
                        p.Invoice!.Repair!.ServiceRequest!.BranchId ==
                        _selectedDashboardBranchId.Value);
            }

            var totalRevenue =
                await totalRevenueQuery
                    .Select(p => (decimal?)p.Amount)
                    .SumAsync()
                ?? 0;


            // ==========================================
            // UPDATE KPI UI
            // ==========================================

            TotalRepairsCountText.Text =
                totalRepairs.ToString();

            PendingRepairsCountText.Text =
                pendingRepairs.ToString();

            CompletedRepairsCountText.Text =
                completedRepairs.ToString();

            TotalRevenueText.Text =
                $"₱{totalRevenue:N2}";


            // ==========================================
            // REPAIR STATUS
            // ==========================================

            var inProgressRepairsQuery =
                db.Repairs
                    .AsNoTracking()
                    .Where(r =>
                        r.Status == "Diagnosing" ||
                        r.Status == "In Repair" ||
                        r.Status == "Ready for Pickup");

            if (_branchManagementEnabled &&
                _selectedDashboardBranchId.HasValue)
            {
                inProgressRepairsQuery =
                    inProgressRepairsQuery.Where(r =>
                        r.ServiceRequest.BranchId ==
                        _selectedDashboardBranchId.Value);
            }

            var inProgressRepairs =
                await inProgressRepairsQuery.CountAsync();


            UpdateRepairStatus(
                pendingRepairs,
                inProgressRepairs,
                completedRepairs,
                totalRepairs);


            // ==========================================
            // REPAIR OVERVIEW
            // ==========================================

            await LoadRepairOverviewAsync(db);
            await LoadProfitVsExpensesAsync(db);
            // ==========================================
            // SALES TRENDS
            // ==========================================

            await LoadSalesTrendsAsync(db);

            // ==========================================
            // RECENT REPAIRS
            // ==========================================

            await LoadRecentRepairsAsync(db);


            // ==========================================
            // RECENT TRANSACTIONS
            // ==========================================

            await LoadRecentTransactionsAsync(db);


            // ==========================================
            // LOW STOCK
            // ==========================================

            await LoadLowStockAsync(db);


            // ==========================================
            // OUTSTANDING PAYMENTS
            // ==========================================

            await LoadOutstandingPaymentsAsync(db);


            // ==========================================
            // TODAY'S ATTENDANCE
            // ==========================================

            await LoadTodayAttendanceAsync(db);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                "Failed to load dashboard: " +
                ex);
        }
    }
    // ==========================================
    // SALES TRENDS
    // ==========================================

    private async Task LoadSalesTrendsAsync(
        TenantDbContext db)
    {
        var period =
            SalesTrendPeriodComboBox.SelectedIndex;

        // ==========================================
        // WEEKLY
        // ==========================================

        if (period == 0)
        {
            var today =
                DateTime.Today;

            var startDate =
                today.AddDays(-6);

            var endDate =
                today.AddDays(1);

            var paymentsQuery =
                db.Payments
                    .AsNoTracking()
                    .Where(p =>
                        p.Status == "Completed" &&
                        p.PaymentDate >= startDate &&
                        p.PaymentDate < endDate);

            if (_branchManagementEnabled &&
                _selectedDashboardBranchId.HasValue)
            {
                var branchId =
                    _selectedDashboardBranchId.Value;

                paymentsQuery =
                    paymentsQuery.Where(p =>
                        p.Invoice!.Repair!.ServiceRequest!.BranchId ==
                        branchId);
            }

            var payments =
                await paymentsQuery
                    .Select(p => new
                    {
                        p.PaymentDate,
                        p.Amount
                    })
                    .ToListAsync();


            var dailySales =
                new List<decimal>();


            for (int i = 0; i < 7; i++)
            {
                var date =
                    startDate.AddDays(i);

                var total =
                    payments
                        .Where(p =>
                            p.PaymentDate.Date == date.Date)
                        .Sum(p => p.Amount);

                dailySales.Add(total);
            }


            var labels =
                Enumerable
                    .Range(0, 7)
                    .Select(i =>
                        startDate
                            .AddDays(i)
                            .ToString("ddd"))
                    .ToList();


            DrawSalesTrendGraph(
                dailySales,
                labels);

            return;
        }


        // ==========================================
        // MONTHLY
        // ==========================================

        if (period == 1)
        {
            var currentMonth =
                new DateTime(
                    DateTime.Today.Year,
                    DateTime.Today.Month,
                    1);

            var startMonth =
                currentMonth.AddMonths(-5);

            var endMonth =
                currentMonth.AddMonths(1);


            var paymentsQuery =
                db.Payments
                    .AsNoTracking()
                    .Where(p =>
                        p.Status == "Completed" &&
                        p.PaymentDate >= startMonth &&
                        p.PaymentDate < endMonth);

            if (_branchManagementEnabled &&
                _selectedDashboardBranchId.HasValue)
            {
                var branchId =
                    _selectedDashboardBranchId.Value;

                paymentsQuery =
                    paymentsQuery.Where(p =>
                        p.Invoice!.Repair!.ServiceRequest!.BranchId ==
                        branchId);
            }

            var payments =
                await paymentsQuery
                    .Select(p => new
                    {
                        p.PaymentDate,
                        p.Amount
                    })
                    .ToListAsync();


            var monthlySales =
                new List<decimal>();

            var labels =
                new List<string>();


            for (int i = 0; i < 6; i++)
            {
                var month =
                    startMonth.AddMonths(i);

                var total =
                    payments
                        .Where(p =>
                            p.PaymentDate.Year == month.Year &&
                            p.PaymentDate.Month == month.Month)
                        .Sum(p => p.Amount);

                monthlySales.Add(total);

                labels.Add(
                    month.ToString("MMM"));
            }


            DrawSalesTrendGraph(
                monthlySales,
                labels);

            return;
        }


        // ==========================================
        // YEARLY
        // ==========================================

        var currentYear =
            DateTime.Today.Year;

        var startYear =
            currentYear - 4;


        var paymentsYearlyQuery =
            db.Payments
                .AsNoTracking()
                .Where(p =>
                    p.Status == "Completed" &&
                    p.PaymentDate.Year >= startYear &&
                    p.PaymentDate.Year <= currentYear);

        if (_branchManagementEnabled &&
            _selectedDashboardBranchId.HasValue)
        {
            var branchId =
                _selectedDashboardBranchId.Value;

            paymentsYearlyQuery =
                paymentsYearlyQuery.Where(p =>
                    p.Invoice!.Repair!.ServiceRequest!.BranchId ==
                    branchId);
        }

        var paymentsYearly =
            await paymentsYearlyQuery
                .Select(p => new
                {
                    p.PaymentDate,
                    p.Amount
                })
                .ToListAsync();


        var yearlySales =
            new List<decimal>();

        var yearlyLabels =
            new List<string>();


        for (int year = startYear;
             year <= currentYear;
             year++)
        {
            var total =
                paymentsYearly
                    .Where(p =>
                        p.PaymentDate.Year == year)
                    .Sum(p => p.Amount);

            yearlySales.Add(total);

            yearlyLabels.Add(
                year.ToString());
        }


        DrawSalesTrendGraph(
            yearlySales,
            yearlyLabels);
    }
    // ==========================================
    // LOAD SUPER ADMIN DASHBOARD
    // ==========================================

    private async Task LoadSuperAdminDashboardAsync()
    {
        // Show Super Admin dashboard
        SuperAdminDashboard.Visibility =
            Visibility.Visible;

        // Hide regular company dashboard
        CompanyDashboard.Visibility =
            Visibility.Collapsed;


        // ==========================================
        // COMPANIES
        // ==========================================

        var companies =
            await _masterDb.Companies
                .AsNoTracking()
                .ToListAsync();


        var totalCompanies =
            companies.Count;


        // ==========================================
        // USERS
        // ==========================================

        var totalUsers =
            await _masterDb.Users
                .AsNoTracking()
                .CountAsync();


        // ==========================================
        // SUBSCRIPTIONS
        // ==========================================

        var totalSubscriptions =
            await _masterDb.Subscriptions
                .AsNoTracking()
                .CountAsync();


        var activeSubscriptions =
            await _masterDb.Subscriptions
                .AsNoTracking()
                .CountAsync(s =>
                    s.Status == "Active");


        // ==========================================
        // UPDATE KPI CARDS
        // ==========================================

        SuperAdminTotalCompaniesText.Text =
            totalCompanies.ToString();

        SuperAdminTotalUsersText.Text =
            totalUsers.ToString();

        SuperAdminTotalSubscriptionsText.Text =
            totalSubscriptions.ToString();

        SuperAdminActiveSubscriptionsText.Text =
            activeSubscriptions.ToString();

        // ==========================================
        // COMPANY CLASSIFICATION COUNTS
        // ==========================================

        var companyTypeCounts =
            await (
                from subscription in _masterDb.Subscriptions.AsNoTracking()

                join company in _masterDb.Companies.AsNoTracking()
                    on subscription.CompanyId
                    equals company.CompanyId

                join plan in _masterDb.SubscriptionPlans.AsNoTracking()
                    on subscription.SubscriptionPlanId
                    equals plan.SubscriptionPlanId

                where company.IsActive
                      && subscription.Status == "Active"

                group company by plan.EnterpriseType
                into companyGroup

                select new
                {
                    EnterpriseType = companyGroup.Key,

                    CompanyCount =
                        companyGroup
                            .Select(c => c.CompanyId)
                            .Distinct()
                            .Count()
                }
            )
            .ToListAsync();


        // Get counts
        var microCount =
            companyTypeCounts
                .FirstOrDefault(x =>
                    x.EnterpriseType == "Micro")
                ?.CompanyCount ?? 0;

        var smallCount =
            companyTypeCounts
                .FirstOrDefault(x =>
                    x.EnterpriseType == "Small")
                ?.CompanyCount ?? 0;

        var mediumCount =
            companyTypeCounts
                .FirstOrDefault(x =>
                    x.EnterpriseType == "Medium")
                ?.CompanyCount ?? 0;
        // ==========================================
        // SUBSCRIPTION GRAPH
        // ==========================================

        var subscriptionGraphMaximum =
            Math.Max(
                Math.Max(microCount, smallCount),
                mediumCount);

        if (subscriptionGraphMaximum == 0)
        {
            subscriptionGraphMaximum = 1;
        }


        // Set graph maximum
        MicroSubscriptionProgress.Maximum =
            subscriptionGraphMaximum;

        SmallSubscriptionProgress.Maximum =
            subscriptionGraphMaximum;

        MediumSubscriptionProgress.Maximum =
            subscriptionGraphMaximum;


        // Set graph values
        MicroSubscriptionProgress.Value =
            microCount;

        SmallSubscriptionProgress.Value =
            smallCount;

        MediumSubscriptionProgress.Value =
            mediumCount;


        // Set count labels
        MicroSubscriptionCountText.Text =
            microCount.ToString();

        SmallSubscriptionCountText.Text =
            smallCount.ToString();

        MediumSubscriptionCountText.Text =
            mediumCount.ToString();

        // Update UI
        MicroCompaniesCountText.Text =
            $"{microCount} {(microCount == 1 ? "company" : "companies")}";

        SmallCompaniesCountText.Text =
            $"{smallCount} {(smallCount == 1 ? "company" : "companies")}";

        MediumCompaniesCountText.Text =
            $"{mediumCount} {(mediumCount == 1 ? "company" : "companies")}";
        // ==========================================
        // RECENT COMPANIES
        // ==========================================

        var recentCompanies =
            await (
                from company in _masterDb.Companies.AsNoTracking()

                join subscription in _masterDb.Subscriptions.AsNoTracking()
                    on company.CompanyId
                    equals subscription.CompanyId
                    into subscriptionGroup

                from subscription in subscriptionGroup
                    .OrderByDescending(s => s.StartDate)
                    .Take(1)
                    .DefaultIfEmpty()

                join plan in _masterDb.SubscriptionPlans.AsNoTracking()
                    on subscription.SubscriptionPlanId
                    equals plan.SubscriptionPlanId
                    into planGroup

                from plan in planGroup
                    .DefaultIfEmpty()

                orderby company.CreatedAt descending

                select new RecentCompanyDisplayItem
                {
                    CompanyName = company.CompanyName,
                    CompanyCode = company.CompanyCode,

                    EnterpriseType =
                        plan != null
                            ? plan.EnterpriseType
                            : "No Subscription",

                    CreatedAt = company.CreatedAt
                }
            )
            .Take(5)
            .ToListAsync();


        RecentCompaniesListView.ItemsSource =
            recentCompanies;
        // ==========================================
        // USERS OVERVIEW
        // ==========================================

        var users =
            await _masterDb.Users
                .AsNoTracking()
                .Include(u => u.Company)
                .ToListAsync();


        // ==========================================
        // USER COUNTS
        // ==========================================

        var totalSystemUsers =
            users.Count;

        var activeSystemUsers =
            users.Count(u => u.IsActive);


        // ==========================================
        // GET USER ROLES
        // ==========================================

        var userRoles =
            await (
                from userRole in _masterDb.UserRoles.AsNoTracking()

                join role in _masterDb.Roles.AsNoTracking()
                    on userRole.RoleId equals role.Id

                select new
                {
                    userRole.UserId,
                    RoleName = role.Name ?? "No Role"
                }
            )
            .ToListAsync();


        // ==========================================
        // ADMIN USER COUNT
        // ==========================================

        var adminUserIds =
            userRoles
                .Where(x =>
                    x.RoleName == "Super Admin" ||
                    x.RoleName == "Admin")
                .Select(x => x.UserId)
                .Distinct()
                .ToHashSet();

        var adminUserCount =
            adminUserIds.Count;


        // ==========================================
        // STAFF USER COUNT
        // ==========================================

        var staffUserCount =
            users.Count -
            adminUserCount;


        // ==========================================
        // UPDATE USER KPI CARDS
        // ==========================================

        SuperAdminUsersTotalText.Text =
            totalSystemUsers.ToString();

        SuperAdminActiveUsersText.Text =
            activeSystemUsers.ToString();

        SuperAdminAdminUsersText.Text =
            adminUserCount.ToString();

        SuperAdminStaffUsersText.Text =
            staffUserCount.ToString();


        // ==========================================
        // USERS BY ROLE
        // ==========================================

        var roleSummary =
            userRoles
                .GroupBy(x => x.RoleName)
                .OrderBy(g => g.Key)
                .Select(g =>
                    $"{g.Key}: {g.Select(x => x.UserId).Distinct().Count()}")
                .ToList();

        SuperAdminUserRoleSummaryText.Text =
            roleSummary.Count > 0
                ? string.Join("   •   ", roleSummary)
                : "No roles assigned.";

        // ==========================================
        // USER LIST
        // ==========================================

        var userDisplayItems =
            users
                .Select(user =>
                {
                    var role =
                        userRoles
                            .FirstOrDefault(x =>
                                x.UserId == user.Id)
                            ?.RoleName
                        ?? "No Role";

                    return new SuperAdminUserDisplayItem
                    {
                        UserName =
                            user.UserName ?? string.Empty,

                        Email =
                            user.Email ?? string.Empty,

                        Role =
                            role,

                        CompanyName =
                            user.Company?.CompanyName
                            ?? "System",

                        Status =
                            user.IsActive
                                ? "Active"
                                : "Inactive",

                        LastLogin =
                            user.LastLogin
                    };
                })
                .OrderBy(x => x.UserName)
                .ToList();


        SuperAdminUsersListView.ItemsSource =
            userDisplayItems;
        // ==========================================
        // RECENT SUBSCRIPTIONS
        // ==========================================

        var recentSubscriptions =
            await (
                from subscription in _masterDb.Subscriptions.AsNoTracking()

                join company in _masterDb.Companies.AsNoTracking()
                    on subscription.CompanyId
                    equals company.CompanyId

                join plan in _masterDb.SubscriptionPlans.AsNoTracking()
                    on subscription.SubscriptionPlanId
                    equals plan.SubscriptionPlanId

                orderby subscription.StartDate descending

                select new SuperAdminSubscriptionDisplayItem
                {
                    CompanyName =
                        company.CompanyName,

                    PlanName =
                        plan.PlanName,

                    EnterpriseType =
                        plan.EnterpriseType,

                    Price =
                        plan.Price,

                    Status =
                        subscription.Status,

                    StartDate =
                        subscription.StartDate,

                    EndDate =
                        subscription.EndDate
                }
            )
            .Take(5)
            .ToListAsync();


        RecentSubscriptionsListView.ItemsSource =
            recentSubscriptions;
    }

    // ==========================================
    // DRAW SALES TREND GRAPH
    // ==========================================

    private void DrawSalesTrendGraph(
        List<decimal> values,
        List<string> labels)
    {
        SalesTrendGraphContainer.Children.Clear();


        if (values.Count == 0)
        {
            return;
        }


        var graph =
            new Canvas
            {
                Height = 170,
                HorizontalAlignment =
                    HorizontalAlignment.Stretch
            };


        var graphWidth =
            Math.Max(
                400,
                SalesTrendGraphContainer.ActualWidth - 20);


        graph.Width =
            graphWidth;


        var graphHeight =
            170.0;

        var leftMargin =
            55.0;

        var rightMargin =
            15.0;

        var topMargin =
            10.0;

        var bottomMargin =
            28.0;


        var plotWidth =
            graphWidth -
            leftMargin -
            rightMargin;

        var plotHeight =
            graphHeight -
            topMargin -
            bottomMargin;


        // ==========================================
        // MAX VALUE
        // ==========================================

        var maxValue =
            values.Max();


        if (maxValue <= 0)
        {
            maxValue = 1;
        }


        var orangeBrush =
            (Brush)Resources["AccentOrangeBrush"];


        var gridBrush =
            new SolidColorBrush(
                Windows.UI.Color.FromArgb(
                    45,
                    255,
                    255,
                    255));


        var textBrush =
            new SolidColorBrush(
                Windows.UI.Color.FromArgb(
                    150,
                    255,
                    255,
                    255));


        // ==========================================
        // HORIZONTAL GRID LINES
        // ==========================================

        for (int i = 0; i <= 4; i++)
        {
            var ratio =
                i / 4.0;

            var y =
                topMargin +
                plotHeight -
                (plotHeight * ratio);


            var line =
                new Line
                {
                    X1 = leftMargin,
                    X2 = graphWidth - rightMargin,
                    Y1 = y,
                    Y2 = y,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                };


            graph.Children.Add(line);


            // Y-axis value

            var axisValue =
                maxValue *
                (decimal)ratio;


            var valueText =
                new TextBlock
                {
                    Text =
                        $"₱{axisValue:N0}",

                    FontSize = 10,
                    Foreground = textBrush
                };


            Canvas.SetLeft(
                valueText,
                0);

            Canvas.SetTop(
                valueText,
                Math.Max(
                    0,
                    y - 7));


            graph.Children.Add(
                valueText);
        }


        // ==========================================
        // GRAPH POINTS
        // ==========================================

        var points =
            new PointCollection();


        var pointCount =
            values.Count;


        for (int i = 0;
             i < pointCount;
             i++)
        {
            var x =
                pointCount == 1
                    ? leftMargin + plotWidth / 2
                    : leftMargin +
                      (plotWidth /
                       (pointCount - 1) *
                       i);


            var ratio =
                (double)(
                    values[i] /
                    maxValue);


            var y =
                topMargin +
                plotHeight -
                (plotHeight * ratio);


            points.Add(
                new Point(
                    x,
                    y));


            // ==================================
            // DATA POINT
            // ==================================

            var point =
                new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = orangeBrush
                };


            Canvas.SetLeft(
                point,
                x - 4);

            Canvas.SetTop(
                point,
                y - 4);


            graph.Children.Add(point);


            // ==================================
            // VALUE LABEL
            // ==================================

            var valueText =
                new TextBlock
                {
                    Text =
                        $"₱{values[i]:N0}",

                    FontSize = 10,
                    Foreground = textBrush
                };


            Canvas.SetLeft(
                valueText,
                Math.Max(
                    leftMargin,
                    x - 22));

            Canvas.SetTop(
                valueText,
                Math.Max(
                    0,
                    y - 22));


            graph.Children.Add(
                valueText);


            // ==================================
            // X-AXIS LABEL
            // ==================================

            var labelText =
                new TextBlock
                {
                    Text =
                        labels[i],

                    FontSize = 11,
                    Foreground = textBrush
                };


            Canvas.SetLeft(
                labelText,
                Math.Max(
                    leftMargin - 10,
                    x - 15));

            Canvas.SetTop(
                labelText,
                graphHeight - 20);


            graph.Children.Add(
                labelText);
        }


        // ==========================================
        // CONNECT POINTS
        // ==========================================

        var polyline =
            new Polyline
            {
                Points = points,
                Stroke = orangeBrush,
                StrokeThickness = 3,
                StrokeLineJoin =
                    PenLineJoin.Round
            };


        graph.Children.Add(
            polyline);


        Canvas.SetZIndex(
            polyline,
            10);


        SalesTrendGraphContainer.Children.Add(
            graph);
    }
    // ==========================================
    // REPAIR STATUS
    // ==========================================

    private void UpdateRepairStatus(
        int pending,
        int inProgress,
        int completed,
        int total)
    {
        PendingRepairsGraphText.Text =
            pending.ToString();

        InProgressRepairsGraphText.Text =
            inProgress.ToString();

        CompletedRepairsGraphText.Text =
            completed.ToString();


        if (total <= 0)
        {
            PendingRepairsProgress.Value = 0;
            InProgressRepairsProgress.Value = 0;
            CompletedRepairsProgress.Value = 0;

            return;
        }


        PendingRepairsProgress.Value =
            (double)pending /
            total *
            100;

        InProgressRepairsProgress.Value =
            (double)inProgress /
            total *
            100;

        CompletedRepairsProgress.Value =
            (double)completed /
            total *
            100;
    }


    // ==========================================
    // REPAIR OVERVIEW GRAPH
    // ==========================================

    private async Task LoadRepairOverviewAsync(
        TenantDbContext db)
    {
        RepairGraphContainer.Children.Clear();


        var currentMonth =
            new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month,
                1);

        var startMonth =
            currentMonth.AddMonths(-5);

        var endMonth =
            currentMonth.AddMonths(1);


        // Use Service Request date because every
        // repair is connected to a service request.

        var repairDatesQuery =
            db.Repairs
                .AsNoTracking()
                .Where(r =>
                    r.ServiceRequest != null &&
                    r.ServiceRequest.RequestDate >= startMonth &&
                    r.ServiceRequest.RequestDate < endMonth);

        if (_branchManagementEnabled &&
            _selectedDashboardBranchId.HasValue)
        {
            var branchId =
                _selectedDashboardBranchId.Value;

            repairDatesQuery =
                repairDatesQuery.Where(r =>
                    r.ServiceRequest!.BranchId ==
                    branchId);
        }

        var repairDates =
            await repairDatesQuery
                .Select(r =>
                    r.ServiceRequest!.RequestDate)
                .ToListAsync();


        var monthlyCounts =
            new List<int>();


        for (int i = 0; i < 6; i++)
        {
            var month =
                startMonth.AddMonths(i);

            var count =
                repairDates.Count(date =>
                    date.Year == month.Year &&
                    date.Month == month.Month);

            monthlyCounts.Add(count);
        }


        // ==========================================
        // CREATE CANVAS
        // ==========================================

        var graph =
            new Canvas
            {
                Height = 170,
                HorizontalAlignment =
                    HorizontalAlignment.Stretch
            };


        var graphWidth =
            Math.Max(
                400,
                RepairGraphContainer.ActualWidth - 20);


        graph.Width =
            graphWidth;


        var graphHeight =
            170.0;

        var leftMargin =
            30.0;

        var rightMargin =
            15.0;

        var topMargin =
            10.0;

        var bottomMargin =
            28.0;

        var plotWidth =
            graphWidth -
            leftMargin -
            rightMargin;

        var plotHeight =
            graphHeight -
            topMargin -
            bottomMargin;


        // ==========================================
        // MAX VALUE
        // ==========================================

        var maxValue =
            monthlyCounts.Count > 0
                ? monthlyCounts.Max()
                : 0;


        if (maxValue == 0)
        {
            maxValue = 1;
        }


        var orangeBrush =
            (Brush)Resources["AccentOrangeBrush"];


        var gridBrush =
            new SolidColorBrush(
                Windows.UI.Color.FromArgb(
                    45,
                    255,
                    255,
                    255));


        var textBrush =
            new SolidColorBrush(
                Windows.UI.Color.FromArgb(
                    150,
                    255,
                    255,
                    255));


        // ==========================================
        // HORIZONTAL GRID LINES
        // ==========================================

        for (int i = 0; i <= 4; i++)
        {
            var ratio =
                i / 4.0;

            var y =
                topMargin +
                plotHeight -
                (plotHeight * ratio);


            var line =
                new Line
                {
                    X1 = leftMargin,
                    X2 = graphWidth - rightMargin,
                    Y1 = y,
                    Y2 = y,
                    Stroke = gridBrush,
                    StrokeThickness = 1
                };


            graph.Children.Add(line);
        }


        // ==========================================
        // GRAPH LINE
        // ==========================================

        var points =
            new PointCollection();


        for (int i = 0; i < 6; i++)
        {
            var x =
                leftMargin +
                (plotWidth /
                 5 *
                 i);


            var ratio =
                (double)monthlyCounts[i] /
                maxValue;


            var y =
                topMargin +
                plotHeight -
                (plotHeight * ratio);


            points.Add(
                new Point(
                    x,
                    y));


            // ==================================
            // DATA POINT
            // ==================================

            var point =
                new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = orangeBrush
                };


            Canvas.SetLeft(
                point,
                x - 4);

            Canvas.SetTop(
                point,
                y - 4);


            graph.Children.Add(point);


            // ==================================
            // VALUE LABEL
            // ==================================

            var valueText =
                new TextBlock
                {
                    Text =
                        monthlyCounts[i]
                            .ToString(),

                    FontSize = 11,
                    Foreground = textBrush
                };


            Canvas.SetLeft(
                valueText,
                Math.Max(
                    0,
                    x - 8));

            Canvas.SetTop(
                valueText,
                Math.Max(
                    0,
                    y - 22));


            graph.Children.Add(valueText);


            // ==================================
            // MONTH LABEL
            // ==================================

            var month =
                startMonth.AddMonths(i);


            var monthText =
                new TextBlock
                {
                    Text =
                        month.ToString("MMM"),

                    FontSize = 11,
                    Foreground = textBrush
                };


            Canvas.SetLeft(
                monthText,
                Math.Max(
                    0,
                    x - 12));

            Canvas.SetTop(
                monthText,
                graphHeight - 20);


            graph.Children.Add(
                monthText);
        }


        // ==========================================
        // CONNECT THE POINTS
        // ==========================================

        var polyline =
            new Polyline
            {
                Points = points,
                Stroke = orangeBrush,
                StrokeThickness = 3,
                StrokeLineJoin = PenLineJoin.Round
            };


        graph.Children.Add(polyline);


        // Bring line in front of grid.
        Canvas.SetZIndex(
            polyline,
            10);


        RepairGraphContainer.Children.Add(
            graph);
    }


    // ==========================================
    // RECENT REPAIRS
    // ==========================================

    private async Task LoadRecentRepairsAsync(
        TenantDbContext db)
    {
        var repairsQuery =
            db.Repairs
                .AsNoTracking()
                .Include(r =>
                    r.ServiceRequest)
                    .ThenInclude(sr =>
                        sr.Device)
                    .ThenInclude(d =>
                        d.Customer)
                .AsQueryable();

        if (_branchManagementEnabled &&
            _selectedDashboardBranchId.HasValue)
        {
            var branchId =
                _selectedDashboardBranchId.Value;

            repairsQuery =
                repairsQuery.Where(r =>
                    r.ServiceRequest!.BranchId ==
                    branchId);
        }

        var repairs =
            await repairsQuery
                .OrderByDescending(
                    r => r.RepairId)
                .Take(5)
                .Select(r => new RecentRepairRow
                {
                    RepairNumber =
                        $"#R-{r.RepairId:D4}",

                    CustomerName =
                        r.ServiceRequest!
                            .Device!
                            .Customer!
                            .FirstName
                        + " " +
                        r.ServiceRequest!
                            .Device!
                            .Customer!
                            .LastName,

                    DeviceName =
                        (
                            r.ServiceRequest!
                                .Device!
                                .Brand
                            + " " +
                            r.ServiceRequest!
                                .Device!
                                .Model
                        ).Trim(),

                    Status =
                        r.Status
                })
                .ToListAsync();


        RecentRepairsListView.ItemsSource =
            repairs;
    }


    // ==========================================
    // RECENT TRANSACTIONS
    // ==========================================

    private async Task LoadRecentTransactionsAsync(
        TenantDbContext db)
    {
        var transactionsQuery =
            db.Payments
                .AsNoTracking()
                .Include(p =>
                    p.Invoice)
                    .ThenInclude(i =>
                        i.Repair)
                    .ThenInclude(r =>
                        r.ServiceRequest)
                    .ThenInclude(sr =>
                        sr.Device)
                    .ThenInclude(d =>
                        d.Customer)
                .AsQueryable();

        if (_branchManagementEnabled &&
            _selectedDashboardBranchId.HasValue)
        {
            var branchId =
                _selectedDashboardBranchId.Value;

            transactionsQuery =
                transactionsQuery.Where(p =>
                    p.Invoice!.Repair!.ServiceRequest!.BranchId ==
                    branchId);
        }

        var transactions =
            await transactionsQuery
                .OrderByDescending(
                    p => p.PaymentDate)
                .Take(5)
                .Select(p => new RecentTransactionRow
                {
                    InvoiceNumber =
                        p.Invoice!.InvoiceNumber,

                    CustomerName =
                        p.Invoice!
                            .Repair!
                            .ServiceRequest!
                            .Device!
                            .Customer!
                            .FirstName
                        + " " +
                        p.Invoice!
                            .Repair!
                            .ServiceRequest!
                            .Device!
                            .Customer!
                            .LastName,

                    PaymentMethod =
                        p.PaymentMethod,

                    AmountText =
                        $"₱{p.Amount:N2}"
                })
                .ToListAsync();


        RecentTransactionsListView.ItemsSource =
            transactions;
    }


    // ==========================================
    // LOW STOCK
    // ==========================================

    private async Task LoadLowStockAsync(
        TenantDbContext db)
    {
        var lowStockQuery =
            db.Inventories
                .AsNoTracking()
                .Include(i =>
                    i.Item)
                .Where(i =>
                    i.Item != null &&
                    i.Item.IsActive &&
                    i.QuantityOnHand <=
                    i.Item.ReorderLevel);

        if (_branchManagementEnabled &&
            _selectedDashboardBranchId.HasValue)
        {
            var branchId =
                _selectedDashboardBranchId.Value;

            lowStockQuery =
                lowStockQuery.Where(i =>
                    i.BranchId == branchId);
        }

        var lowStock =
            await lowStockQuery
                .OrderBy(
                    i => i.QuantityOnHand)
                .Take(8)
                .Select(i => new
                {
                    i.Item!.ItemName,
                    i.QuantityOnHand,
                    i.Item.Unit
                })
                .ToListAsync();


        var lowStockTotalQuery =
            db.Inventories
                .AsNoTracking()
                .Where(i =>
                    i.Item != null &&
                    i.Item.IsActive &&
                    i.QuantityOnHand <=
                    i.Item.ReorderLevel);

        if (_branchManagementEnabled &&
            _selectedDashboardBranchId.HasValue)
        {
            var branchId =
                _selectedDashboardBranchId.Value;

            lowStockTotalQuery =
                lowStockTotalQuery.Where(i =>
                    i.BranchId == branchId);
        }

        var lowStockTotal =
            await lowStockTotalQuery.CountAsync();


        LowStockCountText.Text =
            $"{lowStockTotal} item" +
            (lowStockTotal == 1
                ? string.Empty
                : "s");


        LowStockListView.ItemsSource =
            lowStock.Select(
                x =>
                    $"{x.ItemName} — " +
                    $"{x.QuantityOnHand:0.##} " +
                    $"{x.Unit}")
                .ToList();
    }


    // ==========================================
    // OUTSTANDING PAYMENTS
    // ==========================================

    private async Task LoadOutstandingPaymentsAsync(
        TenantDbContext db)
    {
        var invoicesQuery =
            db.Invoices
                .AsNoTracking()
                .Include(i =>
                    i.Payments)
                .Where(i =>
                    i.Status != "Cancelled");

        if (_branchManagementEnabled &&
            _selectedDashboardBranchId.HasValue)
        {
            var branchId =
                _selectedDashboardBranchId.Value;

            invoicesQuery =
                invoicesQuery.Where(i =>
                    i.Repair!.ServiceRequest!.BranchId ==
                    branchId);
        }

        var invoices =
            await invoicesQuery
                .Select(i => new
                {
                    i.TotalAmount,

                    Paid =
                        i.Payments
                            .Where(p =>
                                p.Status ==
                                "Completed")
                            .Sum(p =>
                                (decimal?)p.Amount)
                        ?? 0
                })
                .ToListAsync();


        var outstandingInvoices =
            invoices
                .Where(x =>
                    x.TotalAmount > x.Paid)
                .ToList();


        var outstandingAmount =
            outstandingInvoices.Sum(
                x =>
                    Math.Max(
                        0,
                        x.TotalAmount -
                        x.Paid));


        OutstandingPaymentsText.Text =
            $"₱{outstandingAmount:N2}";


        OutstandingInvoicesCountText.Text =
            $"{outstandingInvoices.Count} invoice" +
            (outstandingInvoices.Count == 1
                ? string.Empty
                : "s");
    }


    // ==========================================
    // TODAY'S ATTENDANCE
    // ==========================================

    private async Task LoadTodayAttendanceAsync(
        TenantDbContext db)
    {
        var today =
            DateTime.Today;

        var tomorrow =
            today.AddDays(1);


        var attendanceQuery =
            db.Attendances
                .AsNoTracking()
                .Where(a =>
                    a.AttendanceDate >= today &&
                    a.AttendanceDate < tomorrow);

        if (_branchManagementEnabled &&
            _selectedDashboardBranchId.HasValue)
        {
            var branchId =
                _selectedDashboardBranchId.Value;

            attendanceQuery =
                attendanceQuery.Where(a =>
                    a.Employee!.BranchId ==
                    branchId);
        }

        var attendance =
            await attendanceQuery
                .Select(a =>
                    a.Status)
                .ToListAsync();


        var present =
            attendance.Count(s =>
                s == "Present");

        var late =
            attendance.Count(s =>
                s == "Late");

        var absent =
            attendance.Count(s =>
                s == "Absent");

        var leave =
            attendance.Count(s =>
                s == "Leave");


        PresentAttendanceText.Text =
            $"Present: {present}";

        LateAttendanceText.Text =
            $"Late: {late}";

        AbsentAttendanceText.Text =
            $"Absent: {absent}";

        LeaveAttendanceText.Text =
            $"Leave: {leave}";
    }


    // ==========================================
    // RECENT REPAIR ROW
    // ==========================================

    private sealed class RecentRepairRow
    {
        public string RepairNumber { get; set; }
            = string.Empty;

        public string CustomerName { get; set; }
            = string.Empty;

        public string DeviceName { get; set; }
            = string.Empty;

        public string Status { get; set; }
            = string.Empty;
    }


    // ==========================================
    // RECENT TRANSACTION ROW
    // ==========================================

    private sealed class RecentTransactionRow
    {
        public string InvoiceNumber { get; set; }
            = string.Empty;

        public string CustomerName { get; set; }
            = string.Empty;

        public string PaymentMethod { get; set; }
            = string.Empty;

        public string AmountText { get; set; }
            = string.Empty;
    }
    // ==========================================
    // RECENT COMPANY DISPLAY MODEL
    // ==========================================

    public class RecentCompanyDisplayItem
    {
        public string CompanyName { get; set; }
            = string.Empty;

        public string CompanyCode { get; set; }
            = string.Empty;

        public string EnterpriseType { get; set; }
            = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string CreatedAtText =>
            CreatedAt.ToString("MMM dd, yyyy");
    }
    // ==========================================
    // SUPER ADMIN USER DISPLAY MODEL
    // ==========================================

    public class SuperAdminUserDisplayItem
    {
        public string UserName { get; set; }
            = string.Empty;

        public string Email { get; set; }
            = string.Empty;

        public string Role { get; set; }
            = string.Empty;

        public string CompanyName { get; set; }
            = "System";

        public string Status { get; set; }
            = string.Empty;

        public DateTime? LastLogin { get; set; }
    }
    // ==========================================
    // SUPER ADMIN SUBSCRIPTION DISPLAY MODEL
    // ==========================================

    public class SuperAdminSubscriptionDisplayItem
    {
        public string CompanyName { get; set; }
            = string.Empty;

        public string PlanName { get; set; }
            = string.Empty;

        public string EnterpriseType { get; set; }
            = string.Empty;

        public decimal Price { get; set; }

        public string PriceText =>
            $"₱{Price:N2}";

        public string Status { get; set; }
            = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string StartDateText =>
            StartDate.ToString("MMM dd, yyyy");

        public string EndDateText =>
            EndDate.HasValue
                ? $"Ends {EndDate.Value:MMM dd, yyyy}"
                : "No end date";
    }
}