namespace Cook_lib
{
    public static class CookConst
    {
        public const int TICK_NUM_PER_SECOND = 25;

        public static readonly bool[] RESULT_STATE = new bool[] { false, true, true, true, true };

        public const float REQUIRE_EXCEED_TIME = 40;

        public const int REQUIRE_PRODUCE_TIME = 250;

        public const int REQUIRE_NUM_MIN = 2;

        public const int REQUIRE_NUM_MAX = 4;

        public const int WORKER_NUM = 1;

        public const int WORKER_PUNISH_TICK = 25;

        public const float OPTIMIZE_PROBABILITY_MAX = 0.7f;
    }
}
