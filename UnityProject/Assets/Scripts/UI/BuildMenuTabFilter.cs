using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace AIBusinessTycoon.UI
{
    /// <summary>Filters build-menu cards by category and highlights the selected tab.</summary>
    public sealed class BuildMenuTabFilter : MonoBehaviour
    {
        [SerializeField] private Button[] tabs;
        [SerializeField] private string[] tabLabels;
        [SerializeField] private GameObject[] cards;
        [SerializeField] private string[] cardCategories;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Color activeColor = new Color(0.02f, 0.48f, 0.56f);
        [SerializeField] private Color inactiveColor = new Color(0.03f, 0.13f, 0.21f);
        private UnityAction[] _tabCallbacks;

        public void Configure(Button[] tabButtons, string[] labels, GameObject[] cardObjects, string[] categories, ScrollRect scrollView)
        {
            UnbindTabListeners();
            tabs = tabButtons;
            tabLabels = labels;
            cards = cardObjects;
            cardCategories = categories;
            scrollRect = scrollView;
            BindTabListeners();
            SelectTab(0);
        }

        private void OnEnable()
        {
            BindTabListeners();
        }

        private void OnDisable()
        {
            UnbindTabListeners();
        }

        private void BindTabListeners()
        {
            if (tabs == null || _tabCallbacks != null)
                return;

            _tabCallbacks = new UnityAction[tabs.Length];
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == null)
                    continue;

                int index = i;
                _tabCallbacks[i] = () => SelectTab(index);
                tabs[i].onClick.AddListener(_tabCallbacks[i]);
            }
        }

        private void UnbindTabListeners()
        {
            if (tabs == null || _tabCallbacks == null)
                return;

            for (int i = 0; i < tabs.Length && i < _tabCallbacks.Length; i++)
            {
                if (tabs[i] != null && _tabCallbacks[i] != null)
                    tabs[i].onClick.RemoveListener(_tabCallbacks[i]);
            }

            _tabCallbacks = null;
        }

        private void SelectTab(int tabIndex)
        {
            if (tabs == null || tabLabels == null || tabIndex < 0 || tabIndex >= tabLabels.Length)
                return;

            string selected = tabLabels[tabIndex];
            for (int i = 0; i < tabs.Length; i++)
            {
                Image image = tabs[i] != null ? tabs[i].GetComponent<Image>() : null;
                if (image != null)
                    image.color = i == tabIndex ? activeColor : inactiveColor;
            }

            if (cards != null && cardCategories != null)
            {
                for (int i = 0; i < cards.Length && i < cardCategories.Length; i++)
                {
                    if (cards[i] != null)
                        cards[i].SetActive(selected == "ALL" || cardCategories[i] == selected);
                }
            }

            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}