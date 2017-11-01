using System.IO;

namespace Cook_lib
{
    internal struct CommandCompleteDish : ICommand
    {
        public bool isMine;
        public int pos;

        public CommandCompleteDish(bool _isMine, int _pos)
        {
            isMine = _isMine;
            pos = _pos;
        }

        public void ToBytes(BinaryWriter _bw)
        {
            _bw.Write(isMine);

            _bw.Write(pos);
        }

        public void FromBytes(BinaryReader _br)
        {
            isMine = _br.ReadBoolean();

            pos = _br.ReadInt32();
        }
    }
}
