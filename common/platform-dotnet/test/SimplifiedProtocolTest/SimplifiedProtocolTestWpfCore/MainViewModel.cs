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
                try
                {
                    Debug.WriteLine($"Painting {frame.Samples.ShortString}"); // TODO REMOVE
                    PaintMe(FrameBitmap, frame.Samples, (int)frame.Header.GetBeamCount());
                }
                finally
                {
                    FrameBitmap.Unlock();
                }
            }

            unsafe void PaintMe(
                WriteableBitmap bitmap,
                NativeBuffer src,
                int beamCount)
            {
                var bitmapBuffer = bitmap.BackBuffer;

                var nativeSource = src.DangerousGetHandle();
                var rowCount = bitmap.PixelHeight;
                var destinationStride = bitmap.BackBufferStride;

                byte* pDest = (byte*)bitmapBuffer.ToPointer(),
                      pSrc = (byte*)nativeSource.ToPointer();

                for (byte* pDestRow = pDest, pSrcRow = pSrc;
                        pDestRow < pDest + (rowCount * destinationStride);
                        pDestRow += destinationStride, pSrcRow += beamCount)
                {
                    byte* pOut = pDestRow,
                          pIn = pSrcRow;

                    for (var beamIdx = 0; beamIdx < beamCount; ++beamIdx)
                    {
                        *pOut = *pIn;
                        ++pOut;
                        ++pIn;

                        //var count = upsampleCounts[beamIdx];
                        //for (var iter = 0; iter < count; ++iter, ++pOut)
                        //{
                        //    *pOut = *pIn;
                        //}
                    }
                }

                bitmap.AddDirtyRect(
                    new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
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
            new Duration(TimeSpan.FromMilliseconds(2));
    }
}
