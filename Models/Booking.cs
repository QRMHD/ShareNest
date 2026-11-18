using SQLite;

namespace ShareNest.Models;

[Table("Bookings")]
public class Booking
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string ItemName { get; set; } = "";
    public string IconFile { get; set; } = "ic_drill.png";
    public string Detail { get; set; } = "";
    public string Status { get; set; } = "Reserved";         // Reserved / Pickup / Active / Returned / Request / Lent
    public decimal Deposit { get; set; }
    public string PaymentMethod { get; set; } = "DuitNow QR";
    public string TransactionId { get; set; } = "";
    public DateTime ReservedOn { get; set; } = DateTime.Today;
    public DateTime PickupDate { get; set; } = DateTime.Today;
    public DateTime ReturnDate { get; set; } = DateTime.Today;
    public bool IsLending { get; set; }

    // ---- computed (get-only => not stored in the database) ----
    public string StatusText => Status == "Pickup" ? "Reserved" : Status;
    public Color StatusColor => Status switch
    {
        "Returned" => Color.FromArgb("#55655F"),
        "Active" or "Lent" => Color.FromArgb("#0F6F78"),
        _ => Color.FromArgb("#364F6B")                      // Reserved / Request
    };
    public Brush StatusBrush => new SolidColorBrush(StatusColor);

    public bool IsDueSoon => (Status is "Active" or "Lent") && (ReturnDate.Date - DateTime.Today).TotalDays <= 2;
    public Color ReminderColor => IsDueSoon ? Color.FromArgb("#C8285F") : Color.FromArgb("#0F6F78");

    public string ReminderText
    {
        get
        {
            var today = DateTime.Today;
            if (Status == "Returned") return $"✓ Returned {ReturnDate:d MMM}";
            if (Status is "Active" or "Lent")
            {
                int daysLeft = (ReturnDate.Date - today).Days;
                if (daysLeft < 0) return $"⚠️ Overdue by {-daysLeft}d · Late fee applies";
                if (daysLeft == 0) return "⏰ Due back today!";
                if (daysLeft == 1) return "⏰ Due back tomorrow";
                return $"⏰ Return due in {daysLeft} days ({ReturnDate:d MMM})";
            }
            int daysUntilPickup = (PickupDate.Date - today).Days;
            if (daysUntilPickup <= 0) return "📦 Ready for pickup today";
            if (daysUntilPickup == 1) return $"📦 Pickup tomorrow ({PickupDate:d MMM})";
            return $"📅 Pickup in {daysUntilPickup} days ({PickupDate:d MMM})";
        }
    }
}
