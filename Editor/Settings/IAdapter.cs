using UnityEngine.UIElements;

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

        /// <summary>
        /// Fires when the Edit mode would be triggered
        /// </summary>
        /// <param name="entry">The Entry that will be editted</param>
        /// <param name="entryLocaArray">The Language Entry that will be edited, is null if the edit would a key change</param>
        /// <returns>true or false if the edit mode should open or not</returns>
        bool TryEnterEditMode(LocaEntry entry, LocaEntry.LocaArray entryLocaArray = null);

        /// <summary>
        /// Overwrite the CellLabelStyle
        /// </summary>
        /// <param name="label"></param>
        /// <param name="entry"></param>
        /// <param name="entryLocaArray"></param>
        /// <returns>if false is returned, a default font style is applied</returns>
        bool OverwriteCellLabelStyle(Label label, LocaEntry entry, LocaEntry.LocaArray entryLocaArray = null);
    }
}