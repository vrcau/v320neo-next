using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace VAU.V320NeoNext.Runtime.FlightMenu
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public sealed class FlightMenuView : UdonSharpBehaviour
    {
        [Header("Core")] public FlightMenuController menuController;

        [Header("Child Root")] 
        public Transform backgroundRoot;
        public Transform hoverRoot;
        public Transform activatedRoot;

        [Header("Item Image Template")] 
        public GameObject backgroundClipTemplate;
        public GameObject hoverClipTemplate;
        public GameObject activatedClipTemplate;

        [Header("Debug Only")] 
        public int itemNumber = 8;
        public float marginAngle = 2f;

        private GameObject[] _itemGenerated = new GameObject[0];

        private void Start()
        {
            GenerateMenuView(itemNumber);
            menuController.RequestMenuUpdate(itemNumber);
        }

        public void GenerateMenuView(int menuLength)
        {
            foreach (var itemToDestroy in _itemGenerated)
            {
                Destroy(itemToDestroy);
            }

            _itemGenerated = new GameObject[menuLength * 3];

            var anglePerItem = 360f / menuLength;
            var initialItemAngle = anglePerItem / 2f;
            var fillAmount = (anglePerItem - marginAngle) / 360f;

            for (var index = 0; index < menuLength; index++)
            {
                var item = Instantiate(backgroundClipTemplate, backgroundRoot);
                item.transform.localPosition = Vector3.zero;
                item.transform.localEulerAngles = new Vector3(0, 0, initialItemAngle - anglePerItem * index);
                item.gameObject.name = $"BackgroundClip_{index}";
                item.GetComponentInChildren<Image>().fillAmount = fillAmount;

                _itemGenerated[index] = item;
            }

            for (var index = 0; index < menuLength; index++)
            {
                var item = Instantiate(hoverClipTemplate, hoverRoot);
                item.transform.localPosition = Vector3.zero;
                item.transform.localEulerAngles = new Vector3(0, 0, initialItemAngle - anglePerItem * index);
                item.gameObject.name = $"HoverClip_{index}";
                item.GetComponentInChildren<Image>().fillAmount = fillAmount;

                _itemGenerated[menuLength + index] = item;
            }

            for (var index = 0; index < menuLength; index++)
            {
                var item = Instantiate(activatedClipTemplate, activatedRoot);
                item.transform.localPosition = Vector3.zero;
                item.transform.localEulerAngles = new Vector3(0, 0, initialItemAngle - anglePerItem * index);
                item.gameObject.name = $"ActivatedClip_{index}";
                item.GetComponentInChildren<Image>().fillAmount = fillAmount;
                item.SetActive(false);

                _itemGenerated[menuLength * 2 + index] = item;
            }
        }

        public void _OnItemHover(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex > itemNumber)
            {
                Debug.LogWarning(
                    "_OnItemHover: itemIndex is out of range, input: " + itemIndex + ", max: " + itemNumber);
                return;
            }

            _itemGenerated[itemNumber + itemIndex].SetActive(true);
        }

        public void _OnItemHoverLost(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex > itemNumber)
            {
                Debug.LogWarning(
                    "_OnItemHoverLost: itemIndex is out of range, input: " + itemIndex + ", max: " + itemNumber);
                return;
            }

            _itemGenerated[itemNumber + itemIndex].SetActive(false);
        }

        public void _OnItemTrigger(int itemIndex)
        {
            if (itemIndex < 0 || itemIndex > itemNumber)
            {
                Debug.LogWarning(
                    "_OnItemTrigger: itemIndex is out of range, input: " + itemIndex + ", max: " + itemNumber);
                return;
            }

            var item = _itemGenerated[itemNumber  * 2 + itemIndex];
            item.SetActive(!item.activeSelf);
        }
    }
}