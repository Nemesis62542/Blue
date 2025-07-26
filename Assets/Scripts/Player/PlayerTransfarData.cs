using System.Collections.Generic;
using Blue.Entity;

namespace Blue.Player
{
    public class PlayerTransferData
    {
        public Dictionary<EntityData, int> CapturedEntity { get; private set; }

        public PlayerTransferData(Dictionary<EntityData, int> captured_entity)
        {
            CapturedEntity = new Dictionary<EntityData, int>(captured_entity);
        }
    }
}