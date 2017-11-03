namespace Cook_lib
{
    public interface IDishSDS
    {
        int GetID();
        float GetPrepareTime();
        float GetDecreaseValue();
        float GetCookTime();
        float GetExceedTime();
        float GetOptimizeTime();
        bool GetIsUniversal();
        int GetMaxNum();
        int GetMoney();
        int GetMoneyOptimized();
    }
}
