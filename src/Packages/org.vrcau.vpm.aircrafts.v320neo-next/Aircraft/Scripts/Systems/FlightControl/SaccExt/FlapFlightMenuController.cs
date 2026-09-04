using System;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.Systems.LegacyFlightDataProvider.LegacyADRIRU;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl.SaccExt
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FlapFlightMenuController : UdonSharpBehaviour
    {
        public DFUNC_a320_FlapController flapController;
        public ADR adr;

        [NonSerialized] public string currentFlapPositionText;
        [NonSerialized] public string targetFlapPositionText;
        [NonSerialized] public string flapStatusText;

        private void LateUpdate()
        {
            var detentIndex = flapController.detentIndex;
            var isMoving = detentIndex < 0;
            var targetDetentIndex = flapController.targetDetentIndex;

            currentFlapPositionText = isMoving ? GetMovingFlapPositionText() : GetFlapPositionText(detentIndex);
            targetFlapPositionText = GetFlapPositionText(targetDetentIndex);

            if (!isMoving && detentIndex == targetDetentIndex)
            {
                flapStatusText = currentFlapPositionText;
            }
            else
            {
                flapStatusText = $"{currentFlapPositionText} -> {targetFlapPositionText}";
            }
        }

        private string GetMovingFlapPositionText()
        {
            var currentSlatAngle = flapController.slatAngle;
            var currentFlapAngle = flapController.flapAngle;

            for (var index = flapController.slatDetents.Length - 1; index >= 0; index--)
            {
                var slatAngle = flapController.slatDetents[index];
                var flapAngle = flapController.flapDetents[index];
                if (currentSlatAngle > slatAngle && currentFlapAngle > flapAngle)
                {
                    return "> " + GetFlapPositionText(index);
                }

                if (Mathf.Approximately(currentSlatAngle, slatAngle) && Mathf.Approximately(currentFlapAngle, flapAngle))
                {
                    return GetFlapPositionText(index) + "~";
                }
            }

            return "Unknown";
        }

        private string GetFlapPositionText(int index)
        {
            switch (index)
            {
                case 0:
                    return "UP";
                case 1:
                    return "1";
                case 2:
                    return "1+F";
                case 3:
                    return "2";
                case 4:
                    return "3";
                case 5:
                    return "FULL";
            }

            return index.ToString();
        }

        [PublicAPI]
        public void SetFlapUp() => SetFlap(0);

        [PublicAPI]
        public void SetFlap1() => SetFlap(1);

        [PublicAPI]
        public void SetFlap2() => SetFlap(2);

        [PublicAPI]
        public void SetFlap3() => SetFlap(3);

        [PublicAPI]
        public void SetFlapFull() => SetFlap(4);

        private void SetFlap(int index)
        {
            flapController.SetLeverIndex(index);
        }
    }
}