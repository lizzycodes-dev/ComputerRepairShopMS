using ComputerRepairSystem.company.Context;
using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Entities;
using ComputerRepairSystem.infrastructure.data;
using ComputerRepairSystem_winui.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComputerRepairSystem_winui.Pages;

public sealed partial class PayrollManagementPage : Page
{
    private readonly TenantDbContextFactory _tenantDbFactory;
    private readonly MasterErpDbContext _masterDb;
    private readonly CurrentBranchContext _currentBranchContext;

    private List<PayrollRow> _filteredPayrolls = new();
    private int _currentPage = 1;
    private const int _pageSize = 10;
    public PayrollManagementPage(
        TenantDbContextFactory tenantDbFactory,
        MasterErpDbContext masterDb,
        CurrentBranchContext currentBranchContext)
    {
        InitializeComponent();

        _tenantDbFactory = tenantDbFactory;

        _masterDb = masterDb;

        _currentBranchContext = currentBranchContext;

        Loaded += PayrollManagementPage_Loaded;
        Unloaded += PayrollManagementPage_Unloaded;
    }


    // ==========================================
    // BRANCH MANAGEMENT
    // ==========================================

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


    // ==========================================
    // PAGE LOADED / UNLOADED
    // ==========================================

    private async void PayrollManagementPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        _currentBranchContext.BranchChanged +=
            OnGlobalBranchChanged;

        await LoadPayrollsAsync();
    }


    private void PayrollManagementPage_Unloaded(
        object sender,
        RoutedEventArgs e)
    {
        _currentBranchContext.BranchChanged -=
            OnGlobalBranchChanged;
    }


    private async void OnGlobalBranchChanged()
    {
        if (!IsLoaded)
            return;

        PayrollSearchBox.Text = string.Empty;

        await LoadPayrollsAsync();
    }


    // ==========================================
    // LOAD PAYROLLS
    // ==========================================
    private void UpdatePayrollSummary(
        List<Payroll> payrolls)
    {
        PayrollEmployeeCountText.Text =
            payrolls.Count.ToString();

        var totalBasicSalary =
            payrolls.Sum(x => x.BasicSalary);

        var totalDeductions =
            payrolls.Sum(x => x.Deductions);

        var totalNetSalary =
            payrolls.Sum(x => x.NetSalary);

        PayrollBasicSalaryText.Text =
            $"₱{totalBasicSalary:N2}";

        PayrollDeductionsText.Text =
            $"₱{totalDeductions:N2}";

        PayrollNetSalaryText.Text =
            $"₱{totalNetSalary:N2}";
    }
    private async Task LoadPayrollsAsync()
    {
        try
        {
            if (CurrentUser.CompanyId == null)
            {
                PayrollList.ItemsSource = null;
                return;
            }

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var branchManagementEnabled =
                await HasBranchManagementAsync();

            int? currentBranchId = null;

            if (branchManagementEnabled)
            {
                currentBranchId =
                    _currentBranchContext.BranchId;

                if (currentBranchId == null)
                {
                    PayrollList.ItemsSource = null;

                    PayrollEmployeeCountText.Text = "0";
                    PayrollBasicSalaryText.Text = "₱0.00";
                    PayrollDeductionsText.Text = "₱0.00";
                    PayrollNetSalaryText.Text = "₱0.00";

                    await ShowMessageAsync(
                        "Branch Not Selected",
                        "Please select a branch from the global branch selector.");

                    return;
                }
            }

            var query =
                db.Payrolls
                    .AsNoTracking()
                    .Include(x => x.Employee)
                    .AsQueryable();

            if (branchManagementEnabled)
            {
                query = query.Where(x =>
                    x.Employee!.BranchId ==
                    currentBranchId!.Value);
            }

            var payrolls =
                await query
                    .OrderByDescending(x => x.PayPeriodEnd)
                    .ThenBy(x => x.Employee!.LastName)
                    .ToListAsync();

            var rows =
                payrolls
                    .Select(x => new PayrollRow
                    {
                        PayrollId = x.PayrollId,

                        EmployeeName =
                            x.Employee == null
                                ? "Unknown Employee"
                                : $"{x.Employee.FirstName} {x.Employee.LastName}",

                        PayPeriodStartText =
                            x.PayPeriodStart.ToString("MM/dd/yyyy"),

                        PayPeriodEndText =
                            x.PayPeriodEnd.ToString("MM/dd/yyyy"),

                        BasicSalaryText =
                            $"₱{x.BasicSalary:N2}",

                        DeductionsText =
                            $"₱{x.Deductions:N2}",

                        NetSalaryText =
                            $"₱{x.NetSalary:N2}",

                        EmployeeId = x.EmployeeId,

                        PayPeriodStart = x.PayPeriodStart,

                        PayPeriodEnd = x.PayPeriodEnd,

                        BasicSalary = x.BasicSalary,

                        Deductions = x.Deductions
                    })
                    .ToList();

            _filteredPayrolls = rows;

            _currentPage = 1;

            UpdatePagination();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Loading Payroll",
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
                    (double)_filteredPayrolls.Count /
                    _pageSize));

        if (_currentPage > totalPages)
            _currentPage = totalPages;

        if (_currentPage < 1)
            _currentPage = 1;

        var pagedRows =
            _filteredPayrolls
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

        PayrollList.ItemsSource = pagedRows;

        PageInfoText.Text =
            $"Page {_currentPage} of {totalPages}  •  " +
            $"{_filteredPayrolls.Count} total";

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
                    (double)_filteredPayrolls.Count /
                    _pageSize));

        if (_currentPage >= totalPages)
            return;

        _currentPage++;

        UpdatePagination();
    }


    // ==========================================
    // SEARCH
    // ==========================================

    private async void PayrollSearchBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (CurrentUser.CompanyId == null)
        {
            PayrollList.ItemsSource = null;
            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var branchManagementEnabled =
                await HasBranchManagementAsync();

            int? currentBranchId = null;

            if (branchManagementEnabled)
            {
                currentBranchId =
                    _currentBranchContext.BranchId;

                if (currentBranchId == null)
                {
                    PayrollList.ItemsSource = null;
                    return;
                }
            }

            var query =
                db.Payrolls
                    .AsNoTracking()
                    .Include(x => x.Employee)
                    .AsQueryable();

            if (branchManagementEnabled)
            {
                query = query.Where(x =>
                    x.Employee!.BranchId ==
                    currentBranchId!.Value);
            }

            var payrolls =
                await query
                    .OrderByDescending(x => x.PayPeriodEnd)
                    .ThenBy(x => x.Employee!.LastName)
                    .ToListAsync();

            var search =
                PayrollSearchBox.Text.Trim();

            if (!string.IsNullOrWhiteSpace(search))
            {
                payrolls =
                    payrolls
                        .Where(x =>
                            x.Employee != null &&
                            $"{x.Employee.FirstName} {x.Employee.LastName}"
                                .Contains(
                                    search,
                                    StringComparison.OrdinalIgnoreCase))
                        .ToList();
            }

            var rows =
                payrolls
                    .Select(x => new PayrollRow
                    {
                        PayrollId = x.PayrollId,

                        EmployeeName =
                            x.Employee == null
                                ? "Unknown Employee"
                                : $"{x.Employee.FirstName} {x.Employee.LastName}",

                        PayPeriodStartText =
                            x.PayPeriodStart.ToString("MM/dd/yyyy"),

                        PayPeriodEndText =
                            x.PayPeriodEnd.ToString("MM/dd/yyyy"),

                        BasicSalaryText =
                            $"₱{x.BasicSalary:N2}",

                        DeductionsText =
                            $"₱{x.Deductions:N2}",

                        NetSalaryText =
                            $"₱{x.NetSalary:N2}",

                        EmployeeId = x.EmployeeId,

                        PayPeriodStart = x.PayPeriodStart,

                        PayPeriodEnd = x.PayPeriodEnd,

                        BasicSalary = x.BasicSalary,

                        Deductions = x.Deductions
                    })
                    .ToList();

            PayrollList.ItemsSource = rows;
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Search Error",
                ex.Message);
        }
    }


    // ==========================================
    // ADD PAYROLL
    // ==========================================

    private async void AddPayrollButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (CurrentUser.CompanyId == null)
        {
            await ShowMessageAsync(
                "Validation Error",
                "Unable to determine the current company.");

            return;
        }

        try
        {
            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var branchManagementEnabled =
                await HasBranchManagementAsync();

            int? currentBranchId = null;

            if (branchManagementEnabled)
            {
                currentBranchId =
                    _currentBranchContext.BranchId;

                if (currentBranchId == null)
                {
                    await ShowMessageAsync(
                        "Branch Not Selected",
                        "Please select a branch from the global branch selector.");

                    return;
                }
            }


            // ==========================================
            // EMPLOYEES (branch-filtered if enabled)
            // ==========================================

            var employeesQuery =
                db.Employees
                    .AsNoTracking()
                    .Where(x => x.IsActive);

            if (branchManagementEnabled)
            {
                employeesQuery =
                    employeesQuery.Where(x =>
                        x.BranchId ==
                        currentBranchId!.Value);
            }

            var employees =
                await employeesQuery
                    .OrderBy(x => x.LastName)
                    .ThenBy(x => x.FirstName)
                    .ToListAsync();

            if (employees.Count == 0)
            {
                await ShowMessageAsync(
                    "No Employees",
                    branchManagementEnabled
                        ? "There are no active employees in the selected branch."
                        : "There are no active employees available for payroll.");

                return;
            }


            var employeeBox =
                new ComboBox
                {
                    Header = "Employee",
                    PlaceholderText = "Select employee",
                    ItemsSource = employees,
                    DisplayMemberPath = "LastName",
                    MinWidth = 320
                };


            var startDatePicker =
                new CalendarDatePicker
                {
                    Header = "Pay Period Start",
                    Date = DateTimeOffset.Now
                };


            var endDatePicker =
                new CalendarDatePicker
                {
                    Header = "Pay Period End",
                    Date = DateTimeOffset.Now
                };


            var basicSalaryBox =
                new NumberBox
                {
                    Header = "Basic Salary",
                    PlaceholderText = "Enter basic salary",
                    Minimum = 0,
                    SpinButtonPlacementMode =
                        NumberBoxSpinButtonPlacementMode.Compact
                };


            var otherDeductionsBox =
                new NumberBox
                {
                    Header = "Other Deductions",
                    PlaceholderText = "Enter other deductions",
                    Minimum = 0,
                    Value = 0,
                    SpinButtonPlacementMode =
                        NumberBoxSpinButtonPlacementMode.Compact
                };

            var attendanceSummaryText =
                new TextBlock
                {
                    Text =
                        "Attendance: Select an employee and pay period.",
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.8
                };


            var attendanceDeductionText =
                new TextBlock
                {
                    Text =
                        "Attendance Deduction: ₱0.00",
                    FontWeight =
                        Microsoft.UI.Text.FontWeights.SemiBold
                };
            var netSalaryText =
                new TextBlock
                {
                    Text = "Net Salary: ₱0.00",
                    FontSize = 18,
                    FontWeight =
                        Microsoft.UI.Text.FontWeights.SemiBold
                };


            decimal attendanceDeduction = 0;


            void UpdateNetSalary()
            {
                var basic =
                    double.IsNaN(basicSalaryBox.Value)
                        ? 0
                        : basicSalaryBox.Value;

                var otherDeductions =
                    double.IsNaN(otherDeductionsBox.Value)
                        ? 0
                        : otherDeductionsBox.Value;

                var totalDeductions =
                    (decimal)otherDeductions +
                    attendanceDeduction;

                var net =
                    Math.Max(
                        0,
                        (decimal)basic -
                        totalDeductions);

                netSalaryText.Text =
                    $"Net Salary: ₱{net:N2}";
            }
            async Task RefreshAttendancePreviewAsync()
            {
                if (employeeBox.SelectedItem
                    is not Employee selectedEmployee)
                {
                    attendanceDeduction = 0;

                    attendanceSummaryText.Text =
                        "Attendance: Select an employee and pay period.";

                    attendanceDeductionText.Text =
                        "Attendance Deduction: ₱0.00";

                    UpdateNetSalary();

                    return;
                }


                if (!startDatePicker.Date.HasValue ||
                    !endDatePicker.Date.HasValue)
                {
                    attendanceDeduction = 0;

                    attendanceSummaryText.Text =
                        "Attendance: Select both pay period dates.";

                    attendanceDeductionText.Text =
                        "Attendance Deduction: ₱0.00";

                    UpdateNetSalary();

                    return;
                }


                var startDate =
                    startDatePicker.Date.Value.DateTime.Date;

                var endDate =
                    endDatePicker.Date.Value.DateTime.Date;


                if (endDate < startDate)
                {
                    attendanceDeduction = 0;

                    attendanceSummaryText.Text =
                        "Attendance: Invalid pay period.";

                    attendanceDeductionText.Text =
                        "Attendance Deduction: ₱0.00";

                    UpdateNetSalary();

                    return;
                }


                var attendanceRecords =
                    await db.Attendances
                        .AsNoTracking()
                        .Where(x =>
                            x.EmployeeId ==
                                selectedEmployee.EmployeeId
                            &&
                            x.AttendanceDate.Date >=
                                startDate
                            &&
                            x.AttendanceDate.Date <=
                                endDate)
                        .ToListAsync();


                var presentCount =
                    attendanceRecords.Count(x =>
                        string.Equals(
                            x.Status,
                            "Present",
                            StringComparison.OrdinalIgnoreCase));


                var lateCount =
                    attendanceRecords.Count(x =>
                        string.Equals(
                            x.Status,
                            "Late",
                            StringComparison.OrdinalIgnoreCase));


                var absentCount =
                    attendanceRecords.Count(x =>
                        string.Equals(
                            x.Status,
                            "Absent",
                            StringComparison.OrdinalIgnoreCase));


                var leaveCount =
                    attendanceRecords.Count(x =>
                        string.Equals(
                            x.Status,
                            "Leave",
                            StringComparison.OrdinalIgnoreCase));


                var weekdays =
                    Enumerable
                        .Range(
                            0,
                            (endDate - startDate).Days + 1)
                        .Select(days => startDate.AddDays(days))
                        .Count(x =>
                            x.DayOfWeek != DayOfWeek.Saturday &&
                            x.DayOfWeek != DayOfWeek.Sunday);


                var basicSalary =
                    double.IsNaN(basicSalaryBox.Value)
                        ? 0
                        : basicSalaryBox.Value;


                decimal dailyRate = 0;


                if (weekdays > 0 &&
                    basicSalary > 0)
                {
                    dailyRate =
                        (decimal)basicSalary /
                        weekdays;
                }


                attendanceDeduction =
                    dailyRate *
                    absentCount;


                attendanceSummaryText.Text =
                    $"Attendance: " +
                    $"Present {presentCount}  •  " +
                    $"Late {lateCount}  •  " +
                    $"Absent {absentCount}  •  " +
                    $"Leave {leaveCount}";


                attendanceDeductionText.Text =
                    $"Attendance Deduction: " +
                    $"₱{attendanceDeduction:N2}";


                UpdateNetSalary();
            }

            basicSalaryBox.ValueChanged +=
                async (_, _) =>
                {
                    await RefreshAttendancePreviewAsync();
                };


            otherDeductionsBox.ValueChanged +=
                (_, _) =>
                {
                    UpdateNetSalary();
                };


            employeeBox.SelectionChanged +=
                async (_, _) =>
                {
                    await RefreshAttendancePreviewAsync();
                };


            startDatePicker.DateChanged +=
                async (_, _) =>
                {
                    await RefreshAttendancePreviewAsync();
                };


            endDatePicker.DateChanged +=
                async (_, _) =>
                {
                    await RefreshAttendancePreviewAsync();
                };


            var panel =
                new StackPanel
                {
                    Spacing = 12
                };

            panel.Children.Add(employeeBox);
            panel.Children.Add(startDatePicker);
            panel.Children.Add(endDatePicker);
            panel.Children.Add(basicSalaryBox);

            panel.Children.Add(
                attendanceSummaryText);

            panel.Children.Add(
                attendanceDeductionText);

            panel.Children.Add(
                otherDeductionsBox);

            panel.Children.Add(
                netSalaryText);


            var dialog =
                new ContentDialog
                {
                    Title = "Add Payroll",
                    Content = panel,

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

            if (employeeBox.SelectedItem
                is not Employee selectedEmployee)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select an employee.");

                return;
            }

            // Branch safety: employee must belong to selected branch
            if (branchManagementEnabled &&
                selectedEmployee.BranchId != currentBranchId)
            {
                await ShowMessageAsync(
                    "Access Denied",
                    "The selected employee does not belong to the currently selected branch.");

                return;
            }


            if (!startDatePicker.Date.HasValue ||
                !endDatePicker.Date.HasValue)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select both pay period dates.");

                return;
            }


            var payPeriodStart =
                startDatePicker.Date.Value.DateTime.Date;

            var payPeriodEnd =
                endDatePicker.Date.Value.DateTime.Date;


            if (payPeriodEnd < payPeriodStart)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Pay period end cannot be earlier than the start date.");

                return;
            }


            var basicSalary =
                (decimal)(
                    double.IsNaN(basicSalaryBox.Value)
                        ? 0
                        : basicSalaryBox.Value);


            var otherDeductions =
                (decimal)(
                    double.IsNaN(otherDeductionsBox.Value)
                        ? 0
                        : otherDeductionsBox.Value);


            var deductions =
                otherDeductions +
                attendanceDeduction;


            if (basicSalary <= 0)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Basic salary must be greater than zero.");

                return;
            }


            if (deductions < 0)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Deductions cannot be negative.");

                return;
            }


            if (deductions > basicSalary)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Deductions cannot be greater than the basic salary.");

                return;
            }


            // ==========================================
            // CHECK DUPLICATE PAYROLL
            // ==========================================

            var duplicate =
                await db.Payrolls
                    .AnyAsync(x =>
                        x.EmployeeId ==
                            selectedEmployee.EmployeeId &&

                        x.PayPeriodStart ==
                            payPeriodStart &&

                        x.PayPeriodEnd ==
                            payPeriodEnd);

            if (duplicate)
            {
                await ShowMessageAsync(
                    "Duplicate Payroll",
                    "A payroll record already exists for this employee and pay period.");

                return;
            }


            // ==========================================
            // CREATE PAYROLL
            // ==========================================

            var payroll =
                new Payroll
                {
                    EmployeeId =
                        selectedEmployee.EmployeeId,

                    PayPeriodStart =
                        payPeriodStart,

                    PayPeriodEnd =
                        payPeriodEnd,

                    BasicSalary =
                        basicSalary,

                    Deductions =
                        deductions,

                    NetSalary =
                        basicSalary - deductions
                };

            db.Payrolls.Add(payroll);

            await db.SaveChangesAsync();


            // ==========================================
            // CREATE FINANCE PAYROLL EXPENSE
            // ==========================================

            var payrollExpense =
                new Expense
                {
                    PayrollId =
                        payroll.PayrollId,

                    Category = "Payroll",

                    Amount =
                        payroll.NetSalary,

                    ExpenseDate =
                        payroll.PayPeriodEnd,

                    BranchId =
                        selectedEmployee.BranchId,

                    Description =
                        $"Payroll for {selectedEmployee.FirstName} " +
                        $"{selectedEmployee.LastName} " +
                        $"({payPeriodStart:MM/dd/yyyy} - " +
                        $"{payPeriodEnd:MM/dd/yyyy})"
                };

            db.Expenses.Add(payrollExpense);

            await db.SaveChangesAsync();


            await LoadPayrollsAsync();


            await ShowMessageAsync(
                "Payroll Added",
                "The payroll record was added successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Adding Payroll",
                ex.Message);
        }
    }


    // ==========================================
    // EDIT PAYROLL
    // ==========================================

    private async void EditPayrollButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not PayrollRow row)
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

            var branchManagementEnabled =
                await HasBranchManagementAsync();

            int? currentBranchId = null;

            if (branchManagementEnabled)
            {
                currentBranchId =
                    _currentBranchContext.BranchId;

                if (currentBranchId == null)
                {
                    await ShowMessageAsync(
                        "Branch Not Selected",
                        "Please select a branch from the global branch selector.");

                    return;
                }
            }


            var payroll =
                await db.Payrolls
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(
                        x => x.PayrollId == row.PayrollId);

            if (payroll == null)
            {
                await ShowMessageAsync(
                    "Not Found",
                    "The payroll record could not be found.");

                return;
            }


            // ==========================================
            // BRANCH SAFETY CHECK
            // ==========================================

            if (branchManagementEnabled)
            {
                if (payroll.Employee == null ||
                    payroll.Employee.BranchId != currentBranchId)
                {
                    await ShowMessageAsync(
                        "Access Denied",
                        "This payroll record belongs to an employee in a different branch.");

                    return;
                }
            }


            var employeesQuery =
                db.Employees
                    .AsNoTracking()
                    .Where(x => x.IsActive);

            if (branchManagementEnabled)
            {
                employeesQuery =
                    employeesQuery.Where(x =>
                        x.BranchId == currentBranchId!.Value);
            }

            var employees =
                await employeesQuery
                    .OrderBy(x => x.LastName)
                    .ThenBy(x => x.FirstName)
                    .ToListAsync();


            var employeeBox =
                new ComboBox
                {
                    Header = "Employee",
                    ItemsSource = employees,
                    DisplayMemberPath = "LastName",
                    MinWidth = 320,
                    SelectedValuePath = "EmployeeId",
                    SelectedValue = payroll.EmployeeId
                };


            var startDatePicker =
                new CalendarDatePicker
                {
                    Header = "Pay Period Start",
                    Date =
                        new DateTimeOffset(
                            payroll.PayPeriodStart)
                };


            var endDatePicker =
                new CalendarDatePicker
                {
                    Header = "Pay Period End",
                    Date =
                        new DateTimeOffset(
                            payroll.PayPeriodEnd)
                };


            var basicSalaryBox =
                new NumberBox
                {
                    Header = "Basic Salary",
                    Value =
                        (double)payroll.BasicSalary,
                    Minimum = 0,
                    SpinButtonPlacementMode =
                        NumberBoxSpinButtonPlacementMode.Compact
                };


            var deductionsBox =
                new NumberBox
                {
                    Header = "Deductions",
                    Value =
                        (double)payroll.Deductions,
                    Minimum = 0,
                    SpinButtonPlacementMode =
                        NumberBoxSpinButtonPlacementMode.Compact
                };


            var netSalaryText =
                new TextBlock
                {
                    Text =
                        $"Net Salary: ₱{payroll.NetSalary:N2}",
                    FontSize = 18,
                    FontWeight =
                        Microsoft.UI.Text.FontWeights.SemiBold
                };


            void UpdateNetSalary()
            {
                var basic =
                    double.IsNaN(basicSalaryBox.Value)
                        ? 0
                        : basicSalaryBox.Value;

                var deductions =
                    double.IsNaN(deductionsBox.Value)
                        ? 0
                        : deductionsBox.Value;

                var net =
                    Math.Max(0, basic - deductions);

                netSalaryText.Text =
                    $"Net Salary: ₱{net:N2}";
            }


            basicSalaryBox.ValueChanged +=
                (_, _) => UpdateNetSalary();

            deductionsBox.ValueChanged +=
                (_, _) => UpdateNetSalary();


            var panel =
                new StackPanel
                {
                    Spacing = 12
                };

            panel.Children.Add(employeeBox);
            panel.Children.Add(startDatePicker);
            panel.Children.Add(endDatePicker);
            panel.Children.Add(basicSalaryBox);
            panel.Children.Add(deductionsBox);
            panel.Children.Add(netSalaryText);


            var dialog =
                new ContentDialog
                {
                    Title = "Edit Payroll",
                    Content = panel,

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


            if (employeeBox.SelectedValue
                is not int employeeId)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select an employee.");

                return;
            }


            // Branch safety: employee must belong to selected branch
            if (branchManagementEnabled)
            {
                var selectedEmployee =
                    employees.FirstOrDefault(x =>
                        x.EmployeeId == employeeId);

                if (selectedEmployee == null ||
                    selectedEmployee.BranchId != currentBranchId)
                {
                    await ShowMessageAsync(
                        "Access Denied",
                        "The selected employee does not belong to the currently selected branch.");

                    return;
                }
            }


            if (!startDatePicker.Date.HasValue ||
                !endDatePicker.Date.HasValue)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select both pay period dates.");

                return;
            }


            var payPeriodStart =
                startDatePicker.Date.Value.DateTime.Date;

            var payPeriodEnd =
                endDatePicker.Date.Value.DateTime.Date;


            if (payPeriodEnd < payPeriodStart)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Pay period end cannot be earlier than the start date.");

                return;
            }


            var basicSalary =
                (decimal)(
                    double.IsNaN(basicSalaryBox.Value)
                        ? 0
                        : basicSalaryBox.Value);


            var deductions =
                (decimal)(
                    double.IsNaN(deductionsBox.Value)
                        ? 0
                        : deductionsBox.Value);


            if (basicSalary <= 0)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Basic salary must be greater than zero.");

                return;
            }


            if (deductions > basicSalary)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Deductions cannot be greater than the basic salary.");

                return;
            }


            var duplicate =
                await db.Payrolls
                    .AnyAsync(x =>
                        x.PayrollId !=
                            payroll.PayrollId &&

                        x.EmployeeId ==
                            employeeId &&

                        x.PayPeriodStart ==
                            payPeriodStart &&

                        x.PayPeriodEnd ==
                            payPeriodEnd);

            if (duplicate)
            {
                await ShowMessageAsync(
                    "Duplicate Payroll",
                    "Another payroll record already exists for this employee and pay period.");

                return;
            }


            // ==========================================
            // RELOAD EMPLOYEE (for description & branch)
            // ==========================================

            var employee =
                await db.Employees
                    .FirstOrDefaultAsync(x =>
                        x.EmployeeId == employeeId);


            payroll.EmployeeId =
                employeeId;

            payroll.PayPeriodStart =
                payPeriodStart;

            payroll.PayPeriodEnd =
                payPeriodEnd;

            payroll.BasicSalary =
                basicSalary;

            payroll.Deductions =
                deductions;

            payroll.NetSalary =
                basicSalary - deductions;


            // ==========================================
            // UPDATE FINANCE PAYROLL EXPENSE
            // ==========================================

            var payrollExpense =
                await db.Expenses
                    .FirstOrDefaultAsync(
                        x => x.PayrollId == payroll.PayrollId);

            if (payrollExpense != null)
            {
                payrollExpense.Amount =
                    payroll.NetSalary;

                payrollExpense.ExpenseDate =
                    payroll.PayPeriodEnd;

                payrollExpense.BranchId =
                    employee?.BranchId;

                payrollExpense.Description =
                    $"Payroll for {employee?.FirstName} " +
                    $"{employee?.LastName} " +
                    $"({payroll.PayPeriodStart:MM/dd/yyyy} - " +
                    $"{payroll.PayPeriodEnd:MM/dd/yyyy})";
            }

            await db.SaveChangesAsync();

            await LoadPayrollsAsync();


            await ShowMessageAsync(
                "Payroll Updated",
                "The payroll record was updated successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Updating Payroll",
                ex.Message);
        }
    }


    // ==========================================
    // DELETE PAYROLL
    // ==========================================

    private async void DeletePayrollButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not PayrollRow row)
        {
            return;
        }

        if (CurrentUser.CompanyId == null)
        {
            return;
        }

        try
        {
            var confirm =
                new ContentDialog
                {
                    Title = "Delete Payroll",
                    Content =
                        $"Delete payroll #{row.PayrollId} for {row.EmployeeName}?",

                    PrimaryButtonText = "Delete",
                    CloseButtonText = "Cancel",

                    DefaultButton =
                        ContentDialogButton.Close,

                    XamlRoot = XamlRoot
                };


            var result =
                await confirm.ShowAsync();

            if (result !=
                ContentDialogResult.Primary)
            {
                return;
            }


            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var branchManagementEnabled =
                await HasBranchManagementAsync();

            int? currentBranchId = null;

            if (branchManagementEnabled)
            {
                currentBranchId =
                    _currentBranchContext.BranchId;

                if (currentBranchId == null)
                {
                    await ShowMessageAsync(
                        "Branch Not Selected",
                        "Please select a branch from the global branch selector.");

                    return;
                }
            }


            var payroll =
                await db.Payrolls
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(
                        x =>
                            x.PayrollId ==
                            row.PayrollId);

            if (payroll == null)
            {
                await ShowMessageAsync(
                    "Not Found",
                    "The payroll record could not be found.");

                return;
            }


            // ==========================================
            // BRANCH SAFETY CHECK
            // ==========================================

            if (branchManagementEnabled)
            {
                if (payroll.Employee == null ||
                    payroll.Employee.BranchId != currentBranchId)
                {
                    await ShowMessageAsync(
                        "Access Denied",
                        "This payroll record belongs to an employee in a different branch.");

                    return;
                }
            }


            // ==========================================
            // DELETE RELATED FINANCE PAYROLL EXPENSE
            // ==========================================

            var payrollExpense =
                await db.Expenses
                    .FirstOrDefaultAsync(
                        x => x.PayrollId == payroll.PayrollId);

            if (payrollExpense != null)
            {
                db.Expenses.Remove(payrollExpense);
            }


            // ==========================================
            // DELETE PAYROLL
            // ==========================================

            db.Payrolls.Remove(payroll);

            await db.SaveChangesAsync();


            await LoadPayrollsAsync();


            await ShowMessageAsync(
                "Payroll Deleted",
                "The payroll record was deleted successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Deleting Payroll",
                ex.Message);
        }
    }


    // ==========================================
    // MESSAGE DIALOG
    // ==========================================

    private async Task ShowMessageAsync(
        string title,
        string message)
    {
        var dialog =
            new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };

        await dialog.ShowAsync();
    }


    // ==========================================
    // DISPLAY ROW
    // ==========================================

    private sealed class PayrollRow
    {
        public int PayrollId { get; set; }

        public int EmployeeId { get; set; }

        public string EmployeeName { get; set; } =
            string.Empty;

        public string PayPeriodStartText { get; set; } =
            string.Empty;

        public string PayPeriodEndText { get; set; } =
            string.Empty;

        public string BasicSalaryText { get; set; } =
            string.Empty;

        public string DeductionsText { get; set; } =
            string.Empty;

        public string NetSalaryText { get; set; } =
            string.Empty;

        public DateTime PayPeriodStart { get; set; }

        public DateTime PayPeriodEnd { get; set; }

        public decimal BasicSalary { get; set; }

        public decimal Deductions { get; set; }
    }
}