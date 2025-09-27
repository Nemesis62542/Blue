using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Blue.Interface;
using Blue.UI;
using System.Collections;

namespace Blue.Player
{
    public class ScannerController : MonoBehaviour
    {
        [Header("Scan Settings")]
        [SerializeField] private float scanRadius = 12f;
        [SerializeField] private float scanDisableDistance = 18f;
        [SerializeField] private float fieldOfViewAngle = 60f;
        [SerializeField] private ScannerView view;
        [SerializeField] private ScannerEffectView effectView;

        private readonly List<IScannable> scannedObjects = new List<IScannable>();
        private IScannable lookingScannable = null;
        private ScannerEffectView playingScanEffect = null;

        private readonly WaitForSeconds scanDelay = new WaitForSeconds(0.5f);
        private readonly WaitForSeconds itemDelay = new WaitForSeconds(0.05f);

        private void Awake()
        {
            playingScanEffect = Instantiate(effectView, transform.position, Quaternion.identity);
        }

        public void Scan(Vector3 origin, Vector3 forward)
        {
            if (playingScanEffect.gameObject.activeSelf) return;

            CancelScan();
            ToggleLookingScannable(null);
            RemoveScannableAll();
            view.HideScanUIAll();

            playingScanEffect.transform.position = transform.position;
            playingScanEffect.gameObject.SetActive(true);
            playingScanEffect.PlayOnce();

            IEnumerable<IScannable> hits = Physics.OverlapSphere(origin, scanRadius)
                .Select(hit => hit.GetComponent<IScannable>())
                .Where(scannable => scannable != null &&
                                    !scannedObjects.Contains(scannable));

            StartCoroutine(AddScannables(hits));
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

        private bool FindSameSchool(SchoolChild obj)
        {
            IEnumerable<SchoolChild> school_fish = scannedObjects
                                                    .Select(scannable => ((MonoBehaviour)scannable).GetComponent<SchoolChild>())
                                                    .Where(scannable => scannable.Spawner == obj.Spawner);

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
                    scannedObjects.RemoveAt(i);
                    continue;
                }
                view.UpdateDetailUI(scannable, Vector3.Distance(transform.position, target.transform.position) < scanDisableDistance);
            }
        }

        private bool IsWithinFieldOfView(Vector3 origin, Vector3 forward, Vector3 target_position)
        {
            Vector3 direction = (target_position - origin).normalized;
            float dot = Vector3.Dot(forward.normalized, direction);
            float threshold = Mathf.Cos(fieldOfViewAngle * 0.5f * Mathf.Deg2Rad);
            return dot >= threshold;
        }

        private void AddScannable(IScannable scannable)
        {
            scannable.OnScanStart();
            scannedObjects.Add(scannable);
            SetDetailUI(scannable);
        }

        public void SetDetailUI(IScannable scannable)
        {
            Transform target_position = ((MonoBehaviour)scannable).transform;

            if (((MonoBehaviour)scannable).transform.TryGetComponent(out SchoolChild fish))
            {
                if (!FindSameSchool(fish))
                {
                    target_position = fish.Spawner.transform;
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
    }
}
