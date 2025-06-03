using System;
using UnityEngine;

namespace Blue.UI.Common
{
    public class ScanData
    {
        public enum Threat
        {
            None,
            Safety,
            Warning,
            Danger
        }

        public string displayName;
        public Threat threat;

        public ScanData(string display_name, Threat threat)
        {
            displayName = display_name;
            this.threat = threat;
        }
    }
}