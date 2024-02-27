using Google.Apis.Drive.v3.Data;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Loca {
    public static class GoogleLocaApi {
        private static GoogleSheetsHelper sheetsHelper;
        private static GoogleDriveHelper driveHelper;

        /// <summary>
        /// Get the LastModified Date
        /// </summary>
        /// <returns>UTC Time in milliseconds. -1 if something fails</returns>
        public static long GetSheetModifiedDate() {
            long date = GetSheetModifiedDateViaCustomWebRequest();

            if (date != -1) {
                return date;
            }

            date = GetSheetModifiedDataViaRevision();

            if (date != -1) {
                UnityEngine.Debug.Log("<color=red>[Loca] Get LastModified Date via WebRequest failed...fallback to Google Sheet Revision.</color>");
                return date;
            }

            UnityEngine.Debug.Log("<color=red>[Loca] Get LastModified Date failed...maybe you are offline</color>");
            return -1;
        }

        /// <summary>
        /// Get the LastModified Date via dedicated WebRequest
        /// </summary>
        /// <returns>UTC Time in milliseconds. -1 if something fails</returns>
        public static long GetSheetModifiedDateViaCustomWebRequest() {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(LocaSettings.instance.googleSettings.spreadsheetLastModifiedRequest);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Timeout = 4000;

            try {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream)) {
                    string content = reader.ReadToEnd();
                    return long.Parse(content);
                }
            } catch (WebException e) {
                if (e.Status == WebExceptionStatus.Timeout) {
                    //UnityEngine.Debug.Log("Timeout");
                }
                return -1;
            }
        }

        /// <summary>
        /// Get the LastModified Date of the Spreadsheet, max 20.000 calls in 100 seconds
        /// </summary>
        /// <returns>UTC Time in milliseconds</returns>
        public static long GetSheetModifiedDateViaMeta() {
            if (driveHelper == null) {
                driveHelper = new GoogleDriveHelper();
            }

            Google.Apis.Drive.v3.FilesResource.GetRequest request = driveHelper.Service.Files.Get(LocaSettings.instance.googleSettings.spreadsheetId);
            request.Fields = "modifiedTime";

            Google.Apis.Drive.v3.Data.File response = request.Execute();

            if (response != null) {
                return (long)Convert.ToDateTime(response.ModifiedTimeRaw).ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            } else {
                return -1;
            }
        }

        /// <summary>
        /// Get the LastModified Date of the Spreadsheet via Activity, max 100.000 calls in 60 seconds
        /// </summary>
        /// <returns>UTC Time in milliseconds</returns>
        public static long GetSheetModifiedDataViaRevision() {
            if (driveHelper == null) {
                driveHelper = new GoogleDriveHelper();
            }

            RevisionList response = GetNewestRevision(null);

            if (response == null) {
                return -1;
            }

            if (response.Revisions == null) {
                return -1;
            }

            if (response.Revisions.Count != 0) {
                return (long)Convert.ToDateTime(response.Revisions[response.Revisions.Count - 1].ModifiedTimeRaw).ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            } else {
                return -1;
            }
        }

        /// <summary>
        /// Return newest revision via pagination
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static RevisionList GetNewestRevision(string token) {
            Google.Apis.Drive.v3.RevisionsResource.ListRequest request = driveHelper.Service.Revisions.List(LocaSettings.instance.googleSettings.spreadsheetId);
            request.PageToken = token;
            request.PageSize = 1000;

            try {
                RevisionList response = request.Execute();

                if (response.NextPageToken == null) {
                    return response;
                }

                return GetNewestRevision(response.NextPageToken);
            } catch {
                return null;
            }
        }

        /// <summary>
        /// Get the Values of the given Spreadsheet, max 300 calls in 60 seconds
        /// </summary>
        /// <param name="sheetNameAndRange">Name and Range of the Spreadsheet you want to get. Default will be the mainsheet you set in the Settings.</param>
        /// <returns>Values of the SheetResponse</returns>
        public static IList<IList<object>> GetSheet(bool readOnly, string spreadsheetId, string sheetNameAndRange) {
            if (sheetsHelper == null) {
                sheetsHelper = new GoogleSheetsHelper();
            }

            SpreadsheetsResource.ValuesResource.GetRequest request = null;

            if (readOnly) {
                request = sheetsHelper.ServiceApiKey.Spreadsheets.Values.Get(spreadsheetId, sheetNameAndRange);
            } else {
                request = sheetsHelper.ServiceOAuth2.Spreadsheets.Values.Get(spreadsheetId, sheetNameAndRange);
            }

            try {
                ValueRange response = request.Execute();
                return response.Values;
            }catch(Exception e) {
                UnityEngine.Debug.LogError(e);
                return null;
            }
        }

        /// <summary>
        /// Write to Sheet 
        /// </summary>
        /// <param name="sheetNameAndRange"></param>
        /// <param name="values"></param>
        public static void SetSheet(string sheetName, List<IList<object>> values) {
            if (sheetsHelper == null) {
                sheetsHelper = new GoogleSheetsHelper();
            }

            //Clear Sheet
            SpreadsheetsResource.ValuesResource.ClearRequest clearRequest = sheetsHelper.ServiceOAuth2.Spreadsheets.Values.Clear(new ClearValuesRequest(), LocaSettings.instance.googleSettings.spreadsheetId, sheetName);
            clearRequest.Execute();

            //Set Values
            ValueRange valueRange = new ValueRange { Values = values };
            SpreadsheetsResource.ValuesResource.UpdateRequest request = sheetsHelper.ServiceOAuth2.Spreadsheets.Values.Update(valueRange, LocaSettings.instance.googleSettings.spreadsheetId, sheetName);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            request.Execute();

            SetSheetChanged();
        }

        /// <summary>
        /// Set Google Sheet Last Modified Date via dedicated WebRequest
        /// </summary>
        public static void SetSheetChanged() {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(LocaSettings.instance.googleSettings.spreadsheetSetLastModifiedRequest);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.Timeout = 2000;
            request.Method = "POST";

            try {
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                }
            } catch (WebException e) {
                if (e.Status == WebExceptionStatus.Timeout) {
                    //UnityEngine.Debug.Log("Timeout");
                }
            } 
        }
    }
}