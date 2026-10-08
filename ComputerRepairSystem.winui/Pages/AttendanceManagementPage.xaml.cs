using ComputerRepairSystem.company.Context;
using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Entities;
using ComputerRepairSystem.infrastructure.data;
using ComputerRepairSystem_winui.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComputerRepairSystem_winui.Pages;

public sealed partial class AttendanceManagementPage : Page
{
    private readonly TenantDbContextFactory _tenantDbFactory;
    private readonly MasterErpDbContext _masterDb;
    private readonly CurrentBranchContext _currentBranchContext;

    private List<AttendanceRow> _attendanceRecords = new();

    private Attendance? _selectedAttendance;


    public AttendanceManagementPage(
        TenantDbContextFactory tenantDbFactory,
        MasterErpDbContext masterDb,
        CurrentBranchContext currentBranchContext)
    {
        InitializeComponent();

        _tenantDbFactory = tenantDbFactory;

        _masterDb = masterDb;

        _currentBranchContext = currentBranchContext;

        Loaded += AttendanceManagementPage_Loaded;
        Unloaded += AttendanceManagementPage_Unloaded;
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

    private async void AttendanceManagementPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        _currentBranchContext.BranchChanged +=
            OnGlobalBranchChanged;

        await LoadAttendanceAsync();
    }


    private void AttendanceManagementPage_Unloaded(
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

        await LoadAttendanceAsync();
    }


    // ==========================================
    // LOAD ATTENDANCE
    // ==========================================

    private async Task LoadAttendanceAsync()
    {
        try
        {
            if (CurrentUser.CompanyId == null)
            {
                AttendanceListView.ItemsSource = null;
                return;
            }

            await using var db =
                await _tenantDbFactory.CreateAsync(
                    CurrentUser.CompanyId.Value);

            var branchManagementEnabled =
                await HasBranchManagementAsync();

            var query =
                db.Attendances
                    .AsNoTracking()
                    .Include(x => x.Employee)
                    .AsQueryable();

            if (branchManagementEnabled)
            {
                var currentBranchId =
                    _currentBranchContext.BranchId;

                if (currentBranchId == null)
                {
                    _attendanceRecords = new();

                    AttendanceListView.ItemsSource = null;

                    EditAttendanceButton.IsEnabled = false;
                    DeleteAttendanceButton.IsEnabled = false;

                    await ShowMessageAsync(
                        "Branch Not Selected",
                        "Please select a branch from the global branch selector.");

                    return;
                }

                query = query.Where(x =>
                    x.Employee!.BranchId ==
                    currentBranchId.Value);
            }

            var attendance =
                await query
                    .OrderByDescending(x => x.AttendanceDate)
                    .ThenByDescending(x => x.TimeIn)
                    .ToListAsync();

            _attendanceRecords =
                attendance
                    .Select(x => new AttendanceRow
                    {
                        Attendance = x
                    })
                    .ToList();

            ApplySearch();

            EditAttendanceButton.IsEnabled = false;
            DeleteAttendanceButton.IsEnabled = false;
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Error Loading Attendance",
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
        var searchText =
            SearchBox.Text?
                .Trim()
                .ToLower();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            AttendanceListView.ItemsSource =
                _attendanceRecords;

            return;
        }

        var filtered =
            _attendanceRecords
                .Where(x =>
                    x.EmployeeName
                        .ToLower()
                        .Contains(searchText))
                .ToList();

        AttendanceListView.ItemsSource =
            filtered;
    }


    // ==========================================
    // SELECTION
    // ==========================================

    private void AttendanceListView_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        _selectedAttendance = null;

        if (AttendanceListView.SelectedItem
            is AttendanceRow row)
        {
            _selectedAttendance =
                row.Attendance;
        }

        bool hasSelection =
            _selectedAttendance != null;

        EditAttendanceButton.IsEnabled =
            hasSelection;

        DeleteAttendanceButton.IsEnabled =
            hasSelection;
    }


    // ==========================================
    // ADD ATTENDANCE
    // ==========================================

    private async void AddAttendanceButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (CurrentUser.CompanyId == null)
        {
            await ShowMessageAsync(
                "Access Error",
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
                        : "There are no active employees available.");

                return;
            }


            // ==========================================
            // EMPLOYEE
            // ==========================================

            var employeeBox = new ComboBox
            {
                Header = "Employee",
                PlaceholderText = "Select employee",
                DisplayMemberPath = "FullName",
                Width = 350
            };

            foreach (var employee in employees)
            {
                employeeBox.Items.Add(
                    new EmployeeOption
                    {
                        Employee = employee
                    });
            }


            // ==========================================
            // DATE
            // ==========================================

            var datePicker = new CalendarDatePicker
            {
                Header = "Attendance Date",
                Date = DateTimeOffset.Now
            };


            // ==========================================
            // TIME IN
            // ==========================================

            var timeInPicker = new TimePicker
            {
                Header = "Time In",
                ClockIdentifier = "12HourClock"
            };


            // ==========================================
            // TIME OUT
            // ==========================================

            var timeOutPicker = new TimePicker
            {
                Header = "Time Out",
                ClockIdentifier = "12HourClock"
            };


            // ==========================================
            // STATUS
            // ==========================================

            var statusBox = new ComboBox
            {
                Header = "Status",
                PlaceholderText = "Select status"
            };

            statusBox.Items.Add("Present");
            statusBox.Items.Add("Late");
            statusBox.Items.Add("Absent");
            statusBox.Items.Add("Leave");

            statusBox.SelectedIndex = 0;


            // ==========================================
            // REMARKS
            // ==========================================

            var remarksBox = new TextBox
            {
                Header = "Remarks",
                PlaceholderText =
                    "Optional remarks...",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 80
            };


            // ==========================================
            // PANEL
            // ==========================================

            var panel = new StackPanel
            {
                Spacing = 12
            };

            panel.Children.Add(employeeBox);
            panel.Children.Add(datePicker);
            panel.Children.Add(timeInPicker);
            panel.Children.Add(timeOutPicker);
            panel.Children.Add(statusBox);
            panel.Children.Add(remarksBox);


            // ==========================================
            // DIALOG
            // ==========================================

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
                Title = "Add Attendance",
                Content = scrollViewer,

                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",

                DefaultButton =
                    ContentDialogButton.Primary,

                XamlRoot = XamlRoot
            };


            var result =
                await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;


            // ==========================================
            // VALIDATION
            // ==========================================

            if (employeeBox.SelectedItem
                is not EmployeeOption selectedEmployee)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select an employee.");

                return;
            }

            // Branch safety: employee must belong to selected branch
            if (branchManagementEnabled &&
                selectedEmployee.Employee.BranchId !=
                    currentBranchId)
            {
                await ShowMessageAsync(
                    "Access Denied",
                    "The selected employee does not belong to the currently selected branch.");

                return;
            }

            if (!datePicker.Date.HasValue)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select an attendance date.");

                return;
            }

            if (statusBox.SelectedItem == null)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select a status.");

                return;
            }


            var attendanceDate =
                datePicker.Date.Value.Date;


            var existingAttendance =
                await db.Attendances
                    .AnyAsync(x =>
                        x.EmployeeId ==
                        selectedEmployee.Employee.EmployeeId
                        &&
                        x.AttendanceDate ==
                        attendanceDate);

            if (existingAttendance)
            {
                await ShowMessageAsync(
                    "Duplicate Attendance",
                    "Attendance for this employee and date already exists.");

                return;
            }


            // ==========================================
            // CREATE ATTENDANCE
            // ==========================================

            var attendance = new Attendance
            {
                EmployeeId =
                    selectedEmployee.Employee.EmployeeId,

                AttendanceDate =
                    attendanceDate,

                TimeIn =
                    timeInPicker.SelectedTime,

                TimeOut =
                    timeOutPicker.SelectedTime,

                Status =
                    statusBox.SelectedItem.ToString()
                    ?? "Present",

                Remarks =
                    string.IsNullOrWhiteSpace(
                        remarksBox.Text)
                        ? null
                        : remarksBox.Text.Trim()
            };


            // ==========================================
            // SAVE
            // ==========================================

            db.Attendances.Add(attendance);

            await db.SaveChangesAsync();

            await LoadAttendanceAsync();


            await ShowMessageAsync(
                "Attendance Added",
                "Attendance was added successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Add Attendance Error",
                ex.Message);
        }
    }


    // ==========================================
    // EDIT ATTENDANCE
    // ==========================================

    private async void EditAttendanceButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_selectedAttendance == null)
        {
            await ShowMessageAsync(
                "No Attendance Selected",
                "Please select an attendance record first.");

            return;
        }

        if (CurrentUser.CompanyId == null)
        {
            await ShowMessageAsync(
                "Access Error",
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


            var attendance =
                await db.Attendances
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(
                        x =>
                            x.AttendanceId ==
                            _selectedAttendance.AttendanceId);

            if (attendance == null)
            {
                await ShowMessageAsync(
                    "Not Found",
                    "The attendance record no longer exists.");

                await LoadAttendanceAsync();
                return;
            }


            // ==========================================
            // BRANCH SAFETY CHECK
            // ==========================================

            if (branchManagementEnabled)
            {
                if (attendance.Employee == null ||
                    attendance.Employee.BranchId !=
                        currentBranchId)
                {
                    await ShowMessageAsync(
                        "Access Denied",
                        "This attendance record belongs to an employee in a different branch.");

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
                        x.BranchId ==
                        currentBranchId!.Value);
            }

            var employees =
                await employeesQuery
                    .OrderBy(x => x.LastName)
                    .ThenBy(x => x.FirstName)
                    .ToListAsync();


            var employeeBox = new ComboBox
            {
                Header = "Employee",
                DisplayMemberPath = "FullName",
                Width = 350
            };

            foreach (var employee in employees)
            {
                employeeBox.Items.Add(
                    new EmployeeOption
                    {
                        Employee = employee
                    });
            }


            foreach (EmployeeOption option
                in employeeBox.Items)
            {
                if (option.Employee.EmployeeId ==
                    attendance.EmployeeId)
                {
                    employeeBox.SelectedItem =
                        option;

                    break;
                }
            }


            var datePicker = new CalendarDatePicker
            {
                Header = "Attendance Date",
                Date =
                    new DateTimeOffset(
                        attendance.AttendanceDate)
            };


            var timeInPicker = new TimePicker
            {
                Header = "Time In",
                ClockIdentifier = "12HourClock",
                SelectedTime =
                    attendance.TimeIn
            };


            var timeOutPicker = new TimePicker
            {
                Header = "Time Out",
                ClockIdentifier = "12HourClock",
                SelectedTime =
                    attendance.TimeOut
            };


            var statusBox = new ComboBox
            {
                Header = "Status"
            };

            statusBox.Items.Add("Present");
            statusBox.Items.Add("Late");
            statusBox.Items.Add("Absent");
            statusBox.Items.Add("Leave");

            statusBox.SelectedItem =
                attendance.Status;


            var remarksBox = new TextBox
            {
                Header = "Remarks",
                Text =
                    attendance.Remarks
                    ?? string.Empty,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 80
            };


            var panel = new StackPanel
            {
                Spacing = 12
            };

            panel.Children.Add(employeeBox);
            panel.Children.Add(datePicker);
            panel.Children.Add(timeInPicker);
            panel.Children.Add(timeOutPicker);
            panel.Children.Add(statusBox);
            panel.Children.Add(remarksBox);


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
                Title = "Edit Attendance",
                Content = scrollViewer,

                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",

                DefaultButton =
                    ContentDialogButton.Primary,

                XamlRoot = XamlRoot
            };


            var result =
                await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;


            if (employeeBox.SelectedItem
                is not EmployeeOption selectedEmployee)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select an employee.");

                return;
            }

            // Branch safety: employee must belong to selected branch
            if (branchManagementEnabled &&
                selectedEmployee.Employee.BranchId !=
                    currentBranchId)
            {
                await ShowMessageAsync(
                    "Access Denied",
                    "The selected employee does not belong to the currently selected branch.");

                return;
            }

            if (!datePicker.Date.HasValue)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select an attendance date.");

                return;
            }

            if (statusBox.SelectedItem == null)
            {
                await ShowMessageAsync(
                    "Validation Error",
                    "Please select a status.");

                return;
            }


            var newDate =
                datePicker.Date.Value.Date;


            var duplicate =
                await db.Attendances
                    .AnyAsync(x =>
                        x.AttendanceId !=
                        attendance.AttendanceId
                        &&
                        x.EmployeeId ==
                        selectedEmployee.Employee.EmployeeId
                        &&
                        x.AttendanceDate ==
                        newDate);

            if (duplicate)
            {
                await ShowMessageAsync(
                    "Duplicate Attendance",
                    "Another attendance record already exists for this employee and date.");

                return;
            }


            attendance.EmployeeId =
                selectedEmployee.Employee.EmployeeId;

            attendance.AttendanceDate =
                newDate;

            attendance.TimeIn =
                timeInPicker.SelectedTime;

            attendance.TimeOut =
                timeOutPicker.SelectedTime;

            attendance.Status =
                statusBox.SelectedItem.ToString()
                ?? "Present";

            attendance.Remarks =
                string.IsNullOrWhiteSpace(
                    remarksBox.Text)
                    ? null
                    : remarksBox.Text.Trim();


            await db.SaveChangesAsync();

            await LoadAttendanceAsync();


            await ShowMessageAsync(
                "Attendance Updated",
                "Attendance was updated successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Edit Attendance Error",
                ex.Message);
        }
    }


    // ==========================================
    // DELETE ATTENDANCE
    // ==========================================

    private async void DeleteAttendanceButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_selectedAttendance == null)
        {
            await ShowMessageAsync(
                "No Attendance Selected",
                "Please select an attendance record first.");

            return;
        }


        var dialog = new ContentDialog
        {
            Title = "Delete Attendance",

            Content =
                "Are you sure you want to delete this attendance record?",

            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",

            DefaultButton =
                ContentDialogButton.Close,

            XamlRoot = XamlRoot
        };


        var result =
            await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
            return;


        if (CurrentUser.CompanyId == null)
        {
            await ShowMessageAsync(
                "Access Error",
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


            var attendance =
                await db.Attendances
                    .Include(x => x.Employee)
                    .FirstOrDefaultAsync(
                        x =>
                            x.AttendanceId ==
                            _selectedAttendance.AttendanceId);

            if (attendance == null)
            {
                await ShowMessageAsync(
                    "Not Found",
                    "The attendance record no longer exists.");

                await LoadAttendanceAsync();
                return;
            }


            // ==========================================
            // BRANCH SAFETY CHECK
            // ==========================================

            if (branchManagementEnabled)
            {
                if (attendance.Employee == null ||
                    attendance.Employee.BranchId !=
                        currentBranchId)
                {
                    await ShowMessageAsync(
                        "Access Denied",
                        "This attendance record belongs to an employee in a different branch.");

                    return;
                }
            }


            db.Attendances.Remove(attendance);

            await db.SaveChangesAsync();

            await LoadAttendanceAsync();


            await ShowMessageAsync(
                "Attendance Deleted",
                "Attendance was deleted successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Delete Attendance Error",
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
        SearchBox.Text = string.Empty;

        await LoadAttendanceAsync();
    }


    // ==========================================
    // MESSAGE DIALOG
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
    // ATTENDANCE ROW
    // ==========================================

    private sealed class AttendanceRow
    {
        public Attendance Attendance { get; set; } = null!;


        public string EmployeeName =>
            Attendance.Employee == null
                ? "Unknown Employee"
                : string.Join(
                    " ",
                    new[]
                    {
                        Attendance.Employee.FirstName,
                        Attendance.Employee.MiddleName,
                        Attendance.Employee.LastName
                    }
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x)));


        public string AttendanceDateText =>
            Attendance.AttendanceDate
                .ToString("MMM dd, yyyy");


        public string TimeInText =>
            Attendance.TimeIn.HasValue
                ? DateTime.Today
                    .Add(Attendance.TimeIn.Value)
                    .ToString("h:mm tt")
                : "-";


        public string TimeOutText =>
            Attendance.TimeOut.HasValue
                ? DateTime.Today
                    .Add(Attendance.TimeOut.Value)
                    .ToString("h:mm tt")
                : "-";


        public string StatusText =>
            Attendance.Status;


        public string RemarksText =>
            string.IsNullOrWhiteSpace(
                Attendance.Remarks)
                ? "-"
                : Attendance.Remarks;
    }


    // ==========================================
    // EMPLOYEE OPTION
    // ==========================================

    private sealed class EmployeeOption
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
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x)));
    }
}