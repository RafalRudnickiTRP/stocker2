using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

// package installation: 
// open Package Manager Console
// Install-Package Google.Apis.Drive.v3

// login: stocker.application@gmail.com
// pass: 1stocker*

namespace WpfApplication3
{
    public class Drive
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/drive-dotnet-quickstart.json
        static string[] scopes = { DriveService.Scope.Drive };
        static string applicationName = "Stocker";
        private static string currentPath = "";
        private static DriveService service = null;
        
        public static string GetPath()
        {
            string path;
            while (!MainWindow.testMode &&
            (currentPath == "" || currentPath == null))
                ChooseDefaultPath();

            if (MainWindow.testMode)
                path = @"\\samba-users.igk.intel.com\samba\Users\rrudnick\invest\stocker_test\";
            else
                path = currentPath + @"\stocker\";
            return path;
        }

        public static void ChooseDefaultPath()
        {
            // Configure the message box to be displayed
            string messageBoxText = "Use samba path?";
            string caption = "Choose default path";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Question;
            // Display message box
            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

            // Process message box results
            switch (result)
            {
                case MessageBoxResult.Yes:
                    // User pressed Yes button
                    currentPath = @"\\samba-users.igk.intel.com\samba\Users\rrudnick\invest";
                    break;
                case MessageBoxResult.No:
                    // User pressed No button
                    currentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    break;
            }
        }

        public static void SaveReportFile(string raport)
        {
            string today = DateTime.Today.ToString("dd-MM-yyyy");
            string filename = "stocker_report_" + today + ".html";

            Directory.CreateDirectory(Drive.GetPath());
            using (StreamWriter outputFile = new StreamWriter(Drive.GetPath() + filename))
            {
                outputFile.Write(raport);
            }
        }

        public static string CreateDirectory(string path)
        {
            // don't create a dir if it already extists
            string id = GetFileId(path);
            if (id != "")
                return id;

            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = path,
                MimeType = "application/vnd.google-apps.folder"
            };

            var request = service.Files.Create(fileMetadata);
            request.Fields = "id";
            var file = request.Execute();
            
            return file.Id;
        }

        public static string GetFileId(string file)
        {
            // Define parameters of request
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 10;
            listRequest.Q = "name = '" + file + "'";
            listRequest.Fields = "nextPageToken, files(id, name)";

            // List files
            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;
            if (files.Count == 0)
                return "";
            else
                return files[0].Id;
        }

        public static string UploadFile(string folderId, string filename, string content)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File();
            fileMetadata.Name = filename;
            fileMetadata.Parents = new List<string> { folderId };

            FilesResource.CreateMediaUpload request;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                request = service.Files.Create(fileMetadata, stream, "text/plain");
                request.Fields = "id";
                request.Upload();
            }

            var file = request.ResponseBody;
            return file.Id;
        }
        
        public static string DownloadFile(string fileId)
        {
            var request = service.Files.Get(fileId);
            var stream2 = new System.IO.MemoryStream();

            // Add a handler which will be notified on progress changes.
            // It will notify on each chunk download and when the
            // download is completed or failed.
            request.MediaDownloader.ProgressChanged +=
                (IDownloadProgress progress) =>
                {
                    switch (progress.Status)
                    {
                        case DownloadStatus.Downloading:
                        {
                            Console.WriteLine(progress.BytesDownloaded);
                            break;
                        }
                        case DownloadStatus.Completed:
                        {
                            Console.WriteLine(fileId + ": download complete.");
                            break;
                        }
                        case DownloadStatus.Failed:
                        {
                            Console.WriteLine(fileId + ": download failed.");
                            break;
                        }
                    }
                };

            request.Download(stream2);

            stream2.Seek(0, SeekOrigin.Begin);
            var sr = new StreamReader(stream2);
            string content = sr.ReadToEnd();
            return content;
        }

        public static DriveService CreateService()
        {
            UserCredential credential;

            using (var stream =
                new FileStream("../../client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/drive-stocker.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Drive API service
            service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });
            
            return service;
        }

        public static void Main2()
        {
            // Define parameters of request
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 10;
            listRequest.Q = "mimeType='application/vnd.google-apps.folder'";
            listRequest.Fields = "nextPageToken, files(id, name)";

            // List files
            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute()
                .Files;
            Console.WriteLine("Files:");
            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                    Console.WriteLine("{0} ({1})", file.Name, file.Id);
            }
            else
            {
                Console.WriteLine("No files found.");
            }
            Console.Read();            
        }        
    }
}
