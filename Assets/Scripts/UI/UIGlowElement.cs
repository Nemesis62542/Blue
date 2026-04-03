using UnityEngine;
using UnityEngine.UI;

namespace Blue.UI
{
    /// <summary>
    /// UI Imageの発光色を動的に制御するコンポーネント
    /// UI/Glow シェーダーと組み合わせて使用
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UIGlowElement : MonoBehaviour
    {
        [SerializeField, ColorUsage(true, true)]
        private Color glowColor = new Color(1.5f, 0.5f, 0f, 1f);

        private Image _image;
        private Material _materialInstance;

        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        private void Awake()
        {
            _image = GetComponent<Image>();

            // マテリアルのインスタンスを作成（他のImageに影響しないように）
            if (_image.material != null)
            {
                _materialInstance = Instantiate(_image.material);
                _image.material = _materialInstance;
            }

            ApplyGlowColor();
        }

        private void OnDestroy()
        {
            // インスタンス化したマテリアルを破棄
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
            }
        }

        private void ApplyGlowColor()
        {
            if (_materialInstance != null)
            {
                _materialInstance.SetColor(ColorProperty, glowColor);
            }
        }

        /// <summary>
        /// 発光色を動的に変更
        /// </summary>
        public void SetGlowColor(Color color)
        {
            glowColor = color;
            ApplyGlowColor();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // エディタではImageのcolorで確認用に反映
            if (_image == null) _image = GetComponent<Image>();
            if (_image != null && _image.material != null)
            {
                // エディタ上でのプレビュー用（SharedMaterialを変更しないよう注意）
                MaterialPropertyBlock tempBlock = new MaterialPropertyBlock();
                tempBlock.SetColor(ColorProperty, glowColor);
            }
        }
#endif
    }
}
