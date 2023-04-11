using UnityEditor;
using UnityEngine;

namespace Loca {
    public class LocaDebugWindow : EditorWindow {
        [MenuItem(LocaSettings.menuItemBase + nameof(LocaDebugWindow))]
        static void Init() {
            // Get existing open window or if none, make a new one:
            LocaDebugWindow window = (LocaDebugWindow)GetWindow(typeof(LocaDebugWindow));
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

            if (GUILayout.Button("Clear Database")) {
                LocaBase.ClearDatabase();
                LocaDatabase.instance.Save();
            }
        }
    }
}
