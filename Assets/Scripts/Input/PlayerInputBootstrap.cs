using UnityEngine;

namespace Blue.Input
{
    /// <summary>
    /// PlayerInputHandler が無ければ生成する
    /// </summary>
    // PlayerInputHandler は MonoBehaviour ではなく、通常は PlayerController か
    // GarageSceneController が生成する。そのため Aquarium のような途中のシーンを
    // 単体で開くと誰も生成せず、CharacterMovementController が null 参照で落ちる。
    // Awake は他の Start より先に走るので、ここで用意すれば間に合う
    public class PlayerInputBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            if (PlayerInputHandler.Instance != null) return;

            _ = new PlayerInputHandler();
        }
    }
}
