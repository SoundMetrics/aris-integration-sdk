using Serilog;
using System.Threading;
using System.Windows;

namespace SsdpAdHocTestWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        const string LoggingTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Thread.CurrentThread.Name = "UI thread";

            Log.Logger = (new LoggerConfiguration())
                    .MinimumLevel.Debug()
                    .WriteTo.Trace(outputTemplate: LoggingTemplate)
                    .CreateLogger();
            Log.Information("Logging initialized.");
        }
    }
}
