using UnityEditor;
using UnityEngine;
using VAU.V320NeoNext.Runtime.FlightMenu.MenuData.Item.Custom.Slider;

namespace VAU.V320NeoNext.Editor.InspectorEditor.FlightMenu.CustomItems
{
    public class FlightMenuSliderItemExtendGUI
    {
        private readonly FlightMenuSliderItem _sliderItem;
        private readonly SerializedObject _itemSerializedObject;

        private readonly SerializedProperty _sliderLengthProperty;

        private readonly SerializedProperty _sliderDescriptionProperty;

        private readonly SerializedProperty _updateSliderDescriptionFromEventTargetProperty;
        private readonly SerializedProperty _sliderDescriptionTemplateProperty;
        private readonly SerializedProperty _sliderDescriptionVariableNameProperty;

        private readonly SerializedProperty _sliderIndexVariableNameProperty;
        private readonly SerializedProperty _onSliderIndexChangedEventNameProperty;

        public FlightMenuSliderItemExtendGUI(FlightMenuSliderItem sliderItem)
        {
            _sliderItem = sliderItem;

            _itemSerializedObject = new SerializedObject(_sliderItem);

            _sliderLengthProperty = _itemSerializedObject.FindProperty(nameof(FlightMenuSliderItem.sliderLength));

            _sliderDescriptionProperty =
                _itemSerializedObject.FindProperty(nameof(FlightMenuSliderItem.sliderDescription));

            _updateSliderDescriptionFromEventTargetProperty =
                _itemSerializedObject.FindProperty(nameof(FlightMenuSliderItem.updateSliderDescriptionFromEventTarget));
            _sliderDescriptionTemplateProperty =
                _itemSerializedObject.FindProperty(nameof(FlightMenuSliderItem.sliderDescriptionTemplate));
            _sliderDescriptionVariableNameProperty =
                _itemSerializedObject.FindProperty(nameof(FlightMenuSliderItem.sliderDescriptionVariableName));

            _sliderIndexVariableNameProperty =
                _itemSerializedObject.FindProperty(nameof(FlightMenuSliderItem.sliderIndexVariableName));
            _onSliderIndexChangedEventNameProperty =
                _itemSerializedObject.FindProperty(nameof(FlightMenuSliderItem.onSliderIndexChangedEventName));
        }

        public void OnGui()
        {
            _itemSerializedObject.Update();

            GUILayout.BeginVertical(EditorStyles.helpBox);

            if (!_sliderItem.eventTarget)
            {
                EditorGUILayout.HelpBox(
                    "You must assign a event target in order to use slider item",
                    MessageType.Error
                );
            }

            EditorGUILayout.PropertyField(_sliderLengthProperty);

            EditorGUILayout.PropertyField(_sliderIndexVariableNameProperty);
            EditorGUILayout.PropertyField(_onSliderIndexChangedEventNameProperty);

            EditorGUILayout.PropertyField(_updateSliderDescriptionFromEventTargetProperty);
            if (_updateSliderDescriptionFromEventTargetProperty.boolValue)
            {
                EditorGUILayout.PropertyField(_sliderDescriptionTemplateProperty);
                EditorGUILayout.PropertyField(_sliderDescriptionVariableNameProperty);
            }
            else
            {
                EditorGUILayout.PropertyField(_sliderDescriptionProperty);
            }

            GUILayout.EndVertical();

            _itemSerializedObject.ApplyModifiedProperties();
        }
    }
}