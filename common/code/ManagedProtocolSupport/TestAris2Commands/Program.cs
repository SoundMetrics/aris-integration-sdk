// WARNING: This code is meant for integration testing only, it is not production-ready.
// THIS CODE IS UNSUPPORTED.

using System;
using TestAris2Commands;

/// <summary>
/// This program briefly tests the commands in SoundMetrics.Aris2.Protocols.
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        var parsed = CheckArguments(args);
        if (ArisConnection.TryCreate(parsed.ArisIPAddress, out var connection))
        {
            var frameListener = new FrameListener(parsed.ArisIPAddress.ToString());
            Tests.Run(parsed.SystemType, connection, frameListener);
            GC.KeepAlive(frameListener);
        }
        else
        {
            Console.Error.WriteLine($"Could not open a connection to the ARIS at {parsed.ArisIPAddress}");
            Environment.Exit(100);
        }
    }

    private static Arguments CheckArguments(string[] args)
    {
        Arguments parsed = null;
        string error;
        if (Arguments.TryParse(args, out parsed, out error))
        {
            Console.WriteLine(String.Format($"Testing against {parsed.ArisIPAddress} "));
        }
        else
        {
            Console.Error.WriteLine("Error: " + error);
            Environment.Exit(1);
        }

        return parsed;
    }
}
