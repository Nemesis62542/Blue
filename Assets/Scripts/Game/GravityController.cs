using UnityEngine;

namespace Blue.Game
{
    public enum GravityMode
    {
        Aquatic,
        Terrestrial
    }

    public class GravityController : MonoBehaviour
    {
        [Header("モード設定")]
        [SerializeField] private GravityMode mode = GravityMode.Aquatic;

        [Header("重力設定")]
        [SerializeField] private float gravityStrength = 9.8f;
        [SerializeField] private float terminalVelocity = 50f;
        [SerializeField] private float dragCoefficient = 0.1f;
        [SerializeField] private float buoyancy = 0f;

        private Rigidbody targetRigidbody;
        private GravityMode currentMode;

        private void Awake()
        {
            targetRigidbody = GetComponent<Rigidbody>();
            if (targetRigidbody == null)
            {
                Debug.LogError($"GravityController: Rigidbody component not found on {gameObject.name}");
            }
        }

        private void OnValidate()
        {
            if (currentMode != mode)
            {
                ApplyMode();
            }
        }

        private void FixedUpdate()
        {
            if (targetRigidbody != null)
            {
                ApplyCustomGravity();
            }
        }

        private void ApplyMode()
        {
            currentMode = mode;
            switch (mode)
            {
                case GravityMode.Aquatic:
                    SetAquaticMode();
                    break;
                case GravityMode.Terrestrial:
                    SetTerrestrialMode();
                    break;
            }
        }

        private void ApplyCustomGravity()
        {
            Vector3 velocity = targetRigidbody.linearVelocity;

            // 重力を適用
            Vector3 gravity_force = Vector3.down * gravityStrength;

            // 浮力を適用
            if (buoyancy > 0f)
            {
                gravity_force += Vector3.up * buoyancy;
            }

            // 抵抗力を適用（Y軸の速度に対して）
            if (dragCoefficient > 0f)
            {
                Vector3 drag_force = -velocity * dragCoefficient;
                drag_force.x = 0f; // X,Z軸の移動には影響しない
                drag_force.z = 0f;
                gravity_force += drag_force;
            }

            // 最大落下速度を制限
            velocity.y = Mathf.Max(velocity.y, -terminalVelocity);
            targetRigidbody.linearVelocity = velocity;

            // 重力とその他の力を適用
            targetRigidbody.AddForce(gravity_force, ForceMode.Acceleration);
        }

        public void SetGravitySettings(float gravity, float drag, float buoyancy_force, float max_fallSpeed)
        {
            gravityStrength = gravity;
            dragCoefficient = drag;
            buoyancy = buoyancy_force;
            terminalVelocity = max_fallSpeed;
        }

        public void SetAquaticMode()
        {
            gravityStrength = 5.0f;
            dragCoefficient = 5.0f;
            buoyancy = 0f;
            terminalVelocity = 10f;
        }

        public void SetTerrestrialMode()
        {
            gravityStrength = 9.8f;
            dragCoefficient = 0.1f;
            buoyancy = 0f;
            terminalVelocity = 50f;
        }
    }
}