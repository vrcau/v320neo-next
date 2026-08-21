using UdonSharpEditor;
using UnityEditor;
using VAU.V320NeoNext.Runtime.FlightMenu.MenuData.Item;

namespace VAU.V320NeoNext.Editor.InspectorEditor.FlightMenu
{
    [CustomEditor(typeof(FlightMenuItemBase))]
    public sealed class FlightMenuItemEditor : UnityEditor.Editor
    {
        private FlightMenuItemGUI _previewGUI;
        private FlightMenuItemBase _targetMenuItem;

        private void OnEnable()
        {
            _targetMenuItem = (FlightMenuItemBase)target;
            _previewGUI = new FlightMenuItemGUI(_targetMenuItem);
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            _previewGUI?.OnGui();
        }
    }
}