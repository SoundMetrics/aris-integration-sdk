// WARNING: This code is meant for integration testing only, it is not production-ready.
// THIS CODE IS UNSUPPORTED.

using System.Net;

namespace TestAris2Commands
{
    public class Arguments
    {
        private Arguments() { }

        public string SystemType;
        public IPAddress ArisIPAddress;

        public static bool TryParse(string[] args, out Arguments parsed, out string error)
        {
            parsed = null;
            error = null;

            if (args.Length != 2)
            {
                error = "One argument expected: an ARIS serial number and a system type (1200/1800/3000)";
                return false;
            }

            IPAddress addr;
            if (!IPAddress.TryParse(args[0], out addr))
            {
                error = $"Couldn't parse IP address: {args[0]}";
                return false;
            }

            string systemType;
            switch (args[1])
            {
                case "1200":
                case "1800":
                case "3000":
                    systemType = args[1];
                    break;
                default:
                    error = $"Invalid system type: {args[1]}";
                    return false;
            }

            parsed = new Arguments { ArisIPAddress = addr, SystemType = systemType };
            return true;
        }
    }
}
