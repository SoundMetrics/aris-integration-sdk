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
            Console.WriteLine("Watching for beacons...");

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

            Console.WriteLine("Exiting.");
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

            Console.WriteLine($"ARIS {model} {beacon.SerialNumber} [{beacon.IPAddress}]");
        }
    }
}
