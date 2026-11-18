using System.Collections.ObjectModel;
using ShareNest.Services;

namespace ShareNest.ViewModels;

public class ActivityRow
{
    public string Text { get; set; } = "";
    public string? Tag { get; set; }
    public string IconFile { get; set; } = "ic_box.png";
    public bool HasTag => !string.IsNullOrEmpty(Tag);
}

public class ActivityViewModel : BaseViewModel
{
    string _active = "0", _lent = "0", _due = "0", _total = "0";

    public string ActiveCount { get => _active; private set => Set(ref _active, value); }
    public string LentCount { get => _lent; private set => Set(ref _lent, value); }
    public string DueCount { get => _due; private set => Set(ref _due, value); }
    public string TotalCount { get => _total; private set => Set(ref _total, value); }

    public ObservableCollection<ActivityRow> Upcoming { get; } = new();
    public ObservableCollection<ActivityRow> History { get; } = new();

    /// <summary>All numbers are calculated from the database (data manipulation / feedback).</summary>
    public async Task LoadAsync()
    {
        var today = DateTime.Today;
        var borrowing = await Db.GetBookingsAsync(false);
        var lending = await Db.GetBookingsAsync(true);

        ActiveCount = borrowing.Count(b => b.Status is "Reserved" or "Pickup" or "Active").ToString();
        LentCount = lending.Count(b => b.Status is "Lent" or "Request").ToString();
        DueCount = borrowing.Count(b => (b.Status is "Pickup" or "Active") && (b.ReturnDate - today).TotalDays <= 2).ToString();
        TotalCount = (await Db.GetIntAsync("loan_count", 12)).ToString();

        Upcoming.Clear();
        foreach (var bk in borrowing.Where(x => x.Status is "Pickup" or "Active"))
        {
            bool soon = (bk.ReturnDate - today).TotalDays <= 2;
            string when = bk.ReturnDate.Date == today.AddDays(1) ? "tomorrow" : bk.ReturnDate.ToString("d MMM");
            Upcoming.Add(new ActivityRow { Text = $"Return “{Short(bk.ItemName)}” due {when}", Tag = soon ? "Due" : null, IconFile = bk.IconFile });
        }

        History.Clear();
        foreach (var h in await Db.GetHistoryAsync(4))
            History.Add(new ActivityRow { Text = h.Text, IconFile = "ic_box.png" });
    }

    static string Short(string name)
    {
        var n = name.Contains('(') ? name[..name.IndexOf('(')].Trim() : name;
        return n.Split(' ').Last();
    }
}
