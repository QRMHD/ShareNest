namespace ShareNest.Models;

public sealed class CommunityItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Icon { get; set; } = "🧰";
    public string Distance { get; set; } = "Nearby";
    public string Tags { get; set; } = string.Empty;
    public double Rating { get; set; } = 5.0;
    public decimal Deposit { get; set; }
    public decimal LateFeePerDay { get; set; }
    public bool IsAvailable { get; set; } = true;
}

public sealed class LoanBooking
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public CommunityItem Item { get; init; } = new();
    public DateTime PickupDate { get; init; }
    public DateTime ReturnDate { get; init; }
    public string PickupTime { get; init; } = "10:00 AM";
    public string Stage { get; set; } = "Reserved";
    public string DepositStatus { get; set; } = "Held";
    public string ConditionNotes { get; set; } = string.Empty;
}
