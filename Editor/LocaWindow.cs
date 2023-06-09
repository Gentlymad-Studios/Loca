using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings.Switch;

namespace Loca {
    public class LocaWindow : EditorWindow {
        const string WINDOWNAME = "Loca Manager";
        public static LocaWindow window;

        public ScrollView scrollView;
        public MultiColumnListView table;
        private SerializedObject serializedObject;
        private Label statusLabel;
        private TextField filterTxtFld;
        private Label filterPlaceholderLbl;
        private Toggle emptyEntryFilterToggle;
        private DropdownField databaseSelection;
        private DropdownField emptyEntryFilterLanguageSelection;
        private LocaSearchWindow searchWindow;

        //Data
        private LocaSubDatabase curDatabase;
        private List<LocaEntry> tableEntries = new List<LocaEntry>();

        //Cache
        private VisualElement activeElement;
        private string selectedDatabaseName;
        private int selectedDatabaseIndex = 0;

        [MenuItem(LocaSettings.MENUITEMBASE + nameof(Loca) + "/" + WINDOWNAME, priority = 40)]
        public static void Initialize() {
            window = GetWindow<LocaWindow>(WINDOWNAME);
            window.minSize = new UnityEngine.Vector2(1280, 400);

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

            //Filter TextField
            filterTxtFld = rootVisualElement.Q("filterTxtFld") as TextField;
            filterTxtFld.isDelayed = true;
            filterTxtFld.UnregisterValueChangedCallback(FilterTextField_changed);
            filterTxtFld.RegisterValueChangedCallback(FilterTextField_changed);
            filterTxtFld.UnregisterCallback<FocusInEvent>(FilterFocusInEvent);
            filterTxtFld.RegisterCallback<FocusInEvent>(FilterFocusInEvent);
            filterTxtFld.UnregisterCallback<FocusOutEvent>(FilterFocusOutEvent);
            filterTxtFld.RegisterCallback<FocusOutEvent>(FilterFocusOutEvent);

            //Filter Placeholder Text
            filterPlaceholderLbl = rootVisualElement.Q("filterPlaceholderLbl") as Label;

            //Un-/register Update
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;

            //Link Buttons
            Button exportButton = rootVisualElement.Q("exportButton") as Button;
            exportButton.clicked -= ExportButton_clicked;
            exportButton.clicked += ExportButton_clicked;

            Button uploadButton = rootVisualElement.Q("uploadButton") as Button;
            uploadButton.clicked -= UploadButton_clicked;
            uploadButton.clicked += UploadButton_clicked;

            Button pullButton = rootVisualElement.Q("pullButton") as Button;
            pullButton.clicked -= PullButton_clicked;
            pullButton.clicked += PullButton_clicked;

            Button settingsButton = rootVisualElement.Q("settingsButton") as Button;
            settingsButton.clicked -= SettingsButton_clicked;
            settingsButton.clicked += SettingsButton_clicked;
            settingsButton.Add(new Image() { image = EditorGUIUtility.IconContent("Settings@2x").image });

            Button refreshButton = rootVisualElement.Q("refreshButton") as Button;
            refreshButton.clicked -= RefreshButton_clicked;
            refreshButton.clicked += RefreshButton_clicked;
            refreshButton.Add(new Image() { image = EditorGUIUtility.IconContent("Refresh@2x").image });

            Button addButton = rootVisualElement.Q("addEntryButton") as Button;
            addButton.clicked -= AddEntryButton_clicked;
            addButton.clicked += AddEntryButton_clicked;

            Button removeButton = rootVisualElement.Q("removeEntryButton") as Button;
            removeButton.clicked -= RemoveEntryButton_clicked;
            removeButton.clicked += RemoveEntryButton_clicked;

            Button clearFilterButton = rootVisualElement.Q("clearFilterButton") as Button;
            clearFilterButton.clicked -= ClearFilterTextField_clicked;
            clearFilterButton.clicked += ClearFilterTextField_clicked;

            emptyEntryFilterToggle = rootVisualElement.Q("emptyEntryFilterTgl") as Toggle;
            emptyEntryFilterToggle.UnregisterValueChangedCallback(EmptyEntryFilterToggle_changed);
            emptyEntryFilterToggle.RegisterValueChangedCallback(EmptyEntryFilterToggle_changed);

            emptyEntryFilterLanguageSelection = rootVisualElement.Q("emptyEntryFilterLanguageSelection") as DropdownField;
            emptyEntryFilterLanguageSelection.UnregisterValueChangedCallback(EmptyEntryLanguage_changed);
            emptyEntryFilterLanguageSelection.RegisterValueChangedCallback(EmptyEntryLanguage_changed);

            Button searchButton = rootVisualElement.Q("searchButton") as Button;
            searchButton.clicked -= SearchButton_clicked;
            searchButton.clicked += SearchButton_clicked;
            searchButton.Add(new Image() { image = EditorGUIUtility.IconContent("d_Search Icon").image });

            //Link Dropdown
            databaseSelection = rootVisualElement.Q("databaseSelection") as DropdownField;
            databaseSelection.UnregisterValueChangedCallback(DatabaseSelection_changed);
            databaseSelection.RegisterValueChangedCallback(DatabaseSelection_changed);
            SetupDatabaseSelection();

            SetupFilterLanguage();

            CreateMultiColumnListView();
        }

        #region MultiColumnListView Logic
        //Create MultiColumnListView
        private void CreateMultiColumnListView() {
            int fixedItemHeight = LocaSettings.instance.fixedRowHeight;
            int initialColumnWidth = LocaSettings.instance.initialColumnWidth;

            Label noDataLabel = rootVisualElement.Q<Label>("noDataLabel");
            noDataLabel.style.display = DisplayStyle.None;

            table = rootVisualElement.Q<MultiColumnListView>();

            //Reset
            table.itemsSource = null;
            table.columns.Clear();

            if (curDatabase == null) {
                noDataLabel.style.display = DisplayStyle.Flex;
                table.visible = false;
                return;
            }

            //Setup
            FilterList(filterTxtFld.text);

            table.style.paddingRight = 17; //because of the vertical scrollbar
            table.visible = true;
            table.showBorder = true;
            table.horizontalScrollingEnabled = true; //fixes the scale issues
            table.itemsSource = tableEntries; //curDatabase.locaEntries
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
                label.enableRichText = LocaSettings.instance.enableLabelRichText;
                label.style.height = LocaSettings.instance.fixedRowHeight;
                label.RegisterCallback<ClickEvent>(Clicked);
                cell.Add(label);

                //TextField
                TextField textField = new TextField();
                textField.multiline = true;
                AddTextFieldContext(textField);
                cell.Add(textField);

                return cell;
            }

            //BindKeyCell
            void BindKeyCell(VisualElement e, int index) {
                CellUserData cellUserData = new CellUserData(index);

                Label label = e.Q<Label>();
                label.userData = cellUserData;
                label.text = tableEntries[index].key;

                if (tableEntries[index].hasKeyChanges) {
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
                    label.text = tableEntries[index].content[langIndex].content;

                    TextField textField = e.Q<TextField>();
                    textField.value = tableEntries[index].content[langIndex].content;

                    if (tableEntries[index].content[langIndex].hasChanges) {
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
                    textField.value = tableEntries[cellUserData.rowIndex].key;
                } else {
                    textField.value = tableEntries[cellUserData.rowIndex].content[cellUserData.languageIndex].content;
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
                        tableEntries[cellUserData.rowIndex].key = evt.newValue;
                        label.text = tableEntries[cellUserData.rowIndex].key;
                    }
                    label.style.color = LocaSettings.instance.hightlightColor;
                    tableEntries[cellUserData.rowIndex].hasKeyChanges = true;
                    tableEntries[cellUserData.rowIndex].EntryUpdated();
                    curDatabase.ClearEntriesMappingAndStorage();
                } else {
                    //Content
                    tableEntries[cellUserData.rowIndex].content[cellUserData.languageIndex].content = evt.newValue;
                    label.text = tableEntries[cellUserData.rowIndex].content[cellUserData.languageIndex].content;
                    label.style.color = LocaSettings.instance.hightlightColor;
                    tableEntries[cellUserData.rowIndex].content[cellUserData.languageIndex].hasChanges = true;
                    tableEntries[cellUserData.rowIndex].EntryUpdated();
                }
            }
        }

        //FocusLost Event
        void FocusLost(FocusOutEvent e) {
            DisableActiveElement();
        }
        #endregion

        #region Button Events
        private void ExportButton_clicked() {
            LocaJsonHandler.WriteLocasheets();
        }

        private void UploadButton_clicked() {
            LocaBase.SaveDatabasesToSheets();
            SetupDatabaseSelection();

            CreateMultiColumnListView();
        }

        private void PullButton_clicked() {
            LocaBase.ExtractDatabasesFromSheets();
            SetupDatabaseSelection();

            CreateMultiColumnListView();
        }

        private void SettingsButton_clicked() {
            SettingsService.OpenProjectSettings($"{LocaSettings.MENUITEMBASE}{nameof(Loca)}");
        }

        private void RefreshButton_clicked() {
            CreateMultiColumnListView();
        }

        private void AddEntryButton_clicked() {
            string key = LocaNewEntryPopup.Initialize(curDatabase);
            bool wasCreated = curDatabase.CreateLocaEntry(key);

            if (wasCreated) {
                CreateMultiColumnListView();
                scrollView.verticalScroller.value = scrollView.verticalScroller.highValue;
            }
        }

        private void RemoveEntryButton_clicked() {
            LocaEntry selectedEntry = table.selectedItem as LocaEntry;
            curDatabase.RemoveLocaEntry(selectedEntry);
            CreateMultiColumnListView();
        }

        private void SearchButton_clicked() {
            if (searchWindow == null) {
                searchWindow = CreateInstance<LocaSearchWindow>();
                searchWindow.Initialize(curDatabase, this);
                searchWindow.Show();
            }
            searchWindow.Focus();
        }

        private void ClearFilterTextField_clicked() {
            filterTxtFld.SetValueWithoutNotify(string.Empty);
            filterPlaceholderLbl.style.display = DisplayStyle.Flex;
            CreateMultiColumnListView();
        }
        #endregion

        #region Filter Events
        private void FilterFocusInEvent(FocusInEvent evt) {
            filterPlaceholderLbl.style.display = DisplayStyle.None;
        }

        private void FilterFocusOutEvent(FocusOutEvent evt) {
            if (string.IsNullOrEmpty(filterTxtFld.value) || string.IsNullOrWhiteSpace(filterTxtFld.value)) {
                filterPlaceholderLbl.style.display = DisplayStyle.Flex;
            } else {
                filterPlaceholderLbl.style.display = DisplayStyle.None;
            }
        }

        private void FilterTextField_changed(ChangeEvent<string> evt) {
            FilterFocusOutEvent(null);
            CreateMultiColumnListView();
        }

        private void EmptyEntryFilterToggle_changed(ChangeEvent<bool> evt) {
            CreateMultiColumnListView();
        }

        private void EmptyEntryLanguage_changed(ChangeEvent<string> evt) {
            if (emptyEntryFilterToggle.value) {
                CreateMultiColumnListView();
            }
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

            if (databaseChoices.Count == 0) {
                return;
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
        private void DatabaseSelection_changed(ChangeEvent<string> evt) {
            selectedDatabaseName = evt.newValue;
            selectedDatabaseIndex = databaseSelection.index;

            SelectDatabase(LocaDatabase.instance.databases[databaseSelection.index]);
        }

        /// <summary>
        /// Set Visible Database
        /// </summary>
        /// <param name="database">Database to show</param>
        public void SelectDatabase(LocaSubDatabase database) {
            curDatabase = database;
            databaseSelection.SetValueWithoutNotify(database.sheetName);

            if (searchWindow != null) {
                searchWindow.Close();
                searchWindow = null;
            }

            emptyEntryFilterToggle.SetValueWithoutNotify(false);
            emptyEntryFilterLanguageSelection.SetValueWithoutNotify("All Languages");
            filterTxtFld.SetValueWithoutNotify(string.Empty);
            filterPlaceholderLbl.style.display = DisplayStyle.Flex;

            //Redraw ListView
            CreateMultiColumnListView();
            SetupFilterLanguage();
        }
        #endregion

        #region Filter Logic
        private void FilterList(string filter) {
            curDatabase.FillFilteredListOfEntries(filter, emptyEntryFilterToggle.value, emptyEntryFilterLanguageSelection.value, ref tableEntries);
        }

        private void SetupFilterLanguage() {
            List<string> languageChoices = new List<string> { "All Languages" };
            languageChoices.AddRange(curDatabase.languages);
            emptyEntryFilterLanguageSelection.choices = languageChoices;
            emptyEntryFilterLanguageSelection.value = "All Languages";
        }
        #endregion

        #region TextField Context
        private void AddTextFieldContext(TextField textField) {
            textField.RegisterCallback<ContextualMenuPopulateEvent>((evt) =>
            {
                bool textSelected = textField.cursorIndex != textField.selectIndex;
                evt.menu.AppendSeparator();
                for (int i = 0; i < LocaSettings.instance.markups.Count; i++) {
                    LocaSettings.Markup markup = LocaSettings.instance.markups[i];
                    evt.menu.AppendAction($"add {markup.name}", (x) => InsertMarkup(textField, markup), textSelected ? DropdownMenuAction.AlwaysDisabled : DropdownMenuAction.AlwaysEnabled);
                }

                for (int i = 0; i < LocaSettings.instance.enclosedMarkups.Count; i++) {
                    LocaSettings.EnclosedMarkup markup = LocaSettings.instance.enclosedMarkups[i];
                    evt.menu.AppendAction($"add {markup.name}", (x) => InsertEnclosedMarkup(textField, markup), DropdownMenuAction.AlwaysEnabled);
                }
            });
        }

        private void InsertMarkup(TextField textField, LocaSettings.Markup markup) {
            string text = textField.text;

            //default markup

            string tag = markup.tag;
            text = text.Insert(textField.cursorIndex, tag);

            //check for surrounding spaces
            if (markup.surroundingSpace) {
                //check for space after tag
                if (text[textField.cursorIndex + tag.Length] != ' ') {
                    text = text.Insert(textField.cursorIndex + tag.Length, " ");
                }

                //check for space before tag
                if (text[textField.cursorIndex - 1] != ' ') {
                    text = text.Insert(textField.cursorIndex, " ");
                }
            }

            textField.value = text;
            textField.SelectNone();
        }

        private void InsertEnclosedMarkup(TextField textField, LocaSettings.EnclosedMarkup markup) {
            string text = textField.text;

            string openingTag = markup.openingTag;
            string closingTag = markup.closingTag;

            int openingIndex = textField.cursorIndex;
            int closingIndex = textField.selectIndex;

            if (textField.selectIndex < textField.cursorIndex) {
                openingIndex = textField.selectIndex;
                closingIndex = textField.cursorIndex;
            }

            text = text.Insert(closingIndex, closingTag);
            text = text.Insert(openingIndex, openingTag);

            textField.value = text;
            textField.SelectNone();
        }
        #endregion

        #region Helper
        /// <summary>
        /// Opens the LocaManager and select the first LocaEntry with the given key
        /// </summary>
        /// <param name="key">Key of the LocaEntry</param>
        public static void OpenWindowAt(string key) {
            if (window == null) {
                Initialize();
            }

            List<LocaSearchEntry> entries = LocaDatabase.instance.GetFilteredListOfEntries(key);
            if (entries.Count != 0) {
                window.SelectDatabase(entries[0].database);
                window.table.SetSelection(entries[0].index);
                window.table.ScrollToItem(entries[0].index);
            }
        }

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