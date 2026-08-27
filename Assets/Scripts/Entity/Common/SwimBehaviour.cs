namespace Blue.Entity.Common
{
    /// <summary>
    /// 遊泳の駆動状態
    /// </summary>
    public enum SwimMode
    {
        Idle, // 停止
        Move, // 目的地へ移動
        Face  // その場で指定方向を向く
    }

    /// <summary>
    /// 遊泳生物が「どこへ向かうか」を決める行動。移動そのものは BaseSwimmer が担う
    /// </summary>
    // 継承ではなく差し替えで表現する。威嚇→逃走→徘徊のように 1 個体の中で
    // 遷移するものをクラス階層で表すと状態の数だけ派生が増えて破綻するため
    public abstract class SwimBehaviour
    {
        /// <summary>
        /// この行動に切り替わった直後に一度呼ばれる
        /// </summary>
        public virtual void OnEnter(BaseSwimmer swimmer) { }

        /// <summary>
        /// 別の行動へ切り替わる直前に一度呼ばれる
        /// </summary>
        public virtual void OnExit(BaseSwimmer swimmer) { }

        /// <summary>
        /// 毎フレーム呼ばれる
        /// </summary>
        public virtual void Tick(BaseSwimmer swimmer, float deltaTime) { }

        /// <summary>
        /// 目的地に到達したときに呼ばれる
        /// </summary>
        // ここで次の目的地を入れない場合、BaseSwimmer は自動的に停止する
        public virtual void OnDestinationReached(BaseSwimmer swimmer) { }
    }
}
