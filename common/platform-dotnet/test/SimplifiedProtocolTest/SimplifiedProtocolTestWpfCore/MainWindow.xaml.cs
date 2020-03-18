using System.Windows;

namespace SimplifiedProtocolTestWpfCore
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; } = new MainViewModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        private void Feedback_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            FeedbackText.ScrollToEnd();
        }
    }
}
