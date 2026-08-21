using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.FlightMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class FlightMenuController : UdonSharpBehaviour
    {
        [Header("Core")] public FlightMenuView viewCore;

        [Header("Desktop Mode Mouse Axis")] 
        public string horizontalAxisName = "Mouse X";
        public string verticalAxisName = "Mouse Y";

        public float mouseHorizontalMultiplier = 20f;
        public float mouseVerticalMultiplier = 20f;

        public KeyCode desktopModeHoldToMoveMenuCursorKey = KeyCode.Tab;

        [Header("VR Mode Thumbstick Axis")]
        public string thumbstickHorizontalAxis = "Oculus_CrossPlatform_PrimaryThumbstickHorizontal";
        public string thumbstickVerticalAxis = "Oculus_CrossPlatform_PrimaryThumbstickVertical";

        [Header("Debug Only")]
        public KeyCode toggleForceVrKey = KeyCode.RightAlt;
        public bool forceInVr;

        [Header("UI (Internal)")]
        public float menuRadius = 60f;

        public float hoverThreshold = 550f;
        public float activeThreshold = 1900f;

        public RectTransform cursorTransform;

        [Header("Inspector Debug Only")]
        public int itemNumber = 8;
        // Init in RequestMenuUpdate()
        private Vector2 _startEdgeDir;

        private bool _userInVr;
        private float _sqrMagnitudeInLastFrame;
        private int _lastHoverItemIndex = -1;

        private void Start()
        {
            _userInVr = Networking.LocalPlayer.IsUserInVR();
            RequestMenuUpdate(itemNumber);
        }

        public void RequestMenuUpdate(int menuLength)
        {
            itemNumber = menuLength;
            var startAngleDeg = 90f + 180f / itemNumber;
            _startEdgeDir = Quaternion.Euler(0, 0, startAngleDeg) * Vector2.right;
        }

        private void Update()
        {
            var userInVr = _userInVr || forceInVr;
            if (Input.GetKeyDown(toggleForceVrKey))
            {
                forceInVr = !forceInVr;
                Networking.LocalPlayer.Immobilize(forceInVr);
            }

            if (_lastHoverItemIndex != -1)
            {
                viewCore._OnItemHoverLost(_lastHoverItemIndex);
            }

            if (!userInVr && !Input.GetKey(desktopModeHoldToMoveMenuCursorKey))
            {
                cursorTransform.localPosition = new Vector3(0, 0, cursorTransform.localPosition.z);
                return;
            }

            Vector2 cursorPosition;
            if (!userInVr)
            {
                var mouseXInput = Input.GetAxis(horizontalAxisName);
                var mouseYInput = Input.GetAxis(verticalAxisName);

                var oldCursorPosition = cursorTransform.localPosition;
                var newX = oldCursorPosition.x + (mouseXInput * mouseHorizontalMultiplier);
                var newY = oldCursorPosition.y + (mouseYInput * mouseVerticalMultiplier);

                cursorPosition = new Vector2(newX, newY);
                cursorTransform.localPosition = new Vector3(newX, newY, oldCursorPosition.z);
            }
            else
            {
                var thumbstickXInput = Input.GetAxis(thumbstickHorizontalAxis);
                var thumbstickYInput = Input.GetAxis(thumbstickVerticalAxis);

                var oldCursorPosition = cursorTransform.localPosition;
                var newX = thumbstickXInput * menuRadius;
                var newY = thumbstickYInput * menuRadius;

                cursorPosition = new Vector2(newX, newY);
                cursorTransform.localPosition = new Vector3(newX, newY, oldCursorPosition.z);
            }

            // Origin position are always (0, 0), so just pass position as vector to origin
            var cursorSqrMagnitude = cursorPosition.sqrMagnitude;
            
            // Handle "release thumbstick to trigger" in VR mode
            if (userInVr &&
                cursorSqrMagnitude < activeThreshold && _sqrMagnitudeInLastFrame > activeThreshold)
            {
                viewCore._OnItemTrigger(_lastHoverItemIndex);
            }

            if (cursorSqrMagnitude < hoverThreshold)
            {
                _sqrMagnitudeInLastFrame = cursorSqrMagnitude;
                _lastHoverItemIndex = -1;
                return;
            }

            var step = 360f / itemNumber;

            float angleDeg = Mathf.Repeat(-Vector2.SignedAngle(_startEdgeDir, cursorPosition), 360f);
            var itemIndex = Mathf.FloorToInt(angleDeg / step) % itemNumber;

            // Highlight the appropriate item
            if (itemIndex >= 0 && itemIndex < itemNumber)
            {
                viewCore._OnItemHover(itemIndex);
                _lastHoverItemIndex = itemIndex;

                if (Input.GetMouseButtonDown(0))
                {
                    // "release thumbstick to trigger" are handle in above
                    viewCore._OnItemTrigger(itemIndex);
                }
            }
            else
            {
                _lastHoverItemIndex = -1;
            }

            _sqrMagnitudeInLastFrame = cursorSqrMagnitude;
        }
    }
}