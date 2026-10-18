using Microsoft.Extensions.DependencyInjection;
using ShareNest.Services;

namespace ShareNest;

public partial class App : Application
{
	public static ShareNestStore Store { get; private set; } = null!;

	public App(ShareNestStore store)
	{
		InitializeComponent();
		Store = store;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell(Store));
	}
}