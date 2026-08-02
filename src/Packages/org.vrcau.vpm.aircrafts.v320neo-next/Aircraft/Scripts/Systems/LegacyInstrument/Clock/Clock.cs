using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VAU.V320NeoNext.Runtime.Systems.LegacyInstrument.Utils;

namespace VAU.V320NeoNext.Runtime.Systems.LegacyInstrument.Clock {
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Clock : UdonSharpBehaviour {
        private readonly float UPDATE_INTERVAL = UpdateIntervalUtil.GetUpdateIntervalFromSeconds(1);

        [Header("Clock")]
        public Text hhmmText;

        public Text ssText;
        private float _lastUpdate;

        private void Update() {
            if (!UpdateIntervalUtil.CanUpdate(ref _lastUpdate, UPDATE_INTERVAL)) return;

            hhmmText.text = DateTime.UtcNow.ToShortTimeString();
            ssText.text = DateTime.UtcNow.Second.ToString("D2");
        }
    }
}