using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using EditorHelper;

namespace Loca {
    public class LocaSettingsProvider : AdvancedSingletonProviderBase<LocaSettings> {
        [SettingsProvider]
        private static SettingsProvider CreateProvider() {
            return CreateSettingsProvider<LocaSettingsProvider>();
        }

        public LocaSettingsProvider(string path) : base(path) { }

        protected override string[] GetTags() {
            return new string[] { nameof(Loca) };
        }

        /// <summary>
        /// Called when any value changed.
        /// </summary>
        /// <param name="evt"></param>
        protected override void ValueChanged(SerializedPropertyChangeEvent evt) {
            base.ValueChanged(evt);

            //Reset BackgroundWorker fail
            LocaBackgroundWorker.apiFailed = false;
        }


        public override void OnActivate(string searchContext, VisualElement rootElement) {
            base.OnActivate(searchContext, rootElement);
            DrawLocalSettings(rootElement);
            DrawDebugging(rootElement);
        }

        private void DrawLocalSettings(VisualElement rootElement) {
            VisualElement container = new VisualElement();
            container.AddToClassList("unity-decorator-drawers-container");

            //Header
            Label label = new Label("Google Sheet Background Update");
            label.AddToClassList("unity-base-field");
            label.AddToClassList("unity-header-drawer__label");
            container.Add(label);

            //checkForUpdateInterval Field
            IntegerField checkForUpdateIntervalField = new IntegerField("Check For Update Interval Field") { 
                value = LocaSettings.instance.googleSettings.checkForUpdateInterval,
                tooltip = "Interval you want to pull the spreadsheet.In milliseconds. Minimum: 120.000ms"
            };
            checkForUpdateIntervalField.RegisterValueChangedCallback((evt) => {
                LocaSettings.instance.googleSettings.checkForUpdateInterval = Math.Max(evt.newValue, 120000);
                checkForUpdateIntervalField.value = LocaSettings.instance.googleSettings.checkForUpdateInterval;
                LocaSettings.instance.SaveEditorPrefs();
            });
            container.Add(checkForUpdateIntervalField);

            //checkForModifiedInterval Field
            IntegerField checkForModifiedIntervalField = new IntegerField("Check For Modified Interval Field") { 
                value = LocaSettings.instance.googleSettings.checkForModifiedInterval,
                tooltip = "Interval you want to check if the spreadsheet has been modified. In milliseconds. Minimum: 2.000ms"
            };
            checkForModifiedIntervalField.RegisterValueChangedCallback((evt) => {
                LocaSettings.instance.googleSettings.checkForModifiedInterval = Math.Max(evt.newValue, 2000);
                checkForModifiedIntervalField.value = LocaSettings.instance.googleSettings.checkForModifiedInterval;
                LocaSettings.instance.SaveEditorPrefs();
            });
            container.Add(checkForModifiedIntervalField);

            //AutoUpdate Toggle
            Toggle autoUpdateField = new Toggle("Auto Update") { 
                value = LocaSettings.instance.googleSettings.autoUpdate,
                tooltip = "Automaticly pull the spreadsheet to the local database."
            };
            autoUpdateField.RegisterValueChangedCallback((evt) => {
                LocaSettings.instance.googleSettings.autoUpdate = evt.newValue;
                LocaSettings.instance.SaveEditorPrefs();
            });
            container.Add(autoUpdateField);

            ScrollView scrollView = rootElement.Q<ScrollView>();

            scrollView.Add(container);
        }

        private void DrawDebugging(VisualElement rootElement) {
            VisualElement container = new VisualElement();
            container.AddToClassList("unity-decorator-drawers-container");

            //Header
            Label label = new Label("===== Debug =====");
            label.AddToClassList("unity-base-field");
            label.AddToClassList("unity-header-drawer__label");
            container.Add(label);

            for (int i = 0; i < LocaDatabase.instance.databases.Count; i++) {
                //Header
                Label subLabel = new Label(LocaDatabase.instance.databases[i].sheetName);
                subLabel.AddToClassList("unity-base-field");
                subLabel.AddToClassList("unity-header-drawer__label");
                container.Add(subLabel);

                //Entries
                IntegerField entryCount = new IntegerField("Entries") {
                    value = LocaDatabase.instance.databases[i].locaEntries.Count
                };
                entryCount.isReadOnly = true;
                container.Add(entryCount);

                //Languages
                TextField textField = new TextField("Languages");
                textField.SetValueWithoutNotify(string.Join(", ", LocaDatabase.instance.databases[i].languages));
                textField.isReadOnly = true;
                container.Add(textField);
            }

            ScrollView scrollView = rootElement.Q<ScrollView>();

            scrollView.Add(container);
        }
    }
}
