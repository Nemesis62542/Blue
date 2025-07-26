using Blue.Player;

namespace Blue.Game
{
    public static class SceneDataBridge
    {
        public static PlayerTransferData TransferData { get; set; }

        public static void Clear()
        {
            TransferData = null;
        }
    }
}