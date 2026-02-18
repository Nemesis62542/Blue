using Blue.Visual;
using UnityEngine;

namespace Blue.Entity
{
    public class MecaSharkView : BaseEntityView
    {
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField] private Animator animator;
        [SerializeField] private HighlightController highlightController;
        [SerializeField] private Material deadMaterial;
        [SerializeField] private ParticleSystem erectric;

        public SkinnedMeshRenderer Renderer => skinnedMeshRenderer;
        public void EnableHighlight() => highlightController.EnableHighlight();
        public void DisableHighlight() => highlightController.DisableHighlight();

        public void LockOn(bool is_lock_on)
        {
            animator.SetBool("LockOn", is_lock_on);
        }

        public void OnDead()
        {
            Material[] materials = skinnedMeshRenderer.materials;
            if (materials.Length > 0)
            {
                materials[0] = deadMaterial;
                skinnedMeshRenderer.materials = materials;
                highlightController.baseMaterials = materials;
            }

            animator.speed = 0;
            erectric.Play();
        }
    }
}