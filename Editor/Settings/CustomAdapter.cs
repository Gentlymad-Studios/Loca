using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Loca {
    [Serializable]
    public abstract class CustomAdapter : ScriptableObject, IAdapter {
        public abstract bool ValidateEntry(LocaEntry entry, bool log);

        public abstract bool SaveEntry(LocaEntry entry, LocaEntry.LocaArray entryLocaArray = null);

        public abstract bool TryEnterEditMode(LocaEntry entry, LocaEntry.LocaArray entryLocaArray = null);

        public abstract bool OverwriteCellLabelStyle(Label label, LocaEntry entry, LocaEntry.LocaArray entryLocaArray = null);
    }
}