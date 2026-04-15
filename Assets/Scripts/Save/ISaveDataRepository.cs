namespace Blue.Save
{
    /// <summary>
    /// SaveDataの読み書きを抽象化するリポジトリ
    /// </summary>
    public interface ISaveDataRepository
    {
        SaveData LoadCurrent();
        void Save(SaveData data);
    }
}
