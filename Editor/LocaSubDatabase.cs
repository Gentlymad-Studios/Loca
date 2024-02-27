using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Loca {
    [Serializable]
    public class LocaSubDatabase {
        public bool isReadOnly;
        public string name = "Main";
        public string sheetName;

        public List<string> languages = new List<string>();
        public List<string> miscs = new List<string>();

        public int keyColumnIndex;
        public int timestampColumnIndex;
        public List<int> languageColumnIndex;
        public List<int> miscColumnIndex;

        public List<LocaEntry> locaEntries = new List<LocaEntry>();

        private Dictionary<string, int> locaEntriesMapping = new Dictionary<string, int>();
        private Dictionary<string, int> _locaEntriesMapping {
            get {
                if (locaEntriesMapping.Count == 0) {
                    for (int i = 0; i < locaEntries.Count; i++) {
                        locaEntriesMapping.Add(locaEntries[i].key.ToLowerInvariant(), i);
                    }
                }

                return locaEntriesMapping;
            }
        }

        public bool ExtractHeaderData(HeaderData headerData, bool useTimestamp) {
            bool success = true;

            keyColumnIndex = headerData.keyColumnIndex;
            timestampColumnIndex = headerData.timestampColumnsIndex;

            languageColumnIndex = new List<int>();
            for (int i = 0; i < headerData.detectedLanguages.Count; i++) {
                languageColumnIndex.Add(headerData.detectedLanguages[i].columnIndex);
            }

            miscColumnIndex = new List<int>();
            for (int i = 0; i < headerData.miscColumns.Count; i++) {
                miscColumnIndex.Add(headerData.miscColumns[i].columnIndex);
            }

            if (keyColumnIndex == -1) {
                Debug.LogWarning($"[Loca] Unable to find {LocaSettings.instance.headerSettings.keyColumnName} column in sheet {sheetName}");
                success = false;
            }

            if (timestampColumnIndex == -1 && useTimestamp) {
                Debug.LogWarning($"[Loca] Unable to find {LocaSettings.instance.headerSettings.timestampColumnName} column in sheet {sheetName}");
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Update this SubDatabase from the given new one
        /// </summary>
        /// <param name="newSubDatabase">new SubDatabase</param>
        public void Update(LocaSubDatabase newSubDatabase) {
            //check for old entries
            CleanupOldEntries(newSubDatabase._locaEntriesMapping);

            //keyColumnIndex = newSubDatabase.keyColumnIndex;
            //timestampColumnIndex = newSubDatabase.timestampColumnIndex;

            //check for language updated
            bool languageColumnUpdated = !languages.SequenceEqual(newSubDatabase.languages);

            //check the miscColumns aswell
            bool miscColumnUpdated = !miscs.SequenceEqual(newSubDatabase.miscs);

            //Iterate through each LocaEntry
            for (int i = 0; i < newSubDatabase.locaEntries.Count; i++) {
                UpdateLocaEntry(newSubDatabase.locaEntries[i], languageColumnUpdated, miscColumnUpdated);
            }
        }

        /// <summary>
        /// Update or Add the given LocaEntry
        /// </summary>
        /// <param name="updateLocaEntry">new Loca Entry</param>
        private void UpdateLocaEntry(LocaEntry updateLocaEntry, bool languageColumnUpdated, bool miscColumnUpdated) {
            //Try to get the corresponding LocaEntry Index in our current database
            int curLocaEntryIndex = GetLocaEntryIndex(updateLocaEntry.key);

            //If LocaEntry not exists, add directly
            if (curLocaEntryIndex == -1) {
                //only add new entry if the timestamp is newer then our last online poll
                if (updateLocaEntry.timestamp > LocaDatabase.instance.lastModifiedOnline) {
                    locaEntries.Add(updateLocaEntry);
                    locaEntriesMapping.Add(updateLocaEntry.key.ToLowerInvariant(), locaEntries.Count - 1);
                }
            } else {
                if (locaEntries[curLocaEntryIndex].timestamp < updateLocaEntry.timestamp) {
                        locaEntries[curLocaEntryIndex] = updateLocaEntry;
                } else {
                    //entry keeps the local version, so we add new languages if necessary
                    if (languageColumnUpdated) {
                        locaEntries[curLocaEntryIndex].ReorganizeLocaArray(languages);
                    }

                    //do the same for misc columns
                    if (miscColumnUpdated) {
                        locaEntries[curLocaEntryIndex].ReorganizeMiscArray(miscs);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a new LocaEntry if not already exists, columns will be reorganized
        /// </summary>
        /// <param name="locaEntry"></param>
        public void AddLocaEntry(LocaEntry locaEntry) {
            //Try to get the corresponding LocaEntry Index in our current database
            int curLocaEntryIndex = GetLocaEntryIndex(locaEntry.key);

            if (curLocaEntryIndex == -1) {
                locaEntry.ReorganizeLocaArray(languages);
                locaEntry.ReorganizeMiscArray(miscs);
                locaEntry.EntryUpdated();

                locaEntries.Add(locaEntry);
                locaEntriesMapping.Add(locaEntry.key.ToLowerInvariant(), locaEntries.Count - 1);
                LocaKeyHashStorage.Clear();
            }
        }

        /// <summary>
        /// Creates a new LocaEntry from the given key name
        /// </summary>
        /// <param name="key"></param>
        public bool CreateLocaEntry(string key) {
            if (!KeyExists(key) && !string.IsNullOrEmpty(key)) {
                //create new one
                LocaEntry locaEntry = new LocaEntry();
                locaEntry.key = key;

                locaEntry.ReorganizeLocaArray(languages);
                locaEntry.ReorganizeMiscArray(miscs);
                locaEntry.EntryUpdated();

                locaEntries.Add(locaEntry);
                locaEntriesMapping.Add(locaEntry.key.ToLowerInvariant(), locaEntries.Count - 1);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the given LocaEntry
        /// </summary>
        /// <param name="locaEntry"></param>
        public void RemoveLocaEntry(LocaEntry locaEntry) {
            locaEntries.Remove(locaEntry);
            ClearEntriesMappingAndStorage();
        }

        /// <summary>
        /// Remove old LocaEntries
        /// </summary>
        /// <param name="newLocaEntriesMapping"></param>
        private void CleanupOldEntries(Dictionary<string, int> newLocaEntriesMapping) {
            Dictionary<string, int> mappingToRemove = new Dictionary<string, int>();

            foreach (KeyValuePair<string, int> entryMapping in _locaEntriesMapping) {
                //does the new mapping contains the key of the old one? if not, remove it
                if (!newLocaEntriesMapping.ContainsKey(entryMapping.Key)) {
                    //check if the entrie got modified, after our last online poll, if not it should be deleted
                    if (locaEntries[entryMapping.Value].timestamp < LocaDatabase.instance.lastModifiedOnline) {
                        mappingToRemove.Add(entryMapping.Key, entryMapping.Value);
                    }
                }
            }

            foreach (KeyValuePair<string, int> remove in mappingToRemove.Reverse()) {
                locaEntries.RemoveAt(remove.Value);
            }

            ClearEntriesMappingAndStorage();
        }

        #region Utils
        /// <summary>
        /// Get the index of the LocaEntry with the given key
        /// </summary>
        /// <param name="key">locakey</param>
        /// <returns>return the index of the locaentry, returns -1 if the key was not found</returns>
        private int GetLocaEntryIndex(string key) {
            return _locaEntriesMapping.TryGetValue(key.ToLowerInvariant(), out int index) ? index : -1;
        }

        /// <summary>
        /// Return LocaEntry with the given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LocaEntry GetLocaEntry(string key) {
            if (_locaEntriesMapping.TryGetValue(key.ToLowerInvariant(), out int index)) {
                return locaEntries[index];
            }

            return null;
        }

        /// <summary>
        /// Return LocaEntry with the given Hash
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        public LocaSearchEntry GetLocaSearchEntryByHash(int hash) {
            for (int i = 0; i < locaEntries.Count; i++) {
                if (locaEntries[i].Hash == hash) {
                    return new LocaSearchEntry(this, locaEntries[i], i);
                }
            }
            return null;
        }

        /// <summary>
        /// Sort SubDatabase by Key
        /// </summary>
        /// <param name="sortedColumns"></param>
        public void Sort(IEnumerable<UnityEngine.UIElements.SortColumnDescription> sortedColumns) {
            if (sortedColumns.First().direction == UnityEngine.UIElements.SortDirection.Ascending) {
                locaEntries.Sort((p1, p2) => p1.key.CompareTo(p2.key));
            } else {
                locaEntries.Sort((p1, p2) => p2.key.CompareTo(p1.key));
            }

            locaEntriesMapping.Clear();
        }

        /// <summary>
        /// Clear LocaEntriesMapping
        /// </summary>
        public void ClearEntriesMappingAndStorage() {
            locaEntriesMapping.Clear();
            LocaKeyHashStorage.Clear();
        }

        /// <summary>
        /// Checks if the given Key already exists
        /// </summary>
        /// <param name="key">key you want to check</param>
        /// <returns>true if the key already exists</returns>
        public bool KeyExists(string key) {
            return _locaEntriesMapping.ContainsKey(key.ToLowerInvariant());
        }

        /// <summary>
        /// Resets the HasChanges Flag of each Entry
        /// </summary>
        public void ResetChangesFlag() {
            for (int i = 0; i < locaEntries.Count; i++) {
                locaEntries[i].hasGlobalChanges = false;
                locaEntries[i].hasKeyChanges = false;
                for (int j = 0; j < locaEntries[i].content.Count; j++) {
                    locaEntries[i].content[j].hasChanges = false;
                }
            }
        }

        /// <summary>
        /// Returns a list of entries which keys contains the given term
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        public List<LocaSearchEntry> GetFilteredListOfEntries(string term) {
            List<LocaSearchEntry> filteredEntries = new List<LocaSearchEntry>();

            term = term.ToLowerInvariant();

            foreach (KeyValuePair<string, int> mapping in _locaEntriesMapping) {
                if (mapping.Key.ToLowerInvariant().Contains(term)) {
                    filteredEntries.Add(new LocaSearchEntry(this, locaEntries[mapping.Value], mapping.Value));
                }
            }

            return filteredEntries;
        }

        /// <summary>
        /// Fills the list with entries which keys contains the given term
        /// </summary>
        /// <param name="term"></param>
        /// <param name="filteredEntries"></param>
        public void FillFilteredListOfEntries(string term, bool emptyOnly, string language, ref List<LocaEntry> filteredEntries) {
            filteredEntries.Clear();

            term = term.ToLowerInvariant();

            for (int i = 0; i < locaEntries.Count; i++) {
                LocaEntry entry = locaEntries[i];

                if (entry.key.ToLowerInvariant().Contains(term)) {
                    if (!emptyOnly || !entry.IsComplete(language)) {
                        filteredEntries.Add(locaEntries[i]);
                    }
                } else if (entry.ContentContains(language, term)) {
                    if (!emptyOnly || !entry.IsComplete(language)) {
                        filteredEntries.Add(locaEntries[i]);
                    }
                }
            }
        }
        #endregion

        #region JSON Methods
        /// <summary>
        /// Generate the JsonObject of the LocaDatabase for Editor Use
        /// </summary>
        /// <returns></returns>
        public LocaModel GetToEditorJson() {
            LocaModel jsonObject = new LocaModel();
            jsonObject.translations = new Dictionary<int, string>();

            for (int i = 0; i < locaEntries.Count; i++) {
                if (string.IsNullOrEmpty(locaEntries[i].key)) {
                    Debug.LogError("[Loca] " + locaEntries[i].Hash + " has an no Key and was skipped!");
                    continue;
                }

                if (jsonObject.translations.ContainsKey(locaEntries[i].Hash)) {
                    Debug.Log("[Loca] " + jsonObject.translations[locaEntries[i].Hash]);
                    Debug.Log("[Loca] " + locaEntries[i].Hash + " - " + locaEntries[i].key);
                }

                jsonObject.translations.Add(locaEntries[i].Hash, locaEntries[i].key);
            }

            return jsonObject;
        }

        /// <summary>
        /// Generate the JsonObject of the LocaDatabase for Runtime use
        /// </summary>
        /// <param name="langIndex">Index of the Language (eq. to the languages List Index)</param>
        /// <returns></returns>
        public LocaModel ToRuntimeJson(int langIndex) {
            LocaModel jsonObject = new LocaModel();
            jsonObject.translations = new Dictionary<int, string>();

            for (int i = 0; i < locaEntries.Count; i++) {
                if (string.IsNullOrEmpty(locaEntries[i].key)) {
                    Debug.LogError("[Loca] " + locaEntries[i].Hash + " has an no Key and was skipped!");
                    continue;
                }

                jsonObject.translations.Add(locaEntries[i].Hash, locaEntries[i].content[langIndex].content);
            }

            return jsonObject;
        }
        #endregion

        #region ValueRange Methods
        public List<IList<object>> ToValueRangeValue() {
            List<IList<object>> value = new List<IList<object>>();

            //optional we could add the header here aswell, so we later got the option add columns easly inside unity

            int columnCount = GetLastColumnIndex();

            //Create Header
            List<object> header = new List<object>(new object[columnCount + 1]);
            header[keyColumnIndex] = LocaSettings.instance.headerSettings.keyColumnName;
            header[timestampColumnIndex] = LocaSettings.instance.headerSettings.timestampColumnName;

            for (int i = 0; i < languageColumnIndex.Count; i++) {
                header[languageColumnIndex[i]] = languages[i];
            }

            for (int i = 0; i < miscColumnIndex.Count; i++) {
                header[miscColumnIndex[i]] = miscs[i];
            }
            value.Add(header);

            //Add Entries
            for (int i = 0; i < locaEntries.Count; i++) {
                List<object> entry = new List<object>(new object[columnCount + 1]);

                LocaEntry locaEntry = locaEntries[i];

                entry[keyColumnIndex] = locaEntry.key;
                entry[timestampColumnIndex] = locaEntry.timestamp;

                for (int j = 0; j < languageColumnIndex.Count; j++) {
                    entry[languageColumnIndex[j]] = locaEntry.content[j].content;
                }

                for (int j = 0; j < miscColumnIndex.Count; j++) {
                    entry[miscColumnIndex[j]] = locaEntry.miscContent[j].content;
                }

                value.Add(entry);
            }

            return value;
        }

        private int GetLastColumnIndex() {
            int index = -1;

            index = index < keyColumnIndex ? keyColumnIndex : index;

            index = index < timestampColumnIndex ? timestampColumnIndex : index;

            for (int i = 0; i < languageColumnIndex.Count; i++) {
                index = index < languageColumnIndex[i] ? languageColumnIndex[i] : index;
            }

            for (int i = 0; i < miscColumnIndex.Count; i++) {
                index = index < miscColumnIndex[i] ? miscColumnIndex[i] : index;
            }

            return index;
        }
        #endregion
    }

}