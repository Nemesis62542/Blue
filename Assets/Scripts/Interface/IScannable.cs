using Blue.UI.Common;
using UnityEngine;

namespace Blue.Interface
{
    public interface IScannable
    {
        ScanData ScanData { get; }
        Renderer[] TargetRenderers { get; }

        void OnScanStart();
        void OnScanEnd();
    }
}
