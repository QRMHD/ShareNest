using System.Windows.Input;
using ShareNest.Services;

namespace ShareNest.ViewModels;

public class LoginViewModel : BaseViewModel
{
    string _fullName = "", _email = "", _password = "", _errorText = "";
    bool _register;

    public string FullName { get => _fullName; set => Set(ref _fullName, value ?? ""); }
    public string Email { get => _email; set => Set(ref _email, value ?? ""); }
    public string Password { get => _password; set => Set(ref _password, value ?? ""); }

    public bool IsRegister
    {
        get => _register;
        private set
        {
            if (!Set(ref _register, value)) return;
            Raise(nameof(SubmitText)); Raise(nameof(SwitchText)); Raise(nameof(ShowForgot));
        }
    }
    public string SubmitText => _register ? "Create account" : "Log in";
    public string SwitchText => _register ? "Already have an account? Log in" : "New here? Register";
    public bool ShowForgot => !_register;

    public string ErrorText { get => _errorText; private set { if (Set(ref _errorText, value)) Raise(nameof(HasError)); } }
    public bool HasError => _errorText.Length > 0;

    public ICommand SubmitCommand { get; }
    public ICommand SwitchCommand { get; }
    public ICommand ForgotCommand { get; }

    public LoginViewModel()
    {
        SubmitCommand = new Command(async () => await Submit());
        SwitchCommand = new Command(() => { IsRegister = !IsRegister; ErrorText = ""; });
        ForgotCommand = new Command(async () => await Forgot());
    }

    async Task Submit()
    {
        var email = Email.Trim();
        string? error = null;
        if (IsRegister && string.IsNullOrWhiteSpace(FullName)) error = "Please enter your name.";
        else if (!email.Contains('@') || !email.Contains('.')) error = "Enter a valid email address.";
        else if (Password.Length < 6) error = "Password must be at least 6 characters.";

        if (error is null)
        {
            if (IsRegister)
            {
                var (err, user) = await Db.RegisterAsync(FullName, email, Password);      // INSERT user
                error = err;
                if (user is not null) { await Session.SetUserAsync(user); await Shell.Current.DisplayAlert("Welcome to ShareNest", "Your account was created.", "OK"); }
            }
            else
            {
                var user = await Db.LoginAsync(email, Password);                           // SELECT user
                if (user is null) error = "Wrong email or password.";
                else await Session.SetUserAsync(user);
            }
        }

        ErrorText = error ?? "";
        if (error is null) await Shell.Current.GoToAsync("//catalog");
    }

    async Task Forgot()
    {
        var mail = await Shell.Current.DisplayPromptAsync("Reset password", "Enter your email and we’ll send a reset link.",
            "Send", "Cancel", "you@example.com", keyboard: Keyboard.Email);
        if (!string.IsNullOrWhiteSpace(mail))
            await Shell.Current.DisplayAlert("Check your inbox", $"A reset link was sent to {mail.Trim()}.", "OK");
    }
}
