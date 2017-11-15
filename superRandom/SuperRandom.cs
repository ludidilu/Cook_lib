namespace superRandom
{
    internal class SuperRandom
    {
        private const int A = 9301;
        private const int C = 49297;
        private const int M = 233280;

        private long v;

        internal void SetSeed(int _seed)
        {
            v = (A * (long)_seed + C) % M;
        }

        internal double Get()
        {
            v = (A * v + C) % M;

            return (double)v / M;
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
