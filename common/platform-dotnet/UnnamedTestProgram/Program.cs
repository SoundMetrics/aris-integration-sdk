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

                    var settingsCookie = controller.ApplySettings(rawSettingsCommand);
                    Log.Debug("settingsCookie = {settingsCookie}", settingsCookie);

                    var duration = TimeSpan.FromMinutes(minutesDuration);
                    Thread.Sleep(duration);

                    var metrics = controller.Stop();

                    Log.Information(
                        $"Metrics for ARIS {options.SerialNumber}: "
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
