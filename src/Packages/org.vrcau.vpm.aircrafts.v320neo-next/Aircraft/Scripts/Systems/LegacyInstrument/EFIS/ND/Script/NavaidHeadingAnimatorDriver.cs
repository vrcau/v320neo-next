using UdonSharp;
using UnityEngine;
using VirtualCNS;

namespace VAU.V320NeoNext.Runtime.Systems.LegacyInstrument.EFIS.ND.Script
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class NavaidHeadingAnimatorDriver : UdonSharpBehaviour
    {
        public NavSelector navSelector;
        public Animator animator;

        public string navaidHeadingFloatParameter;

        private void LateUpdate()
        {
            var navaidTransform = navSelector.NavaidTransform;
            if (!navaidTransform) return;

            var toTarget = navaidTransform.position - transform.position;
            var horizontalDir = new Vector3(toTarget.x, 0, toTarget.z);

            if (horizontalDir.sqrMagnitude < 0.0001f)
            {
                animator.SetFloat(navaidHeadingFloatParameter, 0);
            }

            var horizontalForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
            var signedAngle = Vector3.SignedAngle(horizontalForward, horizontalDir, Vector3.up);

            var headingFloat = (signedAngle + 360f) % 360f / 360f;
            animator.SetFloat(navaidHeadingFloatParameter, headingFloat);
        }
    }
}