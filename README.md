# ShareNest – .NET MAUI app (frontend, built from the Figma design)

A complete, standalone project: all 12 screens, navigation from the screens map, app icon, splash screen.
Data is stored in a local **SQLite** database (`sharenest.db3` in the app data folder) – it survives closing the app.

## Run it
1. Needs **.NET 10 SDK** + the MAUI workload (Visual Studio 2026 / 2022 with ".NET MAUI"). Targets: Android + Windows (iOS/Mac only when built on a Mac).
2. Open `ShareNest.csproj` in Visual Studio, pick **Android Emulator** (or Windows Machine) and press ▶.
   CLI: `dotnet build -t:Run -f net10.0-android`  or  `dotnet build -t:Run -f net10.0-windows10.0.19041.0`

## Screens → files
| Screen (Figma frame) | File |
|---|---|
| splash_screen | Pages/SplashPage |
| home_catalog | Pages/CatalogPage |
| item_details | Pages/ItemDetailsPage |
| reserve_item | Pages/ReservePage |
| add_listing | Pages/AddListingPage |
| my_bookings | Pages/MyBookingsPage |
| handover_log | Pages/HandoverLogPage |
| payment_escrow | Pages/PaymentPage |
| payment_receipt | Pages/ReceiptPage |
| deposit_trust | Pages/DepositTrustPage |
| activity_dashboard | Pages/ActivityPage |
| profile | Pages/ProfilePage |
| login_register | Pages/LoginPage |
| about_page | Pages/AboutPage |

Navigation (Part B map): Splash → Catalog · Catalog → Item Details → Reserve → Deposit & Trust · Catalog "+" → Add Listing ·
Bookings → Handover Log → Deposit & Trust · Profile → Login / About. Bottom bar switches Catalog / Bookings / Activity / Profile.

## What works
- Catalog: live search, category chips, filter/sort sheet, borrowed items disabled, new listings appear instantly.
- Reserve: real month calendar (pick pickup → return, booked days blocked), pickup-time picker, booking is saved.
- Handover: pickup/return steps, camera/gallery photos, both-parties confirmation, updates booking status + deposit.
- Deposit & Trust: timeline follows the booking state, tap-to-rate stars, report an issue.
- Add Listing: photo picker (up to 4), validation, publish → shows in Catalog.
- Login/Register with validation, Profile actions, About page.

## Design notes
Palette `#364F6B / #3FC1C9 / #F5F5F5 / #FC5185` lives in `App.xaml`. Shared controls: `Controls/AppBar.cs`, `Controls/BottomNav.cs`.
- The drill photo (`Resources/Images/photo_drill.svg`) and avatar are placeholder illustrations – drop your own PNG/JPG with the
  same name (`photo_drill.png`, `avatar_amir.png`) into `Resources/Images` to use the real pictures from Figma.
- To use the Inter font from the design, put `Inter-Regular.ttf` / `Inter-Bold.ttf` in `Resources/Fonts`, register them in
  `MauiProgram` with `builder.ConfigureFonts(...)` and set `FontFamily` in `App.xaml` styles.

## Database (SQLite – sqlite-net-pcl)
`Services/Db.cs` creates the file and 5 tables on first launch and inserts demo data:

| Table | Model | Used by |
|---|---|---|
| Items | Models/Item.cs | Catalog, Item Details, Add Listing, Profile |
| Bookings | Models/Booking.cs | Reserve, My Bookings, Handover, Deposit & Trust, Activity |
| History | Models/Records.cs | Activity (history list) |
| Users | Models/Records.cs | Login / Register (password stored as SHA-256 hash) |
| Settings | Models/Records.cs | loan counter, signed-in user, notification switches |

Demo login: **amir.h@email.com / sharenest123** (or register a new account).
To reset the data: uninstall the app (or clear its storage) – the database is re-created.

## Data binding (MVVM)
ViewModels live in `ViewModels/`; pages set `BindingContext` and use `{Binding ...}` with compiled bindings (`x:DataType`):
Catalog (search, chips, list), My Bookings, Activity, Add Listing (two-way form), Login/Register, Profile.
Reserve, Handover and Deposit use code-behind because of the calendar / camera logic, but still read and write the database.

## Data manipulation / feedback
- Reserve blocks days already booked (read from the Bookings table).
- Handover updates the booking status, the item status (Available <-> Borrowed), the loan counter and the history list.
- Activity numbers are calculated from the tables; Deposit & Trust saves the neighbour rating.
