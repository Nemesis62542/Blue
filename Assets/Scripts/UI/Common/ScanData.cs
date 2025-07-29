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
        public bool isCapturable;

        public ScanData(string display_name, Threat threat, bool is_capturable)
        {
            displayName = display_name;
            this.threat = threat;
            isCapturable = is_capturable;
        }
    }
}