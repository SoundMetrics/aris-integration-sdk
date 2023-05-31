using SoundMetrics.Aris.Core;
using SoundMetrics.Aris.File;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExtractRecordingInfo
{
    using static ErrorCodes;

    public static class OrientationProcessor
    {
        public static int ProcessOrientation(OrientationOptions options)
        {
            if (options.StartIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(options.StartIndex));
            }

            var console = Console.Out;

            var recordingPath = options.Path;

            console.WriteLine($"# Recording=[{recordingPath}]");
            if (File.Exists(recordingPath))
            {
                ExtractOrientationInfo(recordingPath, options.StartIndex, console);
            }
            else
            {
                Console.Error.WriteLine($"File does not exist: [{recordingPath}]");
                return FileNotFound;
            }

            return 0;
        }

        /// <summary>
        /// Fetches oritentation information for all the frames of a recording.
        /// </summary>
        public static IEnumerable<OrientationInfo> ExtractOrientationInfo(string recordingPath)
        {
            foreach (var frameHeader in ArisRecording.EnumerateFrameHeaders(recordingPath))
            {
                yield return OrientationInfo.From(frameHeader);
            }
        }

        /// <summary>
        /// For public ingestion of orientation information.
        /// </summary>
        public struct OrientationInfo
        {
            public int FrameIndex;
            public DateTime DateTime;
            public uint GpsTimeAge;
            public float Heading;
            public float Depth;
            public float CompassHeading;
            public float CompassPitch;
            public float CompassRoll;
            public float SonarPan;
            public float SonarTilt;
            public float SonarRoll;
            public double Latitude;
            public double Longitude;

            public static OrientationInfo From(in FrameHeader hdr) =>
                new OrientationInfo
                {
                    FrameIndex = (int)hdr.FrameIndex,
                    DateTime = GetDateTime(hdr),
                    GpsTimeAge = hdr.GpsTimeAge,
                    Heading = hdr.Heading,
                    Depth = hdr.Depth,
                    CompassHeading = hdr.CompassHeading,
                    CompassPitch = hdr.CompassPitch,
                    CompassRoll = hdr.CompassRoll,
                    SonarPan = hdr.SonarPan,
                    SonarTilt = hdr.SonarTilt,
                    SonarRoll = hdr.SonarRoll,
                    Latitude = hdr.Latitude,
                    Longitude = hdr.Longitude,
                };
        }

        private static DateTime GetDateTime(FrameHeader hdr)
            => new DateTime(
                (int)hdr.YearGPS, (int)hdr.MonthGPS, (int)hdr.DayGPS,
                (int)hdr.HourGPS, (int)hdr.MinuteGPS, (int)hdr.SecondGPS,
                (int)hdr.HSecondGPS * 10);

        private static void ExtractOrientationInfo(
            string recordingPath,
            int startIndex,
            TextWriter writer)
        {
            (string FieldName, GetFieldValue GetFieldValue)[] fieldExtractors =
                new (string, GetFieldValue)[]
                {
                    (nameof(OrientationInfo.FrameIndex), info => info.FrameIndex),
                    (nameof(OrientationInfo.DateTime), info => info.DateTime),
                    (nameof(OrientationInfo.GpsTimeAge), hdr => hdr.GpsTimeAge),
                    (nameof(OrientationInfo.Heading), hdr => hdr.Heading),
                    (nameof(OrientationInfo.Depth), hdr => hdr.Depth),

                    (nameof(OrientationInfo.CompassHeading), hdr => hdr.CompassHeading),
                    (nameof(OrientationInfo.CompassPitch), hdr => hdr.CompassPitch),
                    (nameof(OrientationInfo.CompassRoll), hdr => hdr.CompassRoll),

                    (nameof(OrientationInfo.SonarPan), hdr => hdr.SonarPan),
                    (nameof(OrientationInfo.SonarTilt), hdr => hdr.SonarTilt),
                    (nameof(OrientationInfo.SonarRoll), hdr => hdr.SonarRoll),

                    (nameof(OrientationInfo.Latitude), hdr => hdr.Latitude),
                    (nameof(OrientationInfo.Longitude), hdr => hdr.Longitude),
                };

            writer.WriteLine(GetColumnHeaders());

            foreach (var orientationInfo in ExtractOrientationInfo(recordingPath).Skip(startIndex))
            {
                ProcessOrientationInfo(orientationInfo);
            }

            string GetColumnHeaders()
                => String.Join(",", fieldExtractors.Select(fe => fe.FieldName));

            void ProcessOrientationInfo(OrientationInfo orientationInfo)
            {
                var values =
                    String.Join(",", fieldExtractors.Select(fe => fe.GetFieldValue(orientationInfo).ToString()));
                writer.WriteLine(values);
            }
        }

        private delegate object GetFieldValue(OrientationInfo info);
    }
}
