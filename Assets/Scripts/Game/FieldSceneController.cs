using Blue.Player;
using UnityEngine;

namespace Blue.Game
{
    public class FieldSceneController : MonoBehaviour
    {
        [SerializeField] private GameObject waterSurface;
        [SerializeField] private ParticleSystem godRay;
        [SerializeField] private PlayerController player;

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

            bool shouldPlay = WaterLevel - player.transform.position.y <= GodRayThreshold;

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
        }
    }
}