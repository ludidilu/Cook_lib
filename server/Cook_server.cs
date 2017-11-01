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
            serverSendDataCallBack(_isMine, false, new MemoryStream());

            ICommand command;

            CommandType commandType = (CommandType)_br.ReadByte();

            switch (commandType)
            {
                case CommandType.CHANGE_WORKER_POS:

                    command = new CommandChangeWorkerPos();

                    command.FromBytes(_br);

                    main.GetCommandChangeWorkerPos((CommandChangeWorkerPos)command);

                    break;

                case CommandType.CHANGE_RESULT_POS:

                    command = new CommandChangeResultPos();

                    command.FromBytes(_br);

                    main.GetCommandChangeResultPos((CommandChangeResultPos)command);

                    break;

                case CommandType.COMPLETE_DISH:

                    command = new CommandCompleteDish();

                    command.FromBytes(_br);

                    main.GetCommandCompleteDish((CommandCompleteDish)command);

                    break;

                default:

                    command = new CommandCompleteRequirement();

                    command.FromBytes(_br);

                    main.GetCommandCompleteRequirement((CommandCompleteRequirement)command);

                    break;
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(PackageTag.S2C_DOACTION);

                    command.ToBytes(bw);

                    serverSendDataCallBack(true, true, ms);

                    serverSendDataCallBack(false, true, ms);
                }
            }
        }

        private void ServerRefreshData(bool _isMine)
        {
            using (MemoryStream ms = main.ToBytes())
            {
                serverSendDataCallBack(_isMine, false, ms);
            }
        }

        public void ServerUpdate()
        {
            int randomSeed = random.Next();

            main.Update(randomSeed);

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(PackageTag.S2C_UPDATE);

                    bw.Write(main.tick);

                    bw.Write(randomSeed);

                    serverSendDataCallBack(true, true, ms);

                    serverSendDataCallBack(false, true, ms);
                }
            }
        }
    }
}
