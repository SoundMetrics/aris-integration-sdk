using SoundMetrics.Aris.Core;
using SoundMetrics.Aris.File;
using System;
using System.IO;
using System.Linq;

namespace ExtractRecordingInfo
{
    using static ErrorCodes;
    using static Math;

    public static class GPSProcessor
    {
        public static int ProcessGPS(GpsOptions options)
        {
            var console = Console.Out;

            var process =
                options.Summary ? (FileAction)SummarizeGpsInfo : DumpGpsInfo;
            var recordingPath = options.Path;

            console.WriteLine($"Recording=[{recordingPath}]");
            if (File.Exists(recordingPath))
            {
                process(recordingPath, console);
            }
            else
            {
                Console.Error.WriteLine($"File does not exist: [{recordingPath}]");
                return FileNotFound;
            }

            return 0;
        }

        private delegate void FileAction(string recordingPath, TextWriter writer);

        private static void SummarizeGpsInfo(
            string recordingPath,
            TextWriter writer)
        {
            int frameCount = 0;
            int framesWithGpsAge = 0;
            int framesWithMaxGpsAge = 0;
            uint minGpsAge = 0;
            uint maxGpsAge = 0;

            // A `long` is large enough to sum a couple billion frames' GPS ages.
            long cumulativeGpsAges = 0;
            long cumulativeVelocityCents = 0;

            ProcessFrames(recordingPath);
            LogFrames();

            void ProcessFrames(string recordingPath)
            {
                foreach (var frameHeader in
                    ArisRecording.EnumerateFrameHeaders(recordingPath))
                {
                    ProcessFrameHeader(frameHeader);
                }
            }

            void ProcessFrameHeader(in FrameHeader frameHeader)
            {
                var gpsAge = frameHeader.GpsTimeAge;

                framesWithGpsAge += gpsAge != 0 ? 1 : 0;
                framesWithMaxGpsAge += gpsAge == ~0u ? 1 : 0;
                cumulativeGpsAges += gpsAge;

                minGpsAge = Min(minGpsAge, gpsAge);
                maxGpsAge = Max(maxGpsAge, gpsAge);

                cumulativeVelocityCents += (int)Ceiling(frameHeader.Velocity * 100);

                ++frameCount;
            }

            void LogFrames()
            {
                writer.WriteLine($"Frame count=[{frameCount}]");

                if (frameCount > 0)
                {
                    writer.WriteLine($"Frames with GPS age=[{framesWithGpsAge}]");
                    writer.WriteLine($"Frames with GPS max age=[{framesWithMaxGpsAge}]");

                    var averageGpsAge = (double)cumulativeGpsAges / frameCount;
                    writer.WriteLine($"Average GPS age=[{(FineDuration)averageGpsAge:F1}]");

                    writer.WriteLine($"Minimum GPS age=[{(FineDuration)minGpsAge}]");
                    writer.WriteLine($"Maximum GPS age=[{(FineDuration)maxGpsAge}]");

                    var averageVelocity = ((double)cumulativeVelocityCents) / (frameCount * 100);
                    writer.WriteLine($"Average velocity=[{Velocity.FromMetersPerSecond(averageVelocity):F2}]");
                }
            }
        }

        private delegate object GetFieldValue(FrameHeader frameHeader);

        private static void DumpGpsInfo(
            string recordingPath,
            TextWriter writer)
        {
            (string FieldName, GetFieldValue GetFieldValue)[] fieldExtractors =
                new (string, GetFieldValue)[]
                {
                    ("DateTime", GetDateTime),
                    ("GpsTimeAge", hdr => hdr.GpsTimeAge),
                    ("Latitude", hdr => hdr.Latitude),
                    ("Longitude", hdr => hdr.Longitude),
                    ("Heading", hdr => hdr.Heading),
                    ("CompassHeading", hdr => hdr.CompassHeading),
                    ("YearGPS", hdr => hdr.YearGPS),
                    ("MonthGPS", hdr => hdr.MonthGPS),
                    ("DayGPS", hdr => hdr.DayGPS),
                    ("HourGPS", hdr => hdr.HourGPS),
                    ("MinuteGPS", hdr => hdr.MinuteGPS),
                    ("SecondGPS", hdr => hdr.SecondGPS),
                    ("HSecondGPS", hdr => hdr.HSecondGPS),
                };

            string GetColumnHeaders()
                => String.Join(",", fieldExtractors.Select(fe => fe.FieldName));

            ProcessFrames(recordingPath);

            void ProcessFrames(string recordingPath)
            {
                writer.WriteLine(GetColumnHeaders());

                foreach (var frameHeader in
                    ArisRecording.EnumerateFrameHeaders(recordingPath))
                {
                    ProcessFrameHeader(frameHeader);
                }
            }

            void ProcessFrameHeader(FrameHeader frameHeader)
            {
                var values =
                    String.Join(",", fieldExtractors.Select(fe => fe.GetFieldValue(frameHeader).ToString()));
                writer.WriteLine(values);
            }

            object GetDateTime(FrameHeader hdr)
                => new DateTime(
                    (int)hdr.YearGPS, (int)hdr.MonthGPS, (int)hdr.DayGPS,
                    (int)hdr.HourGPS, (int)hdr.MinuteGPS, (int)hdr.SecondGPS,
                    (int)hdr.HSecondGPS * 10);
        }
    }
}
