using System;
using System.Collections.Generic;

namespace Loca {
    [Serializable]
    public class LocaEntry {
        public string key;
        public List<LocaArray> content = new List<LocaArray>();
        public List<MiscArray> miscContent = new List<MiscArray>();

        public bool hasKeyChanges = false;
        public bool hasGlobalChanges = false;

        public int Hash {
            get {
                return key.ToLowerInvariant().GetHashCode();
            }
        }

        [Serializable]
        public class LocaArray {
            public string languageKey;
            public string content;
            public bool hasChanges = false;
        }

        [Serializable]
        public class MiscArray {
            public string title;
            public string content;
        }

        public long timestamp;

        public void EntryUpdated() {
            hasGlobalChanges = true;

            //update timestamp
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            LocaDatabase.instance.lastModifiedLocal = timestamp;
            LocaDatabase.instance.hasLocalChanges = true;
        }

        public void ReorganizeLocaArray(List<string> languages) {
            List<LocaArray> oldContent = content;

            List<LocaArray> newContent = new List<LocaArray>();
            for (int i = 0; i < languages.Count; i++) {
                bool isNew = true;

                for (int j = 0; j < oldContent.Count; j++) {
                    if (oldContent[j].languageKey == languages[i]) {
                        isNew = false;
                        newContent.Add(oldContent[j]);
                        break;
                    }
                }

                //Add a placeholder if its a new language
                if (isNew) {
                    newContent.Add(new LocaArray {
                        content = "",
                        languageKey = languages[i]
                    });
                }
            }

            content = newContent;
        }

        public void ReorganizeMiscArray(List<string> miscs) {
            List<MiscArray> oldContent = miscContent;

            List<MiscArray> newContent = new List<MiscArray>();
            for (int i = 0; i < miscs.Count; i++) {
                bool isNew = true;

                for (int j = 0; j < oldContent.Count; j++) {
                    if (oldContent[j].title.ToLower() == miscs[i].ToLower()) {
                        isNew = false;
                        newContent.Add(oldContent[j]);
                        break;
                    }
                }

                //Add a placeholder if its a new column
                if (isNew) {
                    newContent.Add(new MiscArray {
                        content = "",
                        title = miscs[i]
                    });
                }
            }

            miscContent = newContent;
        }

        public bool IsComplete(string languages) {
            for (int i = 0; i < content.Count; i++) {
                if (languages == "All Languages" || languages == content[i].languageKey) {
                    if (string.IsNullOrEmpty(content[i].content) || string.IsNullOrWhiteSpace(content[i].content)) {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}