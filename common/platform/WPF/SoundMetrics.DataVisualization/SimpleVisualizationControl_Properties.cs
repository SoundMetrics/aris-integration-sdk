// Copyright 2014-2018 Sound Metrics Corp. All Rights Reserved.

using System;
using System.Windows;
using System.Windows.Media;
using FrameSource = System.IObservable<SoundMetrics.Aris.Comms.Frame>;

namespace SoundMetrics.DataVisualization
{
    public sealed partial class SimpleVisualizationControl
    {
        private static bool ValidateEnumMember<E>(object value) => Enum.IsDefined(typeof(E), value);

        //-------------------------------------------------------------------------------

        public static readonly DependencyProperty FrameSourceProperty =
            DependencyProperty.Register("FrameSource", typeof(FrameSource), typeof(SimpleVisualizationControl),
                                        new PropertyMetadata(OnFrameSourceChanged));

        public FrameSource FrameSource
        {
            get => (FrameSource)GetValue(FrameSourceProperty);
            set => SetValue(FrameSourceProperty, value);
        }

        private static void OnFrameSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((SimpleVisualizationControl)d).OnFrameSourceChanged((FrameSource)e.NewValue);

        //-------------------------------------------------------------------------------

        /// <summary>Defines the IsReversed dependency property.</summary>
        public static readonly DependencyProperty IsReversedProperty =
            DependencyProperty.Register("Reverse", typeof(bool), typeof(SimpleVisualizationControl),
                new UIPropertyMetadata(OnIsReversedChanged));

        /// <summary>Reverses the image left-for-right.</summary>
        public bool IsReversed
        {
            get => (bool)GetValue(IsReversedProperty);
            set => SetValue(IsReversedProperty, value);
        }

        private static void OnIsReversedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((SimpleVisualizationControl)d).OnIsReversedChanged((bool)e.OldValue, (bool)e.NewValue);

        //-------------------------------------------------------------------------------

        public static readonly DependencyProperty RangeLabelModeProperty =
            DependencyProperty.Register("RangeLabelMode", typeof(RangeLabelMode), typeof(SimpleVisualizationControl),
                new UIPropertyMetadata(OnRangeLabelModeChanged), 
                value => ValidateEnumMember<RangeLabelMode>(value));

        public RangeLabelMode RangeLabelMode
        {
            get => (RangeLabelMode)GetValue(RangeLabelModeProperty);
            set => SetValue(RangeLabelModeProperty, value);
        }

        private static void OnRangeLabelModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((SimpleVisualizationControl)d).OnRangeLabelModeChanged((RangeLabelMode)e.OldValue, (RangeLabelMode)e.NewValue);

        //-------------------------------------------------------------------------------

        public static readonly DependencyProperty PaletteTemplateProperty =
            DependencyProperty.Register("PaletteTemplate", typeof(Brush), typeof(SimpleVisualizationControl),
                new UIPropertyMetadata(OnPaletteTemplateChanged));

        public Brush PaletteTemplate
        {
            get => (Brush)GetValue(PaletteTemplateProperty);
            set => SetValue(PaletteTemplateProperty, value);
        }

        private static void OnPaletteTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((SimpleVisualizationControl)d).OnPaletteTemplateChanged((Brush)e.NewValue);

        //-------------------------------------------------------------------------------

        public static readonly DependencyProperty PaletteIndexProperty =
            DependencyProperty.Register("PaletteIndex", typeof(int), typeof(SimpleVisualizationControl),
                new UIPropertyMetadata(OnPaletteIndexChanged),
                value => value is int index && index >= 0);

        public int PaletteIndex
        {
            get => (int)GetValue(PaletteIndexProperty);
            set => SetValue(PaletteIndexProperty, value);
        }

        private static void OnPaletteIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((SimpleVisualizationControl)d).OnPaletteIndexChanged((int)e.OldValue, (int)e.NewValue);
    }
}
