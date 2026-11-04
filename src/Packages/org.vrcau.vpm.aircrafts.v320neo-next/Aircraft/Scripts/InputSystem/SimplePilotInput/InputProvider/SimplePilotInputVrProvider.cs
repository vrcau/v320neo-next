using UdonSharp;
using UnityEngine;
using VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.Abstract;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.InputSystem.SimplePilotInput.InputProvider
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(AircraftScriptsExecutionOrder.SimplePilotInputProvider)]
    public sealed class SimplePilotInputVrProvider : SimplePilotInputProvider
    {
        public float triggerThreshold = 0.75f;
        public bool useRightJoystick;

        public Vector3 maxJoystickAngle = new Vector3(30, 45, 60);

        [Header("Haptic when press the trigger")]
        public float pressedHapticDuration = 0.05f;

        public float pressedHapticAmplitude = 0.222f;
        public float pressedHapticFrequency = 35;

        [Header("Haptic when trigger released")]
        public float releasedHapticDuration = 0.05f;

        public float releasedHapticAmplitude = 0.222f;
        public float releasedHapticFrequency = 35;

        private const string LeftTriggerAxisName = "Oculus_CrossPlatform_PrimaryHandTrigger";
        private const string RightTriggerAxisName = "Oculus_CrossPlatform_SecondaryHandTrigger";

        private bool _isTriggerPressedLastFrame;
        private Quaternion _initialControllerRotation;
        private Quaternion _lastThisRotation;

        private VRCPlayerApi _localPlayer;
        private bool _localPlayerInVr;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            _localPlayerInVr = _localPlayer.IsUserInVR();

            if (!_localPlayerInVr) enabled = false;
        }

        private void LateUpdate()
        {
            if (!_localPlayerInVr)
            {
                enabled = false;
                return;
            }

            _SetInputVector(_GetInputVector());
        }

        // From https://github.com/Sacchan-VRC/SaccFlightAndVehicles/blob/8578813e3627b39f2f564684e865be940c6947d3/Scripts/SaccAirVehicle/SaccAirVehicle.cs
        // MIT License / Copyright (c) 2021 Sacchan-VRC
        private Vector3 _GetInputVector()
        {
            var activatedTriggerAxisName = useRightJoystick ? RightTriggerAxisName : LeftTriggerAxisName;
            var activatedPickupHand = useRightJoystick ? VRC_Pickup.PickupHand.Right : VRC_Pickup.PickupHand.Left;
            var activatedTrackingDataType = useRightJoystick
                ? VRCPlayerApi.TrackingDataType.RightHand
                : VRCPlayerApi.TrackingDataType.LeftHand;
            var thisRotation = transform.rotation;

            var isTriggerPressed = Input.GetAxisRaw(activatedTriggerAxisName) > triggerThreshold;
            var controllerRotation = _localPlayer.GetTrackingData(activatedTrackingDataType).rotation;

            if (isTriggerPressed)
            {
                //difference in vehicle's rotation since last frame
                var diffToLastThieRotation = thisRotation * Quaternion.Inverse(_lastThisRotation);
                _lastThisRotation = thisRotation;
                //zero point rotates with the vehicle so it appears still to the pilot
                _initialControllerRotation = diffToLastThieRotation * _initialControllerRotation;

                if (!_isTriggerPressedLastFrame)
                {
                    _initialControllerRotation = controllerRotation;
                    _localPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left,
                        pressedHapticDuration, pressedHapticAmplitude, pressedHapticFrequency);

                    _isTriggerPressedLastFrame = true;
                    return Vector3.zero;
                }

                // var diffToInitialRotation = controllerRotation * Quaternion.Inverse(_initialControllerRotation);
                var diffToInitialRotation =
                    Quaternion.Inverse(thisRotation) * controllerRotation *
                    Quaternion.Inverse(_initialControllerRotation) * thisRotation;

                //create normalized vectors facing towards the 'forward' and 'up' directions of the joystick
                var yawInput = diffToInitialRotation * Vector3.forward;
                var joystickInput = diffToInitialRotation * Vector3.up;
                //use acos to convert the relevant elements of the array into radians, re-center around zero, then normalize between -1 and 1 and dovide for desired deflection
                //the clamp is there because rotating a vector3 can cause it to go a miniscule amount beyond length 1, resulting in NaN (crashes vrc)

                return new Vector3(
                    -((Mathf.Acos(Mathf.Clamp(joystickInput.z, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) /
                    maxJoystickAngle.x,
                    -((Mathf.Acos(Mathf.Clamp(yawInput.x, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg) /
                    maxJoystickAngle.y,
                    (Mathf.Acos(Mathf.Clamp(joystickInput.x, -1, 1)) - 1.5707963268f) * Mathf.Rad2Deg /
                    maxJoystickAngle.z
                );
            }
            else
            {
                if (_isTriggerPressedLastFrame)
                {
                    _localPlayer.PlayHapticEventInHand(activatedPickupHand, releasedHapticDuration,
                        releasedHapticAmplitude, releasedHapticFrequency);
                }

                _isTriggerPressedLastFrame = false;
                return Vector3.zero;
            }
        }
    }
}