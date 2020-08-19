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
            if (options.SerialNumber is null)
            {
                Log.Error("No serial number given (--serial-number)");
                return;
            }

            Log.Information($"Test drvice {options.SerialNumber}.");
            Log.Information($"Test duration, {options.Duration} minute(s).");

            if (options.Duration is uint minutesDuration)
            {
                using (var cts = new CancellationTokenSource())
                using (var syncContext = QueuedSynchronizationContext.RunOnAThread(cts))
                using (var controller = new ArisController(options.SerialNumber, syncContext))
                {
                    SynchronizationContext.SetSynchronizationContext(syncContext);

                    var rawSettings = new PassthroughSettings(
                        new[]
                        {
                            "#raw",
                        });

                    var settingsCookie = controller.ApplySettings(rawSettings);
                    Log.Debug("settingsCookie = {settingsCookie}", settingsCookie);

                    var duration = TimeSpan.FromMinutes(minutesDuration);
                    Thread.Sleep(duration);

                    var metrics = controller.Stop();

                    Log.Information(
                        "Metrics: "
                            + "framesStarted={framesStarted}; "
                            + "framesCompleted={framesCompleted}; "
                            + "packetsReceived={packetsReceived}; "
                            + "invalidPacketsReceived={invalidPacketsReceived}",
                        metrics.FramesStarted,
                        metrics.FramesCompleted,
                        metrics.PacketsReceived,
                        metrics.InvalidPacketsReceived);
                }
            }
            else
            {
                Log.Error("No duration given");
            }

            Log.Information("Exiting test.");

            string DetermineRecordingFileName()
            {
                return "default.aris"; // TODO
            }
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
