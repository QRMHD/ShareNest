using System.Collections.ObjectModel;
using ShareNest.Models;

namespace ShareNest.Services;

public sealed class ShareNestStore
{
    public ObservableCollection<CommunityItem> Items { get; } = new()
    {
        new() { Name = "Cordless drill", Category = "Power tools", Description = "A reliable 18V cordless drill with two batteries and a full set of bits. Great for shelves, furniture, and weekend projects.", Owner = "Maya Chen", Icon = "🔩", Distance = "0.2 mi", Tags = "18V · 2 batteries · DIY", Rating = 4.9, Deposit = 25, LateFeePerDay = 5 },
        new() { Name = "Extension ladder", Category = "Yard work", Description = "A sturdy 20-foot extension ladder, inspected and ready for home maintenance or painting projects.", Owner = "Daniel Brooks", Icon = "🪜", Distance = "0.4 mi", Tags = "20 ft · Fiberglass · Home", Rating = 5.0, Deposit = 40, LateFeePerDay = 8 },
        new() { Name = "Camping tent", Category = "Event supplies", Description = "Roomy four-person tent with rain fly, ground sheet, and carry bag. Clean and easy to set up.", Owner = "Priya Patel", Icon = "⛺", Distance = "0.6 mi", Tags = "4 person · Camping · Weekend", Rating = 4.8, Deposit = 30, LateFeePerDay = 6 },
        new() { Name = "Pressure washer", Category = "Yard work", Description = "Electric pressure washer with adjustable nozzle. Includes hose and quick-start instructions.", Owner = "Alex Rivera", Icon = "🚿", Distance = "0.8 mi", Tags = "Electric · Patio · Garden", Rating = 4.9, Deposit = 35, LateFeePerDay = 7 },
        new() { Name = "Circular saw", Category = "Power tools", Description = "Compact circular saw with blade guard and safety glasses. Please bring your own extension cord.", Owner = "Jordan Lee", Icon = "🪚", Distance = "1.1 mi", Tags = "7¼ in · Woodworking · DIY", Rating = 4.7, Deposit = 30, LateFeePerDay = 6 },
        new() { Name = "Folding chairs", Category = "Event supplies", Description = "A set of six lightweight folding chairs for backyard gatherings, parties, or neighborhood events.", Owner = "Sam Wilson", Icon = "🪑", Distance = "1.3 mi", Tags = "Set of 6 · Party · Outdoor", Rating = 5.0, Deposit = 15, LateFeePerDay = 3 }
    };

    public ObservableCollection<LoanBooking> Bookings { get; } = new()
    {
        new() { Item = new CommunityItem { Name = "Garden hose", Category = "Yard work", Owner = "Maya Chen", Icon = "🪴", Deposit = 10, LateFeePerDay = 2 }, PickupDate = DateTime.Today.AddDays(2), ReturnDate = DateTime.Today.AddDays(4), PickupTime = "9:00 AM", Stage = "Reserved", DepositStatus = "Held" },
        new() { Item = new CommunityItem { Name = "Moving dolly", Category = "Moving gear", Owner = "Chris Park", Icon = "📦", Deposit = 20, LateFeePerDay = 4 }, PickupDate = DateTime.Today.AddDays(-2), ReturnDate = DateTime.Today.AddDays(1), PickupTime = "11:00 AM", Stage = "Picked up", DepositStatus = "Held" }
    };

    public decimal MonthlySavings => Bookings.Count * 18m + 42m;

    public LoanBooking Reserve(CommunityItem item, DateTime pickup, DateTime returnDate, string time)
    {
        var booking = new LoanBooking
        {
            Item = item,
            PickupDate = pickup,
            ReturnDate = returnDate,
            PickupTime = time,
            Stage = "Reserved",
            DepositStatus = "Held"
        };
        Bookings.Insert(0, booking);
        item.IsAvailable = false;
        return booking;
    }

    public CommunityItem AddListing(string name, string category, string description, decimal deposit)
    {
        var item = new CommunityItem
        {
            Name = name,
            Category = category,
            Description = description,
            Owner = "You",
            Icon = category == "Yard work" ? "🌿" : category == "Event supplies" ? "🎉" : "🧰",
            Distance = "Your listing",
            Tags = "Community · Shared",
            Deposit = deposit,
            LateFeePerDay = Math.Max(1, deposit * 0.2m)
        };
        Items.Insert(0, item);
        return item;
    }
}
