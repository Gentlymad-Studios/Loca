using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace Loca {
    public class GoogleDriveHelper {
        public DriveService Service {
            get; set;
        }
        const string APPLICATION_NAME = nameof(Loca);
        static readonly string[] Scopes = { DriveService.Scope.DriveMetadataReadonly };

        public GoogleDriveHelper() {
            InitializeService();
        }

        private void InitializeService() {
            GoogleCredential credential = GoogleCredential.FromJson(LocaSettings.instance.googleSettings.secret).CreateScoped(Scopes);

            Service = new DriveService(new BaseClientService.Initializer() {
                HttpClientInitializer = credential,
                ApplicationName = APPLICATION_NAME
            });
        }
    }
}