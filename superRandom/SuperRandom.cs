namespace superRandom
{
    internal class SuperRandom
    {
        private const int A = 9;
        private const int B = 7;

        private long v;

        internal void SetSeed(int _seed)
        {
            v = (A * (long)_seed + B) % int.MaxValue;
        }

        internal double Get()
        {
            v = (A * v + B) % int.MaxValue;

            return (double)v / int.MaxValue;
        }

        internal int Get(int _max)
        {
            double vv = Get();

            return (int)(vv * _max);
        }

        internal int Get(int _min, int _max)
        {
            double vv = Get();

            return _min + (int)(vv * (_max - _min));
        }
    }
}
