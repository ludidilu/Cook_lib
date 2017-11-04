using System.IO;

namespace Cook_lib
{
    public struct CommandChangeWorkerPos : ICommand
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

        public void SetIsMine(bool _isMine)
        {
            isMine = _isMine;
        }

        public void ToBytes(BinaryWriter _bw)
        {
            _bw.Write((byte)CommandType.CHANGE_WORKER_POS);

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
