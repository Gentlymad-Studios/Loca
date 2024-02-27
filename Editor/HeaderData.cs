using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using static Loca.LocaSettings;

namespace Loca {

    public class HeaderData {

        public int keyColumnIndex, timestampColumnsIndex;
        public List<LanguageColumn> detectedLanguages = new List<LanguageColumn>();
        public List<MiscColumn> miscColumns = new List<MiscColumn>();
        private CultureInfo[] cultures = null;
        private string[] cultureNamesLowercase = null;
        private CultureInfo detectedCultureInfo = null;

        public void Initialize() {
            miscColumns.Clear();
            detectedLanguages.Clear();
            keyColumnIndex = -1;
            timestampColumnsIndex = -1;
            cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            cultureNamesLowercase = new string[cultures.Length];
            for (int i = 0; i < cultures.Length; i++) {
                cultureNamesLowercase[i] = cultures[i].EnglishName.ToLower();
            }
        }

        public void Extract(string cellData, int columnIndex) {
            if (cellData != null) {
                cellData = cellData.ToLower();

                // check if we have a key column
                if (cellData == LocaSettings.instance.headerSettings.keyColumnName) {
                    keyColumnIndex = columnIndex;

                // check if we have a timestamp column
                } else if (cellData == LocaSettings.instance.headerSettings.timestampColumnName) {
                    timestampColumnsIndex = columnIndex;

                // we don't have a control column so we force to retrieve a language column
                } else {
                    detectedCultureInfo = null;
                    try {
                        detectedCultureInfo = new CultureInfo(cellData);
                    } catch (Exception) {
                        //Debug.Log(e.Message);
                    }

                    if (detectedCultureInfo == null) {
                        for (int i = 0; i < cultures.Length; i++) {
                            if (cultureNamesLowercase[i] == cellData.ToLower()) {
                                Debug.Log("[Loca] " + cultureNamesLowercase[i] + " " + cellData.ToLower() + " " + cultures[i].DisplayName);
                                detectedCultureInfo = cultures[i];
                                break;
                            }
                        }
                    }

                    if (detectedCultureInfo != null && !detectedLanguages.Exists(_ => _.Language.Equals(detectedCultureInfo))) {
                        detectedLanguages.Add(new LanguageColumn(columnIndex,detectedCultureInfo));
                    } else {
                        if (detectedCultureInfo != null) {
                            Debug.Log($"[Loca] Detected duplicate language items in data header! Language: {detectedCultureInfo.EnglishName}");
                        } else {
                            //Debug.Log($"Unable to retrieve language for column: {cellData}");

                            //no language column, store as misc column
                            miscColumns.Add(new MiscColumn(columnIndex, cellData));
                        }
                    }
                }
            }
        }

        public bool Valid(out string error, bool useTimestamp) {
            bool valid = true;
            error = "";

            if (keyColumnIndex == -1) {
                error = $"Unable to find {LocaSettings.instance.headerSettings.keyColumnName}.";
                valid = false;
            }

            if (timestampColumnsIndex == -1 && useTimestamp) {
                error = $"Unable to find {LocaSettings.instance.headerSettings.timestampColumnName}.";
                valid = false;
            }

            return valid;
        }

        public class MiscColumn {
            public int columnIndex;
            public string title;

            public MiscColumn(int columnIndex, string title) {
                this.columnIndex = columnIndex;
                this.title = title;
            }
        }
    }
}
