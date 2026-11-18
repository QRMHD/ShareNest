using ShareNest.Services;

namespace ShareNest.Pages;

public partial class SplashPage : ContentPage
{
    bool _started;
    public SplashPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_started) return;
        _started = true;
        new Animation(v => Fill.WidthRequest = v, 0, 120).Commit(this, "load", 16, 2000, Easing.Linear);

        // open / create the SQLite database and restore the signed-in user while the splash is visible
        var init = Task.Run(async () => { await Db.GetAsync(); await Session.LoadAsync(); });
        await Task.WhenAll(init, Task.Delay(2300));
        await Shell.Current.GoToAsync("//catalog");     // Splash -> Catalog
    }
}
