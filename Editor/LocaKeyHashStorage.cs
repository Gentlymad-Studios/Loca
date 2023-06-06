using System.Collections.Generic;
using System.Linq;

namespace Loca {
    public class LocaKeyHashStorage {
        //array of names we want to Display
        public static string[] displayArray;
        //HASH : KEY Dictionary
        public static Dictionary<int, string> hashKeyDict = new Dictionary<int, string>();
        //HASH : INDEX Dictionary
        public static Dictionary<int, int> hashIndexDict = new Dictionary<int, int>();
        //Multiple Dicts unless we want the performance instead of the memory

        public static void Initialize() {
            List<LocaEntry> allKeys = new List<LocaEntry>();
            for (int i = 0; i < LocaDatabase.instance.databases.Count; i++) {
                for (int j = 0; j < LocaDatabase.instance.databases[i].locaEntries.Count; j++) {
                    allKeys.Add(LocaDatabase.instance.databases[i].locaEntries[j]);
                }
            }

            hashKeyDict.Clear();
            hashIndexDict.Clear();
            displayArray = new string[allKeys.Count + 1]; //+1 because of our fallback (None) field

            //Set first one to None / 0
            hashKeyDict.Add(0, "None");
            hashIndexDict.Add(0, 0);
            displayArray[0] = "None";

            for (int i = 0; i < allKeys.Count; i++) {
                LocaEntry entry = allKeys[i];

                hashKeyDict.Add(entry.Hash, entry.key);
                hashIndexDict.Add(entry.Hash, i + 1);
                displayArray[i + 1] = entry.key;
            }
        }

        public static void Clear() {
            hashKeyDict.Clear();
            hashIndexDict.Clear();
            displayArray = new string[0];
        }

        #region Public Getter
        public static string[] GetDisplayArray() {
            if (displayArray.Length == 0) {
                Initialize();
            }

            return displayArray;
        }

        public static int GetHashFromIndex(int index) {
            if (hashKeyDict.Count == 0) {
                Initialize();
            }

            return hashKeyDict.ElementAt(index).Key;
        }

        public static int GetIndexFromHash(int hash) {
            if (hashIndexDict.Count == 0) {
                Initialize();
            }

            hashIndexDict.TryGetValue(hash, out int value);
            return value;
        }

        public static string GetStringFromHash(int hash) {
            if (hashKeyDict.Count == 0) {
                Initialize();
            }

            hashKeyDict.TryGetValue(hash, out string result);

            if (!string.IsNullOrEmpty(result)) {
                return result;
            }

            return null;
        }

        public static string GetStringFromIndex(int index) {
            if (displayArray.Length == 0) {
                Initialize();
            }

            return displayArray[index];
        }

        public static int GetHashFromString(string value) {
            for (int i = 0; i < displayArray.Length; i++) {
                if (value == displayArray[i]) {
                    return GetHashFromIndex(i);
                }
            }

            return 0;
        }
        #endregion
    }
}
