namespace Cook_lib
{
    public interface IDishSDS
    {
        int GetID();
        float GetPrepareTime();
        float GetPrepareDecreaseValue();
        float GetCookTime();
        float GetOptimizeTime();
        float GetOptimizeDecreaseValue();
        IResultSDS GetResult();
        //float GetExceedTime();
        //bool GetIsUniversal();
        //int GetMaxNum();
        //int GetMoney();
        //int GetMoneyOptimized();
    }
}
