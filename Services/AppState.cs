using ShareNest.Models;

namespace ShareNest.Services;

/// <summary>Only the values handed from one screen to the next. All real data lives in the SQLite database (see Db).</summary>
public static class AppState
{
    public static Item? SelectedItem { get; set; }
    public static Booking? SelectedBooking { get; set; }
    public static Payment? RecentPayment { get; set; }
    public static DateTime? PickupDate { get; set; }
    public static DateTime? ReturnDate { get; set; }
    public static TimeSpan PickupTime { get; set; } = new(10, 0, 0);
}
