using Google.Apis.Sheets.v4.Data;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Loca {
    public static class LocaJsonHandler {
        [MenuItem(LocaSettings.MENUITEMBASE + nameof(Loca) + "/Export to JSON", priority = 41)]
        public static void WriteLocasheets() {
            bool upToDate = LocaBase.LocalDatabaseIsUpToDate(out bool failToGetModifiedDate);
            LocaSettings.JsonDataSettings jsonSettings = LocaSettings.instance.jsonSettings;

            if (failToGetModifiedDate) {
                Debug.LogError("[Loca] Unable to reach LocaSheet modified Date");
            }

            if (!upToDate && !failToGetModifiedDate) {
                LocaBase.ExtractDatabasesFromSheets();
            }

            if (LocaDatabase.instance.databases.Count == 0) {
                Debug.LogError("[Loca] Loca Database is Empty!");
                return;
            }

            string destination = Path.Combine(Application.dataPath, jsonSettings.exportDestination);
            if (!Directory.Exists(destination)) {
                Directory.CreateDirectory(destination);
            }

            string editorDestination = Path.Combine(Application.dataPath, jsonSettings.editorExportDestination);
            if (!Directory.Exists(editorDestination)) {
                Directory.CreateDirectory(editorDestination);
            }

            List<LocaSubDatabase> databases = LocaDatabase.instance.databases;

            for (int i = 0; i < databases.Count; i++) {
                //Editor Json
                LocaModel editorJsonObject = databases[i].GetToEditorJson();
                string editorJson = JsonConvert.SerializeObject(editorJsonObject, Formatting.Indented);
                string editorJsonFilename = $"{databases[i].sheetName}_Editor.json";
                File.WriteAllText(Path.Combine(editorDestination, editorJsonFilename), editorJson);

                //Runtime Json's
                for (int lang = 0; lang < databases[i].languages.Count; lang++) {
                    string runtimeJsonFilename = $"{databases[i].sheetName}_{databases[i].languages[lang]}.json";
                    string runtimeJsonPath = Path.Combine(destination, runtimeJsonFilename);

                    if (IgnoreLanguage(databases[i].languages[lang])) {
                        if (jsonSettings.removeIgnoredLanguages && File.Exists(runtimeJsonPath)) {
                            File.Delete(runtimeJsonPath);
                        }
                        continue;
                    }

                    LocaModel runtimeJsonObject = databases[i].ToRuntimeJson(lang);
                    string runtimeJson = JsonConvert.SerializeObject(runtimeJsonObject, Formatting.Indented);
                    File.WriteAllText(runtimeJsonPath, runtimeJson);
                }
            }
        }

        private static bool IgnoreLanguage(string language) {
            CultureInfo currentCultureInfo = new CultureInfo(language);
            CultureInfo ignoredCultureInfo;

            string[] ignoredLanguages = LocaSettings.instance.jsonSettings.ignoredLanguages;

            for (int i = 0; i < ignoredLanguages.Length; i++) {
                try {
                    ignoredCultureInfo = new CultureInfo(ignoredLanguages[i]);
                } catch {
                    continue;
                }
                if (ignoredCultureInfo.Name == currentCultureInfo.Name) {
                    return true;
                }
            }

            return false;
        }
    }
}