using SimplifiedProtocolTest.Helpers;
using SoundMetrics.Aris.Headers;
using SoundMetrics.Aris.SimplifiedProtocol;
using System;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Threading;
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
            catch (SocketException)
            {
            }
        }

        private void OnFrame(Frame frame)
        {
            FrameIndex = frame.Header.FrameIndex;
            FrameBitmap = LoadBitmap();

        // See the following page for discussion of writing pixels to a
        // UWP WriteableBitmap.
        // https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.media.imaging.writeablebitmap.pixelbuffer#Windows_UI_Xaml_Media_Imaging_WriteableBitmap_PixelBuffer

            // Naive, but functional version to start:
            WriteableBitmap LoadBitmap()
            {
                var width = frame.Header.GetBeamCount();
                var height = frame.Header.SamplesPerBeam;
                var writeableBitmap = new WriteableBitmap(
                    (int)width, (int)height,
                    96, 96,
                    PixelFormats.Gray8,
                    null);

                return writeableBitmap;
            }
        }
    }
}
