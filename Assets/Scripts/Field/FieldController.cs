using Blue.Player;
using UnityEngine;

public class FieldController : MonoBehaviour
{
    [SerializeField] GameObject waterSurface;
    [SerializeField] ParticleSystem godRay;
    [SerializeField] PlayerController player;

    public float WaterLevel => waterSurface.transform.position.y;

    void Update()
    {
        godRay.transform.position = new Vector3(player.transform.position.x, WaterLevel, player.transform.position.z);
    }
}
