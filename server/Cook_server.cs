using System.IO;
using System.Collections.Generic;
using System;

namespace Cook_lib
{
    public class Cook_server
    {
        private static Random random = new Random();

        private CookMain main = new CookMain();

        private Action<bool, bool, MemoryStream> serverSendDataCallBack;

        private List<ICommand> commandList = new List<ICommand>();

        public void ServerSetCallBack(Action<bool, bool, MemoryStream> _serverSendDataCallBack)
        {
            serverSendDataCallBack = _serverSendDataCallBack;
        }

        public void ServerStart(IList<int> _mDish, IList<int> _oDish)
        {
            main.Start(_mDish, _oDish);
        }

        public void ServerGetPackage(bool _isMine, BinaryReader _br)
        {
            byte type = _br.ReadByte();

            switch (type)
            {
                case PackageTag.C2S_DOACTION:

                    ServerGetCommand(_isMine, _br);

                    break;

                case PackageTag.C2S_REFRESH:

                    ServerRefreshData(_isMine);

                    break;
            }
        }

        private void ServerGetCommand(bool _isMine, BinaryReader _br)
        {
            ICommand command;

            CommandType commandType = (CommandType)_br.ReadByte();

            switch (commandType)
            {
                case CommandType.CHANGE_WORKER_POS:

                    command = new CommandChangeWorkerPos();

                    break;

                case CommandType.CHANGE_RESULT_POS:

                    command = new CommandChangeResultPos();

                    break;

                case CommandType.COMPLETE_DISH:

                    command = new CommandCompleteDish();

                    break;

                default:

                    command = new CommandCompleteRequirement();

                    break;
            }

            command.FromBytes(_br);

            command.SetIsMine(_isMine);

            commandList.Add(command);
        }

        private void ServerRefreshData(bool _isMine)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(_isMine);

                    main.ToBytes(bw);

                    serverSendDataCallBack(_isMine, false, ms);
                }
            }
        }

        public void ServerUpdate()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    int randomSeed = random.Next();

                    bw.Write(PackageTag.S2C_UPDATE);

                    bw.Write(main.tick);

                    bw.Write(randomSeed);

                    bw.Write(commandList.Count);

                    for (int i = 0; i < commandList.Count; i++)
                    {
                        ICommand command = commandList[i];

                        command.ToBytes(bw);

                        if (command is CommandChangeWorkerPos)
                        {
                            main.GetCommandChangeWorkerPos((CommandChangeWorkerPos)command);
                        }
                        else if (command is CommandChangeResultPos)
                        {
                            main.GetCommandChangeResultPos((CommandChangeResultPos)command);
                        }
                        else if (command is CommandCompleteDish)
                        {
                            main.GetCommandCompleteDish((CommandCompleteDish)command);
                        }
                        else
                        {
                            main.GetCommandCompleteRequirement((CommandCompleteRequirement)command);
                        }
                    }

                    commandList.Clear();

                    serverSendDataCallBack(true, true, ms);

                    serverSendDataCallBack(false, true, ms);

                    main.Update(randomSeed);
                }
            }
        }
    }
}
