# FixFlowERP

> Computer Repair Shop Enterprise Resource Planning System

FixFlowERP is an Enterprise Resource Planning (ERP) system designed for computer repair shops. It provides a centralized platform for managing customers, devices, repair services, employees, inventory, invoices, payments, and other business operations.

This project is being developed as an academic project for the **Bachelor of Science in Information Technology** program.

---

## 📌 Overview

Computer repair shops often manage customers, repair requests, technicians, spare parts, inventory, payments, and employees using separate processes or manual records.

FixFlowERP aims to provide a centralized system that connects these operations into one platform.

The system follows a **multi-project architecture** and is designed to support **multiple companies and branches** through a Master Database and Tenant Database structure.

---

## 🎯 Objectives

The main objectives of FixFlowERP are to:

- Centralize computer repair shop operations
- Manage customers and their devices
- Manage service requests and repair jobs
- Manage employees, users, roles, and departments
- Track repair parts and inventory
- Manage stock-in and stock-out transactions
- Manage suppliers and purchasing
- Generate invoices
- Record customer payments
- Provide business reports
- Support multiple companies
- Support multiple branches
- Implement role-based access to system modules
- Maintain separation between company data and platform-level data

---

# 🏗️ System Architecture

FixFlowERP uses a multi-project and multi-tenant architecture.

```text
                         FixFlowERP
                              │
              ┌───────────────┴───────────────┐
              │                               │
        Master Database                  Tenant Database
              │                               │
        Platform Data                  Company Operations
              │                               │
        ┌─────┴─────┐                 ┌───────┴────────┐
        │           │                 │                │
     Companies   Modules          Branches         Employees
     Databases  Subscriptions     Departments      Users
     Company     CompanyModules    Customers        Roles
     Information                   Devices          Repairs
                                   Inventory        Invoices
                                   Payments         etc.
```

---

# 🗄️ Database Architecture

FixFlowERP separates platform-level information from company operational information.

## Master Database

The Master Database stores information required to manage the overall ERP platform.

### Main Master Database Entities

- Companies
- Company Databases
- Modules
- Company Modules
- Subscriptions
- Platform Users

The Master Database is responsible for identifying companies, their database information, available modules, and subscription information.

---

## Tenant Database

Each company has its own Tenant Database containing its operational data.

### Main Tenant Database Entities

- Branches
- Departments
- Employees
- Users
- Roles
- Customers
- Devices
- Service Requests
- Repairs
- Repair Items
- Inventory Items
- Inventory
- Invoices
- Payments
- Suppliers
- Stock In
- Stock Out

The Tenant Database is responsible for the day-to-day operations of a specific company.

---
# Computer Repair System – Database Design (Micro)

```mermaid
erDiagram
    Customer ||--o{ Device : "owns"
    Device ||--o{ ServiceRequest : "has"
    ServiceRequest ||--o| Repair : "becomes"
    Employee ||--o{ Repair : "technician"
    Repair ||--o| Invoice : "generates"
    Invoice ||--o{ Payment : "receives"
    Repair ||--o{ RepairItem : "uses"
    InventoryItem ||--o{ RepairItem : "used_in"
    InventoryItem ||--o{ Inventory : "stocked_as"

    Customer {
        int CustomerId PK
        uniqueidentifier SyncId
        nvarchar FirstName
        nvarchar MiddleName "nullable"
        nvarchar LastName
        nvarchar Phone "nullable"
        nvarchar Email "nullable"
        nvarchar Address "nullable"
        datetime CreatedAt
    }

    Device {
        int DeviceId PK
        int CustomerId FK
        nvarchar DeviceType
        nvarchar Brand
        nvarchar Model
        nvarchar SerialNumber "nullable"
        nvarchar DeviceCondition "nullable"
    }

    ServiceRequest {
        int ServiceRequestId PK
        int DeviceId FK
        datetime RequestDate
        nvarchar Description
        nvarchar Status
        nvarchar Priority
    }

    Employee {
        int EmployeeId PK
        int MasterUserId "nullable"
        nvarchar FirstName
        nvarchar MiddleName "nullable"
        nvarchar LastName
        nvarchar Phone "nullable"
        nvarchar Email "nullable"
        nvarchar Address "nullable"
        nvarchar Position
        date HireDate
        bit IsActive
    }

    Repair {
        int RepairId PK
        int ServiceRequestId FK
        int TechnicianId FK "nullable"
        int BranchId "nullable"
        nvarchar Diagnosis "nullable"
        nvarchar RepairDescription "nullable"
        nvarchar Status
        datetime StartDate "nullable"
        datetime EndDate "nullable"
    }

    Invoice {
        int InvoiceId PK
        int RepairId FK
        nvarchar InvoiceNumber
        datetime InvoiceDate
        decimal Subtotal
        decimal LaborAmount
        decimal Discount
        decimal Tax
        decimal TotalAmount
        nvarchar Status
    }

    Payment {
        int PaymentId PK
        int InvoiceId FK
        datetime PaymentDate
        decimal Amount
        nvarchar PaymentMethod
        nvarchar ReferenceNumber "nullable"
        nvarchar Status
    }

    InventoryItem {
        int ItemId PK
        nvarchar ItemName
        nvarchar Category
        nvarchar Description "nullable"
        nvarchar Brand "nullable"
        nvarchar Model "nullable"
        nvarchar Unit
        decimal UnitCost
        decimal UnitPrice
        decimal ReorderLevel
        bit IsActive
        datetime CreatedAt
    }

    Inventory {
        int InventoryId PK
        int BranchId "nullable"
        int ItemId FK
        decimal QuantityOnHand
    }

    RepairItem {
        int RepairItemId PK
        int RepairId FK
        int ItemId FK
        decimal Quantity
        decimal UnitPrice
        decimal Discount
    }

    SystemSettings {
        int SystemSettingsId PK
        nvarchar ShopName
        nvarchar ShopAddress "nullable"
        decimal LowLaborRate
        decimal MediumLaborRate
        decimal HighLaborRate
        datetime UpdatedAt
    }

    SyncQueue {
        int SyncId PK
        nvarchar TableName
        int RecordId
        nvarchar Operation
        datetime CreatedAt
        bit IsSynced
        datetime SyncedAt "nullable"
    }
```

# 🧩 Main System Modules

## 1. Dashboard

Provides an overview of the company's operations.

Possible dashboard information includes:

- Total customers
- Active repairs
- Pending service requests
- Completed repairs
- Available inventory
- Low-stock items
- Sales
- Payments
- Revenue summaries

---

## 2. User & Employee Management

Manages company employees and their system accounts.

### Functions

- Add employees
- Update employee information
- Assign departments
- Assign branches
- Create user accounts
- Assign roles
- Activate/deactivate users
- Manage employee status

### Example Roles

- Administrator
- Manager
- Receptionist
- Technician
- Cashier
- Finance Staff
- HR Staff
- Inventory Staff

---

## 3. Customer & Device Management

Manages customers and the devices they submit for repair.

### Customer Functions

- Register customers
- Update customer information
- Search customers
- View customer history

### Device Functions

- Register devices
- Assign devices to customers
- Record device type
- Record brand
- Record model
- Record serial number
- Record device condition

### Relationship

```text
Customer
   │
   └── Device
          │
          └── Service Request
```

A customer can own multiple devices, while each device belongs to one customer.

---

## 4. Repair Management

Manages the complete repair process.

### Repair Workflow

```text
Customer
    │
    ▼
Device
    │
    ▼
Service Request
    │
    ▼
Repair
    │
    ├───────────────┐
    ▼               ▼
Repair Items    Technician
    │
    ▼
Inventory Item
    │
    ▼
Invoice
    │
    ▼
Payment
```

### Functions

- Create service requests
- Assign technicians
- Record diagnosis
- Record repair details
- Track repair status
- Add repair parts
- Track repair dates
- Complete repair jobs
- Generate invoices

---

## 5. Inventory & Supplier Management

Manages spare parts, supplies, and stock.

### Inventory Functions

- Add inventory items
- Update item information
- Monitor available stock
- Set reorder levels
- Monitor low-stock items
- Track stock movement
- Record stock-in transactions
- Record stock-out transactions

### Supplier Functions

- Register suppliers
- Update supplier information
- Manage supplier records
- Track purchases

### Inventory Structure

```text
Inventory Item
      │
      ├── Stock In
      │
      ├── Stock Out
      │
      └── Branch Inventory
```

Inventory items represent the item catalog, while branch inventory stores the current quantity available at a particular branch.

---

## 6. Sales & Payment Management

Manages invoices and customer payments.

### Functions

- Generate invoices
- Calculate invoice totals
- Apply discounts
- Record taxes
- Record payments
- Track payment status
- Record payment methods
- Store payment reference numbers

### Billing Workflow

```text
Repair
  │
  ▼
Invoice
  │
  ▼
Payment
```

A repair may have one invoice, while an invoice may have multiple payment records.

---

## 7. Reports & Settings

Provides business reports and system configuration.

### Reports

Possible reports include:

- Repair reports
- Sales reports
- Payment reports
- Inventory reports
- Stock movement reports
- Customer reports
- Employee reports
- Technician performance reports
- Revenue reports

### Settings

System settings may include:

- Company information
- Contact information
- System name
- System logo
- Notification settings
- Default system settings
- Other company-specific configurations

---

# 👥 User Roles

FixFlowERP supports role-based access to system functionality.

| Role | Main Responsibility |
|------|---------------------|
| Administrator | Manages the overall company system |
| Manager | Oversees company operations and reports |
| Receptionist | Handles customers, devices, and service requests |
| Technician | Handles diagnosis and repair jobs |
| Cashier | Handles invoices and customer payments |
| Finance Staff | Handles financial records and reports |
| HR Staff | Handles employee and HR-related records |
| Inventory Staff | Handles inventory and stock transactions |

Access to modules can be controlled depending on the assigned role.

---

# 🔗 Core Entity Relationships

The major operational relationships are structured as follows:

```text
Branch
  │
  ├── Employees
  │
  └── Inventory
         │
         └── Inventory Item


Department
  │
  └── Employees


Role
  │
  └── Users


Employee
  │
  └── User Account


Customer
  │
  └── Devices
         │
         └── Service Requests
                │
                └── Repair
                      │
                      ├── Technician
                      │
                      ├── Repair Items
                      │      │
                      │      └── Inventory Item
                      │
                      └── Invoice
                             │
                             └── Payments
```

---

# 🏢 Multi-Company Architecture

FixFlowERP is designed to support multiple companies.

```text
                    Master Database
                          │
             ┌────────────┼────────────┐
             │            │            │
          Company A    Company B    Company C
             │            │            │
          Tenant DB    Tenant DB    Tenant DB
             │            │            │
          Branches     Branches     Branches
          Employees    Employees    Employees
          Customers    Customers    Customers
          Repairs      Repairs      Repairs
          Inventory    Inventory    Inventory
```

Each company's operational data remains inside its own Tenant Database.

This provides logical separation between different companies.

---

# 🏢 Branch Structure

Branches are stored inside the Tenant Database because branches belong to a specific company.

```text
Company
   │
   ├── Branch 1
   │     ├── Employees
   │     ├── Customers
   │     ├── Repairs
   │     └── Inventory
   │
   ├── Branch 2
   │     ├── Employees
   │     ├── Customers
   │     ├── Repairs
   │     └── Inventory
   │
   └── Branch 3
         ├── Employees
         ├── Customers
         ├── Repairs
         └── Inventory
```

---

# 🔐 Authentication & Authorization

The system separates platform-level authentication from company-level users.

## Master Users

Master users are responsible for platform-level administration.

Examples:

- Super Administrator
- Platform Administrator

These users belong to the Master Database.

## Tenant Users

Tenant users are employees of a company and belong to the Tenant Database.

Examples:

- Administrator
- Technician
- Receptionist
- Cashier
- Finance Staff
- HR Staff
- Inventory Staff

Tenant users are assigned roles that determine which parts of the system they can access.

---

# 🛠️ Technologies

## Backend

- C#
- .NET
- ASP.NET Core
- Entity Framework Core

## Database

- Microsoft SQL Server

## Desktop Application

- Windows Forms

## Development Tools

- Visual Studio
- Visual Studio Code
- Git
- GitHub

---

# 📁 Project Structure

```text
ComputerRepairSystem/
│
├── .gitignore
├── README.md
├── ComputerRepairSystem.sln
│
├── ComputerRepairSystem.api/
│   ├── Controllers/
│   ├── Program.cs
│   ├── appsettings.json
│   └── ComputerRepairSystem.api.csproj
│
├── ComputerRepairSystem.company/
│   ├── Data/
│   │   └── TenantDbContext.cs
│   │
│   ├── Entities/
│   │   ├── Branch.cs
│   │   ├── Department.cs
│   │   ├── Employee.cs
│   │   ├── User.cs
│   │   ├── Role.cs
│   │   ├── Customer.cs
│   │   ├── Device.cs
│   │   ├── ServiceRequest.cs
│   │   ├── Repair.cs
│   │   ├── RepairItem.cs
│   │   ├── InventoryItem.cs
│   │   ├── Inventory.cs
│   │   ├── Invoice.cs
│   │   └── Payment.cs
│   │
│   ├── Migrations/
│   └── ComputerRepairSystem.company.csproj
│
├── ComputerRepairSystem.infrastructure/
│   ├── Data/
│   │   ├── MasterErpDbContext.cs
│   │   └── ...
│   │
│   ├── Migrations/
│   └── ComputerRepairSystem.infrastructure.csproj
│
└── ComputerRepairSystem.winforms/
    ├── Forms/
    ├── Pages/
    └── ComputerRepairSystem.winforms.csproj
```

---

# 🗃️ Entity Overview

## Master Database

```text
Companies
    │
    └── CompanyDatabases

Modules
    │
    └── CompanyModules

Subscriptions
```

---

## Tenant Database

```text
Branches
Departments
Employees
Users
Roles

Customers
Devices
ServiceRequests
Repairs
RepairItems

InventoryItems
Inventory

Invoices
Payments

Suppliers
StockIns
StockInItems
StockOuts
StockOutItems
```

---

# 📦 Inventory Transactions

Inventory uses transaction records to maintain stock movement.

## Stock In

Stock In represents inventory entering the company or branch.

Examples:

- Supplier purchases
- Received stock
- Returned items
- Inventory adjustments

```text
Stock In
   │
   └── Stock In Items
          │
          └── Inventory Item
```

## Stock Out

Stock Out represents inventory leaving the company's stock.

Examples:

- Parts used for repairs
- Items sold
- Damaged items
- Lost items
- Inventory adjustments

```text
Stock Out
   │
   └── Stock Out Items
          │
          └── Inventory Item
```

---

# 🧱 Database Design Principles

The database is designed with the following principles:

- Avoid unnecessary data duplication
- Maintain referential integrity
- Use primary keys and foreign keys
- Use appropriate relationship constraints
- Preserve historical transaction values
- Separate master data from transaction data
- Separate platform data from company data
- Support multiple branches
- Support role-based access

---

# 🔄 Core Business Process

The primary computer repair workflow is:

```text
Customer Registration
        │
        ▼
Device Registration
        │
        ▼
Service Request
        │
        ▼
Technician Assignment
        │
        ▼
Diagnosis
        │
        ▼
Repair
        │
        ├───────────────┐
        ▼               ▼
Repair Parts       Repair Details
        │               │
        └───────┬───────┘
                ▼
          Repair Completed
                │
                ▼
             Invoice
                │
                ▼
             Payment
                │
                ▼
          Release Device
```

---

# 📊 Development Status

The project is currently under active development.

## Architecture

- [x] Solution structure
- [x] Master database architecture
- [x] Tenant database architecture
- [x] Multi-company architecture
- [x] Branch architecture
- [x] Project separation
- [ ] Complete service layer
- [ ] Complete repository layer
- [ ] Complete API architecture

## Database

- [x] Company entity
- [x] Company database entity
- [x] Branch entity
- [x] Department entity
- [x] Employee entity
- [x] User entity
- [x] Role entity
- [x] Customer entity
- [x] Device entity
- [x] Service request entity
- [x] Repair entity
- [x] Repair item entity
- [x] Inventory item entity
- [x] Inventory entity
- [x] Invoice entity
- [x] Payment entity
- [ ] Supplier entities
- [ ] Stock-in entities
- [ ] Stock-out entities
- [ ] Complete database relationships
- [ ] Complete EF Core configurations

## Authentication

- [ ] Master authentication
- [ ] Tenant authentication
- [ ] Password hashing
- [ ] Role-based authorization
- [ ] Permission management
- [ ] Login/logout functionality

## Modules

- [ ] Dashboard
- [ ] User & Employee Management
- [ ] Customer & Device Management
- [ ] Repair Management
- [ ] Inventory & Supplier Management
- [ ] Sales & Payment Management
- [ ] Reports & Settings

## Testing

- [ ] Unit testing
- [ ] Integration testing
- [ ] Database testing
- [ ] Authentication testing
- [ ] Authorization testing
- [ ] User acceptance testing

## Deployment

- [ ] Production database
- [ ] API deployment
- [ ] Desktop application deployment
- [ ] Production configuration
- [ ] Backup strategy
- [ ] Monitoring

---

# 🚀 Getting Started

## Prerequisites

Before running the project, install:

- .NET SDK
- Microsoft SQL Server
- Visual Studio or Visual Studio Code
- Git

---

## 1. Clone the Repository

```bash
git clone <repository-url>
cd ComputerRepairSystem
```

---

## 2. Restore Dependencies

```bash
dotnet restore
```

---

## 3. Build the Solution

```bash
dotnet build
```

---

## 4. Configure the Database

Configure the database connection strings locally.

Example:

```json
{
  "ConnectionStrings": {
    "MasterErp": "",
    "TenantErp": ""
  }
}
```

Do not commit real database passwords, API keys, or other secrets to Git.

---

## 5. Apply Entity Framework Core Migrations

Apply the migrations for the appropriate DbContext.

For example:

```bash
dotnet ef database update
```

If multiple DbContexts are present, specify the appropriate context:

```bash
dotnet ef database update --context MasterErpDbContext
```

or:

```bash
dotnet ef database update --context TenantDbContext
```

---

## 6. Run the API

```bash
dotnet run --project ComputerRepairSystem.api
```

---

## 7. Run the Windows Forms Application

Run:

```text
ComputerRepairSystem.winforms
```

from Visual Studio.

---

# 🔧 Development Workflow

A typical development workflow is:

```text
Create / Modify Entity
        │
        ▼
Configure EF Core Relationship
        │
        ▼
Create Migration
        │
        ▼
Update Database
        │
        ▼
Create API / Service Logic
        │
        ▼
Create UI
        │
        ▼
Test
        │
        ▼
Commit Changes
```

---

# 🌿 Git Workflow

The project uses Git for version control.

## Recommended Branch Structure

```text
main
 │
 ├── develop
 │
 ├── feature/customer-management
 │
 ├── feature/repair-management
 │
 ├── feature/inventory-management
 │
 └── feature/payment-management
```

## Create a Feature Branch

```bash
git checkout -b feature/customer-management
```

## Commit Changes

```bash
git add .
git commit -m "Add customer management"
```

## Push the Branch

```bash
git push origin feature/customer-management
```

---

# 🔑 GitHub SSH Setup

The repository uses **SSH authentication** for GitHub instead of HTTPS.

The SSH remote should look like:

```text
git@github.com:USERNAME/ComputerRepairSystem.git
```

Check the current remote:

```bash
git remote -v
```

If the repository is still using HTTPS, change it to SSH:

```bash
git remote set-url origin git@github.com:USERNAME/ComputerRepairSystem.git
```

Verify the remote:

```bash
git remote -v
```

Test the GitHub SSH connection:

```bash
ssh -T git@github.com
```

After SSH authentication is configured, Git operations such as pushing and pulling can be performed without entering a GitHub username and password each time.

---

# 🔒 Configuration & Secrets

Sensitive configuration files should not be committed to Git.

Examples of files that should normally remain local:

```text
appsettings.json
appsettings.Development.json
.env
```

A template configuration file can be provided:

```text
appsettings.example.json
```

The example file should contain placeholders instead of real credentials.

### Example

```json
{
  "ConnectionStrings": {
    "MasterErp": "",
    "TenantErp": ""
  }
}
```

Never commit:

- Database passwords
- API keys
- Access tokens
- Private keys
- Production credentials
- Other sensitive configuration values

---

# 🧪 Testing Strategy

Testing will cover different parts of the system.

## Unit Testing

Tests individual business logic and services.

## Integration Testing

Tests communication between:

- API
- Database
- Services
- Authentication

## Database Testing

Tests:

- Primary keys
- Foreign keys
- Constraints
- Relationships
- Transactions
- Data integrity

## User Acceptance Testing

Tests whether the system satisfies the requirements of the intended users.

---

# 📈 Future Improvements

Possible future enhancements include:

- Advanced role and permission management
- Automated notifications
- SMS notifications
- Email notifications
- Online customer portal
- Customer repair tracking
- Advanced inventory forecasting
- Barcode scanning
- Purchase order management
- Advanced financial reports
- Dashboard analytics
- Audit logs
- Automated database backups
- Cloud deployment
- Mobile application
- Real-time repair status updates

---

# ⚠️ Project Status

> **Development Version**

FixFlowERP is currently an academic project and is not intended for production use yet.

Features, database structures, business rules, and system architecture may change during development.

---

# 👩‍💻 Development Team

Developed as an academic project for the:

**Bachelor of Science in Information Technology**

The project focuses on applying concepts in:

- Software Engineering
- Database Management
- System Integration
- Enterprise Systems
- Object-Oriented Programming
- Web/API Development
- Desktop Application Development

---

# 📄 License

This project is developed for academic and educational purposes.

All rights reserved unless otherwise specified by the project authors.

---

# ⭐ FixFlowERP

**Computer Repair Shop Enterprise Resource Planning System**

> Manage. Repair. Track. Grow.

---

# System Evaluation Results
<img width="1152" height="2048" alt="eval1" src="https://github.com/user-attachments/assets/162f59ba-ef8b-44a9-88f2-d1ccb5ba1c1c" />
<img width="1152" height="2048" alt="eval1c" src="https://github.com/user-attachments/assets/4cf25c51-b5ca-453b-a813-9f673d7f3f6c" />


