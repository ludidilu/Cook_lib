﻿using System.IO;
using System.Collections.Generic;

namespace Cook_lib
{
    public enum DishState
    {
        NULL,
        PREPAREING,
        COOKING,
        OPTIMIZING
    }

    public class PlayerData
    {
        public List<DishData> dish = new List<DishData>();

        public Worker[] workers = new Worker[CookConst.WORKER_NUM];

        public DishResult[] result = new DishResult[CookConst.RESULT_STATE.Length];

        internal PlayerData()
        {
            for (int i = 0; i < CookConst.WORKER_NUM; i++)
            {
                workers[i] = new Worker();
            }
        }

        internal void SetDishData(IList<int> _dish)
        {
            for (int i = 0; i < _dish.Count; i++)
            {
                DishData data = new DishData();

                IDishSDS sds = CookMain.getDishData(_dish[i]);

                data.sds = sds;

                dish.Add(data);
            }
        }

        internal void Clear()
        {
            dish.Clear();

            for (int i = 0; i < CookConst.RESULT_STATE.Length; i++)
            {
                result[i] = null;
            }

            for (int i = 0; i < CookConst.WORKER_NUM; i++)
            {
                Worker worker = workers[i];

                worker.pos = -1;

                worker.punishTick = 0;
            }
        }

        internal void ToBytes(BinaryWriter _bw)
        {
            _bw.Write(dish.Count);

            for (int i = 0; i < dish.Count; i++)
            {
                dish[i].ToBytes(_bw);
            }

            for (int i = 0; i < CookConst.WORKER_NUM; i++)
            {
                workers[i].ToBytes(_bw);
            }

            for (int i = 0; i < CookConst.RESULT_STATE.Length; i++)
            {
                DishResult dishResult = result[i];

                if (dishResult != null)
                {
                    _bw.Write(true);

                    dishResult.ToBytes(_bw);
                }
                else
                {
                    _bw.Write(false);
                }
            }
        }

        internal void FromBytes(BinaryReader _br)
        {
            int num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                DishData data = new DishData();

                data.FromBytes(_br);

                dish.Add(data);
            }

            for (int i = 0; i < CookConst.WORKER_NUM; i++)
            {
                workers[i].FromBytes(_br);
            }

            for (int i = 0; i < CookConst.RESULT_STATE.Length; i++)
            {
                bool b = _br.ReadBoolean();

                if (b)
                {
                    DishResult dishResult = result[i];

                    if (dishResult == null)
                    {
                        dishResult = new DishResult();

                        result[i] = dishResult;
                    }

                    dishResult.FromBytes(_br);
                }
                else
                {
                    result[i] = null;
                }
            }
        }
    }

    public class DishData
    {
        public IDishSDS sds;
        public DishState state;
        public float time;
        public DishResult result;

        internal void ToBytes(BinaryWriter _bw)
        {
            _bw.Write(sds.GetID());

            _bw.Write((byte)state);

            _bw.Write(time);

            if (result != null)
            {
                _bw.Write(true);

                result.ToBytes(_bw);
            }
            else
            {
                _bw.Write(false);
            }
        }

        internal void FromBytes(BinaryReader _br)
        {
            int id = _br.ReadInt32();

            sds = CookMain.getDishData(id);

            state = (DishState)_br.ReadByte();

            time = _br.ReadSingle();

            bool b = _br.ReadBoolean();

            if (b)
            {
                if (result == null)
                {
                    result = new DishResult();
                }

                result.FromBytes(_br);
            }
            else
            {
                result = null;
            }
        }
    }

    public class DishResult : DishResultBase
    {
        public int time;

        internal override void ToBytes(BinaryWriter _bw)
        {
            base.ToBytes(_bw);

            _bw.Write(time);
        }

        internal override void FromBytes(BinaryReader _br)
        {
            base.FromBytes(_br);

            time = _br.ReadInt32();
        }
    }

    public class DishResultBase
    {
        public IDishSDS sds;
        public bool isOptimized;

        internal virtual void ToBytes(BinaryWriter _bw)
        {
            _bw.Write(sds.GetID());

            _bw.Write(isOptimized);
        }

        internal virtual void FromBytes(BinaryReader _br)
        {
            int id = _br.ReadInt32();

            sds = CookMain.getDishData(id);

            isOptimized = _br.ReadBoolean();
        }
    }

    public class DishRequirement
    {
        public int uid;
        public DishResultBase[] dishArr;
        public int time;

        internal void ToBytes(BinaryWriter _bw)
        {
            _bw.Write(uid);

            _bw.Write(dishArr.Length);

            for (int i = 0; i < dishArr.Length; i++)
            {
                dishArr[i].ToBytes(_bw);
            }

            _bw.Write(time);
        }

        internal void FromBytes(BinaryReader _br)
        {
            uid = _br.ReadInt32();

            int num = _br.ReadInt32();

            dishArr = new DishResultBase[num];

            for (int i = 0; i < num; i++)
            {
                DishResultBase result = new DishResultBase();

                result.FromBytes(_br);

                dishArr[i] = result;
            }

            time = _br.ReadInt32();
        }
    }

    public class Worker
    {
        public int pos = -1;
        public int punishTick = 0;

        internal void ToBytes(BinaryWriter _bw)
        {
            _bw.Write(pos);

            _bw.Write(punishTick);
        }

        internal void FromBytes(BinaryReader _br)
        {
            pos = _br.ReadInt32();

            punishTick = _br.ReadInt32();
        }
    }
}