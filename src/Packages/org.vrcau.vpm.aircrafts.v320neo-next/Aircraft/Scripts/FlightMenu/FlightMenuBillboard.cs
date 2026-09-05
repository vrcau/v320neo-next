using UdonSharp;
using UnityEngine;
using VRC.SDK3.Rendering;

namespace VAU.V320NeoNext.Runtime.FlightMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(5000)]
    public class FlightMenuBillboard : UdonSharpBehaviour
    {
        private VRCCameraSettings _screenCamera;
        
        private void Start()
        {
            _screenCamera = VRCCameraSettings.ScreenCamera;
        }

        public override void PostLateUpdate()
        {
            transform.LookAt(_screenCamera.Position + _screenCamera.Forward, _screenCamera.Up);
        }
    }
}
