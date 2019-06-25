// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

using SoundMetrics.Aris.PaletteShader;
using System.Windows.Controls;
using System.Windows.Media;
using ArisFrameSource = System.IObservable<SoundMetrics.Aris.Comms.RawFrame>;

namespace SoundMetrics.DataVisualization
{
    /// <summary>Simple visualization control for ARIS.</summary>
    public sealed partial class ArisImageControl : UserControl
    {
        public ArisImageControl()
        {
            InitializeComponent();
        }

        private void OnFrameSourceChanged(ArisFrameSource newValue)
        {
            // TODO
        }

        private void OnIsReversedChanged(bool oldValue, bool newValue)
        {
            // TODO
        }

        private void OnRangeLabelModeChanged(RangeLabelMode oldValue, RangeLabelMode newValue)
        {
            // TODO
        }

        private void OnPaletteTemplateChanged(Brush newValue)
        {
            // TODO
        }

        private void OnPaletteIndexChanged(int oldValue, int newValue)
        {
            shader.PalletteIndex = GetPaletteIndexFromInteger(newValue);
        }

        private static float GetPaletteIndexFromInteger(int value) => (value / 16f) + (1f / 32f);

        private readonly ArisPaletteShader shader = new ArisPaletteShader();
    }
}
