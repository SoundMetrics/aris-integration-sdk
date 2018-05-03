using System.Windows;
using Serilog;

namespace WpfTestBench
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ConfigureLogging();
        }

        private static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                            .WriteTo.Trace(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                            .CreateLogger();
        }
    }
}
