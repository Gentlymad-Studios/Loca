using UnityEditor;
using UnityEngine;

namespace Loca {
    public class LocaDebugWindow : EditorWindow {
        const string WINDOWNAME = "Loca Debug";

        [MenuItem(LocaSettings.MENUITEMBASE + nameof(Loca) + "/Debug", priority = 42)]
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
                Debug.Log("[Loca] " + GoogleLocaApi.GetSheetModifiedDate());
            }

            if (GUILayout.Button("Get Modified Date via Request URL")) {
                Debug.Log("[Loca] " + GoogleLocaApi.GetSheetModifiedDateViaCustomWebRequest(out _));
            }

            if (GUILayout.Button("Get Modified Date via Revisions")) {
                Debug.Log("[Loca] " + GoogleLocaApi.GetSheetModifiedDataViaRevision(out _));
            }

            if (GUILayout.Button("Get Modified Date via Meta")) {
                Debug.Log("[Loca] " + GoogleLocaApi.GetSheetModifiedDateViaMeta(out _));
            }

            if (GUILayout.Button("Clear Database")) {
                LocaBase.ClearDatabase();
                LocaDatabase.instance.Save();
            }
        }
    }
}
