using System;
using JetBrains.Annotations;
using UdonSharp;

namespace VAU.V320NeoNext.Runtime.Systems.LegacyAutoBrake
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AutoBrakeFlightMenuController : UdonSharpBehaviour
    {
        public AutoBrake autoBrake;

        [PublicAPI] [NonSerialized] public bool isAutoBrakeLowSelected;
        [PublicAPI] [NonSerialized] public bool isAutoBrakeMedSelected;
        [PublicAPI] [NonSerialized] public bool isAutoBrakeMaxSelected;

        [PublicAPI] [NonSerialized] public bool isAutoBrakeArmOrWorking;

        [PublicAPI] [NonSerialized] public string autoBrakeStatusText;

        private void LateUpdate()
        {
            var autoBrakeMode = autoBrake.currentAutoBrakeMode;
            isAutoBrakeLowSelected = autoBrakeMode == AutoBrakeMode.Low;
            isAutoBrakeMedSelected = autoBrakeMode == AutoBrakeMode.Med;
            isAutoBrakeMaxSelected = autoBrakeMode == AutoBrakeMode.Max;

            isAutoBrakeArmOrWorking = autoBrakeMode != AutoBrakeMode.None;

            string autoBrakeModeText;
            switch (autoBrakeMode)
            {
                case AutoBrakeMode.None:
                    autoBrakeModeText = "OFF";
                    break;
                case AutoBrakeMode.Low:
                    autoBrakeModeText = "LOW";
                    break;
                case AutoBrakeMode.Med:
                    autoBrakeModeText = "MED";
                    break;
                case AutoBrakeMode.Max:
                    autoBrakeModeText = "MAX";
                    break;
                default:
                    autoBrakeModeText = autoBrakeMode.ToString();
                    break;
            }

            var statusText = GetStatusText();
            autoBrakeStatusText = statusText + autoBrakeModeText;
        }

        private string GetStatusText()
        {
            if (autoBrake.isAutoBrakeActive)
            {
                return autoBrake.isReachDecelerationRateTarget ? "DECEL\n" : "NO DECEL\n";
            }

            return autoBrake.currentAutoBrakeMode != AutoBrakeMode.None ? "ARM\n" : "";
        }

        [PublicAPI]
        public void SelectAutoBrakeOff() => autoBrake.SelectAutoBrakeOff();

        [PublicAPI]
        public void SelectAutoBrakeLow() => autoBrake.SelectAutoBrakeLow();

        [PublicAPI]
        public void SelectAutoBrakeMed() => autoBrake.SelectAutoBrakeMed();

        [PublicAPI]
        public void SelectAutoBrakeMax() => autoBrake.SelectedAutoBrakeMax();
    }
}