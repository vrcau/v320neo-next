using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.AuxiliarySaccDfunc
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DialPositionCompatibilityHelper : UdonSharpBehaviour
    {
        public SaccEntity EntityControl;
        
        public void SFEXT_L_EntityStart()
        {
            if (!EntityControl)
            {
                Debug.LogWarning($"[{nameof(DialPositionCompatibilityHelper)}] EntityControl is null, this script won't do anything.", this);
                return;
            }

            foreach (var leftDial in EntityControl.Dial_Functions_L)
            {
                leftDial.SendCustomEvent("DFUNC_LeftDial");
            }

            foreach (var rightDial in EntityControl.Dial_Functions_R)
            {
                rightDial.SendCustomEvent("DFUNC_RightDial");
            }
        }
    }
}