namespace Loca {
    public interface IAdapter {
        /// <summary>
        /// Checks if the Entry is Valid for Export
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        bool ValidateEntry(LocaEntry entry, bool log);

        /// <summary>
        /// Fires when an Entry was edited and before its saved
        /// </summary>
        /// <param name="entry">The Entry that changed</param>
        /// <param name="entryLocaArray">The Language Entry that changed, is null if the change is a key change</param>
        /// <returns>true or false if its will be saved</returns>
        bool SaveEntry(LocaEntry entry, LocaEntry.LocaArray entryLocaArray = null);
    }
}