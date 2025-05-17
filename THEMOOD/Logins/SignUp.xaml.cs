using Firebase.Auth;
using THEMOOD.Services;
using System.Text.Json;

namespace THEMOOD.Logins;

public partial class SignUp : ContentPage
{
    private readonly FirebaseAuthService _authService;

    public SignUp()
    {
        InitializeComponent();
        _authService = new FirebaseAuthService();
        _ = ConnectivityService.Instance;

#if WINDOWS
		SignUpCard.Style = (Style)Resources["SignUpCardStyle"];
		SignUpCard.BackgroundColor = Colors.White;
		SignUpCard.HasShadow = true;
		SignUpCard.Padding = new Thickness(36, 32);
		SignUpCard.BorderColor = Colors.HotPink;
		// Center create text for Windows
		CreateTextContainer.HorizontalOptions = LayoutOptions.Center;
		CreateText.HorizontalOptions = LayoutOptions.Center;
		// Reduce space below create text for Windows
		CreateSpacer.HeightRequest = 20;
#else
        SignUpCard.Style = null;
        SignUpCard.BackgroundColor = Colors.Transparent;
        SignUpCard.HasShadow = false;
        SignUpCard.Padding = new Thickness(30, 100, 30, 30);
#endif
    }

    private async void OnSignUpClicked(object sender, EventArgs e)
    {
        try
        {
            // Get the values from the Entry fields
            string name = NameEntry.Text;
            string email = EmailEntry.Text;
            string password = PasswordEntry.Text;

            // Validate inputs
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Error", "Please fill in all fields", "OK");
                return;
            }

            // Check if terms and conditions are accepted
            if (!TermsCheckbox.IsChecked)
            {
                await DisplayAlert("Error", "Please accept the Terms and Conditions to continue", "OK");
                return;
            }

            // Create the user account
            var auth = await _authService.SignUp(email, password);
            string token = await _authService.GetFreshToken(auth);

            // Store user information with display name
            var user = auth.User;
            user.DisplayName = name;  // Set the display name
            UserService.Instance.SetCurrentUser(token, user);

            // Initialize MoodEntryService with user ID
            MoodEntryService.Instance.Initialize(auth.User.LocalId);

            await DisplayAlert("Success", $"Welcome {name}! Your account has been created.", "OK");
            await Shell.Current.GoToAsync("//main");
        }
        catch (FirebaseAuthException authEx)
        {
            string reason = ExtractReasonFromFirebaseError(authEx.ResponseData);
            await DisplayAlert("Sign Up Failed", $"{reason}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Sign Up Failed", $"An error occurred: {ex.Message}", "OK");
        }
    }

    private async void LoginBalik_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//login");
    }

    private string ExtractReasonFromFirebaseError(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return "No response data available";
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out JsonElement errorElement))
            {
                if (errorElement.TryGetProperty("message", out JsonElement messageElement))
                {
                    return messageElement.GetString();
                }

                if (errorElement.TryGetProperty("errors", out JsonElement errorsArray) &&
                    errorsArray.ValueKind == JsonValueKind.Array &&
                    errorsArray.GetArrayLength() > 0)
                {
                    var firstError = errorsArray[0];
                    if (firstError.TryGetProperty("reason", out JsonElement reasonElement))
                    {
                        return reasonElement.GetString();
                    }
                    else if (firstError.TryGetProperty("message", out JsonElement errorMessageElement))
                    {
                        return errorMessageElement.GetString();
                    }
                }
            }

            if (root.TryGetProperty("message", out JsonElement rootMessageElement))
            {
                return rootMessageElement.GetString();
            }
        }
        catch (JsonException jsonEx)
        {
            Console.WriteLine($"Error parsing JSON response: {jsonEx.Message}");
            return $"Invalid response format: {jsonEx.Message}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error processing response: {ex.Message}");
        }

        return "Unknown error";
    }

    private async void OnTermsTapped(object sender, EventArgs e)
    {
        await DisplayAlert("Terms and Conditions",
            "By using this app, you agree to:\n\n" +
            "1. Provide accurate and complete information\n" +
            "2. Maintain the security of your account\n" +
            "3. Use the app in accordance with all applicable laws\n" +
            "4. Respect other users' privacy and rights\n\n" +
            "Please read our full Terms and Conditions for more details.",
            "OK");
    }
}