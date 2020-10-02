using System.Windows;

namespace SoundMetrics.DataVisualization
{
    public class ArisAnnulusPosition
    {
        public ArisAnnulusPosition(
            Point topLeft,
            Point topRight,
            Point bottomRight,
            Point bottomLeft,
            Angle rotation)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomRight = bottomRight;
            BottomLeft = bottomLeft;
            Rotation = rotation;
        }

        public readonly Point TopLeft;
        public readonly Point TopRight;
        public readonly Point BottomRight;
        public readonly Point BottomLeft;
        public readonly Angle Rotation;
    }
}
