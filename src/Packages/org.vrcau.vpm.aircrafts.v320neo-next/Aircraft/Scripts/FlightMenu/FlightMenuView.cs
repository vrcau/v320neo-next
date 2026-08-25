using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VAU.V320NeoNext.Runtime.FlightMenu.MenuData;
using VAU.V320NeoNext.Runtime.FlightMenu.MenuData.Item;

namespace VAU.V320NeoNext.Runtime.FlightMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class FlightMenuView : UdonSharpBehaviour
    {
        public FlightMenuGroup rootMenuGroup;

        [Header("Core")] 
        public FlightMenuController menuController;
        public FlightMenuBackItem backButtonItem;

        [Header("Child Root")] 
        public Transform backgroundRoot;
        public Transform hoverRoot;
        public Transform activatedRoot;
        public Transform titleRoot;

        [Header("Item Image Template")] 
        public GameObject backgroundClipTemplate;
        public GameObject hoverClipTemplate;
        public GameObject activatedClipTemplate;
        public GameObject titleTemplate;
        public float marginAngle = 2f;

        private int _itemNumber = 8;

        [Header("Debug Only")] 
        public FlightMenuGroup[] menuGroupHistory = new FlightMenuGroup[0];
        public FlightMenuGroup menuGroupActivated;

        private GameObject[] _itemGenerated = new GameObject[0];
        private FlightMenuItemBase[] _flightMenuActivated = new FlightMenuItemBase[0];

        private void Start()
        {
            ClearHistory();
            NavigateToMenu(rootMenuGroup);
        }

        private void NavigateToMenu(FlightMenuGroup newMenuGroup)
        {
            var newMenuGroupItems = newMenuGroup.menuItems;
            if (menuGroupActivated)
            {
                PushHistory(menuGroupActivated);
                _flightMenuActivated = new FlightMenuItemBase[newMenuGroupItems.Length + 1];
                _flightMenuActivated[0] = backButtonItem;
                newMenuGroupItems.CopyTo(_flightMenuActivated, 1);
            }
            else
            {
                _flightMenuActivated = new FlightMenuItemBase[newMenuGroupItems.Length];
                newMenuGroupItems.CopyTo(_flightMenuActivated, 0);
            }

            menuGroupActivated = newMenuGroup;
            _itemNumber = _flightMenuActivated.Length;
            GenerateMenuView();
            menuController.RequestMenuUpdate(_itemNumber);
        }

        private void GoBack()
        {
            if (menuGroupHistory.Length == 0) return;
            var menuToGoBack = PopHistory();
            var menuToGoBackItems = menuToGoBack.menuItems;

            if (menuGroupHistory.Length != 0)
            {
                _flightMenuActivated = new FlightMenuItemBase[menuToGoBackItems.Length + 1];
                _flightMenuActivated[0] = backButtonItem;

                menuToGoBackItems.CopyTo(_flightMenuActivated, 1);
            }
            else
            {
                _flightMenuActivated = new FlightMenuItemBase[menuToGoBackItems.Length];
                menuToGoBackItems.CopyTo(_flightMenuActivated, 0);
            }

            menuGroupActivated = menuToGoBack;
            _itemNumber = _flightMenuActivated.Length;
            GenerateMenuView();
            menuController.RequestMenuUpdate(_itemNumber);
        }

        private void GenerateMenuView()
        {
            foreach (var itemToDestroy in _itemGenerated)
            {
                Destroy(itemToDestroy);
            }

            var menuLength = _flightMenuActivated.Length;
            _itemGenerated = new GameObject[menuLength * 4];

            var anglePerItem = 360f / menuLength;
            var initialItemAngle = anglePerItem / 2f;
            var fillAmount = (anglePerItem - marginAngle) / 360f;

            for (var index = 0; index < menuLength; index++)
            {
                var targetLocalEulerAngle = new Vector3(0, 0, initialItemAngle - anglePerItem * index);

                // Background clip
                var backgroundItem = Instantiate(backgroundClipTemplate, backgroundRoot);
                backgroundItem.transform.localPosition = Vector3.zero;
                backgroundItem.transform.localEulerAngles = targetLocalEulerAngle;
                backgroundItem.gameObject.name = $"BackgroundClip_{index}";
                backgroundItem.GetComponentInChildren<Image>().fillAmount = fillAmount;

                _itemGenerated[index] = backgroundItem;

                // Hover clip
                var clipItem = Instantiate(hoverClipTemplate, hoverRoot);
                clipItem.transform.localPosition = Vector3.zero;
                clipItem.transform.localEulerAngles = targetLocalEulerAngle;
                clipItem.gameObject.name = $"HoverClip_{index}";
                clipItem.GetComponentInChildren<Image>().fillAmount = fillAmount;

                _itemGenerated[menuLength + index] = clipItem;

                // activated clip
                var activatedItem = Instantiate(activatedClipTemplate, activatedRoot);
                activatedItem.transform.localPosition = Vector3.zero;
                activatedItem.transform.localEulerAngles = targetLocalEulerAngle;
                activatedItem.gameObject.name = $"ActivatedClip_{index}";
                activatedItem.GetComponentInChildren<Image>().fillAmount = fillAmount;
                activatedItem.SetActive(false);

                _itemGenerated[menuLength * 2 + index] = activatedItem;

                // title and icon
                var titleItem = Instantiate(titleTemplate, titleRoot);
                titleItem.transform.localPosition = Vector3.zero;
                titleItem.transform.localEulerAngles = new Vector3(0, 0, anglePerItem * -index);
                titleItem.gameObject.name = $"TitleTemplate_{index}";

                var menuItem = _flightMenuActivated[index];

                var imageComponent = titleItem.GetComponentInChildren<Image>();
                if (menuItem.icon)
                {
                    imageComponent.sprite = menuItem.icon;
                    imageComponent.gameObject.SetActive(true);
                }
                else
                {
                    imageComponent.gameObject.SetActive(false);
                }

                titleItem.GetComponentInChildren<TextMeshProUGUI>().text = menuItem.title;
                titleItem.GetComponentInChildren<FlightMenuItemTitleRotationTarget>()
                    .transform.localRotation = Quaternion.Inverse(titleItem.transform.localRotation);

                _itemGenerated[menuLength * 3 + index] = titleItem;
            }
        }

        public void _OnItemHover(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex > _itemNumber)
            {
                Debug.LogWarning(
                    "_OnItemHover: itemIndex is out of range, input: " + itemIndex + ", max: " + _itemNumber);
                return;
            }

            _itemGenerated[_itemNumber + itemIndex].SetActive(true);
        }

        public void _OnItemHoverLost(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex > _itemNumber)
            {
                Debug.LogWarning(
                    "_OnItemHoverLost: itemIndex is out of range, input: " + itemIndex + ", max: " + _itemNumber);
                return;
            }

            _itemGenerated[_itemNumber + itemIndex].SetActive(false);
        }

        public void _OnItemTrigger(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex > _itemNumber)
            {
                Debug.LogWarning(
                    "_OnItemTrigger: itemIndex is out of range, input: " + itemIndex + ", max: " + _itemNumber);
                return;
            }

            var menuItem = _flightMenuActivated[itemIndex];
            var triggerResult = menuItem.Trigger();

            switch (triggerResult)
            {
                case FlightMenuTriggerResult.Noop:
                    // Do nothing, all logic should handle by menu item itself
                    break;
                case FlightMenuTriggerResult.OpenNewMenu:
                    var newMenuGroup = menuItem.GetNewMenu();
                    if (!newMenuGroup)
                    {
                        Debug.LogWarning(
                            "_OnItemTrigger: triggerResult is OpenNewMenu, but GetNewMenu() returns null, itemIndex: " +
                            itemIndex);
                        return;
                    }

                    NavigateToMenu(newMenuGroup);
                    break;
                case FlightMenuTriggerResult.InternalBackMenu:
                    GoBack();
                    break;
                case FlightMenuTriggerResult.OpenPopupMenu:
                    // TODO: Handle Popup Menu
                    break;
                default:
                    Debug.LogWarning(
                        "_OnItemTrigger: triggerResult is unknown, input: " + triggerResult + ", itemIndex: " +
                        itemIndex);
                    break;
            }
        }

        private void SetItemActivatedIndicator(int index, bool activated)
        {
            var item = _itemGenerated[_itemNumber  * 2 + index];
            item.SetActive(!item.activeSelf);
        }

        #region Menu Group History

        private void PushHistory(FlightMenuGroup newMenuGroup)
        {
            var newHistoryArray = new FlightMenuGroup[menuGroupHistory.Length + 1];
            menuGroupHistory.CopyTo(newHistoryArray, 0);
            newHistoryArray[menuGroupHistory.Length] = newMenuGroup;
            menuGroupHistory = newHistoryArray;
        }

        private FlightMenuGroup PopHistory()
        {
            var lastMenuGroup = menuGroupHistory[menuGroupHistory.Length - 1];
            var newHistoryArray = new FlightMenuGroup[menuGroupHistory.Length - 1];
            Array.Copy(menuGroupHistory, newHistoryArray, menuGroupHistory.Length - 1);
            menuGroupHistory = newHistoryArray;

            return lastMenuGroup;
        }

        private void ClearHistory()
        {
            menuGroupHistory = new FlightMenuGroup[0];
        }

        #endregion
    }
}