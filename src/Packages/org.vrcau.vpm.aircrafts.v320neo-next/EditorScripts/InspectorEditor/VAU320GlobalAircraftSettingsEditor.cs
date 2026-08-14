using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VAU.V320NeoNext.Runtime.AircraftSetup;

namespace VAU.V320NeoNext.Editor.InspectorEditor
{
    [CustomEditor(typeof(VAU320GlobalAircraftSettings))]
    public sealed class VAU320GlobalAircraftSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            var settings = (VAU320GlobalAircraftSettings)target;

            base.OnInspectorGUI();

            if (GUILayout.Button("Setup"))
            {
                settings.Setup();
            }
        }
    }
}