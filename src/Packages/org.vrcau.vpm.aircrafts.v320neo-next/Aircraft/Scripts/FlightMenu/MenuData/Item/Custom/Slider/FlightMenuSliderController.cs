using System;
using JetBrains.Annotations;
using TMPro;
using UdonSharp;
using UnityEngine;

namespace VAU.V320NeoNext.Runtime.FlightMenu.MenuData.Item.Custom.Slider
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FlightMenuSliderController : UdonSharpBehaviour
    {
        public FlightMenuGroup sliderMenuGroup;

        public TextMeshProUGUI sliderTitleText;
        public TextMeshProUGUI sliderDescriptionText;

        public GameObject sliderUiObject;
        public RectTransform sliderDetentContainer;
        public GameObject sliderDetentTemplate;
        public GameObject sliderFlexSpaceDetentTemplate;

        [Header("Must be the child of slider detent container")]
        public RectTransform sliderPointer;

        // public float sliderPointerDetentHeight = 2.5f;

        // For flight menu item display
        [NonSerialized] [PublicAPI] public bool allowIncreaseIndex;
        [NonSerialized] [PublicAPI] public bool allowDecreaseIndex;

        private GameObject[] _itemsGenerated = new GameObject[0];

        private FlightMenuSliderItem _currentSliderItem;
        private UdonSharpBehaviour _sliderEventTarget;
        private int _sliderLength;
        private string _sliderIndexVariableName;
        private string _sliderDescriptionTemplate;
        private string _sliderDescriptionVariableName;
        private bool _updateSliderDescriptionFromEventTarget;

        public FlightMenuGroup GetSliderMenuAndShowSlider(FlightMenuSliderItem sliderItem)
        {
            _currentSliderItem = sliderItem;
            _sliderEventTarget = sliderItem.eventTarget;
            _sliderLength = sliderItem.sliderLength;
            _sliderIndexVariableName = sliderItem.sliderIndexVariableName;
            _sliderDescriptionTemplate = sliderItem.sliderDescriptionTemplate;
            _sliderDescriptionVariableName = sliderItem.sliderDescriptionVariableName;
            _updateSliderDescriptionFromEventTarget = sliderItem.updateSliderDescriptionFromEventTarget;

            sliderTitleText.text = sliderItem.title;
            if (_updateSliderDescriptionFromEventTarget)
            {
                var descriptionVariableValue = _sliderEventTarget.GetProgramVariable(_sliderDescriptionVariableName);
                sliderDescriptionText.text = string.Format(_sliderDescriptionTemplate, descriptionVariableValue);
            }
            else
            {
                sliderDescriptionText.text = sliderItem.sliderDescription;
            }

            sliderUiObject.SetActive(true);
            GenerateSliderItems(sliderItem);
            return sliderMenuGroup;
        }

        private void LateUpdate()
        {
            var sliderIndexInInt = (int)_sliderEventTarget.GetProgramVariable(_sliderIndexVariableName);
            allowDecreaseIndex = sliderIndexInInt > 0;
            allowIncreaseIndex = sliderIndexInInt < _sliderLength - 1;

            var containerHeight = sliderDetentContainer.rect.height;

            // sliderPointer anchor are at center of detent container
            var sliderPercentage = sliderIndexInInt == 0 ? 0 : sliderIndexInInt / (float)(_sliderLength - 1);
            sliderPointer.anchoredPosition = new Vector2(
                sliderPointer.anchoredPosition.x,
                -(sliderPercentage * containerHeight) + (containerHeight / 2)
            );

            if (_updateSliderDescriptionFromEventTarget)
            {
                var descriptionVariableValue = _sliderEventTarget.GetProgramVariable(_sliderDescriptionVariableName);
                sliderDescriptionText.text = string.Format(_sliderDescriptionTemplate, descriptionVariableValue);
            }
        }

        private void GenerateSliderItems(FlightMenuSliderItem sliderItem)
        {
            var sliderLength = sliderItem.sliderLength;
            _itemsGenerated = new GameObject[sliderLength - 1];
            for (var i = 0; i < _itemsGenerated.Length - 1; i++)
            {
                _itemsGenerated[i] = Instantiate(sliderDetentTemplate, sliderDetentContainer);
            }

            _itemsGenerated[_itemsGenerated.Length - 1] =
                Instantiate(sliderFlexSpaceDetentTemplate, sliderDetentContainer);
        }

        public void CloseSlider()
        {
            foreach (var itemToDestroy in _itemsGenerated)
            {
                Destroy(itemToDestroy);
            }

            _itemsGenerated = new GameObject[0];

            sliderUiObject.SetActive(false);
        }

        #region Handle Menu Item Trigger Events

        [PublicAPI]
        public void RequestIncreaseIndex()
        {
            var currentIndex = (int)_sliderEventTarget.GetProgramVariable(_sliderIndexVariableName);
            if (currentIndex < _sliderLength - 1)
            {
                _sliderEventTarget.SetProgramVariable(_sliderIndexVariableName, currentIndex + 1);
                _sliderEventTarget.SendCustomEvent(_currentSliderItem.onSliderIndexChangedEventName);
            }
        }

        [PublicAPI]
        public void RequestDecreaseIndex()
        {
            var currentIndex = (int)_sliderEventTarget.GetProgramVariable(_sliderIndexVariableName);
            if (currentIndex > 0)
            {
                _sliderEventTarget.SetProgramVariable(_sliderIndexVariableName, currentIndex - 1);
                _sliderEventTarget.SendCustomEvent(_currentSliderItem.onSliderIndexChangedEventName);
            }
        }

        #endregion
    }
}