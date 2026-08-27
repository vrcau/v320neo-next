using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Systems.LandingLight
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class A320LandingLightToggle : UdonSharpBehaviour
    {
        public GameObject lightObj;

        [UdonSynced] public bool isLandingLightOn;

        #region Sacc Events

        public void SFEXT_L_EntityStart()
        {
            TrySetState(false);
        }

        public void SFEXT_G_Explode()
        {
            TrySetState(false);
        }

        public void SFEXT_G_RespawnButton()
        {
            TrySetState(false);
        }

        public void KeyboardInput()
        {
            EnsureOwnership();
            TrySetState(!isLandingLightOn);
        }

        #endregion

        [PublicAPI]
        public void ToggleLandingLight()
        {
            EnsureOwnership();
            TrySetState(!isLandingLightOn);
        }

        public override void OnDeserialization()
        {
            UpdateLandingLightState();
        }

        private void TrySetState(bool value)
        {
            if (!Networking.IsOwner(gameObject)) return;

            isLandingLightOn = value;
            UpdateLandingLightState();

            RequestSerialization();
        }

        private void UpdateLandingLightState()
        {
            lightObj.SetActive(isLandingLightOn);
        }

        private void EnsureOwnership()
        {
            if (!Networking.IsOwner(gameObject))
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
    }
}