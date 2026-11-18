using System.Collections.ObjectModel;
using System.Windows.Input;
using ShareNest.Models;
using ShareNest.Services;

namespace ShareNest.ViewModels;

/// <summary>One section of the Bookings list (Reserved / Active / Returned).</summary>
public class BookingGroup
{
    public string Title { get; set; } = "";
    public Color Accent { get; set; } = Colors.Gray;
    public List<Booking> Items { get; set; } = new();
    public string HeaderText => $"{Title} ({Items.Count})";
}

public class BookingsViewModel : BaseViewModel
{
    static readonly Color Navy = Color.FromArgb("#364F6B");
    bool _lending;
    bool _isEmpty;
    string? _urgentBannerText;

    public ObservableCollection<BookingGroup> Groups { get; } = new();
    public ICommand ShowBorrowingCommand { get; }
    public ICommand ShowLendingCommand { get; }
    public ICommand RefreshCommand { get; }

    bool _isRefreshing;
    public bool IsRefreshing { get => _isRefreshing; set => Set(ref _isRefreshing, value); }

    public bool IsEmpty { get => _isEmpty; private set => Set(ref _isEmpty, value); }
    public string? UrgentBannerText { get => _urgentBannerText; private set { if (Set(ref _urgentBannerText, value)) Raise(nameof(HasUrgentBanner)); } }
    public bool HasUrgentBanner => !string.IsNullOrEmpty(_urgentBannerText);

    public Color BorrowBg => _lending ? Colors.Transparent : Navy;
    public Color BorrowFg => _lending ? Navy : Colors.White;
    public Color LendBg => _lending ? Navy : Colors.Transparent;
    public Color LendFg => _lending ? Colors.White : Navy;

    public BookingsViewModel()
    {
        ShowBorrowingCommand = new Command(async () => await Switch(false));
        ShowLendingCommand = new Command(async () => await Switch(true));
        RefreshCommand = new Command(async () =>
        {
            IsRefreshing = true;
            await LoadAsync();
            IsRefreshing = false;
        });
    }

    public async Task LoadAsync()
    {
        var list = await Db.GetBookingsAsync(_lending);       // SELECT from SQLite
        Groups.Clear();

        void Add(string title, Color accent, Func<Booking, bool> filter)
        {
            var items = list.Where(filter).ToList();
            if (items.Count > 0) Groups.Add(new BookingGroup { Title = title, Accent = accent, Items = items });
        }

        var teal = Color.FromArgb("#178C95");
        var grey = Color.FromArgb("#6F807B");
        var today = DateTime.Today;

        if (!_lending)
        {
            var dueSoonItem = list.FirstOrDefault(b => (b.Status == "Active") && (b.ReturnDate.Date - today).TotalDays <= 2);
            if (dueSoonItem is not null)
            {
                int days = (dueSoonItem.ReturnDate.Date - today).Days;
                string when = days == 1 ? "tomorrow" : days == 0 ? "today" : $"in {days} days";
                UrgentBannerText = $"🔔 Return Reminder: “{dueSoonItem.ItemName}” is due {when}! Tap booking to arrange return.";
            }
            else
            {
                UrgentBannerText = null;
            }

            Add("Reserved", Navy, b => b.Status is "Reserved" or "Pickup");
            Add("Active", teal, b => b.Status == "Active");
            Add("Returned", grey, b => b.Status == "Returned");
        }
        else
        {
            UrgentBannerText = null;
            Add("Requests", Navy, b => b.Status == "Request");
            Add("Active (lent out)", teal, b => b.Status == "Lent");
            Add("Returned", grey, b => b.Status == "Returned");
        }
        IsEmpty = Groups.Count == 0;
    }

    async Task Switch(bool lending)
    {
        _lending = lending;
        Raise(nameof(BorrowBg)); Raise(nameof(BorrowFg)); Raise(nameof(LendBg)); Raise(nameof(LendFg));
        await LoadAsync();
    }
}
