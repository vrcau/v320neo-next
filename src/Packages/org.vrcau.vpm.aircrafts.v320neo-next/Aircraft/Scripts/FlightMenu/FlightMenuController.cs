using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace VAU.V320NeoNext.Runtime.FlightMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class FlightMenuController : UdonSharpBehaviour
    {
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
        public GameObject hoverItem1;
        public GameObject hoverItem2;
        public GameObject hoverItem3;
        public GameObject hoverItem4;
        public GameObject hoverItem5;
        public GameObject hoverItem6;
        public GameObject hoverItem7;
        public GameObject hoverItem8;

        public GameObject activatedItem1;
        public GameObject activatedItem2;
        public GameObject activatedItem3;
        public GameObject activatedItem4;
        public GameObject activatedItem5;
        public GameObject activatedItem6;
        public GameObject activatedItem7;
        public GameObject activatedItem8;

        private bool _userInVr;
        private float _sqrMagnitudeInLastFrame;
        private int _lastHoverItemIndex = -1;

        private void Start()
        {
            _userInVr = Networking.LocalPlayer.IsUserInVR();

            hoverItem1.SetActive(false);
            hoverItem2.SetActive(false);
            hoverItem3.SetActive(false);
            hoverItem4.SetActive(false);
            hoverItem5.SetActive(false);
            hoverItem6.SetActive(false);
            hoverItem7.SetActive(false);
            hoverItem8.SetActive(false);

            activatedItem1.SetActive(false);
            activatedItem2.SetActive(false);
            activatedItem3.SetActive(false);
            activatedItem4.SetActive(false);
            activatedItem5.SetActive(false);
            activatedItem6.SetActive(false);
            activatedItem7.SetActive(false);
            activatedItem8.SetActive(false);
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
                GetHighlightItem(_lastHoverItemIndex).SetActive(false);
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
                var activatedItem = GetActivatedItem(_lastHoverItemIndex);
                activatedItem.SetActive(!activatedItem.activeSelf);
            }

            if (cursorSqrMagnitude < hoverThreshold)
            {
                _sqrMagnitudeInLastFrame = cursorSqrMagnitude;
                _lastHoverItemIndex = -1;
                return;
            }

            var n = 8;
            var step = 360f / n;

            float startAngleDeg = 90f + (180f / n);
            var startEdgeDir = Quaternion.Euler(0, 0, startAngleDeg) * Vector2.right;

            float angleDeg = Mathf.Repeat(-Vector2.SignedAngle(startEdgeDir, cursorPosition), 360f);
            var itemIndex = Mathf.FloorToInt(angleDeg / step) % n;

            // Highlight the appropriate item
            if (itemIndex >= 0 && itemIndex < 8)
            {
                var highlightItem = GetHighlightItem(itemIndex);
                highlightItem.SetActive(true);
                _lastHoverItemIndex = itemIndex;

                if (Input.GetMouseButtonDown(0))
                {
                    // "release thumbstick to trigger" are handle in above
                    var activatedItem = GetActivatedItem(_lastHoverItemIndex);
                    activatedItem.SetActive(!activatedItem.activeSelf);
                }
            }
            else
            {
                _lastHoverItemIndex = -1;
            }

            _sqrMagnitudeInLastFrame = cursorSqrMagnitude;
        }

        private GameObject GetHighlightItem(int index)
        {
            switch (index)
            {
                case 0: return hoverItem1;
                case 1: return hoverItem2;
                case 2: return hoverItem3;
                case 3: return hoverItem4;
                case 4: return hoverItem5;
                case 5: return hoverItem6;
                case 6: return hoverItem7;
                case 7: return hoverItem8;
                default: return null;
            }
        }

        private GameObject GetActivatedItem(int index)
        {
            switch (index)
            {
                case 0: return activatedItem1;
                case 1: return activatedItem2;
                case 2: return activatedItem3;
                case 3: return activatedItem4;
                case 4: return activatedItem5;
                case 5: return activatedItem6;
                case 6: return activatedItem7;
                case 7: return activatedItem8;
                default: return null;
            }
        }
    }
}