using ComputerRepairSystem.company.Context;
using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Interfaces;
using ComputerRepairSystem.company.Repositories;
using ComputerRepairSystem.company.Services;
using ComputerRepairSystem.infrastructure.data;
using ComputerRepairSystem.infrastructure.Entities;
using ComputerRepairSystem.winui.Pages;
using ComputerRepairSystem_winui.Pages;
using ComputerRepairSystem_winui.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace ComputerRepairSystem_winui;

public partial class App : Application
{
    private Window? _window;

    public static IServiceProvider Services { get; private set; }
        = null!;

    public App()
    {
        InitializeComponent();

        // ==========================================
        // CONFIGURATION
        // ==========================================

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(
                "appsettings.json",
                optional: false,
                reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();

        // ==========================================
        // CONNECTION STRINGS
        // ==========================================

        var masterConnectionString =
            configuration.GetConnectionString("MasterLocal")
            ?? throw new InvalidOperationException(
                "Connection string 'MasterLocal' was not found.");

        var tenantConnectionString =
            configuration.GetConnectionString("TenantLocal")
            ?? throw new InvalidOperationException(
                "Connection string 'TenantLocal' was not found.");

        // ==========================================
        // MASTER DATABASE
        // ==========================================

        services.AddDbContext<MasterErpDbContext>(
            options =>
                options.UseSqlServer(
                    masterConnectionString));

        services.AddTransient<TenantDbContextFactory>();
        
        services.AddTransient<ITenantDbContextFactory>(
            provider =>
                provider.GetRequiredService<TenantDbContextFactory>());

        services.AddSingleton<ICompanyContext, CompanyContext>();

        // ==========================================
        // ASP.NET IDENTITY
        // ==========================================

        services.AddIdentityCore<ApplicationUser>(
                options =>
                {
                    options.Password.RequireDigit = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;

                    options.Password.RequiredLength = 6;
                })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<MasterErpDbContext>();

        // ==========================================
        // TENANT DATABASE
        // ==========================================

        services.AddDbContextFactory<TenantDbContext>(
            options =>
                options.UseSqlServer(
                    tenantConnectionString));

        // ==========================================
        // CUSTOMER REPOSITORY
        // ==========================================

        services.AddTransient<
            ICustomerRepository,
            CustomerRepository>();

        // ==========================================
        // SERVICES
        // ==========================================

        services.AddTransient<CustomerService>();

        // ==========================================
        // PAGES
        // ==========================================

        services.AddTransient<LoginPage>();

        services.AddTransient<ServiceManagementPage>();

        services.AddTransient<UserManagementPage>();

        services.AddTransient<RepairManagementPage>();

        services.AddTransient<BillingPage>();

        services.AddTransient<InventoryManagementPage>();

        services.AddTransient<HomePage>();

        services.AddTransient<SettingsPage>();

        services.AddTransient<CustomerManagementPage>();

        services.AddTransient<CompanyManagementPage>();

        services.AddTransient<EmployeeManagementPage>();

        services.AddTransient<AttendanceManagementPage>();

        services.AddTransient<PayrollManagementPage>();

        services.AddTransient<FinanceManagementPage>();

        services.AddTransient<SupplierManagementPage>();

        services.AddTransient<TermsAndConditionsPage>();

        services.AddTransient<SubscriptionManagementPage>();

        services.AddTransient<SubscriptionAccessService>();

        services.AddTransient<PurchaseManagementPage>();

        services.AddTransient<MySubscriptionPage>();

        services.AddTransient<BranchManagementPage>();

        services.AddTransient<DepartmentManagementPage>();

        services.AddTransient<SuperAdminSystemSettingsPage>();

        services.AddSingleton<SuperAdminSystemSettingsService>();

        services.AddSingleton<CurrentBranchContext>();

        services.AddSingleton<MasterErpDbContext>();
        // ==========================================
        // MAIN WINDOW
        // ==========================================

        services.AddSingleton<MainWindow>();

        Services = services.BuildServiceProvider();
    }

    protected override async void OnLaunched(
        LaunchActivatedEventArgs args)
    {
        // ==========================================
        // INITIALIZE MASTER DATABASE
        // ==========================================

        // await InitializeMasterDbAsync();

        // ==========================================
        // INITIALIZE SYSTEM ACCOUNTS
        // ==========================================

        // await InitializeAdminAsync();

        /*
        // ==========================================
        // SEED TENANT CUSTOMERS AND DEVICES
        // ==========================================

        using var scope =
            Services.CreateScope();

        var dbFactory =
            scope.ServiceProvider
                .GetRequiredService<
                    IDbContextFactory<TenantDbContext>>();

        await using var db =
            await dbFactory.CreateDbContextAsync();

        await TenantDbSeeder.SeedAsync(db);
        */

        // ==========================================
        // START APPLICATION
        // ==========================================

        _window =
            Services.GetRequiredService<MainWindow>();

        _window.Activate();
    }

    private async Task InitializeAdminAsync()
    {
        using var scope = Services.CreateScope();

        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<
                    RoleManager<IdentityRole>>();

        // ==========================================
        // CREATE REQUIRED ROLES
        // ==========================================

        var roles = new[]
        {
            "Super Admin",
            "Admin",
            "Technician",
            "Receptionist",
            "HR Staff",
            "Finance Staff"
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(
                    new IdentityRole(role));
            }
        }

        // ==========================================
        // USER MANAGER
        // ==========================================

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        // ==========================================
        // OPTIONAL ADMIN ACCOUNT
        // ==========================================

        ApplicationUser? admin =
            await userManager.FindByNameAsync("admin");

        if (admin != null)
        {
            // ==========================================
            // MAKE SURE ADMIN HAS A PASSWORD
            // ==========================================

            if (string.IsNullOrEmpty(admin.PasswordHash))
            {
                var passwordResult =
                    await userManager.AddPasswordAsync(
                        admin,
                        "Admin123!");

                if (!passwordResult.Succeeded)
                {
                    foreach (var error in passwordResult.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            $"{error.Code}: {error.Description}");
                    }
                }
            }

            // ==========================================
            // MAKE SURE ADMIN HAS ADMIN ROLE
            // ==========================================

            if (!await userManager.IsInRoleAsync(
                    admin,
                    "Admin"))
            {
                await userManager.AddToRoleAsync(
                    admin,
                    "Admin");
            }
        }

        // ==========================================
        // CREATE INITIAL SUPER ADMIN
        // ==========================================

        ApplicationUser? superAdmin =
            await userManager.FindByNameAsync(
                "superadmin");

        if (superAdmin == null)
        {
            superAdmin = new ApplicationUser
            {
                UserName = "superadmin",
                Email = "superadmin@fixflow.com",
                EmailConfirmed = true,
                CompanyId = null,
                IsActive = true
            };

            var createResult =
                await userManager.CreateAsync(
                    superAdmin,
                    "SuperAdmin123!");

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"{error.Code}: {error.Description}");
                }

                return;
            }
        }

        // ==========================================
        // MAKE SURE SUPER ADMIN HAS CORRECT ROLE
        // ==========================================

        if (!await userManager.IsInRoleAsync(
                superAdmin,
                "Super Admin"))
        {
            await userManager.AddToRoleAsync(
                superAdmin,
                "Super Admin");
        }
    }

    private async Task InitializeMasterDbAsync()
    {
        using var scope = Services.CreateScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<
                    MasterErpDbContext>();
        if (db == null)
        {
            throw new Exception("MasterErpDbContext is NULL.");
        }
        // await MasterDbSeeder.SeedAsync(db);
    }
}