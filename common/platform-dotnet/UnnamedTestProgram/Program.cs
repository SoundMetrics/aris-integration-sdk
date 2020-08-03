using Serilog;
using SoundMetrics.Aris;
using SoundMetrics.Aris.Threading;
using System;
using System.Threading;

namespace UnnamedTestProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigureLogger();

            Log.Information("Watching for beacons...");

            using (var cts = new CancellationTokenSource())
            using (var syncContext = QueuedSynchronizationContext.RunOnAThread(cts))
            using (var conduit = new ArisConduit("24", syncContext))
            {
                SynchronizationContext.SetSynchronizationContext(syncContext);

                Thread.Sleep(TimeSpan.FromSeconds(1265));
            }

            Log.Information("Exiting.");
        }

        private static void ConfigureLogger()
        {
            const string loggingTemplate =
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
            Log.Logger = new LoggerConfiguration()
                            .WriteTo.Console(outputTemplate: loggingTemplate)
                            .CreateLogger();
        }
    }
}
