using Serilog;
using System.Windows;

namespace SimplifiedProtocolTestWpfCore
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            const string LoggingTemplate =
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

            Log.Logger = new LoggerConfiguration()
                            //.MinimumLevel.Debug()
                            .WriteTo.Debug(outputTemplate: LoggingTemplate)
                            .CreateLogger();
        }
    }
}
