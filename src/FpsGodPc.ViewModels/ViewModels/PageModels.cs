using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FpsGodPc.Core.Models;

namespace FpsGodPc.App.ViewModels;

public partial class TweakItemViewModel(
    TweakDefinition definition,
    bool enabled,
    string displayName,
    string displayDescription,
    string riskLabel,
    Action<TweakItemViewModel, bool> onToggle) : ViewModelBase
{
    public string Id => definition.Id;
    public string Name => displayName;
    public string Description => displayDescription;
    public string Category => definition.Category;
    public bool RequiresAdmin => definition.RequiresAdmin;
    public bool RequiresReboot => definition.RequiresReboot;
    public bool AdvisorOnly => definition.AdvisorOnly;
    public RiskTier Tier => definition.Tier;
    public string RiskTier => riskLabel;

    [ObservableProperty]
    private bool isEnabled = enabled;

    [RelayCommand]
    private void Toggle()
    {
        var next = !IsEnabled;
        IsEnabled = next;
        onToggle(this, next);
    }

    public void RevertToggle() => IsEnabled = !IsEnabled;

    public void SyncState(bool applied) => IsEnabled = applied;
}

public sealed class AppliedTweakRow(string tweakId, string displayName, string? appliedAt)
{
    public string TweakId => tweakId;
    public string DisplayName => displayName;
    public string? AppliedAt => appliedAt;
}

public sealed class MetricItem(string label, string value, string? detail = null)
{
    public string Label { get; } = label;
    public string Value { get; } = value;
    public string? Detail { get; } = detail;
}

public sealed class CategoryOption(string key, string label)
{
    public string Key { get; } = key;
    public string Label { get; } = label;
    public override string ToString() => Label;
}
