using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.Systems.LegacyFws {
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class WarningMessageLine : UdonSharpBehaviour {
        public WarningColor MessageColor;
        public string MessageText;
        [HideInInspector] public bool isMessageVisible;
    }
}