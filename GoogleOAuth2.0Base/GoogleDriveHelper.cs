
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Threading;


    namespace Google_OAuth2
{ 
    public class GoogleDriveHelper
    {
        private const string CredentialPath = "../credentials";
        private string[] Scopes = {
                                      DriveService.Scope.Drive,
                                      DriveService.Scope.DriveFile,
                                      DriveService.Scope.DriveMetadata
                                  };
        private const string ApplicationName = "google-drive-sample";

        private UserCredential _userCredential;
        public DriveService _driveService;

        public GoogleDriveHelper()
        {

        }


        public DriveService GetAuth()
        {
            using (var stream = new FileStream("./credentials/client_secret.json", System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                var credentials = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(CredentialPath, true));
                _userCredential = credentials.Result;
                if (credentials.IsCanceled || credentials.IsFaulted)
                    throw new Exception("cannot connect");

                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = _userCredential,
                    ApplicationName = ApplicationName,
                });
            }
            return _driveService;
        }

    }
}


