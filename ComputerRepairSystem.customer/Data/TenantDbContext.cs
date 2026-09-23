using Microsoft.EntityFrameworkCore;
using ComputerRepairSystem.company.Entities;

namespace ComputerRepairSystem.company.Data;

public class TenantDbContext : DbContext
{

    //SycStuff
    public DbSet<SyncQueue> SyncQueues => Set<SyncQueue>();



    public TenantDbContext(
        DbContextOptions<TenantDbContext> options)
        : base(options)
    {
    }

    // ==========================================
    // Organization
    // ==========================================

    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Payroll> Payrolls => Set<Payroll>();


    // ==========================================
    // Customers & Devices
    // ==========================================

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Device> Devices => Set<Device>();

    // ==========================================
    // Repair Management
    // ==========================================

    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<Repair> Repairs => Set<Repair>();
    public DbSet<RepairItem> RepairItems => Set<RepairItem>();


    // ==========================================
    // Inventory
    // ==========================================

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<PurchaseOrder> PurchaseOrders
        => Set<PurchaseOrder>();

    public DbSet<PurchaseOrderItem> PurchaseOrderItems
        => Set<PurchaseOrderItem>();

    // ==========================================
    // Billing & Payments
    // ==========================================

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Expense> Expenses => Set<Expense>();


    // System Settings

    public DbSet<SystemSettings> SystemSettings { get; set; }


    // ==========================================
    // Entity Relationships & Constraints
    // ==========================================

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ------------------------------------------
        // Branch
        // Branch 1 ─── * Employee
        // Branch 1 ─── * Repair
        // Branch 1 ─── * Inventory
        // ------------------------------------------

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(x => x.BranchId);

            entity.Property(x => x.BranchCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.BranchName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Address)
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(x => x.Phone)
                .HasMaxLength(30);

            entity.Property(x => x.Email)
                .HasMaxLength(150);

            entity.HasIndex(x => x.BranchCode)
                .IsUnique();
        });


        // ------------------------------------------
        // Department
        // Department 1 ─── * Employee
        // ------------------------------------------

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(x => x.DepartmentId);

            entity.Property(x => x.DepartmentName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(300);
        });



        // ------------------------------------------
        // Employee
        // Employee * ─── 1 Branch
        // Employee * ─── 0..1 Department
        // Employee 1 ─── 0..1 User
        // ------------------------------------------

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(x => x.EmployeeId);

            entity.Property(x => x.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.MiddleName)
                .HasMaxLength(100);

            entity.Property(x => x.LastName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Phone)
                .HasMaxLength(30);

            entity.Property(x => x.Email)
                .HasMaxLength(150);

            entity.Property(x => x.Address)
                .HasMaxLength(300);

            entity.Property(x => x.Position)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasOne(x => x.Branch)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Department)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });


        // ------------------------------------------
        // Attendance
        // Attendance * ─── 1 Employee
        // ------------------------------------------

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(x => x.AttendanceId);

            entity.Property(x => x.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Remarks)
                .HasMaxLength(500);

            entity.HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ------------------------------------------
        // Payroll
        // Payroll * ─── 1 Employee
        // ------------------------------------------

        modelBuilder.Entity<Payroll>(entity =>
        {
            entity.HasKey(x => x.PayrollId);

            entity.Property(x => x.BasicSalary)
                .HasPrecision(18, 2);

            entity.Property(x => x.Deductions)
                .HasPrecision(18, 2);

            entity.Property(x => x.NetSalary)
                .HasPrecision(18, 2);

            entity.HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ------------------------------------------
        // Customer
        // Customer 1 ─── * Device
        // ------------------------------------------

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(x => x.CustomerId);

            entity.HasIndex(x => x.SyncId)
                .IsUnique();

            entity.Property(x => x.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.MiddleName)
                .HasMaxLength(100);

            entity.Property(x => x.LastName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Phone)
                .HasMaxLength(30);

            entity.Property(x => x.Email)
                .HasMaxLength(150);

            entity.Property(x => x.Address)
                .HasMaxLength(300);
        });


        // ------------------------------------------
        // Device
        // Device * ─── 1 Customer
        // Device 1 ─── * ServiceRequest
        // ------------------------------------------

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(x => x.DeviceId);

            entity.Property(x => x.DeviceType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Brand)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Model)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.SerialNumber)
                .HasMaxLength(150);

            entity.Property(x => x.DeviceCondition)
                .HasMaxLength(100);

            entity.HasOne(x => x.Customer)
                .WithMany(x => x.Devices)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.SerialNumber)
                .IsUnique()
                .HasFilter("[SerialNumber] IS NOT NULL");
        });


        // ------------------------------------------
        // Service Request
        // ServiceRequest * ─── 1 Device
        // ServiceRequest 1 ─── 0..1 Repair
        // ------------------------------------------

        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.HasKey(x => x.ServiceRequestId);

            entity.Property(x => x.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Priority)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasOne(x => x.Device)
                .WithMany(x => x.ServiceRequests)
                .HasForeignKey(x => x.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ------------------------------------------
        // Repair
        //
        // Repair * ─── 1 ServiceRequest
        // Repair * ─── 1 Technician (Employee)
        // Repair * ─── 1 Branch
        // Repair 1 ─── * RepairItem
        // Repair 1 ─── 0..1 Invoice
        // ------------------------------------------

        modelBuilder.Entity<Repair>(entity =>
        {
            entity.HasKey(x => x.RepairId);

            entity.Property(x => x.Diagnosis)
                .HasMaxLength(2000);

            entity.Property(x => x.RepairDescription)
                .HasMaxLength(2000);

            entity.Property(x => x.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasOne(x => x.ServiceRequest)
                .WithOne(x => x.Repair)
                .HasForeignKey<Repair>(x => x.ServiceRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Technician)
                .WithMany()
                .HasForeignKey(x => x.TechnicianId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Branch)
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ------------------------------------------
        // Repair Item
        // RepairItem * ─── 1 Repair
        // RepairItem * ─── 1 InventoryItem
        // ------------------------------------------

        modelBuilder.Entity<RepairItem>(entity =>
        {
            entity.HasKey(x => x.RepairItemId);

            entity.Property(x => x.Quantity)
                .HasPrecision(18, 2);

            entity.Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(x => x.Discount)
                .HasPrecision(18, 2);

            entity.HasOne(x => x.Repair)
                .WithMany(x => x.RepairItems)
                .HasForeignKey(x => x.RepairId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Item)
                .WithMany(x => x.RepairItems)
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ------------------------------------------
        // Inventory Item
        // InventoryItem 1 ─── * Inventory
        // InventoryItem 1 ─── * RepairItem
        // ------------------------------------------

        modelBuilder.Entity<InventoryItem>(entity =>
        {
            entity.HasKey(x => x.ItemId);

            entity.Property(x => x.ItemName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Category)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.Property(x => x.Brand)
                .HasMaxLength(100);

            entity.Property(x => x.Model)
                .HasMaxLength(100);

            entity.Property(x => x.Unit)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.UnitCost)
                .HasPrecision(18, 2);

            entity.Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            entity.Property(x => x.ReorderLevel)
                .HasPrecision(18, 2);

            entity.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ------------------------------------------
        // Inventory
        // Branch * ─── 1 Inventory
        // InventoryItem * ─── 1 Inventory
        //
        // One item can have only one inventory record
        // per branch.
        // ------------------------------------------

        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(x => x.InventoryId);

            entity.Property(x => x.QuantityOnHand)
                .HasPrecision(18, 2);

            entity.HasOne(x => x.Branch)
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Item)
                .WithMany(x => x.Inventories)
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new
            {
                x.BranchId,
                x.ItemId
            })
            .IsUnique();
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(x => x.SupplierId);

            entity.Property(x => x.SupplierCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.SupplierName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.ContactPerson)
                .HasMaxLength(150);

            entity.Property(x => x.Phone)
                .HasMaxLength(50);

            entity.Property(x => x.Email)
                .HasMaxLength(150);

            entity.Property(x => x.Address)
                .HasMaxLength(500);

            entity.HasIndex(x => x.SupplierCode)
                .IsUnique();
        });


        // ------------------------------------------
        // Invoice
        // Invoice * ─── 1 Repair
        // Invoice 1 ─── * Payment
        // ------------------------------------------

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(x => x.InvoiceId);

            entity.Property(x => x.InvoiceNumber)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Subtotal)
                .HasPrecision(18, 2);

            entity.Property(x => x.Discount)
                .HasPrecision(18, 2);

            entity.Property(x => x.Tax)
                .HasPrecision(18, 2);

            entity.Property(x => x.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(x => x.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(x => x.InvoiceNumber)
                .IsUnique();

            entity.HasOne(x => x.Repair)
                .WithOne(x => x.Invoice)
                .HasForeignKey<Invoice>(x => x.RepairId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ------------------------------------------
        // Payment
        // Payment * ─── 1 Invoice
        // ------------------------------------------

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(x => x.PaymentId);

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);

            entity.Property(x => x.PaymentMethod)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.ReferenceNumber)
                .HasMaxLength(100);

            entity.Property(x => x.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasOne(x => x.Invoice)
                .WithMany(x => x.Payments)
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ------------------------------------------
        // Expense
        // Expense * ─── 1 Branch
        // ------------------------------------------

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(x => x.ExpenseId);

            entity.Property(x => x.Category)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Amount)
                .HasPrecision(18, 2);

            entity.Property(x => x.Description)
                .HasMaxLength(500);

            entity.HasOne(x => x.Branch)
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SyncQueue>(entity =>
        {
            entity.HasKey(x => x.SyncId);

            entity.Property(x => x.TableName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(x => x.Operation)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.IsSynced,
                x.CreatedAt
            });
        });
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(x => x.PurchaseOrderId);

            entity.Property(x => x.PurchaseOrderNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.TotalAmount)
                .HasPrecision(18, 2);

            entity.Property(x => x.Notes)
                .HasMaxLength(500);

            entity.HasIndex(x => x.PurchaseOrderNumber)
                .IsUnique();

            entity.HasOne(x => x.Supplier)
                .WithMany()
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Branch)
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.HasKey(x => x.PurchaseOrderItemId);

            entity.Property(x => x.Quantity)
                .HasPrecision(18, 2);

            entity.Property(x => x.UnitCost)
                .HasPrecision(18, 2);

            entity.HasOne(x => x.PurchaseOrder)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Item)
                .WithMany()
                .HasForeignKey(x => x.ItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}