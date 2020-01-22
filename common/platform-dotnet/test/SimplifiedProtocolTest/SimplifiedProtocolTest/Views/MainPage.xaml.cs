using System;

using SimplifiedProtocolTest.ViewModels;

using Windows.UI.Xaml.Controls;

namespace SimplifiedProtocolTest.Views
{
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();

        public MainPage()
        {
            InitializeComponent();
        }
    }
}
