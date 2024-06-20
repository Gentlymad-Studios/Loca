namespace Loca {
    public interface IAdapter {
        /// <summary>
        /// Checks if the Entry is Valid for Export
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        bool ValidateEntry(LocaEntry entry, bool log);
    }
}