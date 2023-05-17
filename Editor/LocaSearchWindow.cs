using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Loca {
    public class LocaSearchWindow : EditorWindow {
        const string WINDOWNAME = "Search";

        LocaSearch locaSearch;
        LocaWindow locaWindow;
        Label outputLabel;

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

            Button nextButton = rootVisualElement.Q("nextButton") as Button;
            nextButton.clicked -= NextButton_clicked;
            nextButton.clicked += NextButton_clicked;
            nextButton.Add(new Image() { image = EditorGUIUtility.IconContent("d_tab_next@2x").image });

            Button prevButton = rootVisualElement.Q("prevButton") as Button;
            prevButton.clicked -= PrevButton_clicked;
            prevButton.clicked += PrevButton_clicked;
            prevButton.Add(new Image() { image = EditorGUIUtility.IconContent("d_tab_prev@2x").image });

            outputLabel = rootVisualElement.Q("outputLabel") as Label;
        }

        private void NextButton_clicked() {
            Select(locaSearch.Next());
        }

        private void PrevButton_clicked() {
            Select(locaSearch.Previous());
        }

        public void Search(ChangeEvent<string> evt) {
            locaSearch.Search(evt.newValue);

            int entryCount = locaSearch.GetSearchEntryCount();

            outputLabel.text = $"{entryCount} Entries found";

            Select(locaSearch.Current());
        }

        private void Select(LocaSearchEntry entry) {
            locaWindow.table.SetSelection(entry.index);
            locaWindow.table.ScrollToItem(entry.index);
            //locaWindow.scrollView.ScrollTo();
        }
    }
}
