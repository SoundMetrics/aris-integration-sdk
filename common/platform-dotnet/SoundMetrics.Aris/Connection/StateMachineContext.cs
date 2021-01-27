using SoundMetrics.Aris.Core;
using System;
using System.Net;

namespace SoundMetrics.Aris.Connection
{
    internal sealed class StateMachineContext : IDisposable
    {
        /// <summary>
        /// The IPAddress of the connected device.
        /// </summary>
        public IPAddress? DeviceAddress { get; set; }

        /// <summary>
        /// The UDP port on which we wish to receive frames.
        /// </summary>
        public int? ReceiverPort { get; set;  }

        /// <summary>
        /// The salinity of the surrounding water.
        /// </summary>
        public Salinity Salinity { get; }

        public ApplySettingsRequest? LatestSettingsRequest { get; set; }

        public DateTimeOffset LatestFramePartTimestamp { get; set; }

        /// <summary>
        /// The command connection; may be null when not connected.
        /// </summary>
        public CommandConnection? CommandConnection { get; set; }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    CommandConnection?.Dispose();
                }

                // no unmanaged resources
                disposed = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private bool disposed;
    }
}
