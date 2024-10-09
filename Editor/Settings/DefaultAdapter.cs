using System.Collections.Generic;
using UnityEngine;

namespace Loca {
    public class DefaultAdapter : IAdapter {
        public bool ValidateEntry(LocaEntry entry, bool log) {
            return true;
        }

        public bool SaveEntry(LocaEntry entry, LocaEntry.LocaArray entryLocaArray = null) {
            return true;
        }

        public bool TryEnterEditMode(LocaEntry entry, LocaEntry.LocaArray entryLocaArray = null) {
            return true;
        }
    }
}