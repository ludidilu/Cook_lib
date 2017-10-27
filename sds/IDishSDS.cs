namespace Cook_lib
{
    public interface IDishSDS
    {
        int GetID();
        double GetPrepareTime();
        double GetDecreaseValue();
        double GetCookTime();
        double GetExceedTime();
        double GetOptimizeTime();
        bool GetIsUniversal();
        int GetMaxNum();
        int GetMoney();
        int GetMoneyOptimized();
    }
}
