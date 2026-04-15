namespace Blue.Save
{
    /// <summary>
    /// SaveManagerを利用したSaveDataリポジトリ実装
    /// </summary>
    public class SaveManagerRepository : ISaveDataRepository
    {
        public SaveData LoadCurrent()
        {
            return SaveManager.CurrentSaveData;
        }

        public void Save(SaveData data)
        {
            SaveManager.Save(data);
        }
    }
}
