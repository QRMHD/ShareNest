using Microsoft.Maui.Controls.Shapes;
using ShareNest.Models;
using ShareNest.Services;

namespace ShareNest.Pages;

public partial class ReservePage : ContentPage
{
    static readonly Color Soft = Color.FromArgb("#E3EDEA"), Muted = Color.FromArgb("#3F514D"), Line = Color.FromArgb("#98A8A3"),
                          Teal = Color.FromArgb("#178C95"), Navy = Color.FromArgb("#364F6B"), Range = Color.FromArgb("#CFEFF2");

    DateTime _month = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    DateTime? _start, _end;
    readonly HashSet<DateTime> _busy = new();
    bool _loaded;

    bool IsBusy(DateTime d) => _busy.Contains(d.Date);
    bool RangeHasBusy(DateTime a, DateTime b) { for (var d = a; d <= b; d = d.AddDays(1)) if (IsBusy(d)) return true; return false; }
    decimal Deposit => AppState.SelectedItem?.Deposit ?? 20;

    public ReservePage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var item = AppState.SelectedItem;
        if (item is not null && string.Equals(item.Owner, Session.User.FullName, StringComparison.OrdinalIgnoreCase))
        {
            await DisplayAlert("Cannot reserve", "This is your own listing. You cannot borrow your own item.", "OK");
            await Shell.Current.GoToAsync("..");
            return;
        }

        if (_loaded) return;
        _loaded = true;

        // days already booked by someone else = rows in the Bookings table for this item
        if (item is not null)
            foreach (var b in await Db.GetActiveBookingsForItemAsync(item.Name))
                for (var d = b.PickupDate.Date; d < b.ReturnDate.Date; d = d.AddDays(1)) _busy.Add(d);

        var day = DateTime.Today.AddDays(1);
        while (IsBusy(day)) day = day.AddDays(1);
        _start = day;
        _end = RangeHasBusy(day, day.AddDays(2)) ? day : day.AddDays(2);
        Build();
    }

    void Build()
    {
        Cal.Children.Clear(); Cal.RowDefinitions.Clear(); Cal.ColumnDefinitions.Clear();
        for (int i = 0; i < 7; i++) Cal.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        MonthLbl.Text = _month.ToString("MMMM yyyy");

        string[] wd = { "M", "T", "W", "T", "F", "S", "S" };
        Cal.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (int i = 0; i < 7; i++)
            Cal.Add(new Label { Text = wd[i], FontSize = 12, TextColor = Muted, HorizontalOptions = LayoutOptions.Center }, i, 0);

        int offset = ((int)_month.DayOfWeek + 6) % 7;              // Monday first
        int days = DateTime.DaysInMonth(_month.Year, _month.Month);
        int row = 1, col = offset;
        Cal.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (int d = 1; d <= days; d++)
        {
            Cal.Add(MakeCell(new DateTime(_month.Year, _month.Month, d)), col, row);
            col++;
            if (col == 7 && d < days) { col = 0; row++; Cal.RowDefinitions.Add(new RowDefinition(GridLength.Auto)); }
        }
        UpdateSummary();
    }

    View MakeCell(DateTime date)
    {
        bool past = date < DateTime.Today, busy = IsBusy(date);
        bool edge = _start == date || _end == date;
        bool inRange = _start.HasValue && _end.HasValue && date > _start && date < _end;
        Color bg = Soft, fg = Muted;
        if (busy) { bg = Line; fg = Colors.White; }
        if (inRange) { bg = Range; fg = Navy; }
        if (edge) { bg = Teal; fg = Colors.White; }

        var cell = new Border
        {
            HeightRequest = 36, BackgroundColor = bg, StrokeThickness = 0, Opacity = past ? 0.4 : 1,
            StrokeShape = new RoundRectangle { CornerRadius = 5 },
            Content = new Label { Text = date.Day.ToString(), FontSize = 12.5, TextColor = fg,
                                  FontAttributes = edge ? FontAttributes.Bold : FontAttributes.None,
                                  HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };
        if (!past && !busy)
        {
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => OnDay(date);
            cell.GestureRecognizers.Add(tap);
        }
        return cell;
    }

    async void OnDay(DateTime d)
    {
        if (!_start.HasValue || _end.HasValue) { _start = d; _end = null; }                    // start a new range
        else if (d < _start) _start = d;
        else if (RangeHasBusy(_start.Value, d))
        {
            await DisplayAlert("Not available", "Some days in that range are already booked. Pick another return date.", "OK");
            _start = d; _end = null;
        }
        else _end = d;
        Build();
    }

    string _paymentMethod = "DuitNow QR";

    void UpdateSummary()
    {
        string S(DateTime? d) => d?.ToString("d MMM").ToUpper() ?? "—";
        SumTitle.Text = !_start.HasValue ? "SELECT PICKUP AND RETURN DATES"
                      : !_end.HasValue ? $"PICKUP {S(_start)} → SELECT RETURN"
                      : "BOOKING SCHEDULE";
        SumDates.Text = !_start.HasValue ? "Tap dates on the calendar above"
                      : !_end.HasValue ? $"{S(_start)} → (Select return date)"
                      : $"{S(_start)} → {S(_end)}";
        SumDeposit.Text = $"RM {Deposit:0.00}";
        TotalPayableLbl.Text = $"RM {Deposit:0.00}";
        TimeLbl.Text = $"Pickup time • {DateTime.Today.Add(AppState.PickupTime):h:mm tt}";
        DateLbl.Text = _start?.ToString("d MMM") ?? "—";
        ConfirmBtn.Text = $"Pay Deposit & Confirm (RM {Deposit:0.00})";
        AppState.PickupDate = _start; AppState.ReturnDate = _end;
    }

    async void OnSelectPaymentMethod(object? s, TappedEventArgs e)
    {
        var pick = await DisplayActionSheet("Select payment method", "Cancel", null,
            "📱 DuitNow QR / TNG eWallet",
            "🏦 Online Banking (FPX)",
            "💳 Debit / Credit Card (Visa/Mastercard)",
            "💵 Cash on Pickup (Direct to Owner)");
        if (pick is null or "Cancel") return;
        _paymentMethod = pick.Contains("DuitNow") ? "DuitNow QR" 
                       : pick.Contains("FPX") ? "Online Banking (FPX)" 
                       : pick.Contains("Card") ? "Credit / Debit Card" 
                       : "Cash on Pickup";
        PayMethodLbl.Text = pick;
    }

    void OnPrev(object? s, TappedEventArgs e)
    {
        var prev = _month.AddMonths(-1);
        if (prev >= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)) { _month = prev; Build(); }
    }
    void OnNext(object? s, TappedEventArgs e) { _month = _month.AddMonths(1); Build(); }

    async void OnTime(object? s, TappedEventArgs e)
    {
        var slots = Enumerable.Range(8, 11).Select(h => DateTime.Today.AddHours(h).ToString("h:mm tt")).ToArray();
        var pick = await DisplayActionSheet("Pickup time", "Cancel", null, slots);
        int i = Array.IndexOf(slots, pick);
        if (i >= 0) { AppState.PickupTime = TimeSpan.FromHours(8 + i); UpdateSummary(); }
    }

    async void OnConfirm(object? s, EventArgs e)
    {
        if (!_start.HasValue || !_end.HasValue)
        { await DisplayAlert("Select dates", "Tap a pickup date, then a return date.", "OK"); return; }

        var it = AppState.SelectedItem ?? (await Db.GetItemsAsync()).First();
        string txn = $"SN-{Random.Shared.Next(10000, 99999)}";
        var booking = new Booking
        {
            ItemName = it.Name, IconFile = it.IconFile, Deposit = it.Deposit, Status = "Reserved",
            ReservedOn = DateTime.Today, PickupDate = _start.Value, ReturnDate = _end.Value,
            PaymentMethod = _paymentMethod, TransactionId = txn,
            Detail = $"Reserved for {_start:d MMM}–{_end:d MMM}"
        };
        await Db.AddBookingAsync(booking);                // INSERT into SQLite
        it.Status = "Reserved";
        await Db.UpdateItemAsync(it);                     // mark item unavailable in catalog
        AppState.SelectedBooking = booking;

        // Navigate to payment page (fake QR + spinner) instead of alert
        await Shell.Current.GoToAsync("payment");
    }
}
