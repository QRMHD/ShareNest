using System;
using SQLite;

namespace ShareNest.Models
{
    public enum PaymentStatus
    {
        Held,
        Paid,
        Refunded,
        Declined,
        Deducted
    }

    public enum PaymentMethod
    {
        Card,
        FPX,
        DuitNowQR,
        CashOnPickup
    }

    [Table("Payments")]
    public class Payment
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Foreign key to Booking
        public int BookingId { get; set; }

        public string ItemName { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public PaymentMethod Method { get; set; }

        public string ProviderOrBank { get; set; } = string.Empty;

        public string Reference { get; set; } = string.Empty;

        public string ReceiptNumber { get; set; } = string.Empty;

        public PaymentStatus Status { get; set; } = PaymentStatus.Held;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Display helpers
        [Ignore]
        public string MethodDisplayName => Method switch
        {
            PaymentMethod.DuitNowQR => $"DuitNow QR {(string.IsNullOrEmpty(ProviderOrBank) ? "" : $"({ProviderOrBank})")}",
            PaymentMethod.FPX => $"Online Banking FPX ({ProviderOrBank})",
            PaymentMethod.Card => $"Debit/Credit Card ({ProviderOrBank})",
            _ => "Cash on Pickup"
        };

        [Ignore]
        public string StatusDisplayName => Status switch
        {
            PaymentStatus.Held => "🔒 Held in Escrow",
            PaymentStatus.Refunded => "🟢 100% Refunded",
            PaymentStatus.Deducted => "⚠️ Refunded with Deduction",
            PaymentStatus.Paid => "✓ Paid",
            PaymentStatus.Declined => "❌ Declined",
            _ => Status.ToString()
        };
    }
}
