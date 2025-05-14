using Microsoft.Maui.Controls;

namespace THEMOOD.Controls
{
    public partial class LoadingIndicator : ContentView
    {
        public static readonly BindableProperty LoadingTextProperty = BindableProperty.Create(
            nameof(LoadingText),
            typeof(string),
            typeof(LoadingIndicator),
            "Loading...");

        public string LoadingText
        {
            get => (string)GetValue(LoadingTextProperty);
            set => SetValue(LoadingTextProperty, value);
        }

        public LoadingIndicator()
        {
            InitializeComponent();
        }
    }
} 