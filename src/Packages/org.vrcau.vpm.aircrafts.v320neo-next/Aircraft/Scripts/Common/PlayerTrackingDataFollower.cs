using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.Common
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerTrackingDataFollower : UdonSharpBehaviour
    {
        public Transform moveTarget;
        
        public VRCPlayerApi.TrackingDataType trackingTarget;
        public Vector3 positionOffset;
        public bool trackRotation;

        private VRCPlayerApi _localPlayer;

        private void Start()
        {
            if (!moveTarget) moveTarget = transform;
            _localPlayer = Networking.LocalPlayer;
        }

        public override void PostLateUpdate()
        {
            var trackingData = _localPlayer.GetTrackingData(trackingTarget);
            if (trackRotation)
            {
                moveTarget.SetPositionAndRotation(trackingData.position + positionOffset, trackingData.rotation);
            }
            else
            {
                moveTarget.position = trackingData.position + positionOffset;
            }
        }
    }
}
