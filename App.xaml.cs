namespace ShareNest;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
#if WINDOWS || MACCATALYST
        window.Title = "ShareNest";
        window.Width = 420;      // phone-like window on desktop
        window.Height = 860;
#endif
        return window;
    }
}
