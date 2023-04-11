using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Loca {
    public class LocaWindow : EditorWindow {
        private static LocaWindow window;
        private SerializedObject serializedObject;
        private Label statusLabel;
        private MultiColumnListView table;
        private LocaSubDatabase curDatabase;
        private ScrollView scrollView;
        private DropdownField databaseSelection;

        //Cache
        private VisualElement activeElement;
        private string selectedDatabaseName;
        private int selectedDatabaseIndex = 0;

        [MenuItem(LocaSettings.menuItemBase + nameof(LocaWindow))]
        public static void Initialize() {
            window = GetWindow<LocaWindow>(nameof(LocaWindow));
            window.Show();
        }

        public static void Redraw() {
            if (window == null) {
                Initialize();
            }
            window.Repaint();
        }

        private void OnUpdate() {
            //Update Status-Label
            if (LocaBackgroundWorker.locaStatus != statusLabel.text) {
                statusLabel.text = LocaBackgroundWorker.locaStatus;
            }
        }

        private void OnEnable() {
            //Enable Database EditMode -> prevent Backgroundworker to Update the Database
            LocaDatabase.instance.isInEditMode = true;
        }

        private void OnDisable() {
            //Disable Database EditMode -> prevent Backgroundworker to Update the Database
            LocaDatabase.instance.isInEditMode = false;

            //Save Local Version
            LocaDatabase.instance.Save();

            if (LocaDatabase.instance.hasLocalChanges) {
                bool decision = EditorUtility.DisplayDialog("Unsaved Changes", "You have changes that have not been updated in the Google spreadsheet. Do you want to update the Google spreadsheet?", "Yes", "No (keep the changes local)");

                if (decision) {
                    //Save to Google Sheet
                    LocaBase.SaveDatabasesToSheets();
                }
            }
        }

        private void CreateGUI() {
            serializedObject = new SerializedObject(LocaDatabase.instance);

            VisualElement uxmlRoot = LocaSettings.locaUxml.CloneTree();
            rootVisualElement.Add(uxmlRoot);
            rootVisualElement.styleSheets.Add(LocaSettings.locaStylesheet);
            uxmlRoot.StretchToParentSize();

            //Status string
            statusLabel = rootVisualElement.Q("statusLabel") as Label;
            statusLabel.text = LocaBackgroundWorker.locaStatus;

            //Un-/register Update
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;

            //Link Buttons
            Button saveButton = rootVisualElement.Q("saveButton") as Button;
            saveButton.clicked -= SaveButton_clicked;
            saveButton.clicked += SaveButton_clicked;

            Button pullButton = rootVisualElement.Q("pullButton") as Button;
            pullButton.clicked -= PullButton_clicked;
            pullButton.clicked += PullButton_clicked;

            //Link Dropdown
            databaseSelection = rootVisualElement.Q("databaseSelection") as DropdownField;
            databaseSelection.RegisterValueChangedCallback(DropdownChange);
            SetupDatabaseSelection();

            CreateMultiColumnListView();
        }

        #region MultiColumnListView Logic
        //Create MultiColumnListView
        private void CreateMultiColumnListView() {
            int fixedItemHeight = LocaSettings.instance.fixedRowHeight;
            int initialColumnWidth = LocaSettings.instance.initialColumnWidth;

            table = rootVisualElement.Q<MultiColumnListView>();

            //Reset
            table.itemsSource = null;
            table.columns.Clear();

            //Setup
            table.style.paddingRight = 17; //because of the vertical scrollbar
            //table.showAddRemoveFooter = true;
            table.showBorder = true;
            table.horizontalScrollingEnabled = true; //fixes the scale issues
            table.itemsSource = curDatabase.locaEntries;
            table.fixedItemHeight = fixedItemHeight;
            table.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight;
            table.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            //table.sortingEnabled = true;
            //table.columnSortingChanged -= Table_columnSortingChanged;
            //table.columnSortingChanged += Table_columnSortingChanged;

            void Scroll(float f) {
                DisableActiveElement();
            }

            scrollView = table.Q<ScrollView>();
            scrollView.verticalScroller.valueChanged -= Scroll;
            scrollView.verticalScroller.valueChanged += Scroll;

            //Make Cell
            VisualElement MakeCell() {
                VisualElement cell = new VisualElement();
                cell.AddToClassList(nameof(cell));

                //Label
                Label label = new Label();
                label.RegisterCallback<ClickEvent>(Clicked);
                cell.Add(label);

                //TextField
                TextField textField = new TextField();
                textField.multiline = true;
                cell.Add(textField);

                return cell;
            }

            //BindKeyCell
            void BindKeyCell(VisualElement e, int index) {
                CellUserData cellUserData = new CellUserData(index);

                Label label = e.Q<Label>();
                label.userData = cellUserData;
                label.text = curDatabase.locaEntries[index].key;

                if (curDatabase.locaEntries[index].hasKeyChanges) {
                    label.style.color = LocaSettings.instance.hightlightColor;
                } else {
                    label.style.color = StyleKeyword.Null;
                }
            }

            //Add Key Column
            table.columns.Add(new Column() {
                name = LocaSettings.instance.headerSettings.keyColumnName,
                title = LocaSettings.instance.headerSettings.keyColumnDisplayName,
                width = initialColumnWidth,
                makeCell = MakeCell,
                bindCell = BindKeyCell
            });

            //Add Language Columns
            for (int i = 0; i < curDatabase.languages.Count; i++) {
                int langIndex = i;
                string language = curDatabase.languages[i];

                //BindContentCell
                void BindContentCell(VisualElement e, int index) {
                    CellUserData cellUserData = new CellUserData(index, langIndex);

                    Label label = e.Q<Label>();
                    label.userData = cellUserData;
                    label.text = curDatabase.locaEntries[index].content[langIndex].content;

                    TextField textField = e.Q<TextField>();
                    textField.value = curDatabase.locaEntries[index].content[langIndex].content;

                    if (curDatabase.locaEntries[index].content[langIndex].hasChanges) {
                        label.style.color = LocaSettings.instance.hightlightColor;
                    } else {
                        label.style.color = StyleKeyword.Null;
                    }
                }

                table.columns.Add(new Column() {
                    name = language,
                    title = language,
                    width = initialColumnWidth,
                    sortable = false,
                    makeCell = MakeCell,
                    bindCell = BindContentCell
                });
            }
        }

        //Disable active Element
        private void DisableActiveElement() {
            if (activeElement != null) {
                Label label = activeElement.Q<Label>();
                label.style.display = DisplayStyle.Flex;

                TextField textField = activeElement.Q<TextField>();
                textField.style.display = DisplayStyle.None;
                textField.value = "";
                textField.UnregisterValueChangedCallback(TextChanged);
                textField.UnregisterCallback<FocusOutEvent>(FocusLost);

                activeElement = null;
            }
        }
        #endregion

        #region MultiColumnListView Events
        //Table sorting Callback
        private void ColumnSortingChanged() {
            LocaDatabase.instance.databases[0].Sort(table.sortedColumns);
            table.RefreshItems();
        }

        //Label ClickEvent
        private void Clicked(ClickEvent evt) {
            if (evt.clickCount == 2) {
                Label label = evt.target as Label;
                TextField textField = label.parent.Q<TextField>();

                CellUserData cellUserData = (CellUserData)label.userData;

                DisableActiveElement();
                activeElement = label.parent;

                label.style.display = DisplayStyle.None;
                textField.style.display = DisplayStyle.Flex;
                if (cellUserData.languageIndex == -1) {
                    textField.value = curDatabase.locaEntries[cellUserData.rowIndex].key;
                } else {
                    textField.value = curDatabase.locaEntries[cellUserData.rowIndex].content[cellUserData.languageIndex].content;
                }
                textField.Focus();

                textField.UnregisterValueChangedCallback(TextChanged);
                textField.RegisterValueChangedCallback(TextChanged);

                textField.UnregisterCallback<FocusOutEvent>(FocusLost);
                textField.RegisterCallback<FocusOutEvent>(FocusLost);
            }
        }

        //TextChanged Event
        private void TextChanged(ChangeEvent<string> evt) {
            TextField textField = evt.target as TextField;
            Label label = textField.parent.Q<Label>();

            CellUserData cellUserData = (CellUserData)label.userData;

            if (!string.IsNullOrEmpty(evt.previousValue)) {
                if (cellUserData.languageIndex == -1) {
                    //Key
                    if (curDatabase.KeyExists(evt.newValue)) {
                        textField.Q<VisualElement>("unity-text-input").style.color = LocaSettings.instance.hightlightColor;
                    } else {
                        textField.Q<VisualElement>("unity-text-input").style.color = StyleKeyword.Null;
                        curDatabase.locaEntries[cellUserData.rowIndex].key = evt.newValue;
                        label.text = curDatabase.locaEntries[cellUserData.rowIndex].key;
                    }
                    label.style.color = LocaSettings.instance.hightlightColor;
                    curDatabase.locaEntries[cellUserData.rowIndex].hasKeyChanges = true;
                    curDatabase.locaEntries[cellUserData.rowIndex].EntryUpdated();
                    curDatabase.ClearEntriesMapping();
                } else {
                    //Content
                    curDatabase.locaEntries[cellUserData.rowIndex].content[cellUserData.languageIndex].content = evt.newValue;
                    label.text = curDatabase.locaEntries[cellUserData.rowIndex].content[cellUserData.languageIndex].content;
                    label.style.color = LocaSettings.instance.hightlightColor;
                    curDatabase.locaEntries[cellUserData.rowIndex].content[cellUserData.languageIndex].hasChanges = true;
                    curDatabase.locaEntries[cellUserData.rowIndex].EntryUpdated();
                }
            }
        }

        //FocusLost Event
        void FocusLost(FocusOutEvent e) {
            DisableActiveElement();
        }
        #endregion

        #region Button Events
        private void SaveButton_clicked() {
            LocaBase.SaveDatabasesToSheets();
            SetupDatabaseSelection();
            table.RefreshItems();
        }

        private void PullButton_clicked() {
            LocaBase.ExtractDatabasesFromSheets();
            SetupDatabaseSelection();
            table.RefreshItems();
        }
        #endregion

        #region Database Selection Logic
        /// <summary>
        /// Setup the Database Selection Dropdown
        /// </summary>
        private void SetupDatabaseSelection() {
            List<string> databaseChoices = new List<string>();

            for (int i = 0; i < LocaDatabase.instance.databases.Count; i++) {
                databaseChoices.Add(LocaDatabase.instance.databases[i].sheetName);
            }

            databaseSelection.choices = databaseChoices;

            if (string.IsNullOrEmpty(selectedDatabaseName)) {
                databaseSelection.SetValueWithoutNotify(databaseChoices[0]);
            } else {
                int index = 0;
                for (int i = 0; i < databaseChoices.Count; i++) {
                    if (databaseChoices[i].ToLower() == selectedDatabaseName.ToLower()) {
                        index = i;
                        break;
                    }
                }

                databaseSelection.SetValueWithoutNotify(databaseChoices[index]);
            }

            curDatabase = LocaDatabase.instance.databases[databaseSelection.index];
        }

        /// <summary>
        /// Dropdown Change Event
        /// </summary>
        /// <param name="evt"></param>
        private void DropdownChange(ChangeEvent<string> evt) {
            selectedDatabaseName = evt.newValue;
            selectedDatabaseIndex = databaseSelection.index;

            curDatabase = LocaDatabase.instance.databases[databaseSelection.index];

            //Redraw ListView
            CreateMultiColumnListView();
        }
        #endregion

        #region Helper
        class CellUserData {
            public int rowIndex;
            public int languageIndex;

            public CellUserData(int rowIndex, int languageIndex = -1) {
                this.rowIndex = rowIndex;
                this.languageIndex = languageIndex;
            }
        }
        #endregion
    }
}