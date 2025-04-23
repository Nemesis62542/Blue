using UnityEngine;

namespace Blue.Interface
{
    public interface IScannable
    {
        string DisplayName { get; }
        Renderer[] TargetRenderers { get; }

        void OnScanStart();
        void OnScanEnd();
    }
}
