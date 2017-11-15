using System;
using System.Collections.Generic;
using System.IO;
using superRandom;

namespace Cook_lib
{
    internal class CookMain
    {
        internal static Func<int, IDishSDS> getDishData;

        internal static Func<int, IResultSDS> getResultData;

        internal static void Init<T, U>(Dictionary<int, T> _dishDic, Dictionary<int, U> _resultDic) where T : IDishSDS where U : IResultSDS
        {
            getDishData = delegate (int _id)
            {
                return _dishDic[_id];
            };

            getResultData = delegate (int _id)
            {
                return _resultDic[_id];
            };
        }

        internal PlayerData mData = new PlayerData();

        internal PlayerData oData = new PlayerData();

        internal Dictionary<int, DishRequirement> require = new Dictionary<int, DishRequirement>();

        private List<IResultSDS> dishAll = new List<IResultSDS>();

        internal ushort tick { private set; get; }

        private SuperRandom random = new SuperRandom();

        private Action<ValueType> eventCallBack;

        internal void SetEventCallBack(Action<ValueType> _eventCallBack)
        {
            eventCallBack = _eventCallBack;
        }

        public void Start(IList<int> _mDish, IList<int> _oDish)
        {
            Reset();

            mData.SetDishData(_mDish);

            oData.SetDishData(_oDish);

            InitDishAll(mData);

            InitDishAll(oData);
        }

        private void InitDishAll(PlayerData _playerData)
        {
            for (int i = 0; i < _playerData.dish.Count; i++)
            {
                DishData data = _playerData.dish[i];

                if (!data.sds.GetResult().GetIsUniversal() && !dishAll.Contains(data.sds.GetResult()))
                {
                    dishAll.Add(data.sds.GetResult());
                }
            }
        }

        internal void SetSeed(int _seed)
        {
            random.SetSeed(_seed);
        }

        internal void Update()
        {
            RefreshRequire();

            RefreshWorker(true);

            RefreshWorker(false);

            RefreshResult(true);

            RefreshResult(false);

            RefreshDish(true);

            RefreshDish(false);

            tick++;
        }

        private void RefreshRequire()
        {
            List<int> delList = null;

            IEnumerator<DishRequirement> enumerator = require.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                DishRequirement requirement = enumerator.Current;

                requirement.time++;

                if (requirement.time > CookConst.REQUIRE_EXCEED_TIME * CookConst.TICK_NUM_PER_SECOND)
                {
                    if (delList == null)
                    {
                        delList = new List<int>();
                    }

                    delList.Add(requirement.uid);

                    eventCallBack?.Invoke(new EventRequirementDisappear(requirement.uid));
                }
            }

            if (delList != null)
            {
                for (int i = 0; i < delList.Count; i++)
                {
                    require.Remove(delList[i]);
                }
            }

            if (tick % CookConst.REQUIRE_PRODUCE_TIME == 0)
            {
                DishRequirement requirement = GetRequire();

                require.Add(requirement.uid, requirement);

                eventCallBack?.Invoke(new EventRequirementAppear(requirement.uid));
            }
        }

        private DishRequirement GetRequire()
        {
            DishRequirement requirement = new DishRequirement();

            requirement.uid = GetRequirementUid();

            List<IResultSDS> list = new List<IResultSDS>(dishAll);

            int maxNum = list.Count;

            for (int i = 0; i < list.Count; i++)
            {
                IResultSDS sds = list[i];

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

                IResultSDS sds = list[index];

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
            Worker[] workers = _isMine ? mData.workers : oData.workers;

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
            DishResult[] results = _isMine ? mData.result : oData.result;

            for (int i = 0; i < results.Length; i++)
            {
                DishResult result = results[i];

                if (result != null && CookConst.RESULT_STATE[i])
                {
                    result.time += CookConst.RESULT_STATE[i] ? CookConst.EXCEED_VALUE_2 : CookConst.EXCEED_VALUE_1;

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
                workers = mData.workers;
                dish = mData.dish;
            }
            else
            {
                workers = oData.workers;
                dish = oData.dish;
            }

            for (int i = 0; i < dish.Count; i++)
            {
                DishData data = dish[i];

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

                                    data.result.sds = data.sds.GetResult();

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

                                data.result.sds = data.sds.GetResult();

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
                            else
                            {
                                data.result.time += CookConst.EXCEED_VALUE_3;

                                if (data.result.time > data.result.sds.GetExceedTime() * CookConst.TICK_NUM_PER_SECOND)
                                {
                                    data.time = 0;

                                    data.result = null;

                                    data.state = DishState.NULL;

                                    eventCallBack?.Invoke(new EventDishResultDisappear(_isMine, i));
                                }
                            }

                            break;
                    }
                }
                else
                {
                    if (data.result != null)
                    {
                        data.result.time += CookConst.EXCEED_VALUE_3;

                        if (data.result.time > data.result.sds.GetExceedTime() * CookConst.TICK_NUM_PER_SECOND)
                        {
                            data.time = 0;

                            data.result = null;

                            data.state = DishState.NULL;

                            eventCallBack?.Invoke(new EventDishResultDisappear(_isMine, i));
                        }
                    }

                    switch (data.state)
                    {
                        case DishState.PREPAREING:

                            data.time -= data.sds.GetPrepareDecreaseValue();

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

                                data.result.sds = data.sds.GetResult();

                                eventCallBack?.Invoke(new EventDishResultAppear(_isMine, i));
                            }

                            break;

                        case DishState.OPTIMIZING:

                            data.time -= data.sds.GetOptimizeDecreaseValue();

                            if (data.time < 0)
                            {
                                data.time = 0;
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

            mData.Clear();
            oData.Clear();

            require.Clear();
            dishAll.Clear();
        }













        internal void GetCommandChangeWorkerPos(CommandChangeWorkerPos _command)
        {
            if (_command.isMine)
            {
                GetCommandChangeWorkerPosReal(_command, mData);
            }
            else
            {
                GetCommandChangeWorkerPosReal(_command, oData);
            }
        }

        private void GetCommandChangeWorkerPosReal(CommandChangeWorkerPos _command, PlayerData _playerData)
        {
            if (_command.targetPos >= -CookConst.WORKER_NUM && _command.targetPos < _playerData.dish.Count && _command.workerIndex > -1 && _command.workerIndex < CookConst.WORKER_NUM)
            {
                Worker worker = _playerData.workers[_command.workerIndex];

                if (worker.pos == _command.targetPos)
                {
                    return;
                }

                if (_command.targetPos < 0)
                {
                    worker.pos = _command.targetPos;

                    worker.punishTick = (int)(CookConst.WORKER_PUNISH_TIME * CookConst.TICK_NUM_PER_SECOND);
                }
                else
                {
                    if (!CheckCanChangeWorkerPos(_command.workerIndex, _playerData.workers, _command.targetPos))
                    {
                        return;
                    }

                    if (worker.pos > -1)
                    {
                        worker.punishTick = (int)(CookConst.WORKER_PUNISH_TIME * CookConst.TICK_NUM_PER_SECOND);
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
                GetCommandChangeResultPosReal(_command, mData);
            }
            else
            {
                GetCommandChangeResultPosReal(_command, oData);
            }
        }

        private void GetCommandChangeResultPosReal(CommandChangeResultPos _command, PlayerData _playerData)
        {
            if (_command.pos > -1 && _command.pos < _playerData.result.Length)
            {
                DishResult result = _playerData.result[_command.pos];

                if (result != null)
                {
                    if (_command.targetPos == -1)
                    {
                        _playerData.result[_command.pos] = null;

                        eventCallBack?.Invoke(_command);
                    }
                    else
                    {
                        if (_command.targetPos > -1 && _command.targetPos < _playerData.result.Length && _command.pos != _command.targetPos)
                        {
                            _playerData.result[_command.pos] = _playerData.result[_command.targetPos];

                            _playerData.result[_command.targetPos] = result;

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
                GetCommandCompleteDishReal(_command, mData);
            }
            else
            {
                GetCommandCompleteDishReal(_command, oData);
            }
        }

        private void GetCommandCompleteDishReal(CommandCompleteDish _command, PlayerData _playerData)
        {
            if (_command.pos > -1 && _command.pos < _playerData.dish.Count)
            {
                DishData dish = _playerData.dish[_command.pos];

                if (dish.result != null)
                {
                    if (_command.targetPos > -1 && _command.targetPos < _playerData.result.Length && _playerData.result[_command.targetPos] == null)
                    {
                        _playerData.result[_command.targetPos] = dish.result;
                    }

                    dish.result = null;

                    dish.state = DishState.NULL;

                    dish.time = 0;

                    eventCallBack?.Invoke(_command);
                }
            }
        }

        internal void GetCommandCompleteRequirement(CommandCompleteRequirement _command)
        {
            DishRequirement requirement;

            if (require.TryGetValue(_command.requirementUid, out requirement))
            {
                PlayerData playerData = _command.isMine ? mData : oData;

                if (CheckCanCompleteRequirement(_command.resultList, playerData, requirement))
                {
                    AddReward(_command.isMine, requirement);

                    require.Remove(requirement.uid);

                    for (int m = 0; m < _command.resultList.Count; m++)
                    {
                        int index = _command.resultList[m];

                        if (index > -1)
                        {
                            playerData.result[index] = null;
                        }
                        else
                        {
                            DishData dish = playerData.dish[-index - 1];

                            dish.result = null;

                            dish.state = DishState.NULL;

                            dish.time = 0;
                        }
                    }

                    eventCallBack?.Invoke(_command);
                }
            }
        }

        private void AddReward(bool _isMine, DishRequirement _requirement)
        {

        }

        public bool CheckCanCompleteRequirement(List<int> _resultList, PlayerData _playerData, DishRequirement _requirement)
        {
            if (_resultList.Count != _requirement.dishArr.Length)
            {
                return false;
            }

            List<DishResult> resultList = new List<DishResult>();

            for (int i = 0; i < _resultList.Count; i++)
            {
                int index = _resultList[i];

                if (index > -1 && index < _playerData.result.Length)
                {
                    DishResult result = _playerData.result[index];

                    if (result != null && !resultList.Contains(result))
                    {
                        resultList.Add(result);
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (index < 0 && index > -_playerData.dish.Count - 1)
                {
                    DishResult result = _playerData.dish[-index - 1].result;

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

            mData.ToBytes(_bw);

            oData.ToBytes(_bw);

            _bw.Write(require.Count);

            IEnumerator<DishRequirement> enumerator = require.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                enumerator.Current.ToBytes(_bw);
            }
        }

        internal void FromBytes(BinaryReader _br)
        {
            Reset();

            tick = _br.ReadUInt16();

            mData.FromBytes(_br);

            oData.FromBytes(_br);

            int num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                DishRequirement requirement = new DishRequirement();

                requirement.FromBytes(_br);

                require.Add(requirement.uid, requirement);
            }

            InitDishAll(mData);

            InitDishAll(oData);
        }
    }
}
