using ShareNest.Pages;

namespace ShareNest;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pushed pages = arrows of the Part B screens map
        Routing.RegisterRoute("details", typeof(ItemDetailsPage));    // Catalog -> Item Details
        Routing.RegisterRoute("reserve", typeof(ReservePage));        // Item Details -> Reserve
        Routing.RegisterRoute("add", typeof(AddListingPage));         // Catalog FAB -> Add Listing
        Routing.RegisterRoute("handover", typeof(HandoverLogPage));   // Bookings -> Handover Log
        Routing.RegisterRoute("payment", typeof(PaymentPage));        // Reserve -> Payment
        Routing.RegisterRoute("receipt", typeof(ReceiptPage));        // Payment -> Receipt
        Routing.RegisterRoute("deposit", typeof(DepositTrustPage));   // Payment / Handover -> Deposit & Trust
        Routing.RegisterRoute("login", typeof(LoginPage));            // Profile -> Login / Register
        Routing.RegisterRoute("about", typeof(AboutPage));            // Profile -> About
    }
}
