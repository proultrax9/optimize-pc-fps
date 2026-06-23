using CommunityToolkit.Mvvm.ComponentModel;

namespace FpsGodPc.App.ViewModels;

public partial class NavEntryViewModel(string key, string label) : ViewModelBase
{
    public string Key { get; } = key;

    [ObservableProperty]
    private string label = label;

    [ObservableProperty]
    private bool isSelected;
}

public sealed class NavSectionViewModel
{
    public NavSectionViewModel(string title, IEnumerable<NavEntryViewModel> items, bool isFooter = false)
    {
        Title = title;
        IsFooter = isFooter;
        Items = items.ToList();
    }

    public string Title { get; private set; }
    public bool IsFooter { get; }
    public IReadOnlyList<NavEntryViewModel> Items { get; }

    public void SetTitle(string title) => Title = title;
}
