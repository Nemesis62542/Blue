using Blue.Entity.Common;
using Blue.Interface;
using UnityEngine;

namespace Blue.Entity
{
    public class CoelacanthController : BaseEntityController<CoelacanthModel, CoelacanthView>, IScannable
    {
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private float wanderRadius = 3.0f;
        [SerializeField] private float wanderInterval = 2.0f;
        [SerializeField] private SwimMover swimMover = new SwimMover();

        private float wanderTimer = 0f;
        public string DisplayName => model.Status.Name;
        public Renderer[] TargetRenderers => new Renderer[] { view.Renderer };

        protected override void Awake()
        {
            model = new CoelacanthModel();
            
            model.Initialize(data);
            swimMover.Initialize(transform);
        }

        private void Update()
        {
            swimMover.UpdateMove();

            if (!swimMover.IsMoving)
            {
                wanderTimer += Time.deltaTime;

                if (wanderTimer >= wanderInterval)
                {
                    Vector3 random_pos = transform.position;
                    swimMover.MoveToRandomPosition(random_pos, wanderRadius, () =>
                    {
                        view.SetAnimatorSwim(false);
                    });

                    Vector3 target_position = swimMover.Destination;
                    Vector3 forward_dir = (target_position - transform.position).normalized;

                    if (forward_dir.sqrMagnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.LookRotation(forward_dir);
                    }

                    view.SetAnimatorSwim(true);
                    wanderTimer = 0f;
                }
            }
            else
            {
                wanderTimer = 0f;
            }
        }
        
        public void OnScanEnd()
        {
            view.DisableHighlight();
        }

        public void OnScanStart()
        {
            view.EnableHighlight();
        }
    }
}