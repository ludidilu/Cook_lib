using System.Collections.Generic;
using System.IO;

namespace Cook_lib
{
    public struct CommandCompleteRequirement : ICommand
    {
        public bool isMine;
        public List<int> resultList;
        public int requirementUid;

        public CommandCompleteRequirement(bool _isMine, List<int> _resultList, int _requirementUid)
        {
            isMine = _isMine;
            resultList = _resultList;
            requirementUid = _requirementUid;
        }

        public void ToBytes(BinaryWriter _bw)
        {
            _bw.Write(isMine);

            _bw.Write(resultList.Count);

            for (int i = 0; i < resultList.Count; i++)
            {
                _bw.Write(resultList[i]);
            }

            _bw.Write(requirementUid);
        }

        public void FromBytes(BinaryReader _br)
        {
            isMine = _br.ReadBoolean();

            int num = _br.ReadInt32();

            resultList = new List<int>();

            for (int i = 0; i < num; i++)
            {
                int index = _br.ReadInt32();

                resultList.Add(index);
            }

            requirementUid = _br.ReadInt32();
        }
    }
}
