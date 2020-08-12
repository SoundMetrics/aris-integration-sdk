using CommandLine;
using Serilog;
using SoundMetrics.Aris;
using SoundMetrics.Aris.Connection;
using SoundMetrics.Aris.Threading;
using System;
using System.Threading;

namespace UnnamedTestProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigureLogger();

            Parser.Default.ParseArguments<ScriptOptions, TestOptions>(args)
                .WithParsed<ScriptOptions>(RunScript)
                .WithParsed<TestOptions>(RunTest)
                .WithNotParsed(errors => { });

        }

        private static void RunScript(ScriptOptions options)
        {

        }

        private static void RunTest(TestOptions options)
        {
            Log.Information($"Test duration, {options.Duration} minute(s).");

            if (options.Duration is uint minutesDuration)
            {
                using (var cts = new CancellationTokenSource())
                using (var syncContext = QueuedSynchronizationContext.RunOnAThread(cts))
                using (var conduit = new ArisController("24", syncContext))
                {
                    SynchronizationContext.SetSynchronizationContext(syncContext);

                    conduit.ApplySettings(new TestPatternSettings());

                    var duration = TimeSpan.FromMinutes(minutesDuration);
                    Thread.Sleep(duration);
                }
            }
            else
            {
                Log.Error("No duration given");
            }

            Log.Information("Exiting test.");
        }

        private static void ConfigureLogger()
        {
            const string loggingTemplate =
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                            .MinimumLevel.Debug()
#endif
                            .WriteTo.Console(outputTemplate: loggingTemplate)
                            .CreateLogger();
        }
    }
}
