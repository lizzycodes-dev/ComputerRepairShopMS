using ComputerRepairSystem.domain.Entities;
using ComputerRepairSystem.infrastructure.data;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ComputerRepairSystem_winui.Pages;

public sealed partial class SuperAdminSystemSettingsPage : Page
{
    private readonly MasterErpDbContext
        _masterDb;

    public SuperAdminSystemSettingsPage(
        MasterErpDbContext masterDb)
    {
        InitializeComponent();

        _masterDb = masterDb;

        Loaded +=
            SuperAdminSystemSettingsPage_Loaded;
    }


    // ==========================================
    // LOAD SETTINGS
    // ==========================================

    private async void
        SuperAdminSystemSettingsPage_Loaded(
            object sender,
            RoutedEventArgs e)
    {
        try
        {
            await LoadSettingsAsync();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Settings Error",
                ex.Message);

            System.Diagnostics.Debug.WriteLine(ex);
        }
    }


    private async Task LoadSettingsAsync()
    {
        var settings =
            await _masterDb
                .SuperAdminSystemSettings
                .FirstOrDefaultAsync();

        if (settings == null)
        {
            settings =
                new SuperAdminSystemSettings();

            _masterDb
                .SuperAdminSystemSettings
                .Add(settings);

            await _masterDb.SaveChangesAsync();
        }


        PlatformNameBox.Text =
            settings.PlatformName;

        PlatformDescriptionBox.Text =
            settings.PlatformDescription
            ?? string.Empty;

        SupportEmailBox.Text =
            settings.SupportEmail
            ?? string.Empty;

        SupportPhoneBox.Text =
            settings.SupportPhone
            ?? string.Empty;

        AllowNewCompanyRegistrationToggle.IsOn =
            settings.AllowNewCompanyRegistration;

        DefaultTrialDurationBox.Value =
            settings.DefaultTrialDurationInDays;

        MaintenanceModeToggle.IsOn =
            settings.MaintenanceMode;
    }


    // ==========================================
    // SAVE SETTINGS
    // ==========================================

    private async void SaveButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(
            PlatformNameBox.Text))
        {
            await ShowMessageAsync(
                "Missing Platform Name",
                "Please enter the platform name.");

            return;
        }


        try
        {
            var settings =
                await _masterDb
                    .SuperAdminSystemSettings
                    .FirstOrDefaultAsync();


            if (settings == null)
            {
                settings =
                    new SuperAdminSystemSettings();

                _masterDb
                    .SuperAdminSystemSettings
                    .Add(settings);
            }


            settings.PlatformName =
                PlatformNameBox.Text.Trim();

            settings.PlatformDescription =
                string.IsNullOrWhiteSpace(
                    PlatformDescriptionBox.Text)
                    ? null
                    : PlatformDescriptionBox.Text.Trim();

            settings.SupportEmail =
                string.IsNullOrWhiteSpace(
                    SupportEmailBox.Text)
                    ? null
                    : SupportEmailBox.Text.Trim();

            settings.SupportPhone =
                string.IsNullOrWhiteSpace(
                    SupportPhoneBox.Text)
                    ? null
                    : SupportPhoneBox.Text.Trim();

            settings.AllowNewCompanyRegistration =
                AllowNewCompanyRegistrationToggle.IsOn;

            settings.DefaultTrialDurationInDays =
                double.IsNaN(
                    DefaultTrialDurationBox.Value)
                    ? 0
                    : (int)
                        DefaultTrialDurationBox.Value;

            settings.MaintenanceMode =
                MaintenanceModeToggle.IsOn;

            settings.UpdatedAt =
                DateTime.UtcNow;


            await _masterDb.SaveChangesAsync();


            await ShowMessageAsync(
                "Settings Saved",
                "System settings have been updated.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                "Settings Error",
                ex.Message);

            System.Diagnostics.Debug.WriteLine(ex);
        }
    }


    // ==========================================
    // MESSAGE
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
}