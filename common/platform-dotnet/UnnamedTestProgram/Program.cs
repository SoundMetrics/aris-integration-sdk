using CommandLine;
using Serilog;
using SoundMetrics.Aris;
using SoundMetrics.Aris.Connection;
using SoundMetrics.Aris.Network;
using SoundMetrics.Aris.Threading;
using System;
using System.Linq;
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
            Log.Information($"Test device {options.SerialNumber}.");
            Log.Information($"Test duration, {options.Duration} minute(s).");
            Log.Information($"Test settings [{options.Settings}]");

            if (options.Duration is uint minutesDuration)
            {
                using (var cts = new CancellationTokenSource())
                using (var syncContext = QueuedSynchronizationContext.RunOnAThread(cts))
                using (var controller = new ArisController(options.SerialNumber, syncContext))
                {
                    SynchronizationContext.SetSynchronizationContext(syncContext);

                    bool iscancelling = false;

                    Console.CancelKeyPress += (s, e) =>
                    {
                        iscancelling = OnCancel(e, cts, iscancelling);
                    };

                    var rawSettingsCommand = CreateSettingsCommand(options);

                    Log.Information("Created command:");
                    foreach (var line in rawSettingsCommand.GenerateCommand())
                    {
                        Log.Information($"[{line}]");
                    }

                    var settingsCookie = controller.ApplySettings(rawSettingsCommand);
                    Log.Debug("settingsCookie = {settingsCookie}", settingsCookie);

                    WaitForTestCompletionOrCancellation(minutesDuration, cts);

                    Log.Information("Stopping...");
                    var metrics = controller.Stop();
                    LogMetrics(options, metrics);
                }
            }
            else
            {
                Log.Error("No duration given");
            }

            Log.Information("Exiting test.");

            static void LogMetrics(TestOptions options, FrameListenerMetrics metrics)
            {
                var percentFramesCompleted =
                    100.0 * metrics.FramesCompleted / metrics.FramesStarted;
                Log.Information(
                    $"Metrics for ARIS {options.SerialNumber}: "
                        + "framesStarted={framesStarted}; "
                        + "framesCompleted={framesCompleted} ({percentFramesCompleted}); "
                        + "packetsReceived={packetsReceived}; "
                        + "invalidPacketsReceived={invalidPacketsReceived}",
                    metrics.FramesStarted.ToString("N0"),
                    metrics.FramesCompleted.ToString("N0"),
                    double.IsNaN(percentFramesCompleted)
                        ? "n/a"
                        : percentFramesCompleted.ToString("F0") + "%",
                    metrics.PacketsReceived.ToString("N0"),
                    metrics.InvalidPacketsReceived.ToString("N0"));
            }

            static void WaitForTestCompletionOrCancellation(uint minutesDuration, CancellationTokenSource cts)
            {
                var testDuration = TimeSpan.FromMinutes(minutesDuration);
                cts.Token.WaitHandle.WaitOne(testDuration);
            }

            static bool OnCancel(ConsoleCancelEventArgs e, CancellationTokenSource cts, bool isCancelling)
            {
                if (!isCancelling)
                {
                    Log.Information("{cancelKey} received", e.SpecialKey);
                    e.Cancel = true; // Cancel the Ctrl-C or Ctrl-Break default behavior.
                    cts.Cancel(); // Wake up the sleeping thread.
                }

                return true;
            }

            static PassthroughSettings CreateSettingsCommand(TestOptions options)
            {
                var rawSettings = RawSettings.Deserialize(options.Settings);

                var rawSettingsCommand = new PassthroughSettings(
                    new[] { "#raw" }
                        .Concat(rawSettings.Serialize()));
                return rawSettingsCommand;
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
