using System;
using UnityEngine;

namespace Loca {
    [Serializable]
    public abstract class CustomAdapter : ScriptableObject, IAdapter {
        public abstract bool ValidateEntry(LocaEntry entry, bool log);
    }
}