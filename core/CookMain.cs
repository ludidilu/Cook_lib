using System;
using System.Collections.Generic;
using System.IO;

namespace Cook_lib
{
    public class CookMain
    {
        internal static Func<int, IDishSDS> getDishData;

        public static void Init<T>(Dictionary<int, T> _dic) where T : IDishSDS
        {
            getDishData = delegate (int _id)
            {
                return _dic[_id];
            };
        }

        public enum DishState
        {
            NULL,
            PREPAREING,
            COOKING,
            OPTIMIZING
        }

        public class DishData
        {
            public IDishSDS sds;
            public DishState state;
            public float time;
            public DishResult result;
        }

        public class DishResult : DishResultBase
        {
            public int time;
        }

        public class DishResultBase
        {
            public IDishSDS sds;
            public bool isOptimized;
        }

        public class DishRequirement
        {
            public int uid;
            public DishResultBase[] dishArr;
            public int time;
        }

        public class Worker
        {
            public int pos = -1;
            public int punishTick = 0;
        }

        private List<DishData> mDish = new List<DishData>();

        private List<DishData> oDish = new List<DishData>();

        private Worker[] mWorkers = new Worker[CookConst.WORKER_NUM];

        private Worker[] oWorkers = new Worker[CookConst.WORKER_NUM];

        private DishResult[] mResult = new DishResult[CookConst.RESULT_STATE.Length];

        private DishResult[] oResult = new DishResult[CookConst.RESULT_STATE.Length];

        private List<DishRequirement> require = new List<DishRequirement>();

        private List<IDishSDS> dishAll = new List<IDishSDS>();

        public int tick { private set; get; }

        private SuperRandom random = new SuperRandom();

        public CookMain()
        {
            for (int i = 0; i < CookConst.WORKER_NUM; i++)
            {
                mWorkers[i] = new Worker();
                oWorkers[i] = new Worker();
            }
        }

        public void Start(IList<int> _mDish, IList<int> _oDish)
        {
            Reset();

            for (int i = 0; i < _mDish.Count; i++)
            {
                DishData data = new DishData();

                IDishSDS sds = getDishData(_mDish[i]);

                data.sds = sds;

                mDish.Add(data);
            }

            for (int i = 0; i < _oDish.Count; i++)
            {
                DishData data = new DishData();

                IDishSDS sds = getDishData(_oDish[i]);

                data.sds = sds;

                oDish.Add(data);
            }

            InitDishAll();
        }

        private void InitDishAll()
        {
            for (int i = 0; i < mDish.Count; i++)
            {
                DishData data = mDish[i];

                if (!data.sds.GetIsUniversal() && !dishAll.Contains(data.sds))
                {
                    dishAll.Add(data.sds);
                }
            }

            for (int i = 0; i < oDish.Count; i++)
            {
                DishData data = oDish[i];

                if (!data.sds.GetIsUniversal() && !dishAll.Contains(data.sds))
                {
                    dishAll.Add(data.sds);
                }
            }
        }

        internal void Update(int _randomSeed)
        {
            random.SetSeed(_randomSeed);

            tick++;

            RefreshRequire();

            RefreshResult(mResult);

            RefreshResult(oResult);

            RefreshDish(mWorkers, mDish);

            RefreshDish(oWorkers, oDish);
        }

        private void RefreshRequire()
        {
            for (int i = require.Count - 1; i > -1; i--)
            {
                DishRequirement requirement = require[i];

                requirement.time++;

                if (requirement.time > CookConst.REQUIRE_EXCEED_TIME * CookConst.TICK_NUM_PER_SECOND)
                {
                    require.RemoveAt(i);
                }
            }

            if (tick % CookConst.REQUIRE_PRODUCE_TIME == 0)
            {
                DishRequirement requirement = GetRequire();

                require.Add(requirement);
            }
        }

        private DishRequirement GetRequire()
        {
            DishRequirement requirement = new DishRequirement();

            requirement.uid = GetRequirementUid();

            List<IDishSDS> list = new List<IDishSDS>(dishAll);

            int maxNum = list.Count;

            for (int i = 0; i < list.Count; i++)
            {
                IDishSDS sds = list[i];

                if (sds.GetMaxNum() > 1)
                {
                    maxNum += sds.GetMaxNum() - 1;
                }
            }

            if (maxNum > CookConst.REQUIRE_NUM_MAX)
            {
                maxNum = CookConst.REQUIRE_NUM_MAX;
            }

            if (maxNum < CookConst.REQUIRE_NUM_MIN)
            {
                throw new Exception("GetRequire error!");
            }



            int num = random.Get(CookConst.REQUIRE_NUM_MIN, maxNum + 1);

            DishResultBase[] resultArr = new DishResultBase[num];

            requirement.dishArr = resultArr;

            for (int i = 0; i < num; i++)
            {
                int index = random.Get(list.Count);

                IDishSDS sds = list[index];

                int oldNum = 1;

                for (int m = 0; m < i; m++)
                {
                    DishResultBase tmpResult = resultArr[m];

                    if (tmpResult.sds == sds)
                    {
                        oldNum++;
                    }
                }

                if (oldNum == sds.GetMaxNum())
                {
                    list.RemoveAt(index);
                }

                bool isOptimize = random.Get() < CookConst.REQUIRE_OPTIMIZE_PROBABILITY;

                DishResultBase result = new DishResultBase();

                result.sds = sds;

                result.isOptimized = isOptimize;

                resultArr[i] = result;
            }

            return requirement;
        }

        private void RefreshResult(DishResult[] _result)
        {
            for (int i = 0; i < _result.Length; i++)
            {
                DishResult result = _result[i];

                if (result != null && CookConst.RESULT_STATE[i])
                {
                    result.time++;

                    if (result.time > result.sds.GetExceedTime() * CookConst.TICK_NUM_PER_SECOND)
                    {
                        _result[i] = null;
                    }
                }
            }
        }

        private void RefreshDish(Worker[] _workers, List<DishData> _dish)
        {
            for (int i = 0; i < _dish.Count; i++)
            {
                DishData data = _dish[i];

                if (data.result != null)
                {
                    data.result.time++;

                    if (data.time > data.sds.GetExceedTime() * CookConst.TICK_NUM_PER_SECOND)
                    {
                        data.time = 0;

                        data.result = null;

                        data.state = DishState.NULL;
                    }
                }

                bool hasWorker = false;

                for (int m = 0; m < _workers.Length; m++)
                {
                    Worker worker = _workers[m];

                    if (worker.pos == i)
                    {
                        if (worker.punishTick == 0)
                        {
                            hasWorker = true;
                        }
                        else
                        {
                            worker.punishTick--;
                        }

                        break;
                    }
                }

                if (hasWorker)
                {
                    switch (data.state)
                    {
                        case DishState.PREPAREING:

                            data.time++;

                            if (data.time > data.sds.GetPrepareTime() * CookConst.TICK_NUM_PER_SECOND)
                            {
                                data.time = 0;

                                if (data.sds.GetCookTime() > 0)
                                {
                                    data.state = DishState.COOKING;
                                }
                                else
                                {
                                    data.state = DishState.OPTIMIZING;

                                    data.result = new DishResult();

                                    data.result.sds = data.sds;
                                }
                            }

                            break;

                        case DishState.COOKING:

                            data.time++;

                            if (data.time > data.sds.GetCookTime() * CookConst.TICK_NUM_PER_SECOND)
                            {
                                data.time = 0;

                                data.state = DishState.OPTIMIZING;

                                data.result = new DishResult();

                                data.result.sds = data.sds;
                            }

                            break;

                        case DishState.OPTIMIZING:

                            data.time++;

                            if (data.time > data.sds.GetOptimizeTime() * CookConst.TICK_NUM_PER_SECOND)
                            {
                                data.time = 0;

                                data.state = DishState.NULL;

                                data.result.isOptimized = true;
                            }

                            break;

                        default:

                            if (data.result == null)
                            {
                                data.time++;

                                data.state = DishState.PREPAREING;
                            }

                            break;
                    }
                }
                else
                {
                    switch (data.state)
                    {
                        case DishState.PREPAREING:

                            data.time -= data.sds.GetDecreaseValue();

                            if (data.time < 0)
                            {
                                data.time = 0;

                                data.state = DishState.NULL;
                            }

                            break;

                        case DishState.COOKING:

                            data.time++;

                            if (data.time > data.sds.GetCookTime() * CookConst.TICK_NUM_PER_SECOND)
                            {
                                data.time = 0;

                                data.state = DishState.OPTIMIZING;

                                data.result = new DishResult();

                                data.result.sds = data.sds;
                            }

                            break;
                    }
                }
            }
        }

        private int requirementUid = 0;

        private int GetRequirementUid()
        {
            requirementUid++;

            return requirementUid;
        }

        private void Reset()
        {
            tick = 0;

            mDish.Clear();
            oDish.Clear();

            for (int i = 0; i < CookConst.RESULT_STATE.Length; i++)
            {
                mResult[i] = null;
                oResult[i] = null;
            }

            require.Clear();
            dishAll.Clear();

            for (int i = 0; i < CookConst.WORKER_NUM; i++)
            {
                Worker worker = mWorkers[i];

                worker.pos = -1;

                worker.punishTick = 0;

                worker = oWorkers[i];

                worker.pos = -1;

                worker.punishTick = 0;
            }
        }













        internal void GetCommandChangeWorkerPos(CommandChangeWorkerPos _command)
        {
            if (_command.isMine)
            {
                GetCommandChangeWorkerPosReal(_command, mWorkers, mDish);
            }
            else
            {
                GetCommandChangeWorkerPosReal(_command, oWorkers, oDish);
            }
        }

        private void GetCommandChangeWorkerPosReal(CommandChangeWorkerPos _command, Worker[] _workers, List<DishData> _dish)
        {
            if (_command.pos > -2 && _command.pos < _dish.Count && _command.workerIndex > -1 && _command.workerIndex < CookConst.WORKER_NUM)
            {
                Worker worker = _workers[_command.workerIndex];

                if (worker.pos == _command.pos)
                {
                    return;
                }

                if (_command.pos == -1)
                {
                    worker.pos = -1;

                    worker.punishTick = 0;

                    return;
                }

                for (int i = 0; i < _workers.Length; i++)
                {
                    if (i != _command.workerIndex)
                    {
                        Worker tmpWorker = _workers[i];

                        if (tmpWorker.pos == _command.pos)
                        {
                            return;
                        }
                    }
                }

                worker.pos = _command.pos;

                worker.punishTick = CookConst.WORKER_PUNISH_TICK;
            }
        }

        internal void GetCommandChangeResultPos(CommandChangeResultPos _command)
        {
            if (_command.isMine)
            {
                GetCommandChangeResultPosReal(_command, mResult);
            }
            else
            {
                GetCommandChangeResultPosReal(_command, oResult);
            }
        }

        private void GetCommandChangeResultPosReal(CommandChangeResultPos _command, DishResult[] _result)
        {
            if (_command.pos > -1 && _command.pos < _result.Length)
            {
                DishResult result = _result[_command.pos];

                if (result != null)
                {
                    if (_command.targetPos == -1)
                    {
                        _result[_command.pos] = null;
                    }
                    else
                    {
                        if (_command.targetPos > -1 && _command.targetPos < _result.Length && _command.pos != _command.targetPos)
                        {
                            _result[_command.pos] = _result[_command.targetPos];

                            _result[_command.targetPos] = result;
                        }
                    }
                }
            }
        }

        internal void GetCommandCompleteDish(CommandCompleteDish _command)
        {
            if (_command.isMine)
            {
                GetCommandCompleteDishReal(_command, mDish, mResult);
            }
            else
            {
                GetCommandCompleteDishReal(_command, oDish, oResult);
            }
        }

        private void GetCommandCompleteDishReal(CommandCompleteDish _command, List<DishData> _dish, DishResult[] _result)
        {
            if (_command.pos > -1 && _command.pos < _dish.Count)
            {
                DishData dish = _dish[_command.pos];

                if (dish.result != null && _command.targetPos > -1 && _command.targetPos < _result.Length && _result[_command.targetPos] == null)
                {
                    _result[_command.targetPos] = dish.result;

                    dish.result = null;

                    dish.state = DishState.NULL;
                }
            }
        }

        internal void GetCommandCompleteRequirement(CommandCompleteRequirement _command)
        {
            for (int i = 0; i < require.Count; i++)
            {
                DishRequirement requirement = require[i];

                if (requirement.uid == _command.requirementUid)
                {
                    DishResult[] resultArr = _command.isMine ? mResult : oResult;

                    if (CheckIsCompleteRequirement(_command.resultList, resultArr, requirement))
                    {
                        AddReward(_command.isMine, requirement);

                        require.RemoveAt(i);

                        for (int m = 0; m < _command.resultList.Count; m++)
                        {
                            int index = _command.resultList[m];

                            resultArr[index] = null;
                        }
                    }

                    break;
                }
            }
        }

        private void AddReward(bool _isMine, DishRequirement _requirement)
        {

        }

        public bool CheckIsCompleteRequirement(List<int> _resultList, DishResult[] _resultArr, DishRequirement _requirement)
        {
            if (_resultList.Count != _requirement.dishArr.Length)
            {
                return false;
            }

            List<DishResult> resultList = new List<DishResult>();

            for (int i = 0; i < _resultList.Count; i++)
            {
                int index = _resultList[i];

                if (index > -1 && index < _resultArr.Length)
                {
                    DishResult result = _resultArr[index];

                    if (result != null && !resultList.Contains(result))
                    {
                        resultList.Add(result);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            List<DishResultBase> requirementList = new List<DishResultBase>(_requirement.dishArr);

            for (int i = resultList.Count - 1; i > -1; i--)
            {
                DishResult result = resultList[i];

                for (int m = requirementList.Count - 1; m > -1; m--)
                {
                    DishResultBase requirement = requirementList[m];

                    if (result.sds == requirement.sds && result.isOptimized == requirement.isOptimized)
                    {
                        resultList.RemoveAt(i);

                        requirementList.RemoveAt(m);

                        break;
                    }
                }
            }

            if (requirementList.Count == 0)
            {
                return true;
            }

            for (int i = resultList.Count - 1; i > -1; i--)
            {
                DishResult result = resultList[i];

                for (int m = requirementList.Count - 1; m > -1; m--)
                {
                    DishResultBase requirement = requirementList[m];

                    if (result.sds == requirement.sds && result.isOptimized)
                    {
                        resultList.RemoveAt(i);

                        requirementList.RemoveAt(m);

                        break;
                    }
                }
            }

            if (requirementList.Count == 0)
            {
                return true;
            }

            for (int i = resultList.Count - 1; i > -1; i--)
            {
                DishResult result = resultList[i];

                for (int m = requirementList.Count - 1; m > -1; m--)
                {
                    DishResultBase requirement = requirementList[m];

                    if (result.sds.GetIsUniversal() && (result.isOptimized == requirement.isOptimized || result.isOptimized))
                    {
                        resultList.RemoveAt(i);

                        requirementList.RemoveAt(m);

                        break;
                    }
                }
            }

            if (requirementList.Count == 0)
            {
                return true;
            }

            return false;
        }





















        internal MemoryStream ToBytes()
        {
            MemoryStream ms = new MemoryStream();

            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(tick);

            bw.Write(mDish.Count);

            for (int i = 0; i < mDish.Count; i++)
            {
                WriteDishData(mDish[i], bw);
            }

            bw.Write(oDish.Count);

            for (int i = 0; i < oDish.Count; i++)
            {
                WriteDishData(oDish[i], bw);
            }

            for (int i = 0; i < CookConst.WORKER_NUM; i++)
            {
                Worker worker = mWorkers[i];

                WriteWorker(worker, bw);

                worker = oWorkers[i];

                WriteWorker(worker, bw);
            }

            for (int i = 0; i < CookConst.RESULT_STATE.Length; i++)
            {
                DishResult result = mResult[i];

                if (result != null)
                {
                    bw.Write(true);

                    WriteDishResult(result, bw);
                }
                else
                {
                    bw.Write(false);
                }

                result = oResult[i];

                if (result != null)
                {
                    bw.Write(true);

                    WriteDishResult(result, bw);
                }
                else
                {
                    bw.Write(false);
                }
            }

            bw.Write(require.Count);

            for (int i = 0; i < require.Count; i++)
            {
                WriteDishRequirement(require[i], bw);
            }

            return ms;
        }

        internal void FromBytes(BinaryReader _br)
        {
            Reset();

            tick = _br.ReadInt32();

            int num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                DishData data = ReadDishData(_br);

                mDish.Add(data);
            }

            num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                DishData data = ReadDishData(_br);

                oDish.Add(data);
            }

            for (int i = 0; i < CookConst.WORKER_NUM; i++)
            {
                ReadWorker(mWorkers[i], _br);
                ReadWorker(oWorkers[i], _br);
            }

            for (int i = 0; i < CookConst.RESULT_STATE.Length; i++)
            {
                bool b = _br.ReadBoolean();

                if (b)
                {
                    mResult[i] = ReadDishResult(_br);
                }

                b = _br.ReadBoolean();

                if (b)
                {
                    oResult[i] = ReadDishResult(_br);
                }
            }

            num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                DishRequirement requirement = ReadDishRequirement(_br);

                require.Add(requirement);
            }

            InitDishAll();
        }

        private void WriteDishData(DishData _data, BinaryWriter _bw)
        {
            _bw.Write(_data.sds.GetID());

            _bw.Write((byte)_data.state);

            _bw.Write(_data.time);

            DishResult result = _data.result;

            if (result != null)
            {
                _bw.Write(true);

                WriteDishResult(result, _bw);
            }
            else
            {
                _bw.Write(false);
            }
        }

        private DishData ReadDishData(BinaryReader _br)
        {
            DishData data = new DishData();

            int id = _br.ReadInt32();

            data.sds = getDishData(id);

            data.state = (DishState)_br.ReadByte();

            data.time = _br.ReadSingle();

            bool b = _br.ReadBoolean();

            if (b)
            {
                data.result = ReadDishResult(_br);
            }

            return data;
        }

        private void WriteDishResult(DishResult _result, BinaryWriter _bw)
        {
            _bw.Write(_result.sds.GetID());

            _bw.Write(_result.isOptimized);

            _bw.Write(_result.time);
        }

        private DishResult ReadDishResult(BinaryReader _br)
        {
            DishResult result = new DishResult();

            int id = _br.ReadInt32();

            result.sds = getDishData(id);

            result.isOptimized = _br.ReadBoolean();

            result.time = _br.ReadInt32();

            return result;
        }

        private void WriteDishResultBase(DishResultBase _result, BinaryWriter _bw)
        {
            _bw.Write(_result.sds.GetID());

            _bw.Write(_result.isOptimized);
        }

        private DishResultBase ReadDishResultBase(BinaryReader _br)
        {
            DishResultBase result = new DishResultBase();

            int id = _br.ReadInt32();

            result.sds = getDishData(id);

            result.isOptimized = _br.ReadBoolean();

            return result;
        }

        private void WriteDishRequirement(DishRequirement _requirement, BinaryWriter _bw)
        {
            _bw.Write(_requirement.uid);

            _bw.Write(_requirement.dishArr.Length);

            for (int i = 0; i < _requirement.dishArr.Length; i++)
            {
                WriteDishResultBase(_requirement.dishArr[i], _bw);
            }

            _bw.Write(_requirement.time);
        }

        private DishRequirement ReadDishRequirement(BinaryReader _br)
        {
            DishRequirement requirement = new DishRequirement();

            requirement.uid = _br.ReadInt32();

            int num = _br.ReadInt32();

            requirement.dishArr = new DishResultBase[num];

            for (int i = 0; i < num; i++)
            {
                requirement.dishArr[i] = ReadDishResultBase(_br);
            }

            requirement.time = _br.ReadInt32();

            return requirement;
        }

        private void WriteWorker(Worker _worker, BinaryWriter _bw)
        {
            _bw.Write(_worker.pos);

            _bw.Write(_worker.punishTick);
        }

        private void ReadWorker(Worker _worker, BinaryReader _br)
        {
            _worker.pos = _br.ReadInt32();

            _worker.punishTick = _br.ReadInt32();
        }
    }
}
