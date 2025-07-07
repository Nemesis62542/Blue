using Blue.Player;
using UnityEngine;

public class FieldController : MonoBehaviour
{
    [SerializeField] private GameObject waterSurface;
    [SerializeField] private ParticleSystem godRay;
    [SerializeField] private PlayerController player;

    const float GodRayThreshold = 15.0f;

    public float WaterLevel => waterSurface.transform.position.y;

    void Awake()
    {
        player.SetWaterLevel(WaterLevel);
    }

    void Update()
    {
        godRay.transform.position = new Vector3(player.transform.position.x, WaterLevel, player.transform.position.z);

        bool shouldPlay = WaterLevel - player.transform.position.y <= GodRayThreshold;

        if (shouldPlay && !godRay.isPlaying)
        {
            godRay.Play();
        }
        else if (!shouldPlay && godRay.isPlaying)
        {
            godRay.Stop();
        }
    }
}
