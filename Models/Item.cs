using SQLite;
using ShareNest.Services;

namespace ShareNest.Models;

[Table("Items")]
public class Item
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "Tools";          // Tools / Yard / Event / Moving
    public string Owner { get; set; } = "Amir Hassan";
    public decimal Deposit { get; set; }
    public string Status { get; set; } = "Available";        // Available / Borrowed
    public string Summary { get; set; } = "";
    public string Specs { get; set; } = "";
    public string? Photo { get; set; }                       // bundled image, e.g. photo_drill.png
    public string? PhotoPath { get; set; }                   // photo picked by the user

    // ---- computed (get-only => not stored in the database) ----
    public bool IsBorrowed => Status == "Borrowed";
    public bool IsReserved => Status == "Reserved";
    public bool IsUnavailable => IsBorrowed || IsReserved;
    public string DepositText => $"Deposit RM {Deposit:0}";
    public string StatusText => Status switch
    {
        "Borrowed" => "Borrowed",
        "Reserved" => "Reserved",
        _ => "Available"
    };
    public Brush StatusBrush => new SolidColorBrush(StatusColor);
    public Color StatusTextColor => Status switch
    {
        "Borrowed" => Color.FromArgb("#55655F"),
        "Reserved" => Color.FromArgb("#8B6914"),
        _ => Color.FromArgb("#0F6F78")
    };
    public Color StatusColor => Status switch
    {
        "Borrowed" => Color.FromArgb("#98A8A3"),
        "Reserved" => Color.FromArgb("#E8B84B"),
        _ => Color.FromArgb("#3FC1C9")
    };

    public bool IsMine => string.Equals(Owner, Session.User?.FullName, StringComparison.OrdinalIgnoreCase);
    public string OwnerSubtitle => IsMine ? "Listed by you" : $"Lender: {Owner}";

    public string IconFile => IsUnavailable ? "ic_unavailable.png" : Category switch
    {
        "Yard" => "ic_ladder.png",
        "Event" => "ic_tent.png",
        "Moving" => "ic_box.png",
        _ => "ic_drill.png"
    };

    public ImageSource ThumbnailSource
    {
        get
        {
            if (!string.IsNullOrEmpty(PhotoPath) && File.Exists(PhotoPath))
                return ImageSource.FromFile(PhotoPath);
            if (!string.IsNullOrEmpty(Photo))
                return Photo;
            return IconFile;
        }
    }

    public Aspect ThumbnailAspect => (!string.IsNullOrEmpty(PhotoPath) || !string.IsNullOrEmpty(Photo)) 
        ? Aspect.AspectFill 
        : Aspect.AspectFit;
}
