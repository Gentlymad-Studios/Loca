using Google;
using System;
using System.Net;
using System.Net.Http;
using System.Timers;
using UnityEditor;
using UnityEngine;

namespace Loca {
    public static class LocaBackgroundWorker {
        //will be set true if the backgroundworker failed, reset on setting change
        public static bool apiFailed = false;
        private static bool connectionFailed = false;
        private static Timer checkModifiedTimer, checkUpdateTimer;

        public static string locaStatus = "";

        /// <summary>
        /// Initialize the "BackgroundWorker"
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize() {
            LocaSettings.instance.LoadEditorPrefs();

            if (LocaDatabase.instance.databases.Count == 0) {
                Debug.Log("Loca Keys will be initialized...");
                LocaBase.ExtractLocaKeysFromSheets();
            }

            //Start the timer to check for modified frequently
            checkModifiedTimer = new Timer {
                Interval = LocaSettings.instance.googleSettings.checkForModifiedInterval
            };

            checkModifiedTimer.Enabled = true;
            checkModifiedTimer.Elapsed += CheckModified;


            //Start the timer to check for updates frequently
            checkUpdateTimer = new Timer {
                Interval = LocaSettings.instance.googleSettings.checkForUpdateInterval
            };

            checkUpdateTimer.Enabled = LocaSettings.instance.googleSettings.autoUpdate;
            checkUpdateTimer.Elapsed += CheckUpdate;
        }

        private static void CheckModified(object source, ElapsedEventArgs e) {
            if (!ReadyToCheck()) {
                return;
            }

            checkModifiedTimer.Interval = LocaSettings.instance.googleSettings.checkForModifiedInterval;
            checkUpdateTimer.Enabled = LocaSettings.instance.googleSettings.autoUpdate;

            if (LocaBase.currentlyUpdating) {
                return;
            }

            try {
                LocaBase.currentlyUpdating = true;


                LocaDatabase.instance.hasOnlineChanges = !LocaBase.LocalDatabaseIsUpToDate(out bool failToGetModifiedDate);



                if (LocaDatabase.instance.hasOnlineChanges) {
                    locaStatus = "<color=red>online changes found</color>";
                } else {
                    locaStatus = "<color=green>no online changes found</color>";
                }

                if (failToGetModifiedDate) {
                    locaStatus = "Unable to reach LocaSheet modified Date";
                    Debug.LogWarning("Unable to reach LocaSheet modified Date");
                }

                LocaBase.currentlyUpdating = false;

            } catch (GoogleApiException ex) {
                //...wrong spreadsheet id
                apiFailed = true;
                Debug.LogWarning(ex);
            } catch (InvalidOperationException ex) {
                //...error in secret
                apiFailed = true;
                Debug.LogWarning(ex);
            } catch (FormatException ex) {
                //...error in secret
                apiFailed = true;
                Debug.LogWarning(ex);
            } catch (HttpRequestException ex) {
                connectionFailed = true;
                Debug.LogWarning(ex);
            } catch (Exception ex) {
                Debug.LogWarning(ex);
            }
        }

        private static void CheckUpdate(object source, ElapsedEventArgs e) {
            if (!ReadyToCheck()) {
                return;
            }

            checkUpdateTimer.Interval = LocaSettings.instance.googleSettings.checkForUpdateInterval;

            if (LocaBase.currentlyUpdating || !LocaSettings.instance.googleSettings.autoUpdate || LocaDatabase.instance.isInEditMode) {
                return;
            }

            try {
                LocaDatabase.instance.hasOnlineChanges = LocaBase.LocalDatabaseIsUpToDate(out bool failToGetModifiedDate);

                if (failToGetModifiedDate) {
                    Debug.LogWarning("Unable to reach LocaSheet modified Date");
                    return;
                }

                if (!LocaDatabase.instance.hasOnlineChanges) {
                    Debug.Log("Loca Database is out of date and will be updated in background...");

                    LocaBase.currentlyUpdating = true;
                    LocaBase.ExtractDatabasesFromSheets();
                    LocaBase.currentlyUpdating = false;
                }
            } catch (GoogleApiException ex) {
                //...wrong spreadsheet id
                apiFailed = true;
                Debug.LogWarning(ex);
            } catch (InvalidOperationException ex) {
                //...error in secret
                apiFailed = true;
                Debug.LogWarning(ex);
            } catch (FormatException ex) {
                //...error in secret
                apiFailed = true;
                Debug.LogWarning(ex);
            } catch (HttpRequestException ex) {
                connectionFailed = true;
                Debug.LogWarning(ex);
            } catch (Exception ex) {
                Debug.LogError(ex);
            }
        }

        private static bool ReadyToCheck() {
            if (string.IsNullOrEmpty(LocaSettings.instance.googleSettings.secret)) {
                return false;
            }

            if (string.IsNullOrEmpty(LocaSettings.instance.googleSettings.spreadsheetId)) {
                return false;
            }

            if (apiFailed) {
                return false;
            }

            if (connectionFailed) {
                connectionFailed = !CheckInternetConnection();
                return !connectionFailed;
            }

            return true;
        }

        /// <summary>
        /// Check Internet Connection
        /// </summary>
        /// <returns>Return true if connection is possible</returns>
        public static bool CheckInternetConnection() {
            try {
                using (var client = new WebClient())
                using (var stream = client.OpenRead("http://www.google.com")) {
                    return true;
                }
            } catch {
                return false;
            }
        }
    }
}
