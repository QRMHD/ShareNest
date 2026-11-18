using ShareNest.Services;

namespace ShareNest.ViewModels;

public class ProfileViewModel : BaseViewModel
{
    string _fullName = "Amir Hassan", _emailLine = "", _listingsText = "My listings";

    public string FullName { get => _fullName; private set => Set(ref _fullName, value); }
    public string EmailLine { get => _emailLine; private set => Set(ref _emailLine, value); }
    public string ListingsText { get => _listingsText; private set => Set(ref _listingsText, value); }

    public async Task LoadAsync()
    {
        var user = Session.User;
        FullName = string.IsNullOrWhiteSpace(user.FullName) ? "Amir Hassan" : user.FullName;
        EmailLine = $"{user.Email} · Block C, Unit 12";
        var mine = (await Db.GetItemsAsync()).Count(i => i.Owner == user.FullName);
        ListingsText = $"My listings ({mine})";
    }
}
