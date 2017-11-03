namespace Cook_lib
{
    public struct EventDishResultBeOptimized
    {
        public bool isMine;
        public int index;

        public EventDishResultBeOptimized(bool _isMine, int _index)
        {
            isMine = _isMine;
            index = _index;
        }
    }
}
