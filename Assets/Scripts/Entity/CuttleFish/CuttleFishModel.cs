namespace Blue.Entity
{
    public class CuttleFishModel : BaseEntityModel
    {
        public enum CuttleFishState
        {
            Dim,
            Bright,
            Intimidate,
        }

        private CuttleFishState currentState = CuttleFishState.Dim;

        public CuttleFishModel(EntityData data) : base(data)
        {
        }

        public CuttleFishState CurrentState => currentState;

        public void SetState(CuttleFishState state)
        {
            currentState = state;
        }
    }
}