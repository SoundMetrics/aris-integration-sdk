using CommandLine;
using Serilog;
using SoundMetrics.Aris;
using SoundMetrics.Aris.Connection;
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
            if (options.SerialNumber is null)
            {
                Log.Error("No serial number given (--serial-number)");
                return;
            }

            Log.Information($"Test device {options.SerialNumber}.");
            Log.Information($"Test duration, {options.Duration} minute(s).");

            if (options.Duration is uint minutesDuration)
            {
                using (var cts = new CancellationTokenSource())
                using (var syncContext = QueuedSynchronizationContext.RunOnAThread(cts))
                using (var controller = new ArisController(options.SerialNumber, syncContext))
                {
                    SynchronizationContext.SetSynchronizationContext(syncContext);

                    bool cancelling = false;

                    Console.CancelKeyPress += (s, e) =>
                    {
                        if (!cancelling)
                        {
                            cancelling = true;
                            Log.Information("{cancelKey} received", e.SpecialKey);
                            e.Cancel = true; // Cancel the Ctrl-C or Ctrl-Break default behavior.
                            cts.Cancel(); // Wake up the sleeping thread.
                        }
                    };

                    // Apply acoustic settings: cookie = 16
                    // frame_period = 0
                    // samples_per_channel = 1000 sample_start_delay = 930 cycle_period = 2329
                    // beam_sample_period = 22 pulse_width = 20 enable_xmit = 0 frequency_select = 1 system_type =
                    var rawSettings = new RawSettings
                    {
                        FrameRate = 13.9f,
                        SamplesPerBeam = 1000,
                        SampleStartDelay = 930,
                        CyclePeriod = 2329 * 10,
                        SamplePeriod = 22,
                        PulseWidth = 20,
                        PingMode = 1,
                        EnableTransmit = true,
                        Frequency = RawSettingsFrequency.High,
                        Enable150Volts = true,
                        ReceiverGain = 12.0f,
                    };

                    var rawSettingsCommand = new PassthroughSettings(
                        new[] { "#raw" }
                            .Concat(rawSettings.Serialize()));
                    Log.Information("Sending command:");
                    foreach (var line in rawSettingsCommand.GenerateCommand())
                    {
                        Log.Information($"[{line}]");
                    }

                    var settingsCookie = controller.ApplySettings(rawSettingsCommand);
                    Log.Debug("settingsCookie = {settingsCookie}", settingsCookie);

                    var testDuration = TimeSpan.FromMinutes(minutesDuration);
                    cts.Token.WaitHandle.WaitOne(testDuration);

                    Log.Information("Stopping...");
                    var metrics = controller.Stop();

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
