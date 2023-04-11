using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Loca {
    [Serializable]
    public class LocaEntry {
        public string key;
        public List<LocaArray> content = new List<LocaArray>();
        public List<MiscArray> miscContent = new List<MiscArray>();

        public bool hasKeyChanges = false;
        public bool hasGlobalChanges = false;

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
    }
}