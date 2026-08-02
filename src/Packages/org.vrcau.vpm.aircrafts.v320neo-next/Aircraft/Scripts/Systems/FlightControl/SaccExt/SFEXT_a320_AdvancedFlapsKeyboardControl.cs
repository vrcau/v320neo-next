using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.Systems.FlightControl.SaccExt {
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_a320_AdvancedFlapsKeyboardControl : UdonSharpBehaviour {
        public DFUNC_a320_FlapController advancedFlaps;

        public KeyCode flapsUpKey = KeyCode.Alpha1;
        public KeyCode flapsDownKey = KeyCode.Alpha2;

        private bool _isKeyPress;

        private void LateUpdate() {
            if (Input.GetKey(flapsUpKey)) {
                if (_isKeyPress) return;

                var targetFlapDetentIndex = advancedFlaps.targetDetentIndex - 1;
                if (targetFlapDetentIndex < advancedFlaps.flapDetents.Length && targetFlapDetentIndex >= 0)
                    advancedFlaps.RequestFlapsUp();

                _isKeyPress = true;
                return;
            }

            if (Input.GetKey(flapsDownKey)) {
                if (_isKeyPress) return;

                var targetFlapDetentIndex = advancedFlaps.targetDetentIndex + 1;
                if (targetFlapDetentIndex < advancedFlaps.flapDetents.Length && targetFlapDetentIndex >= 0)
                    advancedFlaps.RequestFlapsDown();

                _isKeyPress = true;
                return;
            }

            _isKeyPress = false;
        }
    }
}