namespace Cook_lib
{
    public struct EventDishResultDisappear
    {
        public bool isMine;
        public int index;

        public EventDishResultDisappear(bool _isMine, int _index)
        {
            isMine = _isMine;
            index = _index;
        }
    }
}
