using Serilog;
using SoundMetrics.Aris.Availability;
using SoundMetrics.Aris.Network;
using SoundMetrics.Aris.Threading;
using System;
using System.Reactive.Linq;
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
            {
                SynchronizationContext.SetSynchronizationContext(syncContext);

                using (var beaconListener = new BeaconListener())
                using (var _ =
                    beaconListener
                        .Beacons
                        .ObserveOn(syncContext)
                        .Subscribe(OnBeaconReceived))
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
            }

            Log.Information("Exiting.");
        }

        private static void OnBeaconReceived(ArisBeacon beacon)
        {
            var beaconType = beacon.GetType();
            string model;

            if (beaconType == typeof(ExplorerBeacon))
            {
                model = "Explorer";
            }
            else if (beaconType == typeof(VoyagerBeacon))
            {
                model = "Voyager";
            }
            else
            {
                throw new Exception($"Unexpected beacon type: {beaconType.Name}");
            }

            Log.Information("ARIS {model} {serialNumber} [{IPAddress}]",
                model, beacon.SerialNumber, beacon.IPAddress);
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
