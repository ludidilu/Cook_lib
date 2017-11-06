using System.IO;

namespace Cook_lib
{
    public struct CommandChangeWorkerPos : ICommand
    {
        public bool isMine;
        public int workerIndex;
        public int targetPos;

        public CommandChangeWorkerPos(bool _isMine, int _workerIndex, int _targetPos)
        {
            isMine = _isMine;
            workerIndex = _workerIndex;
            targetPos = _targetPos;
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

            _bw.Write(targetPos);
        }

        public void FromBytes(BinaryReader _br)
        {
            isMine = _br.ReadBoolean();

            workerIndex = _br.ReadInt32();

            targetPos = _br.ReadInt32();
        }
    }
}
