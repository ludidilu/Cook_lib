using System;
using System.Collections.Generic;
using System.IO;

namespace Cook_lib
{
    public class Cook_client
    {
        private CookMain main = new CookMain();

        private Action<MemoryStream> sendDataCallBack;

        private Action<MemoryStream, Action<BinaryReader>> sendDataCallBackWithReply;

        private Action refreshDataCallBack;

        public bool clientIsMine { private set; get; }

        public void SetCallBack(Action<MemoryStream> _sendDataCallBack, Action<MemoryStream, Action<BinaryReader>> _sendDataCallBackWithReply, Action _refreshDataCallBack, Action<ValueType> _eventCallBack)
        {
            sendDataCallBack = _sendDataCallBack;

            sendDataCallBackWithReply = _sendDataCallBackWithReply;

            refreshDataCallBack = _refreshDataCallBack;

            main.SetEventCallBack(_eventCallBack);
        }

        public void RefreshData()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(PackageTag.C2S_REFRESH);

                    sendDataCallBackWithReply(ms, GetRefreshData);
                }
            }
        }

        private void GetRefreshData(BinaryReader _br)
        {
            clientIsMine = _br.ReadBoolean();

            main.FromBytes(_br);

            refreshDataCallBack();
        }

        public void ClientGetPackage(BinaryReader _br)
        {
            byte type = _br.ReadByte();

            switch (type)
            {
                case PackageTag.S2C_UPDATE:

                    Update(_br);

                    break;
            }
        }

        private void Update(BinaryReader _br)
        {
            int tick = _br.ReadInt32();

            int randomSeed = _br.ReadInt32();

            int num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                CommandType commandType = (CommandType)_br.ReadByte();

                switch (commandType)
                {
                    case CommandType.CHANGE_RESULT_POS:

                        CommandChangeResultPos commandChangeResultPos = new CommandChangeResultPos();

                        commandChangeResultPos.FromBytes(_br);

                        main.GetCommandChangeResultPos(commandChangeResultPos);

                        break;

                    case CommandType.CHANGE_WORKER_POS:

                        CommandChangeWorkerPos commandChangeWorkerPos = new CommandChangeWorkerPos();

                        commandChangeWorkerPos.FromBytes(_br);

                        main.GetCommandChangeWorkerPos(commandChangeWorkerPos);

                        break;

                    case CommandType.COMPLETE_DISH:

                        CommandCompleteDish commandCompleteDish = new CommandCompleteDish();

                        commandCompleteDish.FromBytes(_br);

                        main.GetCommandCompleteDish(commandCompleteDish);

                        break;

                    default:

                        CommandCompleteRequirement commandCompleteRequirement = new CommandCompleteRequirement();

                        commandCompleteRequirement.FromBytes(_br);

                        main.GetCommandCompleteRequirement(commandCompleteRequirement);

                        break;
                }
            }

            main.Update(randomSeed);
        }

        public void ChangeResultPos(int _pos, int _targetPos)
        {
            CommandChangeResultPos command = new CommandChangeResultPos(clientIsMine, _pos, _targetPos);

            SendCommand(command);
        }

        public void ChangeWorkerPos(int _workerIndex, int _targetPos)
        {
            CommandChangeWorkerPos command = new CommandChangeWorkerPos(clientIsMine, _workerIndex, _targetPos);

            SendCommand(command);
        }

        public void CompleteDish(int _pos, int _targetPos)
        {
            CommandCompleteDish command = new CommandCompleteDish(clientIsMine, _pos, _targetPos);

            SendCommand(command);
        }

        public void CompleteRequirement(List<int> _resultList, int _requirementUid)
        {
            CommandCompleteRequirement command = new CommandCompleteRequirement(clientIsMine, _resultList, _requirementUid);

            SendCommand(command);
        }

        private void SendCommand(ICommand _command)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(PackageTag.C2S_DOACTION);

                    _command.ToBytes(bw);

                    sendDataCallBack(ms);
                }
            }
        }
    }
}
