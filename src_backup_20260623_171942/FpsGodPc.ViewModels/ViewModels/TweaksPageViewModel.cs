using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Localization;
using FpsGodPc.Core.Models;
using FpsGodPc.Core.Services;
using FpsGodPc.Core.Tweaks;
using System.Collections.ObjectModel;

namespace FpsGodPc.App.ViewModels;

public partial class TweaksPageViewModel(AppServices services, LocalizationService l10n) : PageViewModelBase(services, l10n, "tweaks")
{
    public ObservableCollection<CategoryOption> Categories { get; } = [];
    public ObservableCollection<TweakItemViewModel> Tweaks { get; } = [];

    private CategoryOption? _selectedCategory;

    public CategoryOption? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value) && value is not null)
            {
                LoadCategory(value.Key);
            }
        }
    }

    protected override void ApplyPageStrings() => Refresh();

    [RelayCommand]
    public void Refresh()
    {
        var key = SelectedCategory?.Key;
        Categories.Clear();

        foreach (var category in Services.GetCategories())
        {
            Categories.Add(new CategoryOption(category, L10n.Category(category)));
        }

        SelectedCategory = Categories.FirstOrDefault(c => c.Key == key) ?? Categories.FirstOrDefault();
    }

    private void LoadCategory(string category)
    {
        Tweaks.Clear();

        foreach (var tweak in Services.GetTweaksByCategory(category))
        {
            Tweaks.Add(new TweakItemViewModel(
                tweak,
                Services.IsTweakApplied(tweak.Id),
                L10n.TweakName(tweak.Id),
                L10n.TweakDescription(tweak.Id),
                L10n.Risk(tweak.Tier),
                ToggleTweak));
        }
    }

    private void ToggleTweak(TweakItemViewModel item, bool enabled)
    {
        var tweak = TweakCatalog.Get(item.Id);
        if (tweak is null)
        {
            item.RevertToggle();
            return;
        }

        if (enabled && tweak.AdvisorOnly)
        {
            StatusMessage = L10n.AdvisorOnlyHint;
            item.RevertToggle();
            return;
        }

        if (enabled && tweak.RequiresAdmin && !Services.IsElevated())
        {
            StatusMessage = L10n.AdminRequiredForTweak;
            item.RevertToggle();
            return;
        }

        if (enabled)
        {
            var settings = Services.GetAppSettings();
            if (settings.ConfirmExtremeTweaks && tweak.Tier is RiskTier.Moderate or RiskTier.Advanced)
            {
                var msg = L10n.ConfirmEnableTweak(
                    L10n.TweakName(tweak.Id),
                    L10n.TweakDescription(tweak.Id),
                    L10n.Risk(tweak.Tier));

                if (!Confirm(msg))
                {
                    item.RevertToggle();
                    return;
                }
            }
        }

        var result = Services.SetTweakState(item.Id, enabled);
        StatusMessage = result.Message;

        if (!result.Success)
        {
            item.RevertToggle();
        }
        else
        {
            item.SyncState(Services.IsTweakApplied(item.Id));
        }
    }
}
