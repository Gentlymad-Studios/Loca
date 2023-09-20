using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Loca {
    public class LocaNewEntryPopup : EditorWindow {
        TextField keyInput;
        Button createButton;
        string output = string.Empty;
        LocaSubDatabase database;
        static Vector2 windowSize = new Vector2(250, 70);

        void CreateGUI() {
            rootVisualElement.style.marginBottom = 2;
            rootVisualElement.style.marginLeft = 2;
            rootVisualElement.style.marginRight = 2;
            rootVisualElement.style.marginTop = 5;

            Label description = new Label("Enter LocaKey:");
            description.style.marginLeft = 2;

            keyInput = new TextField();
            keyInput.RegisterValueChangedCallback(KeyInput_changed);

            createButton = new Button();
            createButton.text = "Create Entry";
            createButton.style.flexGrow = 1;
            createButton.style.width = new StyleLength(100);
            createButton.clicked += CreateButton_clicked;
            createButton.SetEnabled(false);

            Button cancelButton = new Button();
            cancelButton.text = "Cancel";
            cancelButton.style.flexGrow = 1;
            cancelButton.style.width = new StyleLength(100);
            cancelButton.clicked += CancelButton_clicked;

            VisualElement buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            buttonContainer.Add(createButton);
            buttonContainer.Add(cancelButton);

            rootVisualElement.Add(description);
            rootVisualElement.Add(keyInput);
            rootVisualElement.Add(buttonContainer);

            keyInput.Focus();
        }

        private void KeyInput_changed(ChangeEvent<string> evt) {
            if (string.IsNullOrEmpty(evt.newValue)) {
                createButton.SetEnabled(false);
                return;
            } 

            if (database.KeyExists(evt.newValue)) {
                createButton.SetEnabled(false);
                return;
            }

            createButton.SetEnabled(true);
        }

        private void CancelButton_clicked() {
            output = string.Empty;
            Close();
        }

        private void CreateButton_clicked() {
            output = keyInput.text;
            Close();
        }

        public static string Initialize(LocaSubDatabase database) {
            LocaNewEntryPopup window = CreateInstance<LocaNewEntryPopup>();
            window.database = database;
            window.maxSize = windowSize;
            window.minSize = windowSize;
            window.titleContent = new GUIContent("New LocaKey");
            window.position = new Rect(new Vector2(Screen.width / 2, Screen.height / 2), windowSize);
            window.ShowModalUtility();

            return window.output;
        }
    }
}