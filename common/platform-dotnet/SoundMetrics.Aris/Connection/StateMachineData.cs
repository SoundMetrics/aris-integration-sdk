﻿using SoundMetrics.Aris.Device;
using System;
using System.Net;

namespace SoundMetrics.Aris.Connection
{
    internal sealed class StateMachineData : IDisposable
    {
        public StateMachineData(IPAddress deviceAddress)
        {
            DeviceAddress = deviceAddress;
        }

        /// <summary>
        /// The IPAddress of the connected device.
        /// </summary>
        public IPAddress DeviceAddress { get; }

        /// <summary>
        /// The UDP port on which we wish to receive frames.
        /// </summary>
        public int ReceiverPort { get; }

        /// <summary>
        /// The salinity of the surrounding water.
        /// </summary>
        public Salinity Salinity { get; }

        /// <summary>
        /// The command connection; may be null when not connected.
        /// </summary>
        public CommandConnection CommandConnection { get; set; }

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
