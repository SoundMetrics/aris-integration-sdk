using SimplifiedProtocolTest.Helpers;
using SoundMetrics.Aris.Headers;
using SoundMetrics.Aris.SimplifiedProtocol;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SimplifiedProtocolTestWpfCore
{
    public class MainViewModel : SimplifiedProtocolTest.Helpers.Observable
    {
        public MainViewModel()
        {
            ConnectCommand = new RelayCommand(OnConnect);
            StartTestPatternCommand = new RelayCommand(
                () => Connection.StartTestPattern(),
                () => Connection != null);
            StartPassiveModeCommand = new RelayCommand(
                () => Connection.StartPassiveMode(),
                () => Connection != null);
            StartDefaultAcquireCommand = new RelayCommand(
                () => Connection.StartDefaultAcquireMode(),
                () => Connection != null);
        }

        public RelayCommand ConnectCommand { get; private set; }
        public RelayCommand StartTestPatternCommand { get; private set; }
        public RelayCommand StartPassiveModeCommand { get; private set; }
        public RelayCommand StartDefaultAcquireCommand { get; private set; }

        private bool canConnect = true;
        public bool CanConnect
        {
            get { return canConnect; }
            set { Set(ref canConnect, value); }
        }

        private bool isConnected;
        public bool IsConnected
        {
            get { return isConnected; }
            set { Set(ref isConnected, value); }
        }


        private string hostname = "192.168.10.155";

        public string Hostname
        {
            get { return hostname; }
            set { Set(ref hostname, value); }
        }

        private uint frameIndex;

        public uint FrameIndex
        {
            get { return frameIndex; }
            private set { Set(ref frameIndex, value); }
        }

        private WriteableBitmap frameBitmap;

        public WriteableBitmap FrameBitmap
        {
            get { return frameBitmap; }
            private set { Set(ref frameBitmap, value); }
        }



        private ConnectionModel connection;

        public ConnectionModel Connection
        {
            get { return connection; }
            set
            {
                Set(ref connection, value);
                StartTestPatternCommand.OnCanExecuteChanged();
                StartPassiveModeCommand.OnCanExecuteChanged();
                StartDefaultAcquireCommand.OnCanExecuteChanged();
            }
        }



        private void OnConnect()
        {
            try
            {
                Connection = new ConnectionModel(hostname);
                CanConnect = false;
                IsConnected = true;
                Connection.Frames
                    .ObserveOn(SynchronizationContext.Current)
                    .Subscribe(OnFrame);
            }
            catch (SocketException e)
            {
                Debug.WriteLine($"SocketException: {e.Message}");
            }
        }

        private void OnFrame(Frame frame)
        {
            FrameIndex = frame.Header.FrameIndex;
            FrameBitmap = LoadBitmap(FrameBitmap);

            // The render thread may be using while we're here
            // on the UI thread.
            if (FrameBitmap.TryLock(bufferLockTimeout))
            {
                PaintMe(FrameBitmap);
                FrameBitmap.Unlock();
            }

            void PaintMe(WriteableBitmap writeableBitmap)
            {
                unsafe
                {
                    // Get a pointer to the back buffer.
                    IntPtr pBackBuffer = writeableBitmap.BackBuffer;

                    for (int row = 0; row < 16; ++row)
                    {
                        for (int column = 0; row < 16; ++row)
                        {
                            // Find the address of the pixel to draw.
                            pBackBuffer += row * writeableBitmap.BackBufferStride;
                            pBackBuffer += column * 4;

                            // Compute the pixel's color.
                            int color_data = 255 << 16; // R
                            color_data |= 128 << 8;   // G
                            color_data |= 255 << 0;   // B

                            // Assign the color data to the pixel.
                            *((int*)pBackBuffer) = color_data;
                        }
                    }
                }

                // Specify the area of the bitmap that changed.
                writeableBitmap.AddDirtyRect(
                    new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight));
            }

            WriteableBitmap LoadBitmap(WriteableBitmap existing)
            {
                var width = (int)frame.Header.GetBeamCount();
                var height = (int)frame.Header.SamplesPerBeam;

                var useExisting =
                    existing != null
                    && width == existing.PixelWidth
                    && height == existing.PixelHeight;

                var currentBitmap =
                    useExisting
                        ? existing
                        : new WriteableBitmap(
                            width, height,
                            96, 96,
                            PixelFormats.Gray8,
                            null);

                return currentBitmap;
            }
        }

        private static readonly Duration bufferLockTimeout =
            new Duration(TimeSpan.FromMilliseconds(10));
    }
}
