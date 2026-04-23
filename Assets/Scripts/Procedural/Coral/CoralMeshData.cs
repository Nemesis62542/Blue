using UnityEngine;
using System.Collections.Generic;

namespace Blue.Procedural.Coral
{
    /// <summary>
    /// Mesh生成用の中間データ構造
    /// 頂点・三角形の動的追加をサポート
    /// </summary>
    public class CoralMeshData
    {
        private List<Vector3> vertices;
        private List<int> triangles;
        private List<Vector3> normals;
        private List<Vector2> uvs;
        private List<Color> colors;

        public int VertexCount => vertices.Count;

        public CoralMeshData()
        {
            vertices = new List<Vector3>();
            triangles = new List<int>();
            normals = new List<Vector3>();
            uvs = new List<Vector2>();
            colors = new List<Color>();
        }

        /// <summary>
        /// データをクリア
        /// </summary>
        public void Clear()
        {
            vertices.Clear();
            triangles.Clear();
            normals.Clear();
            uvs.Clear();
            colors.Clear();
        }

        /// <summary>
        /// 頂点を追加し、インデックスを返す（カラー付き）
        /// </summary>
        public int AddVertex(Vector3 position, Vector3 normal, Vector2 uv, Color color)
        {
            int index = vertices.Count;
            vertices.Add(position);
            normals.Add(normal);
            uvs.Add(uv);
            colors.Add(color);
            return index;
        }

        /// <summary>
        /// 頂点を追加し、インデックスを返す（カラーは白）
        /// </summary>
        public int AddVertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            return AddVertex(position, normal, uv, Color.white);
        }

        /// <summary>
        /// 頂点を追加（法線とUVはデフォルト）
        /// </summary>
        public int AddVertex(Vector3 position)
        {
            return AddVertex(position, Vector3.up, Vector2.zero, Color.white);
        }

        /// <summary>
        /// 三角形を追加（3つの頂点インデックス）
        /// </summary>
        public void AddTriangle(int i0, int i1, int i2)
        {
            triangles.Add(i0);
            triangles.Add(i1);
            triangles.Add(i2);
        }

        /// <summary>
        /// クワッドを追加（2つの三角形）
        /// 頂点順序: 0-1
        ///           | |
        ///           3-2
        /// </summary>
        public void AddQuad(int i0, int i1, int i2, int i3)
        {
            AddTriangle(i0, i1, i2);
            AddTriangle(i0, i2, i3);
        }

        /// <summary>
        /// 円柱セグメントを追加（2つのリングを接続）
        /// </summary>
        /// <param name="center1">下端の中心</param>
        /// <param name="radius1">下端の半径</param>
        /// <param name="center2">上端の中心</param>
        /// <param name="radius2">上端の半径</param>
        /// <param name="segments">円周方向の分割数</param>
        /// <param name="up">上方向ベクトル</param>
        /// <param name="vOffset">UV V座標のオフセット</param>
        /// <param name="vScale">UV V座標のスケール</param>
        public void AddCylinderSegment(
            Vector3 center1, float radius1,
            Vector3 center2, float radius2,
            int segments, Vector3 up,
            float vOffset = 0f, float vScale = 1f)
        {
            Vector3 direction = (center2 - center1).normalized;

            // 上方向に垂直な2つの軸を計算
            Vector3 right = Vector3.Cross(up, direction);
            if (right.sqrMagnitude < 0.001f)
            {
                right = Vector3.Cross(Vector3.forward, direction);
            }
            right.Normalize();
            Vector3 forward = Vector3.Cross(direction, right).normalized;

            int[] bottomIndices = new int[segments];
            int[] topIndices = new int[segments];

            // 下端リング頂点
            for (int i = 0; i < segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                Vector3 localDir = right * cos + forward * sin;
                Vector3 worldPos = center1 + localDir * radius1;
                Vector3 normal = localDir;
                Vector2 uv = new Vector2((float)i / segments, vOffset);

                bottomIndices[i] = AddVertex(worldPos, normal, uv);
            }

            // 上端リング頂点
            for (int i = 0; i < segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                Vector3 localDir = right * cos + forward * sin;
                Vector3 worldPos = center2 + localDir * radius2;
                Vector3 normal = localDir;
                Vector2 uv = new Vector2((float)i / segments, vOffset + vScale);

                topIndices[i] = AddVertex(worldPos, normal, uv);
            }

            // 三角形を接続
            for (int i = 0; i < segments; i++)
            {
                int nextI = (i + 1) % segments;
                AddQuad(bottomIndices[i], topIndices[i], topIndices[nextI], bottomIndices[nextI]);
            }
        }

        /// <summary>
        /// 閉じたBox（直方体）を追加
        /// 引き伸ばしたCubeとして6面すべてを持つ
        /// </summary>
        /// <param name="start">開始点（中心）</param>
        /// <param name="end">終了点（中心）</param>
        /// <param name="size">断面のサイズ（半幅）</param>
        /// <param name="up">上方向の参照ベクトル</param>
        /// <param name="color">頂点カラー</param>
        public void AddBox(Vector3 start, Vector3 end, float size, Vector3 up, Color color)
        {
            Vector3 direction = (end - start);
            float length = direction.magnitude;
            if (length < 0.0001f) return;

            direction /= length;

            // 上方向に垂直な2つの軸を計算
            Vector3 right = Vector3.Cross(up, direction);
            if (right.sqrMagnitude < 0.001f)
            {
                right = Vector3.Cross(Vector3.forward, direction);
            }
            right.Normalize();
            Vector3 forward = Vector3.Cross(direction, right).normalized;

            // 8つの頂点を計算
            Vector3[] corners = new Vector3[8];
            // 下面（start側）
            corners[0] = start + (right + forward) * size;   // 右前
            corners[1] = start + (-right + forward) * size;  // 左前
            corners[2] = start + (-right - forward) * size;  // 左後
            corners[3] = start + (right - forward) * size;   // 右後
            // 上面（end側）
            corners[4] = end + (right + forward) * size;     // 右前
            corners[5] = end + (-right + forward) * size;    // 左前
            corners[6] = end + (-right - forward) * size;    // 左後
            corners[7] = end + (right - forward) * size;     // 右後

            // 6つの面を追加（各面に独立した頂点を使用してシャープなエッジを維持）

            // 前面 (+forward)
            Vector3 frontNormal = forward;
            int f0 = AddVertex(corners[0], frontNormal, new Vector2(1, 0), color);
            int f1 = AddVertex(corners[1], frontNormal, new Vector2(0, 0), color);
            int f2 = AddVertex(corners[5], frontNormal, new Vector2(0, 1), color);
            int f3 = AddVertex(corners[4], frontNormal, new Vector2(1, 1), color);
            AddQuad(f0, f1, f2, f3);

            // 後面 (-forward)
            Vector3 backNormal = -forward;
            int b0 = AddVertex(corners[2], backNormal, new Vector2(1, 0), color);
            int b1 = AddVertex(corners[3], backNormal, new Vector2(0, 0), color);
            int b2 = AddVertex(corners[7], backNormal, new Vector2(0, 1), color);
            int b3 = AddVertex(corners[6], backNormal, new Vector2(1, 1), color);
            AddQuad(b0, b1, b2, b3);

            // 右面 (+right)
            Vector3 rightNormal = right;
            int r0 = AddVertex(corners[3], rightNormal, new Vector2(1, 0), color);
            int r1 = AddVertex(corners[0], rightNormal, new Vector2(0, 0), color);
            int r2 = AddVertex(corners[4], rightNormal, new Vector2(0, 1), color);
            int r3 = AddVertex(corners[7], rightNormal, new Vector2(1, 1), color);
            AddQuad(r0, r1, r2, r3);

            // 左面 (-right)
            Vector3 leftNormal = -right;
            int l0 = AddVertex(corners[1], leftNormal, new Vector2(1, 0), color);
            int l1 = AddVertex(corners[2], leftNormal, new Vector2(0, 0), color);
            int l2 = AddVertex(corners[6], leftNormal, new Vector2(0, 1), color);
            int l3 = AddVertex(corners[5], leftNormal, new Vector2(1, 1), color);
            AddQuad(l0, l1, l2, l3);

            // 下面（start側、-direction）
            Vector3 bottomNormal = -direction;
            int bt0 = AddVertex(corners[0], bottomNormal, new Vector2(1, 1), color);
            int bt1 = AddVertex(corners[3], bottomNormal, new Vector2(1, 0), color);
            int bt2 = AddVertex(corners[2], bottomNormal, new Vector2(0, 0), color);
            int bt3 = AddVertex(corners[1], bottomNormal, new Vector2(0, 1), color);
            AddQuad(bt0, bt1, bt2, bt3);

            // 上面（end側、+direction）
            Vector3 topNormal = direction;
            int t0 = AddVertex(corners[4], topNormal, new Vector2(1, 0), color);
            int t1 = AddVertex(corners[5], topNormal, new Vector2(0, 0), color);
            int t2 = AddVertex(corners[6], topNormal, new Vector2(0, 1), color);
            int t3 = AddVertex(corners[7], topNormal, new Vector2(1, 1), color);
            AddQuad(t0, t1, t2, t3);
        }

        /// <summary>
        /// 閉じたBox（直方体）を追加（カラーは白）
        /// </summary>
        public void AddBox(Vector3 start, Vector3 end, float size, Vector3 up)
        {
            AddBox(start, end, size, up, Color.white);
        }

        /// <summary>
        /// 球キャップを追加（枝の先端用の半球）
        /// </summary>
        /// <param name="center">球の中心</param>
        /// <param name="radius">球の半径</param>
        /// <param name="direction">キャップの向き（先端方向）</param>
        /// <param name="segments">円周方向の分割数</param>
        /// <param name="rings">緯度方向の分割数</param>
        public void AddSphereCap(Vector3 center, float radius, Vector3 direction, int segments, int rings = 4)
        {
            direction.Normalize();

            // 方向に垂直な2つの軸を計算
            Vector3 right = Vector3.Cross(Vector3.up, direction);
            if (right.sqrMagnitude < 0.001f)
            {
                right = Vector3.Cross(Vector3.forward, direction);
            }
            right.Normalize();
            Vector3 forward = Vector3.Cross(direction, right).normalized;

            // 底面（円柱と接続する面）の頂点インデックス
            int[] prevRing = new int[segments];

            // 底面リングを作成
            for (int i = 0; i < segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                Vector3 localDir = right * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
                Vector3 pos = center + localDir * radius;
                Vector3 normal = localDir;
                Vector2 uv = new Vector2((float)i / segments, 0f);
                prevRing[i] = AddVertex(pos, normal, uv);
            }

            // 緯度方向にリングを追加
            for (int ring = 1; ring <= rings; ring++)
            {
                float phi = Mathf.PI * 0.5f * ring / rings; // 0 to PI/2
                float ringRadius = radius * Mathf.Cos(phi);
                float height = radius * Mathf.Sin(phi);
                Vector3 ringCenter = center + direction * height;

                int[] currentRing = new int[segments];

                for (int i = 0; i < segments; i++)
                {
                    float angle = 2f * Mathf.PI * i / segments;
                    Vector3 localDir = right * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
                    Vector3 pos = ringCenter + localDir * ringRadius;
                    Vector3 normal = (localDir * Mathf.Cos(phi) + direction * Mathf.Sin(phi)).normalized;
                    Vector2 uv = new Vector2((float)i / segments, (float)ring / rings);
                    currentRing[i] = AddVertex(pos, normal, uv);
                }

                // 前のリングと現在のリングを接続
                for (int i = 0; i < segments; i++)
                {
                    int nextI = (i + 1) % segments;
                    AddQuad(prevRing[i], currentRing[i], currentRing[nextI], prevRing[nextI]);
                }

                prevRing = currentRing;
            }

            // 頂点（先端のキャップ）
            Vector3 tipPos = center + direction * radius;
            int tipIndex = AddVertex(tipPos, direction, new Vector2(0.5f, 1f));

            // 最後のリングと頂点を接続
            for (int i = 0; i < segments; i++)
            {
                int nextI = (i + 1) % segments;
                AddTriangle(prevRing[i], tipIndex, prevRing[nextI]);
            }
        }

        /// <summary>
        /// 円形の底面（キャップ）を追加
        /// </summary>
        public void AddCircleCap(Vector3 center, float radius, Vector3 normal, int segments, bool flipNormal = false)
        {
            normal.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, normal);
            if (right.sqrMagnitude < 0.001f)
            {
                right = Vector3.Cross(Vector3.forward, normal);
            }
            right.Normalize();
            Vector3 forward = Vector3.Cross(normal, right).normalized;

            // 中心頂点
            int centerIndex = AddVertex(center, flipNormal ? -normal : normal, new Vector2(0.5f, 0.5f));

            // 外周頂点
            int[] ringIndices = new int[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                Vector3 localDir = right * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
                Vector3 pos = center + localDir * radius;
                Vector2 uv = new Vector2(0.5f + Mathf.Cos(angle) * 0.5f, 0.5f + Mathf.Sin(angle) * 0.5f);
                ringIndices[i] = AddVertex(pos, flipNormal ? -normal : normal, uv);
            }

            // 三角形を作成
            for (int i = 0; i < segments; i++)
            {
                int nextI = (i + 1) % segments;
                if (flipNormal)
                {
                    AddTriangle(centerIndex, ringIndices[nextI], ringIndices[i]);
                }
                else
                {
                    AddTriangle(centerIndex, ringIndices[i], ringIndices[nextI]);
                }
            }
        }

        /// <summary>
        /// Meshに変換
        /// </summary>
        /// <param name="recalculateNormals">法線を再計算するか</param>
        /// <param name="recalculateBounds">バウンドを再計算するか</param>
        public Mesh ToMesh(bool recalculateNormals = false, bool recalculateBounds = true)
        {
            Mesh mesh = new Mesh();
            ApplyToMesh(mesh, recalculateNormals, recalculateBounds);
            return mesh;
        }

        /// <summary>
        /// 既存のMeshに適用
        /// </summary>
        public void ApplyToMesh(Mesh mesh, bool recalculateNormals = false, bool recalculateBounds = true)
        {
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);

            if (recalculateNormals)
            {
                mesh.RecalculateNormals();
            }

            if (recalculateBounds)
            {
                mesh.RecalculateBounds();
            }

            mesh.RecalculateTangents();
        }
    }
}
