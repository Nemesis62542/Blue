using System;
using Blue.UI.Common;
using UniRx;
using UnityEngine;

namespace Blue.Interface
{
    public interface IScannable
    {
        ScanData ScanData { get; }
        Renderer[] TargetRenderers { get; }
        IObservable<Unit> OnScanDataChanged { get; }

        void OnScanStart() { }
        void OnScanEnd() { }
    }
}
