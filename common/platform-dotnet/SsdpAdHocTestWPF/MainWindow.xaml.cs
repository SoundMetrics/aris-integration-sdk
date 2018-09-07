using SsdpAdHocTest.Model;
using System.Threading;
using System.Windows;

namespace SsdpAdHocTestWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new TheModel(SynchronizationContext.Current);
        }
    }
}
