namespace Blue.Entity.Common
{
    /// <summary>
    /// 縄張りの中を休みなく泳ぎ回る。サメなど遊泳を止めない生物の既定行動
    /// </summary>
    public class RoamBehaviour : SwimBehaviour
    {
        public override void OnEnter(BaseSwimmer swimmer)
        {
            swimmer.MoveTo(swimmer.FindRoamPoint());
        }

        public override void OnDestinationReached(BaseSwimmer swimmer)
        {
            swimmer.MoveTo(swimmer.FindRoamPoint());
        }
    }
}
