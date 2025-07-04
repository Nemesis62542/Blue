using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Blue.Interface;
using Blue.UI;

namespace Blue.Player
{
    public class ScannerController : MonoBehaviour
    {
        [Header("Scan Settings")]
        [SerializeField] private float scanRadius = 8f;
        [SerializeField] private float fieldOfViewAngle = 60f;
        [SerializeField] private ScannerView view;

        private readonly List<IScannable> scannedObjects = new List<IScannable>();
        private IScannable lookingScannable = null;

        public void Scan(Vector3 origin, Vector3 forward)
        {
            CancelScan();
            ToggleLookingScannable(null);
            RemoveScannableAll();
            view.ReflashScanUI();

            IEnumerable<IScannable> hits = Physics.OverlapSphere(origin, scanRadius)
                .Select(hit => hit.GetComponent<IScannable>())
                .Where(scannable => scannable != null && 
                                    IsWithinFieldOfView(origin, forward, ((MonoBehaviour)scannable).transform.position) &&
                                    !scannedObjects.Contains(scannable));

            foreach (IScannable scannable in hits)
            {
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
            foreach (IScannable scannable in scannedObjects)
            {
                MonoBehaviour target = (MonoBehaviour)scannable;
                if (target == null)
                {
                    scannedObjects.Remove(scannable);
                    return;
                }
                view.UpdateDetailUI(scannable);
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
