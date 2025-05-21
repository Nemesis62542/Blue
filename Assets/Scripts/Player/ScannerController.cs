using System.Collections.Generic;
using UnityEngine;
using Blue.Interface;
using Blue.UI;

namespace Blue.Player
{
    public class ScannerController : MonoBehaviour
    {
        [Header("Scan Settings")]
        [SerializeField] private float scanRadius = 6f;
        [SerializeField] private float fieldOfViewAngle = 60f;
        [SerializeField] private float scanDisplayDuration = 3f;
        [SerializeField] private ScannerView scannerView;

        private readonly List<IScannable> scannedObjects = new List<IScannable>();
        private readonly Dictionary<IScannable, float> scanTimers = new Dictionary<IScannable, float>();

        public void Scan(Vector3 origin, Vector3 forward)
        {
            Collider[] hits = Physics.OverlapSphere(origin, scanRadius);

            foreach (Collider hit in hits)
            {
                if (!hit.TryGetComponent(out IScannable scannable)) continue;

                if (IsWithinFieldOfView(origin, forward, hit.transform.position) && !scannedObjects.Contains(scannable))
                {
                    AddScannable(scannable);
                }
                else if (scannedObjects.Contains(scannable))
                {
                    ResetScanTimer(scannable);
                }
            }
        }

        private bool IsWithinFieldOfView(Vector3 origin, Vector3 forward, Vector3 target_position)
        {
            Vector3 direction = (target_position - origin).normalized;
            float angle = Vector3.Angle(forward, direction);
            return angle <= fieldOfViewAngle * 0.5f;
        }

        private void AddScannable(IScannable scannable)
        {
            scannable.OnScanStart();
            scannedObjects.Add(scannable);
            scanTimers[scannable] = 0f;

            Transform target_position = ((MonoBehaviour)scannable).transform;
            scannerView.ShowScanUI(target_position, scannable.DisplayName, scanDisplayDuration);
        }

        private void ResetScanTimer(IScannable scannable)
        {
            scanTimers[scannable] = 0f;
        }

        private void Update()
        {
            for (int i = scannedObjects.Count - 1; i >= 0; i--)
            {
                IScannable scannable = scannedObjects[i];
                UpdateScanTimer(scannable, i);
            }
        }

        private void UpdateScanTimer(IScannable scannable, int index)
        {
            scanTimers[scannable] += Time.deltaTime;

            if (scanTimers[scannable] >= scanDisplayDuration)
            {
                RemoveScannable(scannable, index);
            }
        }

        private void RemoveScannable(IScannable scannable, int index)
        {
            scannable.OnScanEnd();
            scannedObjects.RemoveAt(index);
            scanTimers.Remove(scannable);
        }
    }
}
