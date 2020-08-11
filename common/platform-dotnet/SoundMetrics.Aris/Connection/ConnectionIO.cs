using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace SoundMetrics.Aris.Connection
{
    internal sealed class ConnectionIO : IDisposable
    {
        /// <summary>
        /// Constructs the IO bits and takes ownership of the TcpClient.
        /// </summary>
        public ConnectionIO(TcpClient tcp)
        {
            this.tcp = tcp;

            try
            {
                stream = new NetworkStream(tcp.Client);
                reader = new StreamReader(stream, Encoding.ASCII);
                writer = new StreamWriter(stream, Encoding.ASCII);
            }
            catch
            {
                reader?.Dispose();
                writer?.Dispose();
                stream?.Dispose();
                tcp.Dispose();
                throw;
            }
        }

        public CommandResponse SendCommand(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                writer.WriteLine(line);
            }

            // An empty line delimits messages in the SimplifiedProtocol.
            writer.WriteLine("");
            writer.Flush();

            return ReceiveResponse();
        }

        private CommandResponse ReceiveResponse()
        {
            var lines = new List<string>();
            string? line;

            while (!string.IsNullOrWhiteSpace(line = reader.ReadLine()))
            {
                lines.Add(line);
            }

            var success = GetStatusCode() == "200";
            return new CommandResponse(success, lines);

            string GetStatusCode()
            {
                var firstLine = lines[0].Trim();
                var firstWS = firstLine.IndexOfAny(new[] { ' ', '\t' });
                return (firstWS < 0) ? "" : firstLine.Substring(0, firstWS);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    reader.Dispose();
                    writer.Dispose();
                    stream.Dispose();
                    tcp.Dispose();
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

        private readonly TcpClient tcp;
        private readonly NetworkStream stream;
        private readonly StreamReader reader;
        private readonly StreamWriter writer;

        private bool disposed;
    }
}
