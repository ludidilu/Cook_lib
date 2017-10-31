using System;
using System.Collections.Generic;

namespace Cook_lib
{
    public class Cook_server
    {
        private CookMain main = new CookMain();

        public void Start(IList<int> _mDish, IList<int> _oDish)
        {
            main.Start(_mDish, _oDish);
        }

        public void Update()
        {
            main.Update();
        }
    }
}
