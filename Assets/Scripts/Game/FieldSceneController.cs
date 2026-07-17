using Blue.Audio;
using Blue.Player;
using Blue.Visual;
using UnityEngine;

namespace Blue.Game
{
    public class FieldSceneController : MonoBehaviour
    {
        [SerializeField] private GameObject waterSurface;
        [SerializeField] private PlayerController player;
        [SerializeField] private DepthEnvironmentController depthEnvironmentController;

        public float WaterLevel => waterSurface.transform.position.y;

        void Awake()
        {
            InitializeFieldScene();
        }

        void Update()
        {
            UpdateEnvironment();
        }

        private void InitializeFieldScene()
        {
            player.SetWaterLevel(WaterLevel);
        }

        private void UpdateEnvironment()
        {
            float depth = WaterLevel - player.transform.position.y;

            // 深度に応じて環境を更新
            if (depthEnvironmentController != null)
            {
                depthEnvironmentController.UpdateEnvironment(depth);
            }

            SoundController.Instance.PlayEnvironmentSound(EnvironmentSoundType.UnderWater);
        }
    }
}