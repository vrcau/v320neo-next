using System.Collections.Generic;
using System.Text;
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
        private readonly SerializedProperty _isEnabledProperty;

        private readonly SerializedProperty _requestClosePopupProperty;

        private readonly SerializedProperty _isHideProperty;

        // Event Properties
        private readonly SerializedProperty _eventTargetProperty;
        private readonly SerializedProperty _triggerEventNameProperty;

        private readonly SerializedProperty _updateIsActivatedFromEventTargetProperty;
        private readonly SerializedProperty _isActivatedVariableNameProperty;
        private readonly SerializedProperty _invertIsActivatedVariableProperty;

        private readonly SerializedProperty _titleVariableNameProperty;
        private readonly SerializedProperty _titleTemplateProperty;
        private readonly SerializedProperty _updateTitleFromEventTargetProperty;

        private readonly SerializedProperty _updateIsDisabledFromEventTargetProperty;
        private readonly SerializedProperty _isDisabledVariableNameProperty;
        private readonly SerializedProperty _invertIsDisabledVariableProperty;

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
            _isEnabledProperty = _itemSerializedObject.FindProperty(nameof(FlightMenuItemBase.isDisabled));
            _updateIsActivatedFromEventTargetProperty = _itemSerializedObject
                .FindProperty(nameof(FlightMenuItemBase.updateIsActivatedFromEventTarget));

            _requestClosePopupProperty =
                _itemSerializedObject.FindProperty(nameof(FlightMenuItemBase.requestClosePopupWhenTrigger));

            _isHideProperty = _itemSerializedObject.FindProperty(nameof(FlightMenuItemBase.isHide));

            _eventTargetProperty = _itemSerializedObject.FindProperty(nameof(FlightMenuItemBase.eventTarget));
            _triggerEventNameProperty = _itemSerializedObject.FindProperty(nameof(FlightMenuItemBase.triggerEventName));
            _isActivatedVariableNameProperty = _itemSerializedObject
                .FindProperty(nameof(FlightMenuItemBase.isActivatedVariableName));
            _invertIsActivatedVariableProperty = _itemSerializedObject
                .FindProperty(nameof(FlightMenuItemBase.invertIsActivatedVariable));

            _titleVariableNameProperty = _itemSerializedObject
                .FindProperty(nameof(FlightMenuItemBase.titleVariableName));
            _titleTemplateProperty = _itemSerializedObject.FindProperty(nameof(FlightMenuItemBase.titleTemplate));
            _updateTitleFromEventTargetProperty = _itemSerializedObject
                .FindProperty(nameof(FlightMenuItemBase.updateTitleFromEventTarget));

            _updateIsDisabledFromEventTargetProperty = _itemSerializedObject
                .FindProperty(nameof(FlightMenuItemBase.updateIsEnabledFromEventTarget));
            _isDisabledVariableNameProperty = _itemSerializedObject
                .FindProperty(nameof(FlightMenuItemBase.isDisabledVariableName));
            _invertIsDisabledVariableProperty = _itemSerializedObject
                .FindProperty(nameof(FlightMenuItemBase.invertIsDisabledVariable));

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
                    _iconProperty.objectReferenceValue, typeof(Sprite), false);

            bool isEventTargetExist = _itemBase.eventTarget;

            GUILayout.BeginVertical();
            if (isEventTargetExist && (
                    _itemBase.updateIsActivatedFromEventTarget ||
                    _itemBase.updateIsEnabledFromEventTarget ||
                    _itemBase.updateTitleFromEventTarget))
            {
                var managedProperties = "";
                if (_itemBase.updateIsActivatedFromEventTarget)
                    managedProperties += nameof(FlightMenuItemBase.isActivated) + " ";
                if (_itemBase.updateIsEnabledFromEventTarget)
                    managedProperties += nameof(FlightMenuItemBase.isDisabled) + " ";
                if (_itemBase.updateTitleFromEventTarget)
                    managedProperties += nameof(FlightMenuItemBase.title) + " ";

                EditorGUILayout.HelpBox(
                    managedProperties + "are update from event target, see below",
                    MessageType.Info);
            }

            if (!isEventTargetExist || !_itemBase.updateTitleFromEventTarget)
            {
                _titleProperty.stringValue = EditorGUILayout.TextArea(_titleProperty.stringValue);
            }

            if (!isEventTargetExist || !_itemBase.updateIsActivatedFromEventTarget)
            {
                EditorGUILayout.PropertyField(_isActivatedProperty);
            }

            if (!isEventTargetExist || !_itemBase.updateIsEnabledFromEventTarget)
            {
                EditorGUILayout.PropertyField(_isEnabledProperty);
            }

            EditorGUILayout.PropertyField(_isHideProperty);
            EditorGUILayout.PropertyField(_requestClosePopupProperty);

            if (_itemBase is FlightMenuSubMenuItem)
            {
                EditorGUILayout.PropertyField(_isPopupMenuProperty);
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // Event Target Start
            EditorGUILayout.PropertyField(_eventTargetProperty);
            if (isEventTargetExist)
            {
                EditorGUILayout.PropertyField(_triggerEventNameProperty);
                EditorGUILayout.PropertyField(_updateIsActivatedFromEventTargetProperty);
                if (_itemBase.updateIsActivatedFromEventTarget)
                {
                    EditorGUILayout.PropertyField(_isActivatedVariableNameProperty);
                    EditorGUILayout.PropertyField(_invertIsActivatedVariableProperty);
                }

                EditorGUILayout.PropertyField(_updateTitleFromEventTargetProperty);
                if (_itemBase.updateTitleFromEventTarget)
                {
                    EditorGUILayout.PropertyField(_titleVariableNameProperty);
                    EditorGUILayout.LabelField("Title Template (use {0} for the variable value)");
                    _titleTemplateProperty.stringValue = EditorGUILayout.TextArea(_titleTemplateProperty.stringValue);
                }

                EditorGUILayout.PropertyField(_updateIsDisabledFromEventTargetProperty);
                if (_itemBase.updateIsEnabledFromEventTarget)
                {
                    EditorGUILayout.PropertyField(_isDisabledVariableNameProperty);
                    EditorGUILayout.PropertyField(_invertIsDisabledVariableProperty);
                }
            }
            else
            {
                _updateIsActivatedFromEventTargetProperty.boolValue = false;
                _updateTitleFromEventTargetProperty.boolValue = false;
                _itemSerializedObject.ApplyModifiedProperties();
            }

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