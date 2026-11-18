using ShareNest.Models;

namespace ShareNest.Services;

/// <summary>The signed-in user (restored from the database on start-up).</summary>
public static class Session
{
    public static AppUser User { get; private set; } = new() { FullName = "Amir Hassan", Email = "amir.h@email.com" };

    public static async Task LoadAsync()
    {
        var email = await Db.GetSettingAsync("current_user");
        if (email is null) return;
        var user = await Db.FindUserAsync(email);
        if (user is not null) User = user;
    }

    public static async Task SetUserAsync(AppUser user)
    {
        User = user;
        await Db.SetSettingAsync("current_user", user.Email);
    }
}
