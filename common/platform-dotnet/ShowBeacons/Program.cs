using SoundMetrics.Aris.Network;
using SoundMetrics.Aris.Threading;
using System;
using System.Net;
using System.Reactive.Linq;
using System.Threading;

namespace ShowBeacons
{
    class Program
    {
        static void Main(string[] args)
        {
            var targetSerialNumber = "24";
            var findTimeout = TimeSpan.FromSeconds(5);

            Console.WriteLine($"Watching for ARIS {targetSerialNumber}...");
            if (FindAris(targetSerialNumber, findTimeout, out var ipAddress))
            {
                Console.WriteLine($"ARIS {targetSerialNumber} [{ipAddress}]");
            }
            else
            {
                Console.WriteLine($"Could not find ARIS {targetSerialNumber}");
            }

            Console.WriteLine("Exiting.");
        }

        private static bool FindAris(
            string targetSerialNumber,
            TimeSpan timeout,
            out IPAddress ipAddress)
        {
            IPAddress foundAddress = null;

            using (var doneSignal = new ManualResetEventSlim(false))
            using (var cts = new CancellationTokenSource())
            using (var syncContext = QueuedSynchronizationContext.RunOnAThread(cts.Token))
            {
                SynchronizationContext.SetSynchronizationContext(syncContext);

                using (var beaconListener = new BeaconListener())
                using (var _ =
                    beaconListener
                        .Beacons
                        .ObserveOn(syncContext)
                        .Subscribe(beacon =>
                        {
                            if (beacon.SerialNumber == targetSerialNumber)
                            {
                                foundAddress = beacon.IPAddress;
                                doneSignal.Set();
                            }
                        }))
                {
                    doneSignal.Wait(timeout);
                }
            }

            ipAddress = foundAddress;
            return !(ipAddress is null);
        }
    }
}
