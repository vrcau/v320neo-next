using SaccFlightAndVehicles;
using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.AuxiliarySaccDfunc
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SaccAirVehicleAnimatorOverride : UdonSharpBehaviour
    {
        public SaccAirVehicle saccAirVehicle;
        public SAV_EffectsController saccEffectController;

        public Animator overrideAnimator;

        public void SFEXT_L_EntityStart()
        {
            if (saccAirVehicle) saccAirVehicle.VehicleAnimator = overrideAnimator;
            if (saccEffectController) saccEffectController.VehicleAnimator = overrideAnimator;
        }
    }
}