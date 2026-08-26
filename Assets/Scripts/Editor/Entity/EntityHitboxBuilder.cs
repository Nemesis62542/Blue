using System.Collections.Generic;
using Blue.Entity.Common;
using Blue.Interface;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// UnityEditor にも BodyPart(Avatar 設定用) があるため、どちらを指すか明示する
using BodyPart = Blue.Entity.Common.BodyPart;

namespace Blue.EditorTools.Entity
{
    /// <summary>
    /// スキンウェイトからボーンごとの当たり判定を生成するツール
    /// </summary>
    // 胴体が大きく動く生物だと、単一のコライダーでは当たり判定が見た目と合わない。
    // 手で並べると数が多いうえ、モデルを差し替えるたびにやり直しになるので、
    // バインドポーズと頂点ウェイトから形を割り出して生成する。
    public class EntityHitboxBuilder : EditorWindow
    {
        private const string HitboxPrefix = "Hitbox_";

        [SerializeField] private GameObject target;
        [SerializeField] private float weightThreshold = 0.2f;
        [SerializeField] private int minVertexCount = 4;
        [SerializeField] private float radiusScale = 1f;
        [SerializeField] private string skipNameFilter = "_end";
        [SerializeField] private string layerName = "EntityHitbox";

        [MenuItem("Blue/Entity/Hitbox Builder")]
        private static void Open()
        {
            GetWindow<EntityHitboxBuilder>("Hitbox Builder");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "プレハブモードで開いた状態で実行してください。\n" +
                "メッシュの Read/Write Enabled が必要です。生成後は戻して構いません。",
                MessageType.Info);

            target = (GameObject)EditorGUILayout.ObjectField("対象", target, typeof(GameObject), true);

            GameObject resolved = ResolveTarget();
            if (target == null && resolved != null)
            {
                EditorGUILayout.LabelField(" ", $"選択中の {resolved.name} を使います");
            }

            weightThreshold = EditorGUILayout.Slider("ウェイト閾値", weightThreshold, 0.01f, 1f);
            minVertexCount = EditorGUILayout.IntField("最小頂点数", minVertexCount);
            radiusScale = EditorGUILayout.Slider("半径の倍率", radiusScale, 0.5f, 2f);
            skipNameFilter = EditorGUILayout.TextField("除外する名前", skipNameFilter);
            layerName = EditorGUILayout.TextField("レイヤー", layerName);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(resolved == null))
            {
                if (GUILayout.Button("生成（既存の Hitbox は作り直し）")) Build();
                if (GUILayout.Button("削除")) Clear();
            }
        }

        private GameObject ResolveTarget()
        {
            return target != null ? target : Selection.activeGameObject;
        }

        private void Build()
        {
            GameObject root = ResolveTarget();

            SkinnedMeshRenderer renderer = root.GetComponentInChildren<SkinnedMeshRenderer>();
            if (renderer == null || renderer.sharedMesh == null)
            {
                Debug.LogError($"[EntityHitboxBuilder] {root.name}: SkinnedMeshRenderer が見つかりません。");
                return;
            }

            Mesh mesh = renderer.sharedMesh;
            if (!mesh.isReadable)
            {
                Debug.LogError($"[EntityHitboxBuilder] {mesh.name}: メッシュを読めません。" +
                               "FBX の Model タブで Read/Write Enabled を有効にしてから実行してください。");
                return;
            }

            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogError($"[EntityHitboxBuilder] レイヤーが見つかりません: {layerName}");
                return;
            }

            Transform[] bones = renderer.bones;
            Matrix4x4[] bindPoses = mesh.bindposes;

            if (bones.Length == 0 || bindPoses.Length != bones.Length)
            {
                Debug.LogError($"[EntityHitboxBuilder] {root.name}: ボーンとバインドポーズの数が合いません。");
                return;
            }

            MonoBehaviour owner = FindOwner(root);
            if (owner == null)
            {
                // owner が空のまま生成すると、当たり判定からエンティティを解決できず
                // スキャンも捕獲も攻撃も素通りする。生成せずに止める。
                Debug.LogError($"[EntityHitboxBuilder] {root.name}: 所有者が見つかりません。" +
                               "IScannable / ICapturable / IAttackable を実装したコンポーネントを" +
                               "ルートに付けてから実行してください。");
                return;
            }

            ClearInternal(root);

            List<Vector3>[] pointsPerBone = CollectPointsPerBone(mesh, bindPoses, bones.Length);
            int created = 0;

            for (int i = 0; i < bones.Length; i++)
            {
                Transform bone = bones[i];
                if (bone == null) continue;
                if (!string.IsNullOrEmpty(skipNameFilter) && bone.name.Contains(skipNameFilter)) continue;

                List<Vector3> points = pointsPerBone[i];
                if (points == null || points.Count < minVertexCount) continue;

                CreateHitbox(bone, points, owner, layer);
                created++;
            }

            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"[EntityHitboxBuilder] {root.name}: {created} 個の Hitbox を生成しました。");
        }

        /// <summary>
        /// 各ボーンに、そのボーンのローカル空間での担当頂点を集める
        /// </summary>
        // bindposes[i] はメッシュ空間からボーン i のローカル空間への変換なので、
        // 掛けるだけでボーン基準の点群が得られる。
        private List<Vector3>[] CollectPointsPerBone(Mesh mesh, Matrix4x4[] bindPoses, int boneCount)
        {
            Vector3[] vertices = mesh.vertices;
            BoneWeight[] weights = mesh.boneWeights;
            List<Vector3>[] result = new List<Vector3>[boneCount];

            for (int v = 0; v < vertices.Length && v < weights.Length; v++)
            {
                BoneWeight weight = weights[v];

                Add(result, bindPoses, boneCount, weight.boneIndex0, weight.weight0, vertices[v]);
                Add(result, bindPoses, boneCount, weight.boneIndex1, weight.weight1, vertices[v]);
                Add(result, bindPoses, boneCount, weight.boneIndex2, weight.weight2, vertices[v]);
                Add(result, bindPoses, boneCount, weight.boneIndex3, weight.weight3, vertices[v]);
            }

            return result;
        }

        private void Add(List<Vector3>[] result, Matrix4x4[] bindPoses, int boneCount,
            int boneIndex, float weight, Vector3 vertex)
        {
            if (weight < weightThreshold) return;
            if (boneIndex < 0 || boneIndex >= boneCount) return;

            if (result[boneIndex] == null)
            {
                result[boneIndex] = new List<Vector3>();
            }

            result[boneIndex].Add(bindPoses[boneIndex].MultiplyPoint3x4(vertex));
        }

        private void CreateHitbox(Transform bone, List<Vector3> points, MonoBehaviour owner, int layer)
        {
            Bounds bounds = new Bounds(points[0], Vector3.zero);
            for (int i = 1; i < points.Count; i++)
            {
                bounds.Encapsulate(points[i]);
            }

            // 一番長い軸をカプセルの向きにする。ボーンの軸の取り方に依存させないため
            Vector3 size = bounds.size;
            int direction = 0;
            if (size.y >= size.x && size.y >= size.z) direction = 1;
            else if (size.z >= size.x && size.z >= size.y) direction = 2;

            // 軸からの最大距離を半径にする
            float radiusSqr = 0f;
            foreach (Vector3 point in points)
            {
                Vector3 offset = point - bounds.center;
                offset[direction] = 0f;
                radiusSqr = Mathf.Max(radiusSqr, offset.sqrMagnitude);
            }

            GameObject hitbox = new GameObject(HitboxPrefix + bone.name);
            Undo.RegisterCreatedObjectUndo(hitbox, "Create Entity Hitbox");

            hitbox.transform.SetParent(bone, false);
            hitbox.transform.localPosition = bounds.center;
            hitbox.transform.localRotation = Quaternion.identity;
            hitbox.transform.localScale = Vector3.one;
            hitbox.layer = layer;

            CapsuleCollider collider = hitbox.AddComponent<CapsuleCollider>();
            collider.direction = direction;
            collider.height = size[direction];
            collider.radius = Mathf.Sqrt(radiusSqr) * radiusScale;
            collider.isTrigger = true;

            EntityPart part = hitbox.AddComponent<EntityPart>();
            part.Setup(owner, ClassifyPart(bone.name));
        }

        private void Clear()
        {
            GameObject root = ResolveTarget();
            if (root == null) return;

            int removed = ClearInternal(root);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"[EntityHitboxBuilder] {root.name}: {removed} 個の Hitbox を削除しました。");
        }

        private static int ClearInternal(GameObject root)
        {
            List<GameObject> removing = new List<GameObject>();

            foreach (EntityPart part in root.GetComponentsInChildren<EntityPart>(true))
            {
                if (part.gameObject == root) continue;
                if (!part.name.StartsWith(HitboxPrefix)) continue;

                removing.Add(part.gameObject);
            }

            foreach (GameObject go in removing)
            {
                Undo.DestroyObjectImmediate(go);
            }

            return removing.Count;
        }

        private static MonoBehaviour FindOwner(GameObject root)
        {
            foreach (MonoBehaviour behaviour in root.GetComponents<MonoBehaviour>())
            {
                if (behaviour is IScannable || behaviour is ICapturable || behaviour is IAttackable)
                {
                    return behaviour;
                }
            }

            return null;
        }

        /// <summary>
        /// ボーン名から部位を決める
        /// </summary>
        // Jaw は Head の子なので、Head より先に判定する
        private static BodyPart ClassifyPart(string boneName)
        {
            string name = boneName.ToLowerInvariant();

            if (name.Contains("jaw")) return BodyPart.Jaw;
            if (name.Contains("head")) return BodyPart.Head;
            if (name.Contains("fin")) return BodyPart.Fin;
            if (name.Contains("tail")) return BodyPart.Tail;

            return BodyPart.Body;
        }
    }
}
