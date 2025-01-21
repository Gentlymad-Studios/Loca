using System.Collections.Generic;

namespace Loca {
    public class LocaSearch {
        List<LocaSearchEntry> entries = new List<LocaSearchEntry>();
        int current = 0;
        LocaSubDatabase database;

        public LocaSearch(LocaSubDatabase database) {
            this.database = database;
            current = 0;
            entries.Clear();
        }

        public void Search(string searchTerm) {
            current = 0;
            entries.Clear();

            if (!string.IsNullOrWhiteSpace(searchTerm)) {
                entries = database.GetFilteredListOfEntries(searchTerm);
            }
        }

        public void Search(int hash) {
            current = 0;
            entries.Clear();

            LocaSearchEntry entry = database.GetLocaSearchEntryByHash(hash);

            if(entry != null) {
                entries.Add(entry);
            }
        }

        public LocaSearchEntry Current() {
            if (entries.Count != 0) {
                return entries[current];
            } else {
                return null;
            }
        }

        public LocaSearchEntry Next() {
            if (current == entries.Count - 1) {
                current = 0;
            } else {
                current++;
            }

            return entries[current];
        }

        public LocaSearchEntry Previous() {
            if (current == 0) {
                current = entries.Count - 1;
            } else {
                current--;
            }

            return entries[current];
        }

        public int GetSearchEntryCount() {
            return entries.Count;
        }
    }

    public class LocaSearchEntry {
        public LocaSubDatabase database;
        public LocaEntry entry;
        public int index;

        public LocaSearchEntry(LocaSubDatabase database, LocaEntry entry, int index) {
            this.database = database;
            this.entry = entry;
            this.index = index;
        }
    }
}