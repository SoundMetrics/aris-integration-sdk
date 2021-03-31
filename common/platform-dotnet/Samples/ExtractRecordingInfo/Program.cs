using CommandLine;
using System;
using System.Diagnostics;

namespace ExtractRecordingInfo
{
    using static ErrorCodes;

    class Program
    {

        static int Main(string[] args)
        {
            Console.WriteLine("Starting {0}", Process.GetCurrentProcess().ProcessName);

            return
                Parser.Default
                    .ParseArguments<GpsOptions, DepthOptions>(args)
                    .MapResult(
                        (GpsOptions opts) => GPSProcessor.ProcessGPS(opts),
                        (DepthOptions opts) => DepthProcessor.ProcessDepth(opts),
                        _ => StartupError // errors
                    );
        }
    }
}
