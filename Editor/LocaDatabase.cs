using System.Collections.Generic;
using UnityEditor;

namespace Loca {
    [FilePath("Library/Gentlymad/" + nameof(LocaDatabase) + ".asset", FilePathAttribute.Location.ProjectFolder)]
    public class LocaDatabase : ScriptableSingleton<LocaDatabase> {
        public bool hasOnlineChanges = false;
        public bool hasLocalChanges = false;
        public bool isInEditMode = false;
        public long lastModifiedOnline = 0;
        public long lastModifiedLocal = 0;
        public List<LocaSubDatabase> databases = new List<LocaSubDatabase>();

        /// <summary>
        /// Save the LocaDatabase to Disk
        /// </summary>
        public void Save() {
            Save(true);
        }

        /// <summary>
        /// Resets the whole Database
        /// </summary>
        public void Reset() {
            hasOnlineChanges = false;
            hasLocalChanges = false;
            isInEditMode = false;
            lastModifiedOnline = 0;
            lastModifiedLocal = 0;
            databases.Clear();
        }

        /// <summary>
        /// Update this Database from the given new one
        /// </summary>
        /// <param name="newDatabases">List of the new Subdatabases</param>
        public void UpdateDatabase(List<LocaSubDatabase> newDatabases) {
            if (databases == null) {
                databases = newDatabases;
                hasOnlineChanges = false;
                return;
            }

            //Remove the Databases we dont need anymore
            CleanupOldDatabases(newDatabases);

            for (int i = 0; i < newDatabases.Count; i++) {
                bool isNew = true;

                for (int j = 0; j < databases.Count; j++) {
                    if (newDatabases[i].sheetName == databases[j].sheetName) {
                        //Update the database
                        newDatabases[i].Update(databases[j]);
                        databases[j] = newDatabases[i];

                        isNew = false;
                        break;
                    }

                }

                //...its a new one, so we add it directly
                if (isNew) {
                    databases.Add(newDatabases[i]);
                }
            }

            hasOnlineChanges = false;
        }

        /// <summary>
        /// Return the First LocaEntry found in all databases
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LocaEntry GetLocaEntry(string key) {
            for (int i = 0; i < databases.Count; i++) {
                LocaEntry entry = databases[i].GetLocaEntry(key);
                if (entry != null) {
                    return entry;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a list of entries which keys contains the given term un all databases
        /// </summary>
        /// <param name="term"></param>
        /// <returns></returns>
        public List<LocaSearchEntry> GetFilteredListOfEntries(string term) {
            List<LocaSearchEntry> filteredEntries = new List<LocaSearchEntry>();

            for (int i = 0; i < databases.Count; i++) {
                filteredEntries.AddRange(databases[i].GetFilteredListOfEntries(term));
            }
            
            return filteredEntries;
        }

        /// <summary>
        /// Remove old Databases (Sheets) based on the newly polled databases
        /// </summary>
        /// <param name="newDatabases">new databases</param>
        private void CleanupOldDatabases(List<LocaSubDatabase> newDatabases) {
            List<LocaSubDatabase> oldDatabases = new List<LocaSubDatabase>();

            //Check if the current databases already exists in our update
            for (int i = 0; i < databases.Count; i++) {
                bool isOld = true;

                for (int j = 0; j < newDatabases.Count; j++) {
                    if (databases[i].sheetName == newDatabases[j].sheetName) {
                        isOld = false;
                        break;
                    }
                }

                if (isOld) {
                    oldDatabases.Add(databases[i]);
                }
            }

            //Remove the old ones
            for (int i = 0; i < oldDatabases.Count; i++) {
                databases.Remove(oldDatabases[i]);
            }
        }
    }
}