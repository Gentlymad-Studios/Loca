using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static Loca.LocaSettings;
using static Loca.HeaderData;

namespace Loca {
    public static class LocaBase {
        public static bool currentlyUpdating = false;
        //private static HeaderData headerData;

        private static void ForEachCellOfRow(Action<object, int> onElement, IList<object> row) {
            for (int j = 0; j < row.Count; j++) {
                onElement(row[j], j);
            }
        }

        private static void ForEachCellOfRow(Action<HeaderData, object, int> onElement, IList<object> row, HeaderData headerData) {
            for (int j = 0; j < row.Count; j++) {
                onElement(headerData, row[j], j);
            }
        }

        private static void ExtractHeaderData(HeaderData headerData, object cellData, int columnIndex) {
            headerData.Extract(((string)cellData), columnIndex);
        }

        /// <summary>
        /// Check if the local database is older then the online database
        /// </summary>
        /// <returns>True if Local Database is already UpToDate</returns>
        public static bool LocalDatabaseIsUpToDate(out bool fail) {
            long onlineDate = GoogleLocaApi.GetSheetModifiedDate();

            if (onlineDate == -1) {
                //optional we could provide a fallback to antoher GetSheetModifiedDate method, best case would be the revision method, but we need to implement pagenation correctly (slow)
                fail = true;
            } else {
                fail = false;
            }

            return onlineDate <= LocaDatabase.instance.lastModifiedOnline;
        }

        /// <summary>
        /// Clears the complete Database
        /// </summary>
        public static void ClearDatabase() {
            LocaDatabase.instance.Reset();
        }

        #region Update DB to Sheet
        /// <summary>
        /// Saving our Database back to the GoogleSheet, an Update will be performed if necessary. Double check modify dates after we Updated
        /// </summary>
        /// <returns></returns>
        public static bool SaveDatabasesToSheets() {
            bool upToDate = false;

            currentlyUpdating = true;

            //========== Check #01 ==========
            //Check if local version is newer, if not, we update it first
            upToDate = LocalDatabaseIsUpToDate(out bool failToGetModifiedDate);

            if (failToGetModifiedDate) {
                Debug.Log("Loca Sheet save was aborted...unable to reach the modifiedDate.");
                currentlyUpdating = false;
                return false;
            }

            if (!upToDate) {
                Debug.Log("Loca Database got changes, update will be performend...");
                ExtractDatabasesFromSheets();
            }

            //========== Check #02 ==========
            //Check again if the local version is newer, if not anyone else also pushed in this short time so we just break up....retry should performed manually
            upToDate = LocalDatabaseIsUpToDate(out failToGetModifiedDate);

            if (failToGetModifiedDate) {
                Debug.Log("Loca Sheet save was aborted...unable to reach the modifiedDate.");
                currentlyUpdating = false;
                return false;
            }

            if (!upToDate) {
                Debug.Log("Loca Sheet got an last minute update, please retry manually...");
                currentlyUpdating = false;
                return false;
            }

            //========== Save ==========
            //Collect values for changed entries
            for (int i = 0; i < LocaDatabase.instance.databases.Count; i++) {
                GoogleLocaApi.SetSheet(LocaDatabase.instance.databases[i].sheetName, LocaDatabase.instance.databases[i].ToValueRangeValue());
                LocaDatabase.instance.databases[i].ResetChangesFlag();
            }

            currentlyUpdating = false;
            LocaDatabase.instance.hasLocalChanges = false;

            return true;
        }
        #endregion

        #region Full Sheet Extraction
        /// <summary>
        /// Extract Database from each Google Sheet given in our Settings
        /// </summary>
        public static void ExtractDatabasesFromSheets() {
            Debug.Log("Start extracting Loca from GoogleSheet...");

            List<LocaSubDatabase> subDatabases = new List<LocaSubDatabase>();

            for (int i = 0; i < LocaSettings.instance.googleSettings.sheets.Count; i++) {
                if (string.IsNullOrEmpty(LocaSettings.instance.googleSettings.sheets[i])) {
                    continue;
                }

                //Create Database from each GoogleSheet
                LocaSubDatabase newSubDatabase = ExtractSubDatabaseFromSheet(false, LocaSettings.instance.googleSettings.spreadsheetId, LocaSettings.instance.googleSettings.sheets[i]);

                if (newSubDatabase != null) {
                    subDatabases.Add(newSubDatabase);
                } else {
                    Debug.Log($"No data in \"{LocaSettings.instance.googleSettings.sheets[i]}\" found.");
                }
            }

            LocaDatabase database = LocaDatabase.instance;
            database.UpdateDatabase(subDatabases);
            database.lastModifiedOnline = GoogleLocaApi.GetSheetModifiedDate();

            Debug.Log("Loca from GoogleSheet extracted...");
        }

        /// <summary>
        /// Extract Database from each ReadOnly Google Sheet given in our Settings
        /// </summary>
        public static void ExtractDatabasesFromReadOnlySheets() {
            Debug.Log("Start extracting Loca from ReadOnly GoogleSheet...");

            List<LocaSubDatabase> subDatabases = new List<LocaSubDatabase>();

            for (int i = 0; i < LocaSettings.instance.googleSettings.spreadsheets.Count; i++) {
                GoogleSheet spreadsheet = LocaSettings.instance.googleSettings.spreadsheets[i];

                if (string.IsNullOrEmpty(spreadsheet.spreadsheetId)) {
                    continue;
                }

                for (int j = 0; j < spreadsheet.sheets.Count; j++) {
                    if (string.IsNullOrEmpty(spreadsheet.sheets[j])) {
                        continue;
                    }

                    //Create Database from each GoogleSheet
                    LocaSubDatabase newSubDatabase = ExtractSubDatabaseFromSheet(true, spreadsheet.spreadsheetId, spreadsheet.sheets[j]);
                    newSubDatabase.name = spreadsheet.name;
                    newSubDatabase.isReadOnly = true;

                    if (newSubDatabase != null) {
                        subDatabases.Add(newSubDatabase);
                    } else {
                        Debug.Log($"No data in \"{spreadsheet.sheets[j]}\" found.");
                    }
                }
            }

            LocaDatabase database = LocaDatabase.instance;
            database.readOnlyDatabases = subDatabases;

            Debug.Log("Loca from ReadOnly GoogleSheet extracted...");
        }

        private static LocaSubDatabase ExtractSubDatabaseFromSheet(bool readOnly, string spreadsheetId, string sheetNameAndRange) {
            IList<IList<object>> values = GoogleLocaApi.GetSheet(readOnly, spreadsheetId, sheetNameAndRange);

            IList<object> row, dataRow;
            object cell;

            LocaSubDatabase newSubDatabase = new LocaSubDatabase();
            newSubDatabase.sheetName = sheetNameAndRange;

            if (values != null && values.Count > 0) {
                dataRow = values[0];

                HeaderData headerData = new HeaderData();
                headerData.Initialize();
                ForEachCellOfRow(ExtractHeaderData, dataRow, headerData);
                newSubDatabase.ExtractHeaderData(headerData, !readOnly);

                if (!headerData.Valid(out string error, !readOnly)) {
                    Debug.Log($"{error} Sheet: {sheetNameAndRange}");
                    return null;
                }

                if (values.Count > 1) {
                    for (int i = 1; i < values.Count; i++) {
                        row = values[i];

                        LocaEntry locaEntry = new LocaEntry();

                        //Key
                        locaEntry.key = (string)row[headerData.keyColumnIndex];

                        //Timestamp
                        if (headerData.timestampColumnsIndex < row.Count && headerData.timestampColumnsIndex > 0) {
                            locaEntry.timestamp = long.Parse((string)row[headerData.timestampColumnsIndex]);
                        } else {
                            locaEntry.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }

                        //Languages
                        List<string> languages = new List<string>();
                        LanguageColumn languageColumn;
                        for (int j = 0; j < headerData.detectedLanguages.Count; j++) {
                            languageColumn = headerData.detectedLanguages[j];

                            if (languageColumn.columnIndex < row.Count) {
                                cell = row[languageColumn.columnIndex];
                            } else {
                                cell = "";
                            }

                            languages.Add(languageColumn.languageCode);

                            locaEntry.content.Add(new LocaEntry.LocaArray() { content = (string)cell, languageKey = languageColumn.languageCode });
                        }
                        newSubDatabase.languages = languages;

                        //Misc
                        List<string> miscs = new List<string>();
                        MiscColumn miscColumn;
                        for (int j = 0; j < headerData.miscColumns.Count; j++) {
                            miscColumn = headerData.miscColumns[j];

                            if (miscColumn.columnIndex < row.Count) {
                                cell = row[miscColumn.columnIndex];
                            } else {
                                cell = "";
                            }

                            miscs.Add(miscColumn.title);

                            locaEntry.miscContent.Add(new LocaEntry.MiscArray() { content = (string)cell, title = miscColumn.title });
                        }
                        newSubDatabase.miscs = miscs;

                        newSubDatabase.locaEntries.Add(locaEntry);
                    }
                }
                return newSubDatabase;

            } else {
                return null;
            }
        }
        #endregion

        #region Key Extraction
        /// <summary>
        /// Extract Keys from each Google Sheet given in our Settings 
        /// </summary>
        public static void ExtractLocaKeysFromSheets() {
            Debug.Log("Start extracting LocaKeys from GoogleSheet...");

            List<LocaSubDatabase> subDatabases = new List<LocaSubDatabase>();

            for (int i = 0; i < LocaSettings.instance.googleSettings.sheets.Count; i++) {
                if (string.IsNullOrEmpty(LocaSettings.instance.googleSettings.sheets[i])) {
                    continue;
                }

                LocaSubDatabase newSubDatabase = ExtractLocaKeysFromSheet(false, LocaSettings.instance.googleSettings.spreadsheetId, LocaSettings.instance.googleSettings.sheets[i]);

                if (newSubDatabase != null) {
                    subDatabases.Add(newSubDatabase);
                } else {
                    Debug.Log($"No data in \"{LocaSettings.instance.googleSettings.sheets[i]}\" found.");
                }
            }

            LocaDatabase database = LocaDatabase.instance;
            database.UpdateDatabase(subDatabases);
        }

        /// <summary>
        /// Extract Keys from each Google Sheet given in our Settings 
        /// </summary>
        public static void ExtractLocaKeysFromReadOnlySheets() {
            Debug.Log("Start extracting LocaKeys from GoogleSheet...");

            List<LocaSubDatabase> subDatabases = new List<LocaSubDatabase>();

            for (int i = 0; i < LocaSettings.instance.googleSettings.spreadsheets.Count; i++) {
                GoogleSheet spreadsheet = LocaSettings.instance.googleSettings.spreadsheets[i];

                if (string.IsNullOrEmpty(spreadsheet.spreadsheetId)) {
                    continue;
                }

                for (int j = 0; j < spreadsheet.sheets.Count; j++) {
                    if (string.IsNullOrEmpty(spreadsheet.sheets[j])) {
                        continue;
                    }

                    //Create Database from each GoogleSheet
                    LocaSubDatabase newSubDatabase = ExtractLocaKeysFromSheet(true, spreadsheet.spreadsheetId, spreadsheet.sheets[j]);
                    newSubDatabase.name = spreadsheet.name;
                    newSubDatabase.isReadOnly = true;

                    if (newSubDatabase != null) {
                        subDatabases.Add(newSubDatabase);
                    } else {
                        Debug.Log($"No data in \"{spreadsheet.sheets[j]}\" found.");
                    }
                }
            }

            LocaDatabase database = LocaDatabase.instance;
            database.readOnlyDatabases = subDatabases;
        }

        private static LocaSubDatabase ExtractLocaKeysFromSheet(bool readOnly, string spreadsheetid, string sheetNameAndRange) {
            HeaderData headerData = ExtractHeaderDataOnly(readOnly, spreadsheetid, sheetNameAndRange);
            string col = ((char)(headerData.keyColumnIndex + 65)).ToString();

            IList<IList<object>> values = GoogleLocaApi.GetSheet(readOnly, spreadsheetid, sheetNameAndRange + $"!{col}:{col}");

            LocaSubDatabase newSubDatabase = new LocaSubDatabase();
            newSubDatabase.sheetName = sheetNameAndRange;

            newSubDatabase.ExtractHeaderData(headerData, !readOnly);

            if (!headerData.Valid(out string error, !readOnly)) {
                Debug.Log($"{error} Sheet: {sheetNameAndRange}");
                return null;
            }

            if (values != null && values.Count > 0) {
                if (values.Count > 1) {
                    for (int i = 1; i < values.Count; i++) {
                        LocaEntry locaEntry = new LocaEntry();

                        //Key
                        locaEntry.key = (string)values[i][0];

                        //Timestamp
                        locaEntry.timestamp = 0;

                        //Languages
                        List<string> languages = new List<string>();
                        LanguageColumn languageColumn;
                        for (int j = 0; j < headerData.detectedLanguages.Count; j++) {
                            languageColumn = headerData.detectedLanguages[j];
                            languages.Add(languageColumn.languageCode);

                            locaEntry.content.Add(new LocaEntry.LocaArray() { content = "", languageKey = languageColumn.languageCode });
                        }
                        newSubDatabase.languages = languages;

                        //Misc
                        List<string> miscs = new List<string>();
                        MiscColumn miscColumn;
                        for (int j = 0; j < headerData.miscColumns.Count; j++) {
                            miscColumn = headerData.miscColumns[j];
                            miscs.Add(miscColumn.title);

                            locaEntry.miscContent.Add(new LocaEntry.MiscArray() { content = "", title = miscColumn.title });
                        }
                        newSubDatabase.miscs = miscs;

                        newSubDatabase.locaEntries.Add(locaEntry);
                    }
                }

                return newSubDatabase;

            } else {
                return null;
            }
        }

        /// <summary>
        /// Request the First Line of the given sheet und generate the headerdata
        /// </summary>
        private static HeaderData ExtractHeaderDataOnly(bool readOnly, string spreadsheetId, string sheetNameAndRange) {
            HeaderData headerData = new HeaderData();

            IList<IList<object>> values = GoogleLocaApi.GetSheet(readOnly, spreadsheetId, sheetNameAndRange + "!1:1");

            IList<object> dataRow;

            if (values != null && values.Count > 0) {
                dataRow = values[0];

                headerData.Initialize();
                ForEachCellOfRow(ExtractHeaderData, dataRow, headerData);
            }

            return headerData;
        }
        #endregion
    }

    //use this callback to save our database when project is saved, because we dont save after polling the database in background
    public class GentlyLocaSaveCallback : AssetModificationProcessor {
        static string[] OnWillSaveAssets(string[] paths) {
            LocaDatabase.instance.Save();
            return paths;
        }
    }
}
