using ComputerRepairSystem.domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairSystem.infrastructure.data;

public static class MasterDbSeeder
{
    public static async Task SeedAsync(
        MasterErpDbContext context)
    {
        // ============================
        // SAMPLE COMPANIES
        // ============================

        if (!await context.Companies.AnyAsync())
        {
            var companies = new List<Company>
            {
                new Company
                {
                    CompanyCode = "COMP001",
                    CompanyName = "TechFix Solutions"
                },

                new Company
                {
                    CompanyCode = "COMP002",
                    CompanyName = "PC Repair Hub"
                },

                new Company
                {
                    CompanyCode = "COMP003",
                    CompanyName = "Davao Computer Services"
                }
            };

            context.Companies.AddRange(companies);

            await context.SaveChangesAsync();
        }


        // ============================
        // MODULE DEFINITIONS
        // ============================

        if (!await context.ModuleDefinitions.AnyAsync())
        {
            var modules = new List<ModuleDefinition>
            {
                new ModuleDefinition
                {
                    ModuleCode = "DASHBOARD",
                    ModuleName = "Dashboard",
                    Description = "Business dashboard and key performance indicators.",
                    DisplayOrder = 1
                },

                new ModuleDefinition
                {
                    ModuleCode = "CUSTOMER",
                    ModuleName = "Customer Management",
                    Description = "Manage customers and customer information.",
                    DisplayOrder = 2
                },

                new ModuleDefinition
                {
                    ModuleCode = "DEVICE",
                    ModuleName = "Device Management",
                    Description = "Manage customer devices and equipment.",
                    DisplayOrder = 3
                },

                new ModuleDefinition
                {
                    ModuleCode = "REPAIR",
                    ModuleName = "Repair Management",
                    Description = "Manage repair requests and repair operations.",
                    DisplayOrder = 4
                },

                new ModuleDefinition
                {
                    ModuleCode = "INVENTORY",
                    ModuleName = "Inventory Management",
                    Description = "Manage repair parts and stock levels.",
                    DisplayOrder = 5
                },

                new ModuleDefinition
                {
                    ModuleCode = "SUPPLIER",
                    ModuleName = "Supplier Management",
                    Description = "Manage suppliers and supply information.",
                    DisplayOrder = 6
                },

                new ModuleDefinition
                {
                    ModuleCode = "EMPLOYEE",
                    ModuleName = "Employee Management",
                    Description = "Manage employee information.",
                    DisplayOrder = 7
                },

                new ModuleDefinition
                {
                    ModuleCode = "ATTENDANCE",
                    ModuleName = "Attendance Management",
                    Description = "Manage employee attendance and time records.",
                    DisplayOrder = 8
                },

                new ModuleDefinition
                {
                    ModuleCode = "PAYROLL",
                    ModuleName = "Payroll Management",
                    Description = "Manage employee payroll records.",
                    DisplayOrder = 9
                },

                new ModuleDefinition
                {
                    ModuleCode = "FINANCE",
                    ModuleName = "Finance Management",
                    Description = "Manage payments, invoices, and expenses.",
                    DisplayOrder = 10
                },

                new ModuleDefinition
                {
                    ModuleCode = "HR",
                    ModuleName = "Human Resources",
                    Description = "Manage employees, attendance, and payroll.",
                    DisplayOrder = 11
                }
            };

            context.ModuleDefinitions.AddRange(modules);

            await context.SaveChangesAsync();

            // ============================
            // ENSURE HR MODULE EXISTS
            // ============================

            var hrModuleExists =
                await context.ModuleDefinitions
                    .AnyAsync(x => x.ModuleCode == "HR");

            if (!hrModuleExists)
            {
                context.ModuleDefinitions.Add(
                    new ModuleDefinition
                    {
                        ModuleCode = "HR",
                        ModuleName = "Human Resources",
                        Description =
                            "Manage employees, attendance, and payroll.",
                        DisplayOrder = 11
                    });

                await context.SaveChangesAsync();
            }
        }


        // ============================
        // SUBSCRIPTION PLANS
        // ============================

        if (!await context.SubscriptionPlans.AnyAsync())
        {
            var plans = new List<SubscriptionPlan>
            {
                new SubscriptionPlan
                {
                    PlanName = "Trial",
                    Price = 0,
                    DurationInDays = 7,
                    IsActive = true
                },

                new SubscriptionPlan
                {
                    PlanName = "Standard",
                    Price = 0,
                    DurationInDays = 30,
                    IsActive = true
                }
            };

            context.SubscriptionPlans.AddRange(plans);

            await context.SaveChangesAsync();
        }


        // ============================
        // SUBSCRIPTION PLAN MODULES
        // ============================

        if (!await context.SubscriptionPlanModules.AnyAsync())
        {
            var trialPlan = await context.SubscriptionPlans
                .FirstAsync(x => x.PlanName == "Trial");

            var standardPlan = await context.SubscriptionPlans
                .FirstAsync(x => x.PlanName == "Standard");

            var modules = await context.ModuleDefinitions
                .Where(x => x.IsActive)
                .ToListAsync();

            var trialModules = modules
                .Where(x =>
                    x.ModuleCode == "DASHBOARD" ||
                    x.ModuleCode == "CUSTOMER" ||
                    x.ModuleCode == "DEVICE" ||
                    x.ModuleCode == "REPAIR")
                .Select(x => new SubscriptionPlanModule
                {
                    SubscriptionPlanId =
                        trialPlan.SubscriptionPlanId,

                    ModuleDefinitionId =
                        x.ModuleDefinitionId
                });

            var standardModules = modules
                .Select(x => new SubscriptionPlanModule
                {
                    SubscriptionPlanId =
                        standardPlan.SubscriptionPlanId,

                    ModuleDefinitionId =
                        x.ModuleDefinitionId
                });

            context.SubscriptionPlanModules.AddRange(
                trialModules);

            context.SubscriptionPlanModules.AddRange(
                standardModules);

            await context.SaveChangesAsync();
        }
    }
}