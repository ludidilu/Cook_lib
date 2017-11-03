namespace Cook_lib
{
    public struct EventResultDisappear
    {
        public bool isMine;
        public int index;

        public EventResultDisappear(bool _isMine, int _index)
        {
            isMine = _isMine;
            index = _index;
        }
    }
}
