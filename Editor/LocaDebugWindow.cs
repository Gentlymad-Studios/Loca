using UnityEditor;
using UnityEngine;

namespace Loca {
    public class LocaDebugWindow : EditorWindow {
        const string WINDOWNAME = "Loca Debug";

        [MenuItem(LocaSettings.MENUITEMBASE + nameof(Loca) + "/" + WINDOWNAME, priority = 41)]
        static void Init() {
            // Get existing open window or if none, make a new one:
            LocaDebugWindow window = GetWindow<LocaDebugWindow>(WINDOWNAME);
            window.name = "Loca Debug";
            window.Show();
        }

        private void OnGUI() {
            if (GUILayout.Button("Load GoogleSheet")) {
                LocaBase.ExtractDatabasesFromSheets();
            }

            if (GUILayout.Button("Export Loca to JSON's")) {
                LocaJsonHandler.WriteLocasheets();
            }

            if (GUILayout.Button("Write back to Sheet")) {
                LocaBase.SaveDatabasesToSheets();
            }

            if (GUILayout.Button("Get Modified Date")) {
                Debug.Log(GoogleLocaApi.GetSheetModifiedDate());
            }

            if (GUILayout.Button("Get Modified Date via Revisions")) {
                Debug.Log(GoogleLocaApi.GetSheetModifiedDataViaRevision());
            }

            if (GUILayout.Button("Get Modified Date via Meta")) {
                Debug.Log(GoogleLocaApi.GetSheetModifiedDateViaMeta());
            }

            if (GUILayout.Button("Clear Database")) {
                LocaBase.ClearDatabase();
                LocaDatabase.instance.Save();
            }
        }
    }
}
