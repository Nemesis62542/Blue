using System.Collections.Generic;
using UnityEngine;
using Blue.Interface;

namespace Blue.Player
{
    public class ScannerController : MonoBehaviour
    {
        [Header("Scan Settings")]
        [SerializeField] private float scanRadius = 6f;
        [SerializeField] private float fieldOfViewAngle = 60f;
        [SerializeField] private float scanDisplayDuration = 3f;

        private readonly List<IScannable> scannedObjects = new List<IScannable>();
        private readonly Dictionary<IScannable, float> scanTimers = new Dictionary<IScannable, float>();

        private void Update()
        {
            UpdateScannedObjects();
        }

        public void Scan(Vector3 origin, Vector3 forward)
        {
            Collider[] hits = Physics.OverlapSphere(origin, scanRadius);

            foreach (Collider hit in hits)
            {
                if (!hit.TryGetComponent<IScannable>(out IScannable scannable)) continue;

                Vector3 direction = (hit.transform.position - origin).normalized;
                float angle = Vector3.Angle(forward, direction);

                if (angle > fieldOfViewAngle * 0.5f) continue;

                if (!scannedObjects.Contains(scannable))
                {
                    scannable.OnScanStart();
                    scannedObjects.Add(scannable);
                    scanTimers[scannable] = 0f;
                }
                else
                {
                    scanTimers[scannable] = 0f;
                }
            }
        }

        private void UpdateScannedObjects()
        {
            for (int i = scannedObjects.Count - 1; i >= 0; i--)
            {
                IScannable scannable = scannedObjects[i];

                scanTimers[scannable] += Time.deltaTime;
                if (scanTimers[scannable] >= scanDisplayDuration)
                {
                    scannable.OnScanEnd();
                    scannedObjects.RemoveAt(i);
                    scanTimers.Remove(scannable);
                }
            }
        }
    }
}
