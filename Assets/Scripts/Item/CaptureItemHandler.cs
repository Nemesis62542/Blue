using Blue.Entity.Common;
using Blue.Interface;
using Blue.Item;
using Blue.Player;
using Blue.Projectile;
using UnityEngine;

public class CaptureItemHandler : ItemUseHandler
{
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

        // ボーンごとに分けたコライダーに当たっても、消すのは所有者側でなければならない
        if (!EntityHit.TryResolve(other, out ICapturable capturable)) return;
        if (!capturable.IsCapturable) return;

        // ICapturable の実装は Controller なので MonoBehaviour のはずだが、念のため確認する
        MonoBehaviour behaviour = capturable as MonoBehaviour;
        if (behaviour == null) return;

        player.CaptureEntity(capturable.EntityData);
        Instantiate(captureEffect, behaviour.transform.position, Quaternion.identity);
        Destroy(behaviour.gameObject);
    }
}
