namespace Blue.Audio
{
    public enum SEType
    {
        None,
        Click,
        Respiratory,  // 水中の呼吸音
        Scan,         // スキャン
        CraftCharge,  // クラフトボタン押し続け中（ループ）
        CraftSuccess, // クラフト成功
        CraftFailed,  // 素材不足
    }
}
