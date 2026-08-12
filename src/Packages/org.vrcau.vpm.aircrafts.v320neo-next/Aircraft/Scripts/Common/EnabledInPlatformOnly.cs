using System;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class EnabledInPlatformOnly : UdonSharpBehaviour
    {
        public GameObject targetGameObject;
        public bool enableInVR;
        public bool enableInDesktop;

        private void Start()
        {
            if (!targetGameObject) targetGameObject = gameObject;
            var userInVR = Networking.LocalPlayer.IsUserInVR();
            var shouldActive =
                (userInVR && enableInVR) ||
                (!userInVR && enableInDesktop);

            targetGameObject.SetActive(shouldActive);
        }
    }
}