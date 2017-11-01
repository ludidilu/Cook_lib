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
            public double time;
            public DishResult result;
        }

        public class DishResult
        {
            public IDishSDS sds;
            public bool isOptimized;
            public int time;
        }

        public class DishRequirement
        {
            public int uid;
            public DishResult[] dishArr;
            public int time;
        }

        private List<DishData> mDish = new List<DishData>();

        private List<DishData> oDish = new List<DishData>();

        private int[] mWorkPos = new int[CookConst.WORKER_NUM];

        private int[] oWorkPos = new int[CookConst.WORKER_NUM];

        private List<DishResult> mResult = new List<DishResult>();

        private List<DishResult> oResult = new List<DishResult>();

        private List<DishRequirement> require = new List<DishRequirement>();

        private List<IDishSDS> dishAll = new List<IDishSDS>();

        public int tick { private set; get; }

        private SuperRandom random = new SuperRandom();

        public void Start(IList<int> _mDish, IList<int> _oDish)
        {
            for (int i = 0; i < CookConst.WORKER_NUM; i++)
            {
                mWorkPos[i] = -1;
                oWorkPos[i] = -1;
            }

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

            RefreshDish(mWorkPos, mDish);

            RefreshDish(oWorkPos, oDish);
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

            DishResult[] resultArr = new DishResult[num];

            requirement.dishArr = resultArr;

            for (int i = 0; i < num; i++)
            {
                int index = random.Get(list.Count);

                IDishSDS sds = list[index];

                int oldNum = 1;

                for (int m = 0; m < i; m++)
                {
                    DishResult tmpResult = resultArr[m];

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

                DishResult result = new DishResult();

                result.sds = sds;

                result.isOptimized = isOptimize;

                resultArr[i] = result;
            }

            return requirement;
        }

        private void RefreshResult(List<DishResult> _result)
        {
            for (int i = _result.Count - 1; i > -1; i--)
            {
                DishResult result = _result[i];

                if (CookConst.RESULT_STATE[i])
                {
                    result.time++;

                    if (result.time > result.sds.GetExceedTime() * CookConst.TICK_NUM_PER_SECOND)
                    {
                        _result.RemoveAt(i);
                    }
                }
            }
        }

        private void RefreshDish(int[] _workerPos, List<DishData> _dish)
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

                if (Array.IndexOf(_workerPos, i) != -1)
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
            mDish.Clear();
            oDish.Clear();
            mResult.Clear();
            oResult.Clear();
            require.Clear();
            dishAll.Clear();

            tick = 0;
        }













        internal void GetCommandChangeWorkerPos(CommandChangeWorkerPos _command)
        {
            if (_command.isMine)
            {
                if (_command.pos > -2 && _command.pos < mDish.Count && _command.workerIndex > -1 && _command.workerIndex < CookConst.WORKER_NUM)
                {
                    if (_command.pos == -1 || Array.IndexOf(mWorkPos, _command.pos) == -1)
                    {
                        mWorkPos[_command.workerIndex] = _command.pos;
                    }
                }
            }
            else
            {
                if (_command.pos > -2 && _command.pos < oDish.Count && _command.workerIndex > -1 && _command.workerIndex < CookConst.WORKER_NUM)
                {
                    if (_command.pos == -1 || Array.IndexOf(oWorkPos, _command.pos) == -1)
                    {
                        oWorkPos[_command.workerIndex] = _command.pos;
                    }
                }
            }
        }

        internal void GetCommandChangeResultPos(CommandChangeResultPos _command)
        {
            if (_command.isMine)
            {
                if (_command.pos > -1 && _command.pos < mResult.Count && _command.targetPos > -1 && _command.targetPos < mResult.Count)
                {
                    DishResult result = mResult[_command.pos];

                    mResult[_command.pos] = mResult[_command.targetPos];

                    mResult[_command.targetPos] = result;
                }
            }
            else
            {
                if (_command.pos > -1 && _command.pos < oResult.Count && _command.targetPos > -1 && _command.targetPos < oResult.Count)
                {
                    DishResult result = oResult[_command.pos];

                    oResult[_command.pos] = oResult[_command.targetPos];

                    oResult[_command.targetPos] = result;
                }
            }
        }

        internal void GetCommandCompleteDish(CommandCompleteDish _command)
        {
            if (_command.isMine)
            {
                if (_command.pos > -1 && _command.pos < mDish.Count)
                {
                    DishData dish = mDish[_command.pos];

                    if (dish.result != null && mResult.Count < CookConst.RESULT_STATE.Length)
                    {
                        mResult.Add(dish.result);

                        dish.result = null;

                        dish.state = DishState.NULL;
                    }
                }
            }
            else
            {
                if (_command.pos > -1 && _command.pos < oDish.Count)
                {
                    DishData dish = oDish[_command.pos];

                    if (dish.result != null && oResult.Count < CookConst.RESULT_STATE.Length)
                    {
                        oResult.Add(dish.result);

                        dish.result = null;

                        dish.state = DishState.NULL;
                    }
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
                    List<DishResult> tmpList = new List<DishResult>();

                    List<DishResult> tmpList2 = _command.isMine ? mResult : oResult;

                    for (int m = 0; m < _command.resultList.Count; m++)
                    {
                        int index = _command.resultList[m];

                        if (index > -1 && index < tmpList2.Count)
                        {
                            DishResult result = tmpList2[index];

                            if (!tmpList.Contains(result))
                            {
                                tmpList.Add(result);
                            }
                        }
                    }

                    if (CheckIsCompleteRequirement(tmpList, requirement))
                    {
                        AddReward(_command.isMine, requirement);

                        require.RemoveAt(i);

                        for (int m = 0; m < tmpList.Count; m++)
                        {
                            tmpList2.Remove(tmpList[m]);
                        }
                    }

                    break;
                }
            }
        }

        private void AddReward(bool _isMine, DishRequirement _requirement)
        {

        }

        public bool CheckIsCompleteRequirement(List<DishResult> _result, DishRequirement _requirement)
        {
            if (_result.Count != _requirement.dishArr.Length)
            {
                return false;
            }

            List<DishResult> resultList = new List<DishResult>(_result);

            List<DishResult> requirementList = new List<DishResult>(_requirement.dishArr);

            for (int i = resultList.Count - 1; i > -1; i--)
            {
                DishResult result = resultList[i];

                for (int m = requirementList.Count - 1; m > -1; m--)
                {
                    DishResult requirement = requirementList[m];

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
                    DishResult requirement = requirementList[m];

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
                    DishResult requirement = requirementList[m];

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
                bw.Write(mWorkPos[i]);
                bw.Write(oWorkPos[i]);
            }

            bw.Write(mResult.Count);

            for (int i = 0; i < mResult.Count; i++)
            {
                WriteDishResult(mResult[i], bw);
            }

            bw.Write(oResult.Count);

            for (int i = 0; i < oResult.Count; i++)
            {
                WriteDishResult(oResult[i], bw);
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
                mWorkPos[i] = _br.ReadInt32();
                oWorkPos[i] = _br.ReadInt32();
            }

            num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                DishResult result = ReadDishResult(_br);

                mResult.Add(result);
            }

            num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                DishResult result = ReadDishResult(_br);

                oResult.Add(result);
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

            data.time = _br.ReadDouble();

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

        private void WriteDishRequirement(DishRequirement _requirement, BinaryWriter _bw)
        {
            _bw.Write(_requirement.uid);

            _bw.Write(_requirement.dishArr.Length);

            for (int i = 0; i < _requirement.dishArr.Length; i++)
            {
                WriteDishResult(_requirement.dishArr[i], _bw);
            }

            _bw.Write(_requirement.time);
        }

        private DishRequirement ReadDishRequirement(BinaryReader _br)
        {
            DishRequirement requirement = new DishRequirement();

            requirement.uid = _br.ReadInt32();

            int num = _br.ReadInt32();

            requirement.dishArr = new DishResult[num];

            for (int i = 0; i < num; i++)
            {
                requirement.dishArr[i] = ReadDishResult(_br);
            }

            requirement.time = _br.ReadInt32();

            return requirement;
        }
    }
}
