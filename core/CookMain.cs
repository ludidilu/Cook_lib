using System;
using System.Collections.Generic;
using System.IO;
using superRandom;

namespace Cook_lib
{
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

    internal class CookMain
    {
        internal static Func<int, IDishSDS> getDishData;

        internal static void Init<T>(Dictionary<int, T> _dic) where T : IDishSDS
        {
            getDishData = delegate (int _id)
            {
                return _dic[_id];
            };
        }

        internal List<DishData> mDish = new List<DishData>();

        internal List<DishData> oDish = new List<DishData>();

        internal Worker[] mWorkers = new Worker[CookConst.WORKER_NUM];

        internal Worker[] oWorkers = new Worker[CookConst.WORKER_NUM];

        internal DishResult[] mResult = new DishResult[CookConst.RESULT_STATE.Length];

        internal DishResult[] oResult = new DishResult[CookConst.RESULT_STATE.Length];

        internal List<DishRequirement> require = new List<DishRequirement>();

        private List<IDishSDS> dishAll = new List<IDishSDS>();

        internal int tick { private set; get; }

        private SuperRandom random = new SuperRandom();

        private Action<ValueType> eventCallBack;

        public CookMain()
        {
            for (int i = 0; i < CookConst.WORKER_NUM; i++)
            {
                mWorkers[i] = new Worker();
                oWorkers[i] = new Worker();
            }
        }

        internal void SetEventCallBack(Action<ValueType> _eventCallBack)
        {
            eventCallBack = _eventCallBack;
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

            RefreshWorker(true);

            RefreshWorker(false);

            RefreshResult(true);

            RefreshResult(false);

            RefreshDish(true);

            RefreshDish(false);
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

                    eventCallBack?.Invoke(new EventRequirementDisappear(requirement.uid));
                }
            }

            if (tick % CookConst.REQUIRE_PRODUCE_TIME == 0)
            {
                DishRequirement requirement = GetRequire();

                require.Add(requirement);

                eventCallBack?.Invoke(new EventRequirementAppear(requirement.uid));
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

            int optimizeNum = random.Get((int)(CookConst.OPTIMIZE_PROBABILITY_MAX * num) + 1);

            for (int i = 0; i < num; i++)
            {
                int index = random.Get(list.Count);

                IDishSDS sds = list[index];

                if (sds.GetMaxNum() > 1)
                {
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
                }
                else
                {
                    list.RemoveAt(index);
                }

                bool isOptimize = i < optimizeNum;

                DishResultBase result = new DishResultBase();

                result.sds = sds;

                result.isOptimized = isOptimize;

                resultArr[i] = result;
            }

            return requirement;
        }

        private void RefreshWorker(bool _isMine)
        {
            Worker[] workers = _isMine ? mWorkers : oWorkers;

            for (int i = 0; i < CookConst.WORKER_NUM; i++)
            {
                Worker worker = workers[i];

                if (worker.punishTick > 0)
                {
                    worker.punishTick--;
                }
            }
        }

        private void RefreshResult(bool _isMine)
        {
            DishResult[] results = _isMine ? mResult : oResult;

            for (int i = 0; i < results.Length; i++)
            {
                DishResult result = results[i];

                if (result != null && CookConst.RESULT_STATE[i])
                {
                    result.time++;

                    if (result.time > result.sds.GetExceedTime() * CookConst.TICK_NUM_PER_SECOND)
                    {
                        results[i] = null;

                        eventCallBack?.Invoke(new EventResultDisappear(_isMine, i));
                    }
                }
            }
        }

        private void RefreshDish(bool _isMine)
        {
            Worker[] workers;
            List<DishData> dish;

            if (_isMine)
            {
                workers = mWorkers;
                dish = mDish;
            }
            else
            {
                workers = oWorkers;
                dish = oDish;
            }

            for (int i = 0; i < dish.Count; i++)
            {
                DishData data = dish[i];

                if (data.result != null)
                {
                    data.result.time++;

                    if (data.time > data.sds.GetExceedTime() * CookConst.TICK_NUM_PER_SECOND)
                    {
                        data.time = 0;

                        data.result = null;

                        data.state = DishState.NULL;

                        eventCallBack?.Invoke(new EventDishResultDisappear(_isMine, i));
                    }
                }

                bool hasWorker = false;

                for (int m = 0; m < workers.Length; m++)
                {
                    Worker worker = workers[m];

                    if (worker.pos == i)
                    {
                        if (worker.punishTick == 0)
                        {
                            hasWorker = true;
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

                                    eventCallBack?.Invoke(new EventDishResultAppear(_isMine, i));
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

                                eventCallBack?.Invoke(new EventDishResultAppear(_isMine, i));
                            }

                            break;

                        case DishState.OPTIMIZING:

                            data.time++;

                            if (data.time > data.sds.GetOptimizeTime() * CookConst.TICK_NUM_PER_SECOND)
                            {
                                data.time = 0;

                                data.state = DishState.NULL;

                                data.result.isOptimized = true;

                                eventCallBack?.Invoke(new EventDishResultBeOptimized(_isMine, i));
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

                                eventCallBack?.Invoke(new EventDishResultAppear(_isMine, i));
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
            if (_command.targetPos > -2 && _command.targetPos < _dish.Count && _command.workerIndex > -1 && _command.workerIndex < CookConst.WORKER_NUM)
            {
                Worker worker = _workers[_command.workerIndex];

                if (worker.pos == _command.targetPos)
                {
                    return;
                }

                if (_command.targetPos == -1)
                {
                    worker.pos = -1;

                    worker.punishTick = CookConst.WORKER_PUNISH_TICK;
                }
                else
                {
                    if (!CheckCanChangeWorkerPos(_command.workerIndex, _workers, _command.targetPos))
                    {
                        return;
                    }

                    if (worker.pos != -1)
                    {
                        worker.punishTick = CookConst.WORKER_PUNISH_TICK;
                    }

                    worker.pos = _command.targetPos;
                }

                eventCallBack?.Invoke(_command);
            }
        }

        private bool CheckCanChangeWorkerPos(int _workerIndex, Worker[] _workers, int _targetPos)
        {
            for (int i = 0; i < _workers.Length; i++)
            {
                if (i != _workerIndex)
                {
                    Worker tmpWorker = _workers[i];

                    if (tmpWorker.pos == _targetPos)
                    {
                        return false;
                    }
                }
            }

            return true;
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

                        eventCallBack?.Invoke(_command);
                    }
                    else
                    {
                        if (_command.targetPos > -1 && _command.targetPos < _result.Length && _command.pos != _command.targetPos)
                        {
                            _result[_command.pos] = _result[_command.targetPos];

                            _result[_command.targetPos] = result;

                            eventCallBack?.Invoke(_command);
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

                    eventCallBack?.Invoke(_command);
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

                    if (CheckCanCompleteRequirement(_command.resultList, resultArr, requirement))
                    {
                        AddReward(_command.isMine, requirement);

                        require.RemoveAt(i);

                        for (int m = 0; m < _command.resultList.Count; m++)
                        {
                            int index = _command.resultList[m];

                            resultArr[index] = null;
                        }

                        eventCallBack?.Invoke(_command);
                    }

                    break;
                }
            }
        }

        private void AddReward(bool _isMine, DishRequirement _requirement)
        {

        }

        public bool CheckCanCompleteRequirement(List<int> _resultList, DishResult[] _resultArr, DishRequirement _requirement)
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





















        internal void ToBytes(BinaryWriter _bw)
        {
            _bw.Write(tick);

            _bw.Write(mDish.Count);

            for (int i = 0; i < mDish.Count; i++)
            {
                WriteDishData(mDish[i], _bw);
            }

            _bw.Write(oDish.Count);

            for (int i = 0; i < oDish.Count; i++)
            {
                WriteDishData(oDish[i], _bw);
            }

            for (int i = 0; i < CookConst.WORKER_NUM; i++)
            {
                Worker worker = mWorkers[i];

                WriteWorker(worker, _bw);

                worker = oWorkers[i];

                WriteWorker(worker, _bw);
            }

            for (int i = 0; i < CookConst.RESULT_STATE.Length; i++)
            {
                DishResult result = mResult[i];

                if (result != null)
                {
                    _bw.Write(true);

                    WriteDishResult(result, _bw);
                }
                else
                {
                    _bw.Write(false);
                }

                result = oResult[i];

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

            _bw.Write(require.Count);

            for (int i = 0; i < require.Count; i++)
            {
                WriteDishRequirement(require[i], _bw);
            }
        }

        internal void FromBytes(BinaryReader _br)
        {
            Reset();

            tick = _br.ReadInt32();

            int num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                DishData data = new DishData();

                ReadDishData(data, _br);

                mDish.Add(data);
            }

            num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                DishData data = new DishData();

                ReadDishData(data, _br);

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
                    DishResult result = mResult[i];

                    if (result == null)
                    {
                        result = new DishResult();

                        mResult[i] = result;
                    }

                    ReadDishResult(result, _br);
                }
                else
                {
                    mResult[i] = null;
                }

                b = _br.ReadBoolean();

                if (b)
                {
                    DishResult result = oResult[i];

                    if (result == null)
                    {
                        result = new DishResult();

                        oResult[i] = result;
                    }

                    ReadDishResult(result, _br);
                }
                else
                {
                    oResult[i] = null;
                }
            }

            num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                DishRequirement requirement = new DishRequirement();

                ReadDishRequirement(requirement, _br);

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

        private void ReadDishData(DishData _data, BinaryReader _br)
        {
            int id = _br.ReadInt32();

            _data.sds = getDishData(id);

            _data.state = (DishState)_br.ReadByte();

            _data.time = _br.ReadSingle();

            bool b = _br.ReadBoolean();

            if (b)
            {
                DishResult result = _data.result;

                if (result == null)
                {
                    result = new DishResult();

                    _data.result = result;
                }

                ReadDishResult(result, _br);
            }
            else
            {
                _data.result = null;
            }
        }

        private void WriteDishResult(DishResult _result, BinaryWriter _bw)
        {
            _bw.Write(_result.sds.GetID());

            _bw.Write(_result.isOptimized);

            _bw.Write(_result.time);
        }

        private void ReadDishResult(DishResult _result, BinaryReader _br)
        {
            int id = _br.ReadInt32();

            _result.sds = getDishData(id);

            _result.isOptimized = _br.ReadBoolean();

            _result.time = _br.ReadInt32();
        }

        private void WriteDishResultBase(DishResultBase _result, BinaryWriter _bw)
        {
            _bw.Write(_result.sds.GetID());

            _bw.Write(_result.isOptimized);
        }

        private void ReadDishResultBase(DishResultBase _result, BinaryReader _br)
        {
            int id = _br.ReadInt32();

            _result.sds = getDishData(id);

            _result.isOptimized = _br.ReadBoolean();
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

        private void ReadDishRequirement(DishRequirement _requirement, BinaryReader _br)
        {
            _requirement.uid = _br.ReadInt32();

            int num = _br.ReadInt32();

            _requirement.dishArr = new DishResultBase[num];

            for (int i = 0; i < num; i++)
            {
                DishResultBase result = new DishResultBase();

                ReadDishResultBase(result, _br);

                _requirement.dishArr[i] = result;
            }

            _requirement.time = _br.ReadInt32();
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
