using System.Collections.Generic;
using UnityEngine;

namespace Loca {
    public class DefaultAdapter : IAdapter {
        public bool ValidateEntry(LocaEntry entry, bool log) {
            return true;
        }
    }
}