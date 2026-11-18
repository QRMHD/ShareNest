using System.Security.Cryptography;
using System.Text;
using SQLite;
using ShareNest.Models;

namespace ShareNest.Services;

/// <summary>Local SQLite database (sqlite-net-pcl). Tables: Items, Bookings, History, Users, Settings.</summary>
public static class Db
{
    static SQLiteAsyncConnection? _conn;
    static readonly SemaphoreSlim Gate = new(1, 1);

    public static async Task<SQLiteAsyncConnection> GetAsync()
    {
        if (_conn is not null) return _conn;
        await Gate.WaitAsync();
        try
        {
            if (_conn is null)
            {
                var conn = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, "sharenest.db3"));
                await conn.CreateTableAsync<Item>();
                await conn.CreateTableAsync<Booking>();
                await conn.CreateTableAsync<HistoryEntry>();
                await conn.CreateTableAsync<AppUser>();
                await conn.CreateTableAsync<Setting>();
                await conn.CreateTableAsync<Payment>();
                await SeedAsync(conn);

                // Ensure neighbor items have distinct community owners for realistic sharing
                var ladder = await conn.Table<Item>().Where(i => i.Name == "Folding Ladder (6ft)").FirstOrDefaultAsync();
                if (ladder is not null)
                {
                    if (ladder.Owner == "Amir Hassan") ladder.Owner = "Sara Lim";
                    ladder.Status = "Available";
                    await conn.UpdateAsync(ladder);
                }

                // Ensure any old seed active booking for Folding Ladder is marked Returned so the item is genuinely available
                var ladderBookings = await conn.Table<Booking>().Where(b => b.ItemName == "Folding Ladder (6ft)" && b.Status == "Active").ToListAsync();
                foreach (var b in ladderBookings)
                {
                    b.Status = "Returned";
                    b.Detail = $"Returned {DateTime.Today.AddDays(-3):d MMM}";
                    await conn.UpdateAsync(b);
                }

                var tent = await conn.Table<Item>().Where(i => i.Name == "Camping Tent (4-person)").FirstOrDefaultAsync();
                if (tent is not null && tent.Owner == "Amir Hassan") { tent.Owner = "Daniel Tan"; await conn.UpdateAsync(tent); }
                var washer = await conn.Table<Item>().Where(i => i.Name == "Pressure Washer").FirstOrDefaultAsync();
                if (washer is not null && washer.Owner == "Amir Hassan") { washer.Owner = "David Lee"; await conn.UpdateAsync(washer); }

                _conn = conn;
            }
        }
        finally { Gate.Release(); }
        return _conn;
    }

    // ---------------------------------------------------------------- seed (first launch only)
    static async Task SeedAsync(SQLiteAsyncConnection c)
    {
        if (await c.Table<Item>().CountAsync() > 0) return;
        var t = DateTime.Today;

        // inserted in reverse so that "newest id first" shows them in the Figma order
        await c.InsertAllAsync(new[]
        {
            new Item { Name = "Pressure Washer", Category = "Yard", Deposit = 50, Status = "Borrowed", Owner = "David Lee", Summary = "1800 PSI • hose • spray tip", Specs = "Electric 1800 PSI, hose and spray tip included" },
            new Item { Name = "Camping Tent (4-person)", Category = "Event", Deposit = 30, Owner = "Daniel Tan", Summary = "4-person • waterproof • poles included", Specs = "Waterproof, poles and pegs included" },
            new Item { Name = "Folding Ladder (6ft)", Category = "Yard", Deposit = 15, Owner = "Sara Lim", Summary = "6 ft • 200 lb • compact fold", Specs = "Aluminium, folds flat, supports 200 lb" },
            new Item { Name = "Cordless Drill", Category = "Tools", Deposit = 20, Owner = "Amir Hassan", Photo = "photo_drill.png", Summary = "18V • 1/2\" chuck • 2 batts", Specs = "Battery-powered, 2 bits included" },
        });

        await c.InsertAllAsync(new[]
        {
            new Booking { ItemName = "Camping Tent (4-person)", IconFile = "ic_tent.png", Detail = $"Returned {t.AddDays(-15):d MMM}", Status = "Returned", Deposit = 30,
                          ReservedOn = t.AddDays(-20), PickupDate = t.AddDays(-18), ReturnDate = t.AddDays(-15) },
            new Booking { ItemName = "Pressure Washer", IconFile = "ic_unavailable.png", Detail = "Reserved for Oct 8–10", Status = "Reserved", Deposit = 50,
                          ReservedOn = t.AddDays(-1), PickupDate = t.AddDays(5), ReturnDate = t.AddDays(7) },
            new Booking { ItemName = "Folding Ladder (6ft)", IconFile = "ic_ladder.png", Detail = $"Returned {t.AddDays(-3):d MMM}", Status = "Returned", Deposit = 15,
                          ReservedOn = t.AddDays(-9), PickupDate = t.AddDays(-7), ReturnDate = t.AddDays(-3) },
            new Booking { ItemName = "Cordless Drill", IconFile = "ic_drill.png", Detail = "Due back tomorrow", Status = "Active", Deposit = 20,
                          ReservedOn = t.AddDays(-4), PickupDate = t.AddDays(-2), ReturnDate = t.AddDays(1) },
        });

        await c.InsertAllAsync(new[]
        {
            new Booking { ItemName = "Cordless Drill", IconFile = "ic_drill.png", Detail = "Requested by Sara Lim · starts in 3 days", Status = "Request", Deposit = 20, IsLending = true,
                          ReservedOn = t, PickupDate = t.AddDays(3), ReturnDate = t.AddDays(5) },
            new Booking { ItemName = "Camping Tent (4-person)", IconFile = "ic_tent.png", Detail = "Lent to Daniel Tan · due in 2 days", Status = "Lent", Deposit = 30, IsLending = true,
                          ReservedOn = t.AddDays(-5), PickupDate = t.AddDays(-3), ReturnDate = t.AddDays(2) },
        });

        await c.InsertAsync(new HistoryEntry { Text = "Borrowed “Ladder” — 12 Sep", CreatedOn = t.AddDays(-30) });
        await c.InsertAsync(new AppUser { FullName = "Amir Hassan", Email = "amir.h@email.com", PasswordHash = Hash("amir.h@email.com", "sharenest123") });
        await c.InsertOrReplaceAsync(new Setting { Key = "loan_count", Value = "12" });
        await c.InsertOrReplaceAsync(new Setting { Key = "current_user", Value = "amir.h@email.com" });
    }

    // ---------------------------------------------------------------- items
    public static async Task<List<Item>> GetItemsAsync() =>
        await (await GetAsync()).Table<Item>().OrderByDescending(i => i.Id).ToListAsync();

    public static async Task<Item?> FindItemAsync(string name) =>
        await (await GetAsync()).Table<Item>().Where(i => i.Name == name).FirstOrDefaultAsync();

    public static async Task AddItemAsync(Item item) => await (await GetAsync()).InsertAsync(item);
    public static async Task UpdateItemAsync(Item item) => await (await GetAsync()).UpdateAsync(item);
    public static async Task DeleteItemAsync(int id) => await (await GetAsync()).DeleteAsync<Item>(id);

    // ---------------------------------------------------------------- bookings
    public static async Task<List<Booking>> GetBookingsAsync(bool lending) =>
        await (await GetAsync()).Table<Booking>().Where(b => b.IsLending == lending).OrderByDescending(b => b.Id).ToListAsync();

    public static async Task<List<Booking>> GetActiveBookingsForItemAsync(string itemName) =>
        await (await GetAsync()).Table<Booking>()
            .Where(b => b.ItemName == itemName && b.IsLending == false && b.Status != "Returned").ToListAsync();

    public static async Task AddBookingAsync(Booking b) => await (await GetAsync()).InsertAsync(b);
    public static async Task UpdateBookingAsync(Booking b) => await (await GetAsync()).UpdateAsync(b);

    // ---------------------------------------------------------------- history
    public static async Task<List<HistoryEntry>> GetHistoryAsync(int take) =>
        await (await GetAsync()).Table<HistoryEntry>().OrderByDescending(h => h.Id).Take(take).ToListAsync();

    public static async Task AddHistoryAsync(string text) =>
        await (await GetAsync()).InsertAsync(new HistoryEntry { Text = text });

    // ---------------------------------------------------------------- settings
    public static async Task<string?> GetSettingAsync(string key) =>
        (await (await GetAsync()).FindAsync<Setting>(key))?.Value;

    public static async Task SetSettingAsync(string key, string value) =>
        await (await GetAsync()).InsertOrReplaceAsync(new Setting { Key = key, Value = value });

    public static async Task<int> GetIntAsync(string key, int fallback) =>
        int.TryParse(await GetSettingAsync(key), out var v) ? v : fallback;

    public static Task SetIntAsync(string key, int value) => SetSettingAsync(key, value.ToString());

    public static async Task<bool> GetBoolAsync(string key, bool fallback)
    {
        var v = await GetSettingAsync(key);
        return v is null ? fallback : v == "1";
    }

    public static Task SetBoolAsync(string key, bool value) => SetSettingAsync(key, value ? "1" : "0");

    // ---------------------------------------------------------------- users
    public static string Hash(string email, string password) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant() + ":" + password)));

    public static async Task<AppUser?> FindUserAsync(string email)
    {
        var e = email.Trim().ToLowerInvariant();
        return await (await GetAsync()).Table<AppUser>().Where(u => u.Email == e).FirstOrDefaultAsync();
    }

    public static async Task<AppUser?> LoginAsync(string email, string password)
    {
        var e = email.Trim().ToLowerInvariant();
        var h = Hash(e, password);
        return await (await GetAsync()).Table<AppUser>().Where(u => u.Email == e && u.PasswordHash == h).FirstOrDefaultAsync();
    }

    /// <summary>Returns (error, user). error is null when the account was created.</summary>
    public static async Task<(string? Error, AppUser? User)> RegisterAsync(string fullName, string email, string password)
    {
        var e = email.Trim().ToLowerInvariant();
        if (await FindUserAsync(e) is not null) return ("An account with this email already exists.", null);
        var user = new AppUser { FullName = fullName.Trim(), Email = e, PasswordHash = Hash(e, password) };
        await (await GetAsync()).InsertAsync(user);
        return (null, user);
    }

    // ---------------------------------------------------------------- payments
    public static async Task InsertPaymentAsync(Payment p) => await (await GetAsync()).InsertAsync(p);

    public static async Task UpdatePaymentAsync(Payment p)
    {
        p.UpdatedAt = DateTime.UtcNow;
        await (await GetAsync()).UpdateAsync(p);
    }

    public static async Task<Payment?> GetPaymentByBookingAsync(int bookingId) =>
        await (await GetAsync()).Table<Payment>().Where(p => p.BookingId == bookingId).FirstOrDefaultAsync();

    public static async Task<List<Payment>> GetAllPaymentsAsync() =>
        await (await GetAsync()).Table<Payment>().OrderByDescending(p => p.Id).ToListAsync();

    // ---------------------------------------------------------------- reset demo data
    public static async Task ResetDemoDataAsync()
    {
        var conn = await GetAsync();
        await conn.DeleteAllAsync<Payment>();
        await conn.DeleteAllAsync<Booking>();
        await conn.DeleteAllAsync<Item>();
        await conn.DeleteAllAsync<HistoryEntry>();
        await SeedAsync(conn);
    }
}
