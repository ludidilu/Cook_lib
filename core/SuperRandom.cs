namespace Cook_lib
{
    internal class SuperRandom
    {
        private static readonly int MAX = 1 << 31;

        private const int A = 9;
        private const int B = 7;

        private int v;

        internal void SetSeed(int _seed)
        {
            v = (A * _seed + B) % MAX;
        }

        internal float Get()
        {
            v = (A * v + B) % MAX;

            return (float)v / MAX;
        }

        internal int Get(int _max)
        {
            float v = Get();

            return (int)(v * _max);
        }

        internal int Get(int _min, int _max)
        {
            float v = Get();

            return _min + (int)(v * (_max - _min));
        }
    }
}
