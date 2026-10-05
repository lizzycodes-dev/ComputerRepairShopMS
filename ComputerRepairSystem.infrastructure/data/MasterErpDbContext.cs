using ComputerRepairSystem.domain.Entities;
using ComputerRepairSystem.infrastructure.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairSystem.infrastructure.data
{
    public class MasterErpDbContext
        : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public DbSet<Company> Companies
            => Set<Company>();

        public DbSet<CompanyDatabase> CompanyDatabases
            => Set<CompanyDatabase>();

        public DbSet<ModuleDefinition> ModuleDefinitions
            => Set<ModuleDefinition>();

        public DbSet<SubscriptionPlan> SubscriptionPlans
            => Set<SubscriptionPlan>();

        public DbSet<SubscriptionPlanModule> SubscriptionPlanModules
            => Set<SubscriptionPlanModule>();

        public DbSet<Subscription> Subscriptions
            => Set<Subscription>();

        public DbSet<TermsAndConditions> TermsAndConditions
            => Set<TermsAndConditions>();

        public DbSet<SuperAdminSystemSettings> SuperAdminSystemSettings
        => Set<SuperAdminSystemSettings>();

        public MasterErpDbContext(
            DbContextOptions<MasterErpDbContext> options)
            : base(options)
        {
        }


        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            // ==========================================
            // ASP.NET IDENTITY
            // ==========================================

            base.OnModelCreating(builder);


            // ==========================================
            // COMPANY
            // ==========================================

            builder.Entity<Company>(entity =>
            {
                entity.HasKey(x => x.CompanyId);

                entity.Property(x => x.CompanyCode)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.CompanyName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.HasIndex(x => x.CompanyCode)
                    .IsUnique();
            });


            // ==========================================
            // COMPANY DATABASE
            // ==========================================

            builder.Entity<CompanyDatabase>(entity =>
            {
                entity.HasKey(
                    x => x.CompanyDatabaseId);

                entity.Property(x => x.ServerName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.DatabaseName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.HasOne(x => x.Company)
                    .WithMany()
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });

            // ==========================================
            // MODULE DEFINITION
            // ==========================================

            builder.Entity<ModuleDefinition>(entity =>
            {
                entity.HasKey(x => x.ModuleDefinitionId);

                entity.Property(x => x.ModuleCode)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(x => x.ModuleName)
                    .HasMaxLength(150)
                    .IsRequired();

                entity.Property(x => x.Description)
                    .HasMaxLength(300);

                entity.Property(x => x.DisplayOrder)
                    .IsRequired();

                entity.HasIndex(x => x.ModuleCode)
                    .IsUnique();
            });


            // ==========================================
            // SUBSCRIPTION PLAN
            // ==========================================

            builder.Entity<SubscriptionPlan>(entity =>
            {
                entity.HasKey(x => x.SubscriptionPlanId);

                entity.Property(x => x.PlanName)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(x => x.Price)
                    .HasPrecision(18, 2);

                entity.Property(x => x.DurationInDays)
                    .IsRequired();

                entity.Property(x => x.IsActive)
                    .IsRequired();
            });

            // ==========================================
            // SUBSCRIPTION PLAN MODULE
            // ==========================================

            builder.Entity<SubscriptionPlanModule>(entity =>
            {
                entity.HasKey(x => x.SubscriptionPlanModuleId);

                entity.HasOne(x => x.SubscriptionPlan)
                    .WithMany()
                    .HasForeignKey(x => x.SubscriptionPlanId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.ModuleDefinition)
                    .WithMany()
                    .HasForeignKey(x => x.ModuleDefinitionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(x => new
                {
                    x.SubscriptionPlanId,
                    x.ModuleDefinitionId
                })
                .IsUnique();
            });

            // ==========================================
            // SUBSCRIPTION
            // ==========================================

            builder.Entity<Subscription>(entity =>
            {
                entity.HasKey(x => x.SubscriptionId);

                entity.Property(x => x.Status)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.HasOne(x => x.Company)
                    .WithMany()
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.SubscriptionPlan)
                    .WithMany()
                    .HasForeignKey(x => x.SubscriptionPlanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ==========================================
            // APPLICATION USER
            // ==========================================

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.HasOne(x => x.Company)
                    .WithMany()
                    .HasForeignKey(x => x.CompanyId)
                    .OnDelete(
                        DeleteBehavior.Restrict);
            });

            builder.Entity<TermsAndConditions>(entity =>
            {
                entity.HasKey(t => t.TermsAndConditionsId);

                entity.Property(t => t.Version)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(t => t.Content)
                    .IsRequired();

                entity.Property(t => t.CreatedAt)
                    .IsRequired();

                entity.Property(t => t.IsPublished)
                    .IsRequired();
            });

            // ==========================================
            // SUPER ADMIN SYSTEM SETTINGS
            // ==========================================

            builder.Entity<SuperAdminSystemSettings>(entity =>
            {
                entity.HasKey(
                    x => x.SuperAdminSystemSettingsId);

                entity.Property(x => x.PlatformName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(x => x.PlatformDescription)
                    .HasMaxLength(500);

                entity.Property(x => x.SupportEmail)
                    .HasMaxLength(200);

                entity.Property(x => x.SupportPhone)
                    .HasMaxLength(50);

                entity.Property(x => x.MaintenanceMode)
                    .IsRequired();

                entity.Property(x => x.AllowNewCompanyRegistration)
                    .IsRequired();

                entity.Property(x => x.DefaultTrialDurationInDays)
                    .IsRequired();

                entity.Property(x => x.UpdatedAt)
                    .IsRequired();
            });
        }
    }
}