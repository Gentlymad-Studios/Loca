using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Loca {
    public static class LocaJsonHandler {

        public static void WriteLocasheets() {
            bool upToDate = LocaBase.LocalDatabaseIsUpToDate(out bool failToGetModifiedDate);

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

            string destination = Path.Combine(Application.dataPath, LocaSettings.instance.jsonSettings.exportDestination);
            if (!Directory.Exists(destination)) {
                Directory.CreateDirectory(destination);
            }

            string editorDestination = Path.Combine(Application.dataPath, LocaSettings.instance.jsonSettings.editorExportDestination);
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
                    LocaModel runtimeJsonObject = databases[i].ToRuntimeJson(lang);
                    string runtimeJson = JsonConvert.SerializeObject(runtimeJsonObject, Formatting.Indented);
                    string runtimeJsonFilename = $"{databases[i].sheetName}_{databases[i].languages[lang]}.json";
                    File.WriteAllText(Path.Combine(destination, runtimeJsonFilename), runtimeJson);
                }
            }
        }
    }
}