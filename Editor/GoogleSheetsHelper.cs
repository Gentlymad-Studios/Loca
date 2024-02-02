using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace Loca {
    public class GoogleSheetsHelper {
        public enum AuthenticationMethod {
            ApiKey,
            OAuth2
        }

        public SheetsService ServiceOAuth2 {
            get; set;
        }
        public SheetsService ServiceApiKey {
            get; set;
        }
        const string APPLICATION_NAME = nameof(Loca);
        static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };

        public GoogleSheetsHelper() {
            InitializeServices();
        }

        private void InitializeServices() {
            if (!string.IsNullOrEmpty(LocaSettings.instance.googleSettings.secret)) {
                GoogleCredential credential = GoogleCredential.FromJson(LocaSettings.instance.googleSettings.secret).CreateScoped(Scopes);

                ServiceOAuth2 = new SheetsService(new BaseClientService.Initializer() {
                    HttpClientInitializer = credential,
                    ApplicationName = APPLICATION_NAME
                });
            }

            if (!string.IsNullOrEmpty(LocaSettings.instance.googleSettings.apikey)) {
                ServiceApiKey = new SheetsService(new BaseClientService.Initializer() {
                    ApplicationName = APPLICATION_NAME,
                    ApiKey = LocaSettings.instance.googleSettings.apikey
                });
            }
        }
    }
}
