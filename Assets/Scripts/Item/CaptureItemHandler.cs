using Blue.Interface;
using Blue.Item;
using Blue.Player;
using Blue.Projectile;
using UnityEngine;

public class CaptureItemHandler : ItemUseHandler
{
    [SerializeField] private float captureDistance = 5.0f;
    [SerializeField] private CaptureBullet captureBullet;
    [SerializeField] private ParticleSystem captureEffect;

    private MonoBehaviour user;
    
    private void Awake()
    {
        captureBullet.OnHit = OnCaptured;
    }

    public override void OnUse(MonoBehaviour user)
    {
        this.user = user;
        base.OnUse(this.user);
        captureBullet.PlayParticle();
    }

    private void OnCaptured(GameObject other)
    {
        if (!user.TryGetComponent(out PlayerController player)) return;

        if (other.TryGetComponent(out ICapturable capturable))
        {
            player.CaptureEntity(capturable.EntityData);
            Instantiate(captureEffect, other.transform.position, Quaternion.identity);
            Destroy(other);
        }
    }
}
