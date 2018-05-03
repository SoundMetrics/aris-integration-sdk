// Copyright 2010-2018 Sound Metrics Corp. All Rights Reserved.

namespace SoundMetrics.Aris.PaletteShader
{
    /// <summary>
    /// The pixel shader effect applied to the image displayed by the sonar image control.
    /// Applies to <see cref="ArisPaletteShader.ShaderIndex"/>.
    /// </summary>
    public enum ShaderIndex
    {
        /// <summary> None ( Default) </summary>
        NoEffect,

        /// <summary> Smoothing  (Noise reduction) </summary>
        Smooth,

        /// <summary> Sharpening filter </summary>
        Sharpen,

        /// <summary> Edge detect ( Disabled in the UI for now) </summary>
        Edges,
    }
}
