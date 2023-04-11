using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace Loca {
    public class GoogleSheetsHelper {
        public SheetsService Service { get; set; }
        const string APPLICATION_NAME = nameof(Loca);
        static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };

        public GoogleSheetsHelper() {
            InitializeService();
        }

        private void InitializeService() {
            GoogleCredential credential = GoogleCredential.FromJson(LocaSettings.instance.googleSettings.secret).CreateScoped(Scopes);

            Service = new SheetsService(new BaseClientService.Initializer() {
                HttpClientInitializer = credential,
                ApplicationName = APPLICATION_NAME
            });
        }
    }
}
