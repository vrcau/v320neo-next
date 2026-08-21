using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VAU.V320NeoNext.Runtime.FlightMenu.MenuData;
using VAU.V320NeoNext.Runtime.FlightMenu.MenuData.Item;

namespace VAU.V320NeoNext.Editor.InspectorEditor.FlightMenu
{
    [CustomEditor(typeof(FlightMenuGroup))]
    public sealed class FlightMenuGroupEditor : UnityEditor.Editor
    {
        private FlightMenuPreviewGUI _previewGUI;
        private FlightMenuGroup _targetMenuGroup;

        private void OnEnable()
        {
            _targetMenuGroup = (FlightMenuGroup)target;
            _previewGUI = new FlightMenuPreviewGUI(_targetMenuGroup);
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            EditorGUILayout.HelpBox(
                "Child FlightMenuGroup only got update when there is a SubMenuItem in same object",
                MessageType.Info
            );

            if (GUILayout.Button("Scan child menu item update"))
            {
                _targetMenuGroup.menuItems = ScanChildMenuItem(_targetMenuGroup);
                EditorUtility.SetDirty(_targetMenuGroup);
            }

            _previewGUI?.OnGui();
        }

        internal static FlightMenuItemBase[] ScanChildMenuItem(FlightMenuGroup targetMenuGroup)
        {
            var newItemList = new List<FlightMenuItemBase>();
            foreach (Transform child in targetMenuGroup.transform)
            {
                var item = child.GetComponent<FlightMenuItemBase>();
                if (!item) continue;

                if (item is FlightMenuSubMenuItem subMenuItem)
                {
                    var menuGroupInSameObject = subMenuItem.GetComponent<FlightMenuGroup>();
                    if (menuGroupInSameObject)
                    {
                        menuGroupInSameObject.menuItems = ScanChildMenuItem(menuGroupInSameObject);

                        if (!subMenuItem.subMenu) subMenuItem.subMenu = menuGroupInSameObject;
                    }
                }

                newItemList.Add(item);
            }

            return newItemList.ToArray();
        }
    }
}