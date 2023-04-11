using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Loca {
    [Serializable]
    public class LocaSubDatabase {
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
                        locaEntriesMapping.Add(locaEntries[i].key.ToLower(), i);
                    }
                }

                return locaEntriesMapping;
            }
        }

        public bool ExtractHeaderData(HeaderData headerData) {
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
                Debug.LogWarning($"Unable to find {LocaSettings.instance.headerSettings.keyColumnName} column in sheet {sheetName}");
                success = false;
            }

            if (timestampColumnIndex == -1) {
                Debug.LogWarning($"Unable to find {LocaSettings.instance.headerSettings.timestampColumnName} column in sheet {sheetName}");
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Upade this SubDatabase from the given new one
        /// </summary>
        /// <param name="newSubDatabase">new SubDatabase</param>
        public void Update(LocaSubDatabase newSubDatabase) {
            //check for old entries
            CleanupOldEntries(newSubDatabase._locaEntriesMapping);

            keyColumnIndex = newSubDatabase.keyColumnIndex;
            timestampColumnIndex = newSubDatabase.timestampColumnIndex;

            //check for language updated
            bool languageColumnUpdated = !languages.SequenceEqual(newSubDatabase.languages);
            languages = newSubDatabase.languages;
            languageColumnIndex = newSubDatabase.languageColumnIndex;

            //check the miscColumns aswell
            bool miscColumnUpdated = !miscs.SequenceEqual(newSubDatabase.miscs);
            miscs = newSubDatabase.miscs;
            miscColumnIndex = newSubDatabase.miscColumnIndex;

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
                    locaEntriesMapping.Add(updateLocaEntry.key.ToLower(), locaEntries.Count - 1);
                }
            } else {
                if (locaEntries[curLocaEntryIndex].timestamp < updateLocaEntry.timestamp) {
                        locaEntries[curLocaEntryIndex] = updateLocaEntry;
                } else {
                    //entry keeps the local version, so we add new languages if necessary
                    if (languageColumnUpdated) {
                        List<LocaEntry.LocaArray> entriesContentBackup = locaEntries[curLocaEntryIndex].content;

                        List<LocaEntry.LocaArray> entriesContent = new List<LocaEntry.LocaArray>();
                        for (int i = 0; i < languages.Count; i++) {
                            bool isNew = true;

                            for (int j = 0; j < entriesContentBackup.Count; j++) {
                                if (entriesContentBackup[j].languageKey == languages[i]) {
                                    isNew = false;
                                    entriesContent.Add(entriesContentBackup[j]);
                                    break;
                                }
                            }

                            //Add a placeholder if its a new language
                            if (isNew) {
                                entriesContent.Add(new LocaEntry.LocaArray {
                                    content = "",
                                    languageKey = languages[i]
                                });
                            }
                        }

                        locaEntries[curLocaEntryIndex].content = entriesContent;
                    }

                    //do the same for misc columns
                    if (miscColumnUpdated) {
                        List<LocaEntry.MiscArray> entriesContentBackup = locaEntries[curLocaEntryIndex].miscContent;

                        List<LocaEntry.MiscArray> entriesContent = new List<LocaEntry.MiscArray> ();
                        for (int i = 0; i < miscs.Count; i++) {
                            bool isNew = true;

                            for (int j = 0; j < entriesContentBackup.Count; j++) {
                                if (entriesContentBackup[j].title.ToLower() == miscs[i].ToLower()) {
                                    isNew = false;
                                    entriesContent.Add(entriesContentBackup[j]);
                                    break;
                                }
                            }

                            //Add a placeholder if its a new column
                            if (isNew) {
                                entriesContent.Add(new LocaEntry.MiscArray {
                                    content = "",
                                    title = miscs[i]
                                });
                            }
                        }

                        locaEntries[curLocaEntryIndex].miscContent = entriesContent;
                    }
                }
            }
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
                ClearEntriesMapping();
            }
        }

        #region Utils
        /// <summary>
        /// Get the index of the LocaEntry with the given key
        /// </summary>
        /// <param name="key">locakey</param>
        /// <returns>return the index of the locaentry, returns -1 if the key was not found</returns>
        private int GetLocaEntryIndex(string key) {
            return _locaEntriesMapping.TryGetValue(key.ToLower(), out int index) ? index : -1;
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
        public void ClearEntriesMapping() {
            locaEntriesMapping.Clear();
        }

        /// <summary>
        /// Checks if the given Key already exists
        /// </summary>
        /// <param name="key">key you want to check</param>
        /// <returns>true if the key already exists</returns>
        public bool KeyExists(string key) {
            return _locaEntriesMapping.ContainsKey(key.ToLower());
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

                if (jsonObject.translations.ContainsKey(locaEntries[i].key.ToLowerInvariant().GetHashCode())) {
                    Debug.Log(jsonObject.translations[locaEntries[i].key.ToLowerInvariant().GetHashCode()]);
                    Debug.Log(locaEntries[i].key.ToLowerInvariant().GetHashCode() + " - " + locaEntries[i].key);
                }

                jsonObject.translations.Add(locaEntries[i].key.ToLowerInvariant().GetHashCode(), locaEntries[i].key);
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
                jsonObject.translations.Add(locaEntries[i].key.ToLowerInvariant().GetHashCode(), locaEntries[i].content[langIndex].content);
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