using EditorHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Loca {
    [FilePath("ProjectSettings/" + nameof(LocaSettings) + ".asset", FilePathAttribute.Location.ProjectFolder)]
    public class LocaSettings : AdvancedSettings<LocaSettings> {
        public override string Path => "Tools/" + nameof(Loca);
        public const string MENUITEMBASE = "Tools/";

        [Header("Adapter")]
        [SerializeReference]
        public CustomAdapter customAdapter = null;

        [NonSerialized]
        private IAdapter adapter;
        public IAdapter Adapter {
            get {
                if (adapter == null) {
                    if (customAdapter == null) {
                        adapter = new DefaultAdapter();
                    } else {
                        adapter = customAdapter;
                    }
                }
                return adapter;
            }
        }

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
        [Tooltip("When false, richt tags will not be parsed for labels.")]
        public bool enableLabelRichText = false;

        [Header("Search View Settings")]
        [Tooltip("GUID of the UXML File")]
        [SerializeField]
        private string searchUxmlIdentifier = "57a565a7e0944f84aa96db85d9a8d7dc";
        public static VisualTreeAsset searchUxml => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(instance.searchUxmlIdentifier));

        [Header("Markups")]
        public List<Markup> markups = new List<Markup>();
        public List<EnclosedMarkup> enclosedMarkups = new List<EnclosedMarkup>();

        [Serializable]
        public class JsonDataSettings {
            public string exportDestination = "Package/Languages";
            public string editorExportDestination = "PackageEditor/Languages";
            [Tooltip("List of languages that should not be exported. Use language codes!")]
            public string[] ignoredLanguages;
            [Tooltip("If this is enabled, JSON of ignored Languages will be removed.")]
            public bool removeIgnoredLanguages = true;
        }

        [Serializable]
        public class GoogleDataSettings {
            [Header("Read/Write Spreadsheet")]
            [Tooltip("Secrets for the google access. Google Account need Access to Spreadsheets and the Drive. A restart or recompile may be required.")]
            [TextArea]
            public string secret = "";

            [Tooltip("ID of the Spreadsheet you want to Access")]
            public string spreadsheetId = "";

            [Tooltip("Name of Sheets you want to retrieve")]
            public List<string> sheets = new List<string>() { "All Languages" };

            [Tooltip("Request URL to recieve the LastModified Date of the given Spreadsheet. (If this is not given, we fallback to the Google Sheet Revision)")]
            public string spreadsheetLastModifiedRequest = "";

            [Tooltip("Request URL to set the LastModified Date of the given Spreadsheet.")]
            public string spreadsheetSetLastModifiedRequest = "";

            [Tooltip("Send Log if we are unable to get the LastModified Date via the Request URL.")]
            public bool logLastModifiedRequestFail = false;

            [Tooltip("If this is enabled, we use the Request URL and the Google Sheet Revision combined to check the Modified date.")]
            public bool useRequestUrlAndRevisionCheck = false;

            [Header("ReadOnly Spreadsheets")]
            [Tooltip("API Key to Access the Google Sheets API")]
            public string apikey = "";
            [Tooltip("ReadOnly Spreadsheets")]
            public List<GoogleSheet> spreadsheets = new List<GoogleSheet>();

            //Settings only store locally
            [NonSerialized]
            public int checkForModifiedInterval = 1000;
            [NonSerialized]
            public int checkForUpdateInterval = 120000;
            [NonSerialized]
            public bool autoUpdate = false;
        }

        [Serializable]
        public class GoogleSheet {
            [Tooltip("Just a custom choosen Name")]
            public string name;
            public string spreadsheetId;
            public List<string> sheets;
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

        [Serializable]
        public class Markup {
            public string name;
            public string tag;
            [Tooltip("Creates spaces around the Markup when inserted.")]
            public bool surroundingSpace = true;
            [Tooltip("The color the markup is highlighted in the Loca Manager. Only visible if the Rich Text for the Labels is enabled.")]
            public Color highlighting;
        }

        [Serializable]
        public class EnclosedMarkup {
            public string name;
            public string openingTag;
            public string closingTag;
            [Tooltip("The color the markup is highlighted in the Loca Manager. Only visible if the Rich Text for the Labels is enabled.")]
            public Color highlighting;
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
