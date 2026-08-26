using System;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VAU.V320NeoNext.Runtime.FlightMenu.MenuData;
using VAU.V320NeoNext.Runtime.FlightMenu.MenuData.Item;

namespace VAU.V320NeoNext.Editor.InspectorEditor.FlightMenu
{
    public class FlightMenuPreviewGUI
    {
        private readonly FlightMenuGroup _flightMenuGroup;
        private readonly SerializedObject _serializedObject;

        private readonly SerializedProperty _menuItemsProperty;
        private readonly SerializedProperty _menuGroupNameProperty;
        private readonly SerializedProperty _menuGroupDescriptionProperty;

        private readonly Dictionary<FlightMenuItemBase, FlightMenuItemGUI> _itemGuis = new();

        public FlightMenuPreviewGUI(FlightMenuGroup flightMenuGroup)
        {
            _flightMenuGroup = flightMenuGroup;
            _serializedObject = new SerializedObject(flightMenuGroup);

            _menuItemsProperty = _serializedObject
                .FindProperty(nameof(FlightMenuGroup.menuItems));
            _menuGroupNameProperty = _serializedObject.FindProperty(nameof(FlightMenuGroup.groupName));
            _menuGroupDescriptionProperty = _serializedObject.FindProperty(nameof(FlightMenuGroup.description));
        }

        public void OnGui()
        {
            if (_flightMenuGroup.menuItems == null)
            {
                _flightMenuGroup.menuItems = Array.Empty<FlightMenuItemBase>();
                EditorUtility.SetDirty(_flightMenuGroup);
            }

            GUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_menuGroupNameProperty);
            EditorGUILayout.PropertyField(_menuGroupDescriptionProperty);
            if (EditorGUI.EndChangeCheck())
            {
                _serializedObject.ApplyModifiedProperties();
            }

            for (var index = 0; index < _flightMenuGroup.menuItems.Length; index++)
            {
                var menuItem = _flightMenuGroup.menuItems[index];
                if (!menuItem)
                {
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.HelpBox(
                        "Menu item reference missing, please re-assign or re-scan child menu items",
                        MessageType.Error
                    );

                    EditorGUI.BeginChangeCheck();

                    var objectProperty = _menuItemsProperty.GetArrayElementAtIndex(index);
                    EditorGUILayout.PropertyField(objectProperty);

                    GUILayout.EndVertical();
                    if (EditorGUI.EndChangeCheck())
                    {
                        _serializedObject.ApplyModifiedProperties();
                    }

                    continue;
                }

                if (!_itemGuis.TryGetValue(menuItem, out var itemGui))
                {
                    itemGui = new FlightMenuItemGUI(menuItem);
                    _itemGuis[menuItem] = itemGui;
                }

                itemGui.OnGui();
            }

            if (EditorGUILayout.DropdownButton(new GUIContent("Create new menu item"), FocusType.Keyboard))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Button"), false,
                    () =>
                    {
                        var newGameObject = new GameObject("[Empty]");
                        newGameObject.transform.SetParent(_flightMenuGroup.transform);

                        newGameObject.AddUdonSharpComponent<FlightMenuButtonItem>();

                        _flightMenuGroup.menuItems = FlightMenuGroupEditor.ScanChildMenuItem(_flightMenuGroup);
                        EditorUtility.SetDirty(_flightMenuGroup);
                    });

                menu.AddItem(new GUIContent("SubMenu"), false, () =>
                {
                    var newGameObject = new GameObject("[Empty]");
                    newGameObject.transform.SetParent(_flightMenuGroup.transform);

                    var subMenuItem = newGameObject.AddUdonSharpComponent<FlightMenuSubMenuItem>();
                    subMenuItem.isPopupMenu = false;

                    _flightMenuGroup.menuItems = FlightMenuGroupEditor.ScanChildMenuItem(_flightMenuGroup);
                    EditorUtility.SetDirty(_flightMenuGroup);
                });

                menu.AddItem(new GUIContent("Popup Menu"), false, () =>
                {
                    var newGameObject = new GameObject("[Empty]");
                    newGameObject.transform.SetParent(_flightMenuGroup.transform);

                    var popupMenuItem = newGameObject.AddUdonSharpComponent<FlightMenuSubMenuItem>();
                    popupMenuItem.isPopupMenu = true;

                    _flightMenuGroup.menuItems = FlightMenuGroupEditor.ScanChildMenuItem(_flightMenuGroup);
                    EditorUtility.SetDirty(_flightMenuGroup);
                });

                menu.ShowAsContext();
            }

            GUILayout.EndVertical();
            GUILayout.Space(8);
        }
    }
}