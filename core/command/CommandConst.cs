using System.IO;

namespace Cook_lib
{
    internal enum CommandType
    {
        CHANGE_WORKER_POS,
        CHANGE_RESULT_POS,
        COMPLETE_DISH,
        COMPLETE_REQUIREMENT
    }

    internal interface ICommand
    {
        void ToBytes(BinaryWriter _bw);
        void FromBytes(BinaryReader _br);
    }
}
