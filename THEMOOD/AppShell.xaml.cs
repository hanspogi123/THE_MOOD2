using THEMOOD.Pages;    
using THEMOOD.Logins;
using System.Diagnostics;

namespace THEMOOD
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Debug.WriteLine("AppShell initializing...");

            // Register routes
            Routing.RegisterRoute("main", typeof(MainPage));
            Routing.RegisterRoute("login", typeof(Logins.Login));
            Routing.RegisterRoute("signup", typeof(Logins.SignUp));
            Routing.RegisterRoute("moodentry", typeof(Pages.MoodEntryPage));
            Routing.RegisterRoute("game", typeof(Pages.Game));
            Routing.RegisterRoute("home", typeof(Pages.Home));

            Debug.WriteLine("Routes registered");
            Shell.SetNavBarIsVisible(this, false);
        }
    }
}