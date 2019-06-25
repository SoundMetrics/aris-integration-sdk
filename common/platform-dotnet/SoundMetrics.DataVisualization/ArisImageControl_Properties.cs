using System.Windows;
using System.Windows.Controls;

namespace SoundMetrics.DataVisualization
{
    public sealed partial class ArisImageControl : UserControl
    {
        public static readonly DependencyProperty AnnulusPositionProperty =
            DependencyProperty.Register(
                nameof(AnnulusPosition), typeof(ArisAnnulusPosition), typeof(ArisImageControl));

        public ArisAnnulusPosition AnnulusPosition
        {
            get { return (ArisAnnulusPosition)GetValue(AnnulusPositionProperty); }
        }
    }
}
