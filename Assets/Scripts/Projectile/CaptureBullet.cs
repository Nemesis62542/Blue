using System;
using UnityEngine;

namespace Blue.Projectile
{
    public class CaptureBullet : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particle;

        public Action<GameObject> OnHit { private get; set; }

        private void OnParticleCollision(GameObject other)
        {
            OnHit?.Invoke(other);
        }

        public void PlayParticle()
        {
            particle.Play();
        }
    }
}