using System.Text.Json;
using Firebase.Auth;
using THEMOOD.Services;
using THEMOOD.ViewModels;
using THEMOOD.Pages;
namespace THEMOOD.Logins;
public partial class Login : ContentPage
{
    private readonly FirebaseAuthService _authService;
    public Login()
    {
        InitializeComponent();
        _authService = new FirebaseAuthService();
        _ = ConnectivityService.Instance;

#if WINDOWS
        LoginCard.Style = (Style)Resources["LoginCardStyle"];
        LoginCard.BackgroundColor = Colors.White;
        LoginCard.HasShadow = true;
        LoginCard.Padding = new Thickness(36, 32);
        LoginCard.BorderColor = Colors.HotPink;
        // Center welcome text for Windows
        WelcomeTextContainer.HorizontalOptions = LayoutOptions.Center;
        WelcomeText.HorizontalOptions = LayoutOptions.Center;
        // Reduce space below welcome text for Windows
        WelcomeSpacer.HeightRequest = 20;
#else
        LoginCard.Style = null;
        LoginCard.BackgroundColor = Colors.Transparent;
        LoginCard.HasShadow = false;
        LoginCard.Padding = new Thickness(30, 100, 30, 30);
#endif
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            var auth = await _authService.SignIn(Email.Text, Password.Text);
            string token = await _authService.GetFreshToken(auth);
            
            // Store user information
            UserService.Instance.SetCurrentUser(token, auth.User);
            
            // Initialize MoodEntryService with user ID
            MoodEntryService.Instance.Initialize(auth.User.LocalId);
            
            await DisplayAlert("Success", $"Welcome {auth.User.Email}", "OK");
            
            // Create a new Chat view instance
            var chatView = new Chat();
            
            // Set the Chat view as the main content using the NavBarViewModel instance
            if (NavBarViewModel.Instance.NavigateToChatCommand.CanExecute(null))
            {
                await NavBarViewModel.Instance.NavigateToChatCommand.ExecuteAsync(null);
            }

            NavBarViewModel.SetMainPageContent?.Invoke(chatView);

            // Navigate to main page
            await Shell.Current.GoToAsync("//main");
        }
        catch (FirebaseAuthException authEx)
        {
            // Parse the error response from Firebase
            string reason = ExtractReasonFromFirebaseError(authEx.ResponseData);
            await DisplayAlert("Login Failed", $"{reason}", "OK");
        }
        catch (Exception ex)
        {
            // Generic error handling for non-Firebase exceptions
            await DisplayAlert("Login Failed", $"An error occurred: {ex.Message}", "OK");
        }
    }

    private async void SignUp_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//signup");
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

            // Check for the Firebase error structure
            if (root.TryGetProperty("error", out JsonElement errorElement))
            {
                // First, try to get the message directly if available
                if (errorElement.TryGetProperty("message", out JsonElement messageElement))
                {
                    return messageElement.GetString();
                }

                // Otherwise, look for the first error's reason
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

            // Handle the possibility of a different structure
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
}