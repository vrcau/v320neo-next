using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VAU.V320NeoNext.Runtime.FlightMenu.MenuData;
using VAU.V320NeoNext.Runtime.FlightMenu.MenuData.Item;

namespace VAU.V320NeoNext.Editor.InspectorEditor.FlightMenu
{
    public class FlightMenuItemGUI
    {
        private readonly SerializedObject _itemSerializedObject;
        private readonly FlightMenuItemBase _itemBase;

        // Base Properties
        private readonly SerializedProperty _titleProperty;
        private readonly SerializedProperty _iconProperty;
        private readonly SerializedProperty _isActivatedProperty;

        // Sub/PopMenu Properties
        private readonly SerializedProperty _isPopupMenuProperty;
        private readonly SerializedProperty _subMenuProperty;

        private bool _expandSubMenuPreview;
        private FlightMenuPreviewGUI _subMenuPreviewGui;

        public FlightMenuItemGUI(FlightMenuItemBase itemBase)
        {
            _itemBase = itemBase;
            _itemSerializedObject = new SerializedObject(itemBase);

            _titleProperty = _itemSerializedObject.FindProperty(nameof(FlightMenuItemBase.title));
            _iconProperty = _itemSerializedObject.FindProperty(nameof(FlightMenuItemBase.icon));
            _isActivatedProperty = _itemSerializedObject.FindProperty(nameof(FlightMenuItemBase.isActivated));

            _isPopupMenuProperty = _itemSerializedObject.FindProperty(nameof(FlightMenuSubMenuItem.isPopupMenu));
            _subMenuProperty = _itemSerializedObject.FindProperty(nameof(FlightMenuSubMenuItem.subMenu));
        }

        public void OnGui()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(_itemBase, typeof(FlightMenuItemBase), false);
            EditorGUI.EndDisabledGroup();

            // [Icon] [ title ] [X]
            // [    ] [ isAct ] [ ]

            #region Icon, Title and other basic fields

            EditorGUI.BeginChangeCheck();

            GUILayout.BeginHorizontal();
            _iconProperty.objectReferenceValue =
                EditorGUI.ObjectField(
                    EditorGUILayout.GetControlRect(GUILayout.Width(64), GUILayout.Height(64)),
                    _iconProperty.objectReferenceValue, typeof(Texture), false);

            GUILayout.BeginVertical();
            EditorGUILayout.PropertyField(_titleProperty, new GUIContent());
            EditorGUILayout.PropertyField(_isActivatedProperty);

            if (_itemBase is FlightMenuSubMenuItem)
            {
                EditorGUILayout.PropertyField(_isPopupMenuProperty);
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                _itemSerializedObject.ApplyModifiedProperties();

                _itemBase.gameObject.name = !string.IsNullOrWhiteSpace(_itemBase.title) ? _itemBase.title : "[Empty]";
                EditorUtility.SetDirty(_itemBase.gameObject);
            }

            #endregion

            // SubMenu menu preview
            DrawSubMenuPreviewIfNecessary();

            GUILayout.EndVertical();
        }

        private void DrawSubMenuPreviewIfNecessary()
        {
            if (_itemBase is not FlightMenuSubMenuItem subMenuItem) return;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_subMenuProperty);
            if (EditorGUI.EndChangeCheck())
            {
                _itemSerializedObject.ApplyModifiedProperties();
            }

            if (!subMenuItem.subMenu)
            {
                if (!GUILayout.Button("Create new menu group"))
                    return;

                // Handle Create new menu group click
                var menuGroup = subMenuItem.gameObject.AddUdonSharpComponent<FlightMenuGroup>();
                _subMenuProperty.objectReferenceValue = menuGroup;
                _itemSerializedObject.ApplyModifiedProperties();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(12);
            GUILayout.BeginVertical();
            _expandSubMenuPreview = EditorGUILayout.Foldout(_expandSubMenuPreview, "Preview Menu");
            if (_expandSubMenuPreview)
            {
                _subMenuPreviewGui ??= new FlightMenuPreviewGUI(subMenuItem.subMenu);
                _subMenuPreviewGui.OnGui();
            }
            else
            {
                _subMenuPreviewGui = null;
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
}