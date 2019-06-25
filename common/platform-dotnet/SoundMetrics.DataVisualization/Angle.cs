namespace SoundMetrics.DataVisualization
{
    /// <summary>
    /// Angle; always in radians.
    /// </summary>
    public struct Angle
    {
        public Angle(float radians)
        {
            Radians = radians;
        }

        public readonly float Radians;

        public static explicit operator Angle(float radians)
        {
            return new Angle(radians);
        }
    }
}
