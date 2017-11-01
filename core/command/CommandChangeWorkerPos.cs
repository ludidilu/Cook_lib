using System.IO;

namespace Cook_lib
{
    internal struct CommandChangeWorkerPos : ICommand
    {
        public bool isMine;
        public int workerIndex;
        public int pos;

        public CommandChangeWorkerPos(bool _isMine, int _workerIndex, int _pos)
        {
            isMine = _isMine;
            workerIndex = _workerIndex;
            pos = _pos;
        }

        public void ToBytes(BinaryWriter _bw)
        {
            _bw.Write(isMine);

            _bw.Write(workerIndex);

            _bw.Write(pos);
        }

        public void FromBytes(BinaryReader _br)
        {
            isMine = _br.ReadBoolean();

            workerIndex = _br.ReadInt32();

            pos = _br.ReadInt32();
        }
    }
}
