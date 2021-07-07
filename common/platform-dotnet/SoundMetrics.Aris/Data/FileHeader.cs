// ArisFileHeader.cs

// Disable CS1591 so we don't get a huge number of xmldoc warnings in projects that use it.
#pragma warning disable CS1591

namespace SoundMetrics.Aris.Data
{

    using System;
    using System.Runtime.InteropServices;

    // Defines the metadata at the start of an ARIS recording.
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public unsafe struct FileHeader
    {
        public const uint ArisFileSignature = 0x05464444;
        public const uint ArisFrameSignature = 0x05464444;

        // File format version DDF_05 = 0x05464444
        public UInt32 Version;

        // Total frames in file
        // Note: Writers should populate; readers should calculate the number of frames from file size & beams*samples.
        public UInt32 FrameCount;

        // Initial recorded frame rate
        [Obsolete("See frame header instead.")]
        public UInt32 FrameRate;

        // Non-zero if HF, zero if LF
        [Obsolete("See frame header instead.")]
        public UInt32 HighResolution;

        // ARIS 3000 = 128/64, ARIS 1800 = 96/48, ARIS 1200 = 48
        // Note: Writers should populate; readers should see frame header instead.
        public UInt32 NumRawBeams;

        // 1/Sample Period
        [Obsolete("See frame header instead.")]
        public float SampleRate;

        // Number of range samples in each beam
        // Note: Writers should populate; readers should see frame header instead.
        public UInt32 SamplesPerChannel;

        // Relative gain in dB:  0 - 40
        [Obsolete("See frame header instead.")]
        public UInt32 ReceiverGain;

        // Image window start range in meters (code [0..31] in DIDSON)
        [Obsolete("See frame header instead.")]
        public float WindowStart;

        // Image window length in meters  (code [0..3] in DIDSON)
        [Obsolete("See frame header instead.")]
        public float WindowLength;

        // Non-zero = lens down (DIDSON) or lens up (ARIS), zero = opposite
        [Obsolete("See frame header instead.")]
        public UInt32 Reverse;

        // Sonar serial number
        public UInt32 SN;

        // Date that file was recorded
        public fixed byte strDate[32];

        // User input to identify file in 256 characters
        public fixed byte strHeaderID[256];

        // User-defined integer quantity
        public int UserID1;

        // User-defined integer quantity
        public int UserID2;

        // User-defined integer quantity
        public int UserID3;

        // User-defined integer quantity
        public int UserID4;

        // First frame number from source file (for DIDSON snippet files)
        public UInt32 StartFrame;

        // Last frame number from source file (for DIDSON snippet files)
        public UInt32 EndFrame;

        // Non-zero indicates time lapse recording
        public UInt32 TimeLapse;

        // Number of frames/seconds between recorded frames
        public UInt32 RecordInterval;

        // Frames or seconds interval
        public UInt32 RadioSeconds;

        // Record every Nth frame
        public UInt32 FrameInterval;

        // See DDF_04 file format document
        [Obsolete("Obsolete.")]
        public UInt32 Flags;

        // See DDF_04 file format document
        public UInt32 AuxFlags;

        // Sound velocity in water
        [Obsolete("See frame header instead.")]
        public UInt32 Sspd;

        // See DDF_04 file format document
        public UInt32 Flags3D;

        // DIDSON software version that recorded the file
        public UInt32 SoftwareVersion;

        // Water temperature code:  0 = 5-15C, 1 = 15-25C, 2 = 25-35C
        public UInt32 WaterTemp;

        // Salinity code:  0 = fresh, 1 = brackish, 2 = salt
        public UInt32 Salinity;

        // Added for ARIS but not used
        public UInt32 PulseLength;

        // Added for ARIS but not used
        public UInt32 TxMode;

        // Reserved for future use
        public UInt32 VersionFGPA;

        // Reserved for future use
        public UInt32 VersionPSuC;

        // Frame index of frame used for thumbnail image of file
        public UInt32 ThumbnailFI;

        // Total file size in bytes
        [Obsolete("Do not use; query your filesystem instead.")]
        public UInt64 FileSize;

        // Reserved for future use
        [Obsolete("Obsolete; not used.")]
        public UInt64 OptionalHeaderSize;

        // Reserved for future use
        [Obsolete("Obsolete; not used.")]
        public UInt64 OptionalTailSize;

        // DIDSON_ADJUSTED_VERSION_MINOR
        [Obsolete("Obsolete.")]
        public UInt32 VersionMinor;

        // Non-zero if telephoto lens (large lens, hi-res lens, big lens) is present
        [Obsolete("See frame header instead.")]
        public UInt32 LargeLens;

        // Padding to fill out to 1024 bytes
        public fixed byte padding[568];

    }

}
