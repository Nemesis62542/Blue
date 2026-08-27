using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Blue.Entity.Common;
using Blue.Interface;
using Blue.UI;
using Blue.UI.Common;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Blue.Player
{
    public class ScannerController : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int HighlightColorId = Shader.PropertyToID("_HighlightColor");

        [Header("Scan Settings")]
        [SerializeField] private float scanRadius = 12f;
        [SerializeField] private float scanDisableDistance = 18f;
        [SerializeField] private float scanCooldown = 5f;
        [SerializeField] private ScannerView view;
        [SerializeField] private ScannerEffectView effectView;
        [SerializeField] private Slider cooldownSlider;

        [Header("Highlight Settings")]
        [SerializeField] private Material highlightMaterial;
        [ColorUsage(true, true)]
        [SerializeField] private Color safetyColor = new Color(0f, 2f, 0.5f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color warningColor = new Color(2f, 1.5f, 0f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color dangerColor = new Color(2f, 0f, 0f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color defaultColor = new Color(0f, 1f, 2f, 1f);

        private readonly List<IScannable> scannedObjects = new List<IScannable>();
        private readonly Dictionary<IScannable, HighlightData> highlightDataMap = new Dictionary<IScannable, HighlightData>();
        private IScannable lookingScannable = null;
        private ScannerEffectView playingScanEffect = null;
        private bool isCooldown = false;
        private CancellationTokenSource cts;

        private readonly WaitForSeconds scanDelay = new WaitForSeconds(0.5f);
        private readonly WaitForSeconds itemDelay = new WaitForSeconds(0.05f);

        private class HighlightData
        {
            public Renderer[] Renderers;
            public Material[][] OriginalMaterials;
        }

        private void Awake()
        {
            playingScanEffect = Instantiate(effectView, transform.position, Quaternion.identity);
        }

        private void OnDestroy()
        {
            cts?.Cancel();
            cts?.Dispose();
            cooldownSlider?.DOKill();
        }

        public bool Scan(Vector3 origin, Vector3 forward)
        {
            if (isCooldown) return false;

            CancelScan();
            ToggleLookingScannable(null);
            RemoveScannableAll();
            view.HideScanUIAll();

            playingScanEffect.transform.position = transform.position;
            playingScanEffect.gameObject.SetActive(true);
            playingScanEffect.PlayOnce();

            // ボーンごとに分けたエンティティは 1 体から複数ヒットが返るので Distinct が要る
            IEnumerable<IScannable> hits = Physics.OverlapSphere(origin, scanRadius)
                .Select(hit => EntityHit.TryResolve(hit, out IScannable scannable) ? scannable : null)
                .Where(scannable => scannable != null &&
                                    !scannedObjects.Contains(scannable))
                .Distinct();

            StartCoroutine(AddScannables(hits));
            StartCooldownAsync().Forget();
            return true;
        }

        private async UniTaskVoid StartCooldownAsync()
        {
            isCooldown = true;
            cts?.Cancel();
            cts = new CancellationTokenSource();

            // スライダーを0にしてクールダウン中にアニメーション
            if (cooldownSlider != null)
            {
                cooldownSlider.value = 0f;
                cooldownSlider.DOValue(1f, scanCooldown).SetEase(Ease.Linear);
            }

            try
            {
                await UniTask.Delay((int)(scanCooldown * 1000), cancellationToken: cts.Token);
            }
            catch (System.OperationCanceledException) { }

            isCooldown = false;
        }
        
        private IEnumerator AddScannables(IEnumerable<IScannable> scannables)
        {
            yield return scanDelay;
            List<IScannable> scannableList = scannables.ToList();

            foreach (IScannable scannable in scannableList)
            {
                yield return itemDelay;
                AddScannable(scannable);
            }
        } 

        private bool FindSameSchool(SchoolMember obj)
        {
            IEnumerable<SchoolMember> school_fish = scannedObjects
                                                    .Select(scannable => ((MonoBehaviour)scannable).GetComponent<SchoolMember>())
                                                    .Where(scannable => scannable != null && scannable.School == obj.School);

            return school_fish.Count() > 1;
        }

        public void ToggleLookingScannable(IScannable scannable)
        {
            if (scannable == lookingScannable) return;
            view.ToggleLookingUI(lookingScannable, false);
            lookingScannable = scannable;
            view.ToggleLookingUI(lookingScannable, true);
        }

        public void CancelScan()
        {
            if (lookingScannable == null) return;
            view.UpdateScanProgress(lookingScannable, 0f);
        }

        private void Update()
        {
            for (int i = scannedObjects.Count - 1; i >= 0; i--)
            {
                IScannable scannable = scannedObjects[i];
                MonoBehaviour target = (MonoBehaviour)scannable;

                if (target == null)
                {
                    highlightDataMap.Remove(scannable);
                    scannedObjects.RemoveAt(i);
                    continue;
                }

                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance >= scanDisableDistance)
                {
                    // 距離が離れたらスキャン解除
                    view.HideScanUI(scannable);
                    RemoveScannable(scannable, i);
                    continue;
                }

                view.UpdateDetailUI(scannable, true);
            }
        }

        private void AddScannable(IScannable scannable)
        {
            scannedObjects.Add(scannable);
            EnableHighlight(scannable);
            scannable.OnScanStart();
            SetDetailUI(scannable);
        }

        public void SetDetailUI(IScannable scannable)
        {
            Transform target_position = ((MonoBehaviour)scannable).transform;

            if (((MonoBehaviour)scannable).transform.TryGetComponent(out SchoolMember fish))
            {
                if (!FindSameSchool(fish))
                {
                    target_position = fish.School.transform;
                    view.SetDetailUI(target_position, scannable);
                }
            }
            else
            {
                view.SetDetailUI(target_position, scannable);
            }
        }

        private void RemoveScannable(IScannable scannable, int index)
        {
            DisableHighlight(scannable);
            scannable?.OnScanEnd();
            scannedObjects.RemoveAt(index);
        }

        private void RemoveScannableAll()
        {
            for (int i = scannedObjects.Count - 1; i >= 0; i--)
            {
                RemoveScannable(scannedObjects[i], i);
            }
        }

        private void EnableHighlight(IScannable scannable)
        {
            Renderer[] renderers = scannable.TargetRenderers;
            if (renderers == null || renderers.Length == 0) return;

            HighlightData data = new HighlightData
            {
                Renderers = renderers,
                OriginalMaterials = new Material[renderers.Length][]
            };

            Color highlightColor = GetColorForThreat(scannable.ScanData.threat);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;

                // 元のマテリアルをバックアップ
                data.OriginalMaterials[i] = renderer.sharedMaterials;

                // ハイライトマテリアルを追加
                Material[] newMaterials = new Material[data.OriginalMaterials[i].Length + 1];
                data.OriginalMaterials[i].CopyTo(newMaterials, 0);
                newMaterials[newMaterials.Length - 1] = highlightMaterial;
                renderer.sharedMaterials = newMaterials;

                // PropertyBlockで色とテクスチャを設定
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                int highlightIndex = newMaterials.Length - 1;
                renderer.GetPropertyBlock(propertyBlock, highlightIndex);

                // 元マテリアルからBaseMapを取得してアルファクリップ用に設定
                if (data.OriginalMaterials[i].Length > 0 && data.OriginalMaterials[i][0] != null)
                {
                    Texture baseMap = data.OriginalMaterials[i][0].GetTexture(BaseMapId);
                    if (baseMap != null)
                    {
                        propertyBlock.SetTexture(BaseMapId, baseMap);
                    }
                }

                propertyBlock.SetColor(HighlightColorId, highlightColor);
                renderer.SetPropertyBlock(propertyBlock, highlightIndex);
            }

            highlightDataMap[scannable] = data;
        }

        private void DisableHighlight(IScannable scannable)
        {
            if (scannable == null || !highlightDataMap.TryGetValue(scannable, out HighlightData data))
                return;

            for (int i = 0; i < data.Renderers.Length; i++)
            {
                Renderer renderer = data.Renderers[i];
                if (renderer == null) continue;

                // 元のマテリアルに戻す
                if (data.OriginalMaterials[i] != null)
                {
                    renderer.sharedMaterials = data.OriginalMaterials[i];
                }
            }

            highlightDataMap.Remove(scannable);
        }

        private Color GetColorForThreat(ScanData.Threat threat)
        {
            return threat switch
            {
                ScanData.Threat.Safety => safetyColor,
                ScanData.Threat.Warning => warningColor,
                ScanData.Threat.Danger => dangerColor,
                _ => defaultColor
            };
        }
    }
}
