using System;
using System.Collections.Generic;

namespace Cook_lib
{
    public class CookMain
    {
        private static Random random = new Random();

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

        private int tick = 0;

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

                if (!sds.GetIsUniversal() && !dishAll.Contains(sds))
                {
                    dishAll.Add(sds);
                }

                data.sds = sds;

                mDish.Add(data);
            }

            for (int i = 0; i < _oDish.Count; i++)
            {
                DishData data = new DishData();

                IDishSDS sds = getDishData(_oDish[i]);

                if (!sds.GetIsUniversal() && !dishAll.Contains(sds))
                {
                    dishAll.Add(sds);
                }

                data.sds = sds;

                oDish.Add(data);
            }
        }

        internal void Update()
        {
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

            int num = random.Next(CookConst.REQUIRE_NUM_MIN, maxNum + 1);

            DishResult[] resultArr = new DishResult[num];

            requirement.dishArr = resultArr;

            for (int i = 0; i < num; i++)
            {
                int index = random.Next(list.Count);

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

                bool isOptimize = random.NextDouble() < CookConst.REQUIRE_OPTIMIZE_PROBABILITY;

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






        private void GetCommandChangeWorkPos(bool _isMine, int _workerIndex, int _pos)
        {
            if (_isMine)
            {
                if (_pos > -2 && _pos < mDish.Count && _workerIndex > -1 && _workerIndex < CookConst.WORKER_NUM)
                {
                    if (Array.IndexOf(mWorkPos, _pos) == -1)
                    {
                        mWorkPos[_workerIndex] = _pos;
                    }
                }
            }
            else
            {
                if (_pos > -2 && _pos < oDish.Count && _workerIndex > -1 && _workerIndex < CookConst.WORKER_NUM)
                {
                    if (Array.IndexOf(oWorkPos, _pos) == -1)
                    {
                        oWorkPos[_workerIndex] = _pos;
                    }
                }
            }
        }

        private void GetCommandChangeResultIndex(bool _isMine, int _index, int _targetIndex)
        {
            if (_isMine)
            {
                if (_index > -1 && _index < mResult.Count && _targetIndex > -1 && _targetIndex < mResult.Count)
                {
                    DishResult result = mResult[_index];

                    mResult[_index] = mResult[_targetIndex];

                    mResult[_targetIndex] = result;
                }
            }
            else
            {
                if (_index > -1 && _index < oResult.Count && _targetIndex > -1 && _targetIndex < oResult.Count)
                {
                    DishResult result = oResult[_index];

                    oResult[_index] = oResult[_targetIndex];

                    oResult[_targetIndex] = result;
                }
            }
        }

        private void GetCommandCompleteDish(bool _isMine, int _index)
        {
            if (_isMine)
            {
                if (_index > -1 && _index < mDish.Count)
                {
                    DishData dish = mDish[_index];

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
                if (_index > -1 && _index < oDish.Count)
                {
                    DishData dish = oDish[_index];

                    if (dish.result != null && oResult.Count < CookConst.RESULT_STATE.Length)
                    {
                        oResult.Add(dish.result);

                        dish.result = null;

                        dish.state = DishState.NULL;
                    }
                }
            }
        }

        private void GetCommandCompleteRequirement(bool _isMine, List<int> _list, int _requirementUid)
        {
            for (int i = 0; i < require.Count; i++)
            {
                DishRequirement requirement = require[i];

                if (requirement.uid == _requirementUid)
                {
                    List<DishResult> tmpList = new List<DishResult>();

                    List<DishResult> tmpList2 = _isMine ? mResult : oResult;

                    for (int m = 0; m < _list.Count; m++)
                    {
                        int index = _list[m];

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
                        AddReward(_isMine, requirement);

                        require.RemoveAt(i);

                        while (tmpList.Count > 0)
                        {
                            tmpList2.Remove(tmpList[0]);
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
    }
}
