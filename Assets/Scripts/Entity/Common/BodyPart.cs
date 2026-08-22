namespace Blue.Entity.Common
{
    /// <summary>
    /// 当たり判定の部位
    /// </summary>
    // 値を明示しているのは、並び替えてもシリアライズ済みの設定がずれないようにするため。
    public enum BodyPart
    {
        Body = 0,
        Head = 1,
        Jaw = 2,
        Tail = 3,
        Fin = 4,
    }
}
