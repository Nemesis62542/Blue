using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 編集モードの俯瞰カメラ。床の一点を注視し、その点を動かして見て回る
    /// </summary>
    // 位置ではなく「床のどこを見ているか」を状態に持つ。ズームしても注視点が動かないので、
    // 拡大したら見ていた場所を見失う、という編集中に一番困る挙動を避けられる
    public class AquariumEditCamera : MonoBehaviour
    {
        [SerializeField] private float panSpeed = 12f;
        [SerializeField] private float zoomSpeed = 6f;
        [SerializeField] private float minHeight = 4f;
        [SerializeField] private float maxHeight = 40f;
        [SerializeField, Range(20f, 89f)] private float pitch = 55f;
        [SerializeField] private float yaw = 0f;

        [Header("初期状態")]
        [SerializeField] private Vector3 initialFocus = Vector3.zero; // 最初に見る床の位置
        [SerializeField] private float initialHeight = 14f;

        private Vector3 focus;
        private float height;

        /// <summary>
        /// 注視している床の位置
        /// </summary>
        public Vector3 Focus => focus;

        private void Awake()
        {
            focus = initialFocus;
            height = Mathf.Clamp(initialHeight, minHeight, maxHeight);
            Apply();
        }

        /// <summary>
        /// 指定した床の位置を見る
        /// </summary>
        public void Frame(Vector3 point)
        {
            focus = new Vector3(point.x, 0f, point.z);
            Apply();
        }

        public void Pan(Vector2 direction, float delta_time)
        {
            if (direction.sqrMagnitude < 0.0001f) return;

            // 画面の上下左右で動かしたいので、カメラの向きを床に寝かせた軸を使う
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

            // 高いほど1回の移動量を大きくしないと、引きの状態で端まで動かすのに時間がかかる
            float scale = panSpeed * delta_time * (height / minHeight);

            focus += (right * direction.x + forward * direction.y) * scale;
            Apply();
        }

        public void Zoom(float delta)
        {
            if (Mathf.Approximately(delta, 0f)) return;

            height = Mathf.Clamp(height - delta * zoomSpeed, minHeight, maxHeight);
            Apply();
        }

        private void Apply()
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

            // 注視点からの距離は、望む高さと見下ろし角から逆算する
            float distance = height / Mathf.Sin(pitch * Mathf.Deg2Rad);

            transform.SetPositionAndRotation(focus + rotation * Vector3.back * distance, rotation);
        }
    }
}
