using Blue.Audio;
using Blue.Player;
using Blue.Visual;
using UnityEngine;

namespace Blue.Game
{
    public class FieldSceneController : MonoBehaviour
    {
        [SerializeField] private GameObject waterSurface;
        [SerializeField] private ParticleSystem godRay;
        [SerializeField] private PlayerController player;
        [SerializeField] private DepthEnvironmentController depthEnvironmentController;

        private bool isGodRayPlaying = false;

        private const float GodRayThreshold = 15.0f;

        public float WaterLevel => waterSurface.transform.position.y;

        void Awake()
        {
            InitializeFieldScene();
        }

        void Update()
        {
            UpdateGodRayPos();
        }

        private void InitializeFieldScene()
        {
            player.SetWaterLevel(WaterLevel);
        }

        private void UpdateGodRayPos()
        {
            godRay.transform.position = new Vector3(player.transform.position.x, WaterLevel, player.transform.position.z);

            float depth = WaterLevel - player.transform.position.y;
            bool shouldPlay = depth <= GodRayThreshold;

            if (shouldPlay != isGodRayPlaying)
            {
                if (shouldPlay)
                {
                    godRay.Play();
                }
                else
                {
                    godRay.Stop();
                }
                isGodRayPlaying = shouldPlay;
            }

            // 深度に応じて環境を更新
            if (depthEnvironmentController != null)
            {
                depthEnvironmentController.UpdateEnvironment(depth);
            }

            SoundController.Instance.PlayEnvironmentSound(EnvironmentSoundType.UnderWater);
        }
    }
}