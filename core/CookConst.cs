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

        public const int WORKER_NUM = 2;

        public const float WORKER_PUNISH_TIME = 1;

        public const float OPTIMIZE_PROBABILITY_MAX = 0.7f;

        public const float EXCEED_VALUE_1 = 0f;

        public const float EXCEED_VALUE_2 = 0.5f;

        public const float EXCEED_VALUE_3 = 1.0f;

        public const float MAX_TIME = 180;

        public const float REQUIRE_REWARD_FIX = 1.5f;

        public const float REQUIRE_REWARD_ALL_OPTIMIZED_FIX = 1.5f;
    }
}
