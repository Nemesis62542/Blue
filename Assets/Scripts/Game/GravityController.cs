using UnityEngine;

namespace Blue.Game
{
    public class GravityController : MonoBehaviour
    {
        [Header("重力設定")]
        [SerializeField] private float gravityStrength = 9.8f;
        [SerializeField] private float terminalVelocity = 50f;
        [SerializeField] private float dragCoefficient = 0.1f;
        [SerializeField] private float buoyancy = 0f;

        private Rigidbody targetRigidbody;

        private void Awake()
        {
            targetRigidbody = GetComponent<Rigidbody>();
            if (targetRigidbody == null)
            {
                Debug.LogError($"GravityController: Rigidbody component not found on {gameObject.name}");
            }
        }

        private void FixedUpdate()
        {
            if (targetRigidbody != null)
            {
                ApplyCustomGravity();
            }
        }

        private void ApplyCustomGravity()
        {
            Vector3 velocity = targetRigidbody.linearVelocity;

            // 重力を適用
            Vector3 gravityForce = Vector3.down * gravityStrength;

            // 浮力を適用
            if (buoyancy > 0f)
            {
                gravityForce += Vector3.up * buoyancy;
            }

            // 抵抗力を適用（Y軸の速度に対して）
            if (dragCoefficient > 0f)
            {
                Vector3 dragForce = -velocity * dragCoefficient;
                dragForce.x = 0f; // X,Z軸の移動には影響しない
                dragForce.z = 0f;
                gravityForce += dragForce;
            }

            // 最大落下速度を制限
            velocity.y = Mathf.Max(velocity.y, -terminalVelocity);
            targetRigidbody.linearVelocity = velocity;

            // 重力とその他の力を適用
            targetRigidbody.AddForce(gravityForce, ForceMode.Acceleration);
        }

        public void SetGravitySettings(float gravity, float drag, float buoyancyForce, float maxFallSpeed)
        {
            gravityStrength = gravity;
            dragCoefficient = drag;
            buoyancy = buoyancyForce;
            terminalVelocity = maxFallSpeed;
        }

        public void SetAquaticMode()
        {
            gravityStrength = 2.0f;
            dragCoefficient = 5.0f;
            buoyancy = 3.0f;
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