namespace Cook_lib
{
    public struct EventDishResultAppear
    {
        public bool isMine;
        public int index;

        public EventDishResultAppear(bool _isMine, int _index)
        {
            isMine = _isMine;
            index = _index;
        }
    }
}
