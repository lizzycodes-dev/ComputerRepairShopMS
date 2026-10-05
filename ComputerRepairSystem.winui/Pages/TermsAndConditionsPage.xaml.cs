using ComputerRepairSystem.infrastructure.data;
using ComputerRepairSystem.domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ComputerRepairSystem_winui.Services;

namespace ComputerRepairSystem.winui.Pages;

public sealed partial class TermsAndConditionsPage : Page
{
    private readonly MasterErpDbContext _masterDb;

    private TermsAndConditions? _currentTerms;
    private TermsAndConditions? _draftTerms;
    public TermsAndConditionsPage(
        MasterErpDbContext masterDb)
    {
        this.InitializeComponent();

        _masterDb = masterDb;

        if (CurrentUser.Role == "Super Admin")
        {
            EditTermsButton.Visibility =
                Visibility.Visible;
        }

        Loaded += TermsAndConditionsPage_Loaded;
    }

    private async void TermsAndConditionsPage_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        await LoadTermsAndConditionsAsync();
    }

    private async Task LoadTermsAndConditionsAsync()
    {
        try
        {
            _currentTerms = await _masterDb.TermsAndConditions
                .AsNoTracking()
                .Where(t => t.IsPublished)
                .OrderByDescending(t => t.PublishedAt)
                .FirstOrDefaultAsync();

            _draftTerms = null;

            if (CurrentUser.Role == "Super Admin")
            {
                _draftTerms = await _masterDb.TermsAndConditions
                    .AsNoTracking()
                    .Where(t => !t.IsPublished)
                    .OrderByDescending(t => t.TermsAndConditionsId)
                    .FirstOrDefaultAsync();
            }

            if (_currentTerms == null)
            {
                TermsContentTextBlock.Text =
                    "No Terms and Conditions have been published yet.";

                VersionTextBlock.Text =
                    "No version available";

                LastUpdatedTextBlock.Text =
                    "Not available";

                return;
            }

            TermsContentTextBlock.Text =
                _currentTerms.Content;

            VersionTextBlock.Text =
                $"Published: Version {_currentTerms.Version}";

            LastUpdatedTextBlock.Text =
                _currentTerms.UpdatedAt.HasValue
                    ? $"Last updated: {_currentTerms.UpdatedAt.Value:MMMM yyyy}"
                    : $"Last updated: {_currentTerms.CreatedAt:MMMM yyyy}";

            UpdateDraftInformation();
        }
        catch (Exception ex)
        {
            TermsContentTextBlock.Text =
                "Unable to load the Terms and Conditions.";

            VersionTextBlock.Text = "Error";

            LastUpdatedTextBlock.Text =
                ex.Message;
        }
    }

    private void UpdateDraftInformation()
    {
        if (CurrentUser.Role != "Super Admin")
        {
            return;
        }

        if (_draftTerms != null)
        {
            LastUpdatedTextBlock.Text =
                $"Published: Version {_currentTerms?.Version ?? "None"} • " +
                $"Draft: Version {_draftTerms.Version}";
        }
    }

    private void EditTermsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_currentTerms == null)
        {
            return;
        }

        TermsContentTextBlock.Visibility =
            Visibility.Collapsed;

        TermsContentEditor.Visibility =
            Visibility.Visible;

        if (_draftTerms != null)
        {
            TermsContentEditor.Text =
                _draftTerms.Content;

            VersionTextBlock.Text =
                $"Editing Draft: Version {_draftTerms.Version}";
        }
        else
        {
            TermsContentEditor.Text =
                _currentTerms.Content;

            VersionTextBlock.Text =
                $"Creating Draft: Version {GetNextVersion(_currentTerms.Version)}";
        }

        EditActionsPanel.Visibility =
            Visibility.Visible;

        EditTermsButton.Visibility =
            Visibility.Collapsed;
    }

    private void CancelEditButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ExitEditMode();
    }

    private void ExitEditMode()
    {
        TermsContentEditor.Visibility =
            Visibility.Collapsed;

        TermsContentTextBlock.Visibility =
            Visibility.Visible;

        EditActionsPanel.Visibility =
            Visibility.Collapsed;

        if (CurrentUser.Role == "Super Admin")
        {
            EditTermsButton.Visibility =
                Visibility.Visible;
        }
    }

    private async void SaveDraftButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await SaveTermsAsync(false);
    }

    private async void PublishButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await SaveTermsAsync(true);
    }

    private async Task SaveTermsAsync(bool publish)
    {
        if (_currentTerms == null)
        {
            return;
        }

        string content = TermsContentEditor.Text.Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            await ShowMessageAsync(
                "Terms and Conditions cannot be empty.");

            return;
        }

        try
        {
            // If a draft already exists, update it.
            if (_draftTerms != null)
            {
                var draft = await _masterDb.TermsAndConditions
                    .FirstOrDefaultAsync(t =>
                        t.TermsAndConditionsId ==
                        _draftTerms.TermsAndConditionsId);

                if (draft == null)
                {
                    await ShowMessageAsync(
                        "The draft could not be found.");

                    return;
                }

                draft.Content = content;
                draft.UpdatedAt = DateTime.Now;

                if (publish)
                {
                    // Archive the currently published version.
                    var publishedTerms =
                        await _masterDb.TermsAndConditions
                            .Where(t =>
                                t.IsPublished &&
                                t.TermsAndConditionsId !=
                                draft.TermsAndConditionsId)
                            .ToListAsync();

                    foreach (var terms in publishedTerms)
                    {
                        terms.IsPublished = false;
                    }

                    draft.IsPublished = true;
                    draft.PublishedAt = DateTime.Now;
                }

                await _masterDb.SaveChangesAsync();

                await ShowMessageAsync(
                    publish
                        ? $"Terms and Conditions version {draft.Version} published successfully."
                        : $"Terms and Conditions version {draft.Version} updated successfully.");

                await LoadTermsAndConditionsAsync();
                ExitEditMode();

                return;
            }

            // No existing draft — create a new version.
            string newVersion =
                GetNextVersion(_currentTerms.Version);

            var newTerms = new TermsAndConditions
            {
                Version = newVersion,
                Content = content,
                IsPublished = publish,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                PublishedAt = publish
                    ? DateTime.Now
                    : null
            };

            // If publishing immediately, archive the old version.
            if (publish)
            {
                var publishedTerms =
                    await _masterDb.TermsAndConditions
                        .Where(t => t.IsPublished)
                        .ToListAsync();

                foreach (var terms in publishedTerms)
                {
                    terms.IsPublished = false;
                }
            }

            _masterDb.TermsAndConditions.Add(newTerms);

            await _masterDb.SaveChangesAsync();

            await ShowMessageAsync(
                publish
                    ? $"Terms and Conditions version {newVersion} published successfully."
                    : $"Terms and Conditions version {newVersion} saved as a draft.");

            await LoadTermsAndConditionsAsync();
            ExitEditMode();
        }
        catch (Exception ex)
        {
            await ShowMessageAsync(
                $"Unable to save the Terms and Conditions.\n\n{ex.Message}");
        }
    }

    private string GetNextVersion(
        string currentVersion)
    {
        if (decimal.TryParse(
            currentVersion,
            out decimal version))
        {
            return (version + 0.1m)
                .ToString("0.0");
        }

        return "1.1";
    }

    private async Task ShowMessageAsync(
        string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Terms and Conditions",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
    }
}