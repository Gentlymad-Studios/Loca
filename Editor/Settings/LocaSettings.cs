using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Loca {
    [FilePath("ProjectSettings/" + nameof(LocaSettings) + ".asset", FilePathAttribute.Location.ProjectFolder)]
    public class LocaSettings : ScriptableSingleton<LocaSettings> {
        public const string MENUITEMBASE = "Tools/";

        [Space]
        public HeaderDataSettings headerSettings = new HeaderDataSettings();

        [Space]
        public GoogleDataSettings googleSettings = new GoogleDataSettings();

        [Space]
        public JsonDataSettings jsonSettings = new JsonDataSettings();

        [Header("Base View Settings")]
        [Tooltip("GUID of the UXML File")]
        [SerializeField]
        private string uxmlIdentifier = "1abcc2e99cae6a4498df98599603eef6";
        public static VisualTreeAsset locaUxml => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(instance.uxmlIdentifier));

        [Tooltip("GUID of the USS File")]
        [SerializeField]
        private string stylesheetIdentifier = "166e37ee41ef52744bcef4e69d192e1d";
        public static StyleSheet locaStylesheet => AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(instance.stylesheetIdentifier));

        [Tooltip("Height in px of each row. (neccessary for virtualization)")]
        public int fixedRowHeight = 50;
        [Tooltip("Initital width of each Column.")]
        public int initialColumnWidth = 300;
        [Tooltip("Fontcolor for changed Entries.")]
        public Color hightlightColor = Color.red;

        [Header("Search View Settings")]
        [Tooltip("GUID of the UXML File")]
        [SerializeField]
        private string searchUxmlIdentifier = "57a565a7e0944f84aa96db85d9a8d7dc";
        public static VisualTreeAsset searchUxml => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(instance.searchUxmlIdentifier));

        public void OnEnable() {
            hideFlags &= ~HideFlags.NotEditable;
        }

        [Serializable]
        public class JsonDataSettings {
            public string exportDestination = "Package/Languages";
            public string editorExportDestination = "PackageEditor/Languages";
        }

        [Serializable]
        public class GoogleDataSettings {
            [Tooltip("Secrets for the google access. Google Account need Access to Spreadsheets and the Drive. A restart or recompile may be required.")]
            [TextArea]
            public string secret = "";

            [Header("Spreadsheet")]
            [Tooltip("ID of the Spreadsheet you want to Access")]
            public string spreadsheetId = "";

            [Tooltip("Name of Sheets you want to retrieve")]
            public List<string> sheets = new List<string>() { "All Languages" };

            [Tooltip("Request URL to recieve the LastModified Date of the given Spreadsheet.")]
            public string spreadsheetLastModifiedRequest = "";

            [Tooltip("Request URL to set the LastModified Date of the given Spreadsheet.")]
            public string spreadsheetSetLastModifiedRequest = "";

            //Settings only store locally
            [NonSerialized]
            public int checkForModifiedInterval = 1000;
            [NonSerialized]
            public int checkForUpdateInterval = 120000;
            [NonSerialized]
            public bool autoUpdate = false;
        }

        [Serializable]
        public class HeaderDataSettings {
            public string timestampColumnDisplayName = "Timestamp";
            public string timestampColumnName = "*timestamp";
            public string timestampColumnTooltip = "The timestamp of the last change to any content/language.";
            public string keyColumnDisplayName = "Key";
            public string keyColumnName = "key";
            public string keyColumnTooltip = "The unique identifier string.";
            public string languageColumnTooltip = "The localized content for a specific language.";
            public LanguageScaffold[] languageScaffolds;
            [Serializable]
            public class LanguageScaffold {
                public string columnName = "";
                public string validLanguageName = "";
            }
        }

        [Serializable]
        public class LanguageColumn {
            public LanguageColumn(int columnIndex, CultureInfo language) {
                this.columnIndex = columnIndex;
                Language = language;
            }

            public string languageCode;
            public CultureInfo Language {
                get {
                    return _language;
                }
                set {
                    languageCode = value.IetfLanguageTag;
                    _language = value;
                }
            }
            private CultureInfo _language;
            public int columnIndex;
        }

        public void Save() {
            Save(true);
        }

        /// <summary>
        /// Set some variables from EditorPrefs
        /// </summary>
        public void LoadEditorPrefs() {
            int checkForModifiedInterval = EditorPrefs.GetInt(nameof(LocaSettings) + "_" + nameof(googleSettings.checkForModifiedInterval));
            if (checkForModifiedInterval == 0) {
                EditorPrefs.SetInt(nameof(LocaSettings) + "_" + nameof(googleSettings.checkForModifiedInterval), googleSettings.checkForModifiedInterval);
            } else {
                googleSettings.checkForModifiedInterval = checkForModifiedInterval;
            }

            int checkForUpdateInterval = EditorPrefs.GetInt(nameof(LocaSettings) + "_" + nameof(googleSettings.checkForUpdateInterval));
            if (checkForUpdateInterval == 0) {
                EditorPrefs.SetInt(nameof(LocaSettings) + "_" + nameof(googleSettings.checkForUpdateInterval), googleSettings.checkForUpdateInterval);
            } else {
                googleSettings.checkForUpdateInterval = checkForUpdateInterval;
            }

            googleSettings.autoUpdate = EditorPrefs.GetBool(nameof(LocaSettings) + "_" + nameof(googleSettings.autoUpdate));
        }

        /// <summary>
        /// Store some variables to EditorPrefs
        /// </summary>
        public void SaveEditorPrefs() {
            EditorPrefs.SetInt(nameof(LocaSettings) + "_" + nameof(googleSettings.checkForModifiedInterval), googleSettings.checkForModifiedInterval);
            EditorPrefs.SetInt(nameof(LocaSettings) + "_" + nameof(googleSettings.checkForUpdateInterval), googleSettings.checkForUpdateInterval);
            EditorPrefs.SetBool(nameof(LocaSettings) + "_" + nameof(googleSettings.autoUpdate), googleSettings.autoUpdate);
        }
    }
}
