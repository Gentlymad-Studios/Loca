using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Loca {
    public class LocaSearchWindow : EditorWindow {
        const string WINDOWNAME = "Search";

        LocaSearch locaSearch;
        LocaWindow locaWindow;
        Label outputLabel;
        Button nextButton;
        Button prevButton;

        public void Initialize(LocaSubDatabase database, LocaWindow locaWindow) {
            minSize = new UnityEngine.Vector2(300, 75);
            maxSize = new UnityEngine.Vector2(300, 75);
            titleContent = new GUIContent(WINDOWNAME);
            this.locaWindow = locaWindow;
            locaSearch = new LocaSearch(database);
        }

        private void CreateGUI() {
            VisualElement uxmlRoot = LocaSettings.searchUxml.CloneTree();
            rootVisualElement.Add(uxmlRoot);
            rootVisualElement.styleSheets.Add(LocaSettings.locaStylesheet);
            uxmlRoot.StretchToParentSize();

            //Link SearchTextField
            TextField searchTextField = rootVisualElement.Q("searchTxtFld") as TextField;
            //searchTextField.isDelayed = true; //to listen on focus lost / press enter
            searchTextField.RegisterValueChangedCallback(Search);

            nextButton = rootVisualElement.Q("nextButton") as Button;
            nextButton.clicked -= NextButton_clicked;
            nextButton.clicked += NextButton_clicked;
            nextButton.Add(new Image() { image = EditorGUIUtility.IconContent("d_tab_next@2x").image });

            prevButton = rootVisualElement.Q("prevButton") as Button;
            prevButton.clicked -= PrevButton_clicked;
            prevButton.clicked += PrevButton_clicked;
            prevButton.Add(new Image() { image = EditorGUIUtility.IconContent("d_tab_prev@2x").image });

            outputLabel = rootVisualElement.Q("outputLabel") as Label;

            ToggleNextPrevButtons();
        }

        private void NextButton_clicked() {
            Select(locaSearch.Next());
        }

        private void PrevButton_clicked() {
            Select(locaSearch.Previous());
        }

        public void Search(ChangeEvent<string> evt) {
            string value = evt.newValue;

            if (int.TryParse(value, out int hash)) {
                locaSearch.Search(hash);

                if (locaSearch.GetSearchEntryCount() == 1) {
                    outputLabel.text = $"Entry '{locaSearch.Current().entry.key}' found";
                    Select(locaSearch.Current());
                    return;
                }
            }

            locaSearch.Search(value);

            int entryCount = locaSearch.GetSearchEntryCount();

            outputLabel.text = $"{entryCount} Entries found";

            Select(locaSearch.Current());

            ToggleNextPrevButtons();
        }

        private void Select(LocaSearchEntry entry) {
            if (entry == null) {
                return;
            }

            locaWindow.table.SetSelection(entry.index);
            locaWindow.table.ScrollToItem(entry.index);
        }

        private void ToggleNextPrevButtons() {
            bool state = locaSearch.GetSearchEntryCount() > 1;
            nextButton.SetEnabled(state);
            prevButton.SetEnabled(state);
        }
    }
}
