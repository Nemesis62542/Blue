using System.Collections.Generic;
using UnityEngine;

namespace Blue.Aquarium
{
    /// <summary>
    /// 設置しようとしているものの下見表示。置ける場所かどうかを色で示す
    /// </summary>
    // 半透明マテリアルを別に用意すると設置物ごとに作る必要が出るので、
    // MaterialPropertyBlock で既存マテリアルの色だけを塗り替える。
    // 不透明なままでも、緑と赤で可否は充分に伝わる
    public class PlacementGhost : MonoBehaviour
    {
        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorID = Shader.PropertyToID("_Color");

        [SerializeField] private Color validColor = new Color(0.4f, 1f, 0.5f);
        [SerializeField] private Color warningColor = new Color(1f, 0.85f, 0.35f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.35f, 0.35f);

        private readonly List<Renderer> renderers = new List<Renderer>();
        private MaterialPropertyBlock propertyBlock;
        private AquariumPieceData currentPiece;
        private GameObject instance;
        private PlacementFeedback lastFeedback;
        private bool hasColor;

        /// <summary>
        /// 下見に使う設置物を差し替える。同じものなら作り直さない
        /// </summary>
        public void SetPiece(AquariumPieceData piece)
        {
            if (currentPiece == piece) return;

            currentPiece = piece;
            Rebuild();
        }

        public void SetVisible(bool visible)
        {
            if (instance != null) instance.SetActive(visible);
        }

        /// <summary>
        /// 置き場所と可否を反映する
        /// </summary>
        public void UpdatePlacement(Vector3 position, Quaternion rotation, PlacementFeedback feedback)
        {
            if (instance == null) return;

            instance.transform.SetPositionAndRotation(position, rotation);

            if (hasColor && lastFeedback == feedback) return;

            lastFeedback = feedback;
            hasColor = true;

            ApplyColor(feedback switch
            {
                PlacementFeedback.Blocked => invalidColor,
                PlacementFeedback.NotOnPath => warningColor,
                _ => validColor,
            });
        }

        private void Rebuild()
        {
            if (instance != null) Destroy(instance);

            renderers.Clear();
            hasColor = false;

            if (currentPiece == null || currentPiece.Prefab == null)
            {
                instance = null;
                return;
            }

            instance = Instantiate(currentPiece.Prefab, transform);
            instance.name = $"Ghost_{currentPiece.Name}";

            StripInteractive(instance);
            instance.GetComponentsInChildren(true, renderers);
        }

        // 下見は見えるだけでよい。コライダーが残ると水槽の中の生物を押しのけ、
        // AquariumPieceView が残ると展示まで生成してしまう
        private static void StripInteractive(GameObject target)
        {
            foreach (Collider collider in target.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (AquariumPieceView view in target.GetComponentsInChildren<AquariumPieceView>(true))
            {
                view.enabled = false;
            }
        }

        private void ApplyColor(Color color)
        {
            propertyBlock ??= new MaterialPropertyBlock();

            foreach (Renderer target in renderers)
            {
                if (target == null) continue;

                target.GetPropertyBlock(propertyBlock);

                // URP は _BaseColor、旧来のシェーダは _Color。どちらか持っているほうへ入れる
                if (target.sharedMaterial != null && target.sharedMaterial.HasProperty(BaseColorID))
                {
                    propertyBlock.SetColor(BaseColorID, color);
                }
                else
                {
                    propertyBlock.SetColor(ColorID, color);
                }

                target.SetPropertyBlock(propertyBlock);
            }
        }

        private void OnDestroy()
        {
            if (instance != null) Destroy(instance);
        }
    }

    /// <summary>
    /// 下見で示す状態
    /// </summary>
    public enum PlacementFeedback
    {
        Ready,     // そのまま置ける
        NotOnPath, // 置けるが、繋がった通路に面していない
        Blocked,   // 置けない
    }
}
