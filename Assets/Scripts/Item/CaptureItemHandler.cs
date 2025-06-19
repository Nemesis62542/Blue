using Blue.Interface;
using Blue.Item;
using Blue.Player;
using UnityEngine;

public class CaptureItemHandler : ItemUseHandler
{
    [SerializeField] private float captureDistance = 5.0f;
    [SerializeField] private LayerMask captureLayer;

    public override void OnUse(MonoBehaviour user)
    {
        if (!user.TryGetComponent(out PlayerController player)) return;

        Camera camera = Camera.main;
        Ray ray = new Ray(camera.transform.position, camera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, captureDistance, captureLayer))
        {
            if (hit.collider.TryGetComponent(out ICapturable capturable))
            {
                player.CaptureEntity(capturable.CapturedItem);
                Destroy(hit.collider.gameObject);
            }
        }
    }
}
