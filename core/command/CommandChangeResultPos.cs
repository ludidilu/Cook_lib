﻿using System.IO;

namespace Cook_lib
{
    public struct CommandChangeResultPos : ICommand
    {
        public bool isMine;
        public int pos;
        public int targetPos;

        public CommandChangeResultPos(bool _isMine, int _pos, int _targetPos)
        {
            isMine = _isMine;
            pos = _pos;
            targetPos = _targetPos;
        }

        public void SetIsMine(bool _isMine)
        {
            isMine = _isMine;
        }

        public void ToBytes(BinaryWriter _bw)
        {
            _bw.Write((byte)CommandType.CHANGE_RESULT_POS);

            _bw.Write(isMine);

            _bw.Write(pos);

            _bw.Write(targetPos);
        }

        public void FromBytes(BinaryReader _br)
        {
            isMine = _br.ReadBoolean();

            pos = _br.ReadInt32();

            targetPos = _br.ReadInt32();
        }
    }
}
