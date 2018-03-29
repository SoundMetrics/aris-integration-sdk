// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.Comms

open Microsoft.FSharp.Data.UnitSystems.SI.UnitSymbols
open Serilog
open System
open System.IO
open System.Runtime.InteropServices

module internal RecordingLog =

    let logStartedRecording (description : string) (path : string) =
        Log.Information("Started recording; description={description}; path={path}", description, path)

    let logDuplicateRecordingRequest (description : string) =
        Log.Error("Duplicate recording request ignored; description={description}. Logic error.", description)

    let logStopRequestNotFound (description : string) =
        Log.Error("Couldn't find recording request to stop it; description={description}.", description)

    let logStoppedRecording (description : string) (path : string) =
        Log.Information("Stopped recording; description={description}; path={path}", description, path)

    let logStoppedAllRecordings () = Log.Information("Stopped all recording")

    let logTimeToProcess (workType : string) (milliseconds : int64) =
        Log.Verbose("Time to {workType}: {milliseconds} ms", workType, milliseconds)

    let logRecordingError (msg : string) (path : string) =
        Log.Warning("Recording error {msg} on \"{path}\"", msg, path)

    let logNoAvailableRecordingPath (description : string) =
        Log.Warning("No available recording path for '{description}'", description)

open RecordingLog

#nowarn "44" // We need to have access to deprecated fields in headers.

module internal FileRecording = 
    open Aris.FileTypes

    let private ArisFileHeaderSize = Marshal.SizeOf<ArisFileHeader>()
    let private ArisFrameHeaderSize = Marshal.SizeOf<ArisFrameHeader>()
    let private zeroPad = Array.zeroCreate<byte> (max ArisFileHeaderSize ArisFrameHeaderSize)

    [<AutoOpen>]
    module private FileRecorderImpl =

        let performOperationReturnToPosition (stream: Stream) op =
            let curpos = stream.Position
            try
                op stream
            finally
                stream.Position <- curpos

        let writeValueAt (stream: Stream) offset bytes =
            stream.Position <- offset
            stream.Write(bytes, 0, bytes.Length)

        let writeFileHeader stream (frame: Frame option) =
            performOperationReturnToPosition stream (fun stream ->
                match frame with
                | Some f ->
                    // Frame count is maintained elsewhere.
                    writeValueAt stream (int64 ArisFileHeaderOffsets.FrameRate)      (BitConverter.GetBytes(f.Header.FrameRate))
                    writeValueAt stream (int64 ArisFileHeaderOffsets.HighResolution) (BitConverter.GetBytes(f.Header.FrequencyHiLow))
                    writeValueAt stream (int64 ArisFileHeaderOffsets.NumRawBeams)    (BitConverter.GetBytes(f.BeamCount))
                    writeValueAt stream (int64 ArisFileHeaderOffsets.SamplesPerChannel) (BitConverter.GetBytes(f.Header.SamplesPerBeam))
                    writeValueAt stream (int64 ArisFileHeaderOffsets.ReceiverGain)   (BitConverter.GetBytes(f.Header.ReceiverGain))
                    writeValueAt stream (int64 ArisFileHeaderOffsets.WindowStart)    (BitConverter.GetBytes(f.Header.WindowStart))
                    writeValueAt stream (int64 ArisFileHeaderOffsets.WindowLength)   (BitConverter.GetBytes(f.Header.WindowLength))
                    writeValueAt stream (int64 ArisFileHeaderOffsets.SN)             (BitConverter.GetBytes(f.Header.SonarSerialNumber))
                    writeValueAt stream (int64 ArisFileHeaderOffsets.Sspd)           (BitConverter.GetBytes(f.Header.SoundSpeed))
                | None ->
                    // First time in; blank the master header
                    stream.Write(zeroPad, 0, ArisFileHeaderSize)
                    writeValueAt stream (int64 ArisFileHeaderOffsets.Version) (BitConverter.GetBytes(uint32 ArisFileHeader.ArisFileSignature))
            )

        let updateFrameCount stream frameCount =
            performOperationReturnToPosition stream (fun stream ->
                let bytes = BitConverter.GetBytes(int frameCount)
                assert (bytes.Length = 4)
                stream.Position <- int64 ArisFileHeaderOffsets.FrameCount
                stream.Write(bytes, 0, bytes.Length)
            )

        let inline structToBytes<'T> (s: 'T) =
            let buf = Array.zeroCreate<byte> (Marshal.SizeOf(s))
            let h = GCHandle.Alloc(buf, GCHandleType.Pinned)
            try
                Marshal.StructureToPtr(s, h.AddrOfPinnedObject(), false)
            finally
                h.Free()

            buf

        let writeFrame (stream: Stream) (frame: Frame) newFrameIndex =

            // Caller deals with I/O exceptions.
            let mutable hdr = frame.Header
            hdr.FrameIndex <- newFrameIndex

            let buf = structToBytes hdr
            assert (buf.Length <= ArisFrameHeaderSize)

            stream.Write(buf, 0, buf.Length)

            // Pad header
            stream.Write(zeroPad, 0, ArisFrameHeaderSize - buf.Length)

            // Write sample data
            stream.Write(frame.SampleData.ToArray(), 0, int frame.SampleData.Length)

            updateFrameCount stream (newFrameIndex + 1u)

    type FixedFrameSizeRecorder (path: RecordingPath) =
        let disposed = ref false
        let mutable fi = 0u
        let mutable firstFrame = true
        let mutable frameSize = 0u // until we actually see a frame
        let stream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read)

        do
            try
                writeFileHeader stream None

                // Check that we have at some substantial chunk of storage.
                // If not, we'll close the file and clean up so we don't have
                // a bunch of small files created.
                let minFreeSpace = 2L * 1024L * 1024L
                let largerSize = int64 ArisFileHeaderSize + minFreeSpace
                
                stream.SetLength(largerSize)

                // If we didn't throw, set the file size back to the file header
                // size and seek to where the frames should start.
                stream.Position <- int64 ArisFileHeaderSize
                stream.SetLength(int64 ArisFileHeaderSize)
            with
                | _ ->
                    // If stream.Close() fails it will still close its SafeFileHandle,
                    // releasing the file system resource. So we can try deleting the
                    // (empty) file even if Close() fails (e.g., due to no disk space to
                    // flush to, etc.).
                    ignoreException
                        "Stream may fail to close due to no disk space (etc.) and we're already cleaning up from a failure"
                        (fun () -> stream.Close())

                    ignoreException
                        "File should be closed by stream.Close(), but be cautious here as we're clean up a failure"
                        (fun () -> File.Delete(path))

                    reraise ()


        interface IDisposable with
            member __.Dispose () =
                Dispose.theseWith disposed []
                    (fun () ->
                        // FileStream can throw when disposing, especially if there's no
                        // disk space to flush to. So eat any exceptions when closing it.
                        // Note that even then it should successfully release the file
                        // system resource.
                        ignoreException
                            "Stream may not close quietly if out of disk space or other condition."
                            (fun () -> stream.Close())
                    )

        member s.Dispose () = (s :> IDisposable).Dispose ()

        /// Writes the frame and returns the recorded frame index offset
        /// (vs the incoming frame index).
        member __.WriteFrame (frame: Frame) : int =

            let startPosition = stream.Position

            try // Handle I/O problems (e.g., out of space).
                let initialFrameIndex = frame.Header.FrameIndex
                if firstFrame then
                    firstFrame <- false
                    frameSize <- frame.SampleData.Length
                    writeFileHeader stream (Some frame)

                assert (stream.Position = int64(ArisFileHeaderSize + (int fi * (ArisFrameHeaderSize + int frameSize))))
                writeFrame stream frame fi

                let newFrameIndex = fi
                fi <- fi + 1u
                int newFrameIndex - int initialFrameIndex
            with
                | :? IOException as ex ->
                    ignoreException
                        "Don't let logging blow up during exception handling"
                        (fun () -> logRecordingError ex.Message path)

                    // Roll back to where we started & truncate the file.
                    // This assumes we're at the start of a frame position
                    // when this function is called.
                    ignoreException
                        "Stream may not clean up quietly when handling a stream exception"
                        (fun () ->  stream.Position <- startPosition
                                    stream.SetLength(startPosition))

                    reraise()
