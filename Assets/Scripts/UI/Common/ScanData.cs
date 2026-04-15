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

        // カスタム表記（nullの場合はデフォルト表記を使用）
        public string threatLabel;
        public string capturableLabel;
        public string description;

        public ScanData(string display_name, Threat threat, bool is_capturable)
        {
            displayName = display_name;
            this.threat = threat;
            isCapturable = is_capturable;
            threatLabel = null;
            capturableLabel = null;
            description = null;
        }
    }
}