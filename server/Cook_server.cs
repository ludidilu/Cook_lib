using System.IO;
using System.Collections.Generic;
using System;

namespace Cook_lib
{
    public class Cook_server
    {
        public static void Init<T, U>(Dictionary<int, T> _dishDic, Dictionary<int, U> _resultDic) where T : IDishSDS where U : IResultSDS
        {
            CookMain.Init(_dishDic, _resultDic);
        }

        private static Random random = new Random();

        private CookMain main = new CookMain();

        private Action<bool, bool, MemoryStream> serverSendDataCallBack;

        private Action<GameResult> battleOverCallBack;

        private List<ushort> seedList = new List<ushort>();

        private ushort tick;

        private List<ICommand> commandList = new List<ICommand>();

        public void ServerSetCallBack(Action<bool, bool, MemoryStream> _serverSendDataCallBack, Action<GameResult> _battleOverCallBack)
        {
            serverSendDataCallBack = _serverSendDataCallBack;

            battleOverCallBack = _battleOverCallBack;
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
            if (tick > main.tick)
            {
                main.UpdateTo(tick, seedList);
            }

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
                    bw.Write(PackageTag.S2C_UPDATE);

                    bw.Write(main.tick);

                    if (main.tick % CookConst.REQUIRE_PRODUCE_TIME == 0)
                    {
                        ushort randomSeed = (ushort)random.Next(ushort.MaxValue);

                        bw.Write(randomSeed);

                        main.SetSeed(randomSeed);
                    }

                    main.Update();

                    bw.Write((ushort)commandList.Count);

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

                    if (commandList.Count > 0)
                    {
                        CookTest.server = main;

                        CookTest.Check();
                    }

                    commandList.Clear();

                    serverSendDataCallBack(true, true, ms);

                    serverSendDataCallBack(false, true, ms);

                    if (tick > CookConst.MAX_TIME * CookConst.TICK_NUM_PER_SECOND)
                    {
                        GameResult gameResult = main.GetGameResult();

                        battleOverCallBack(gameResult);
                    }
                }
            }
        }

        public void ServerUpdateTo()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(PackageTag.S2C_UPDATE);

                    bw.Write(tick);

                    if (tick % CookConst.REQUIRE_PRODUCE_TIME == 0)
                    {
                        ushort randomSeed = (ushort)random.Next(ushort.MaxValue);

                        bw.Write(randomSeed);

                        seedList.Add(randomSeed);
                    }

                    bw.Write((ushort)commandList.Count);

                    tick++;

                    if (commandList.Count > 0 || tick > CookConst.MAX_TIME * CookConst.TICK_NUM_PER_SECOND)
                    {
                        main.UpdateTo(tick, seedList);
                    }

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

                    if (commandList.Count > 0)
                    {
                        CookTest.server = main;

                        CookTest.Check();
                    }

                    commandList.Clear();

                    serverSendDataCallBack(true, true, ms);

                    serverSendDataCallBack(false, true, ms);

                    if (tick > CookConst.MAX_TIME * CookConst.TICK_NUM_PER_SECOND)
                    {
                        GameResult gameResult = main.GetGameResult();

                        battleOverCallBack(gameResult);
                    }
                }
            }
        }
    }
}
