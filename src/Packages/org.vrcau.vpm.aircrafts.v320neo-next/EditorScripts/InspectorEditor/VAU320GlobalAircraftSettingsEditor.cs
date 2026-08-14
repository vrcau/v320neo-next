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
                SetupLayer();
            }
        }

        // https://github.com/esnya/EsnyaSFAddons/blob/d2984ab062190521d701c9b4651c84abc6e424b9/Packages/com.nekometer.esnya.esnya-sf-addons/Editor/ESFAMenu.cs
        [MenuItem("SaccFlight/VAU320/Setup EsnyaSFAddons Boarding Collider Layer")]
        private static void SetupLayer()
        {
            SetLayerName(29, "BoardingCollider");
            for (var i = 0; i < 32; i++)
            {
                Physics.IgnoreLayerCollision(29, i, ((1 << i) & 0b0101_1111_1111_1101_1010_1111_1101_1111) == 0);
            }
        }

        private static void SetLayerName(int layer, string name)
        {
            var tagManager =
                new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset"));
            tagManager.Update();

            var layersProperty = tagManager.FindProperty("layers");
            layersProperty.arraySize = Mathf.Max(layersProperty.arraySize, layer);
            layersProperty.GetArrayElementAtIndex(layer).stringValue = name;

            tagManager.ApplyModifiedProperties();
        }
    }
}