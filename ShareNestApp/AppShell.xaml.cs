namespace ShareNest;

public partial class AppShell : Shell
{
	public AppShell(Services.ShareNestStore store)
	{
		InitializeComponent();
		FlyoutBehavior = FlyoutBehavior.Disabled;
		Shell.SetNavBarIsVisible(this, false);

		var tabs = new TabBar();
		tabs.Items.Add(new ShellContent
		{
			Title = "Explore",
			Route = "ExploreTab",
			Icon = "",
			Content = new MainPage(store)
		});
		Items.Add(tabs);

		Routing.RegisterRoute(nameof(ItemDetailsPage), typeof(ItemDetailsPage));
	}
}
