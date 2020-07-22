namespace SoundMetrics.Aris.File
{
    public sealed class Box<T>
        where T : struct
    {
        public Box(in T value)
        {
            this.Value = value;
        }

        public T Value { get; private set; }
    }
}
