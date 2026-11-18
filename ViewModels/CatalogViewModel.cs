using System.Collections.ObjectModel;
using System.Windows.Input;
using ShareNest.Models;
using ShareNest.Services;

namespace ShareNest.ViewModels;

/// <summary>One category chip (All / Tools / Yard ...). Colours change with IsSelected.</summary>
public class ChipItem : BaseViewModel
{
    static readonly Color Navy = Color.FromArgb("#364F6B");
    bool _selected;

    public string Name { get; }
    public ICommand Command { get; }
    public bool IsSelected
    {
        get => _selected;
        set { if (Set(ref _selected, value)) { Raise(nameof(Background)); Raise(nameof(Foreground)); } }
    }
    public Color Background => _selected ? Navy : Colors.Transparent;
    public Color Foreground => _selected ? Colors.White : Navy;

    public ChipItem(string name, Action<string> onTap)
    {
        Name = name;
        Command = new Command(() => onTap(name));
    }
}

public class CatalogViewModel : BaseViewModel
{
    List<Item> _all = new();
    string _search = "", _category = "All", _mode = "all", _countText = "";
    bool _hasNoResults, _hasMyItems, _hasCommunityItems;
    string _myItemsTitle = "YOUR LISTINGS", _communityItemsTitle = "COMMUNITY EQUIPMENT";

    public ObservableCollection<Item> Items { get; } = new();
    public ObservableCollection<Item> MyItems { get; } = new();
    public ObservableCollection<Item> CommunityItems { get; } = new();
    public ObservableCollection<ChipItem> Chips { get; } = new();
    public ICommand FilterCommand { get; }
    public ICommand RefreshCommand { get; }

    bool _isRefreshing;
    public bool IsRefreshing { get => _isRefreshing; set => Set(ref _isRefreshing, value); }

    public bool HasNoResults { get => _hasNoResults; private set => Set(ref _hasNoResults, value); }
    public bool HasMyItems { get => _hasMyItems; private set => Set(ref _hasMyItems, value); }
    public bool HasCommunityItems { get => _hasCommunityItems; private set => Set(ref _hasCommunityItems, value); }

    public string MyItemsTitle { get => _myItemsTitle; private set => Set(ref _myItemsTitle, value); }
    public string CommunityItemsTitle { get => _communityItemsTitle; private set => Set(ref _communityItemsTitle, value); }

    public string SearchText
    {
        get => _search;
        set { if (Set(ref _search, value ?? "")) Apply(); }
    }

    public string CountText { get => _countText; private set => Set(ref _countText, value); }

    public CatalogViewModel()
    {
        foreach (var name in new[] { "All", "Tools", "Yard", "Event", "Moving" })
            Chips.Add(new ChipItem(name, SelectCategory) { IsSelected = name == "All" });
        FilterCommand = new Command(async () => await ChooseFilter());
        RefreshCommand = new Command(async () =>
        {
            IsRefreshing = true;
            await LoadAsync();
            IsRefreshing = false;
        });
    }

    /// <summary>Reads the items from SQLite.</summary>
    public async Task LoadAsync()
    {
        _all = await Db.GetItemsAsync();
        Apply();
    }

    void SelectCategory(string category)
    {
        _category = category;
        foreach (var chip in Chips) chip.IsSelected = chip.Name == category;
        Apply();
    }

    void Apply()
    {
        var q = _search.Trim();
        var currentUserName = Session.User.FullName;

        IEnumerable<Item> list = _all
            .Where(i => _category == "All" || i.Category == _category)
            .Where(i => q.Length == 0 || i.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                                      || i.Summary.Contains(q, StringComparison.OrdinalIgnoreCase));

        if (_mode == "avail") list = list.Where(i => !i.IsUnavailable);
        else if (_mode == "mine") list = list.Where(i => string.Equals(i.Owner, currentUserName, StringComparison.OrdinalIgnoreCase));
        else if (_mode == "community") list = list.Where(i => !string.Equals(i.Owner, currentUserName, StringComparison.OrdinalIgnoreCase));

        if (_mode == "low") list = list.OrderBy(i => i.Deposit);
        if (_mode == "high") list = list.OrderByDescending(i => i.Deposit);

        var result = list.ToList();

        Items.Clear();
        foreach (var item in result) Items.Add(item);

        var my = result.Where(i => string.Equals(i.Owner, currentUserName, StringComparison.OrdinalIgnoreCase)).ToList();
        var comm = result.Where(i => !string.Equals(i.Owner, currentUserName, StringComparison.OrdinalIgnoreCase)).ToList();

        MyItems.Clear();
        foreach (var item in my) MyItems.Add(item);

        CommunityItems.Clear();
        foreach (var item in comm) CommunityItems.Add(item);

        HasMyItems = MyItems.Count > 0;
        HasCommunityItems = CommunityItems.Count > 0;
        HasNoResults = result.Count == 0;

        MyItemsTitle = $"YOUR LISTINGS ({MyItems.Count})";
        CommunityItemsTitle = $"COMMUNITY EQUIPMENT ({CommunityItems.Count})";

        if (MyItems.Count > 0 && CommunityItems.Count > 0)
            CountText = $"{CommunityItems.Count} community item{(CommunityItems.Count == 1 ? "" : "s")} · {MyItems.Count} listed by you";
        else if (CommunityItems.Count > 0)
            CountText = $"{CommunityItems.Count} item{(CommunityItems.Count == 1 ? "" : "s")} available near you";
        else if (MyItems.Count > 0)
            CountText = $"{MyItems.Count} item{(MyItems.Count == 1 ? "" : "s")} listed by you";
        else
            CountText = "0 items found";
    }

    async Task ChooseFilter()
    {
        var pick = await Shell.Current.DisplayActionSheet("Filter & sort", "Cancel", null,
            "Show all", "Available only", "Community items only", "My listings only", "Deposit: low to high", "Deposit: high to low");
        _mode = pick switch
        {
            "Available only" => "avail",
            "Community items only" => "community",
            "My listings only" => "mine",
            "Deposit: low to high" => "low",
            "Deposit: high to low" => "high",
            "Show all" => "all",
            _ => _mode
        };
        Apply();
    }
}
