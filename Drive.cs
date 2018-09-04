using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        private static DriveService service = null;

        public static void SaveFile(string text, string filename)
        {
            string folderId = CreateDirectory("temp");
            string fileId = GetFileId(filename);
            if (fileId != "")
                DeleteFile(filename);

            UploadFile(folderId, filename, text);
        }

        public static void SaveReportFile(string raport)
        {
            string today = DateTime.Today.ToString("dd-MM-yyyy");
            string filename = "stocker_report_" + today + ".html";

            SaveFile(raport, filename);
        }

        public static string CreateDirectory(string path)
        {
            // don't create a dir if it already exists
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

        public static string GetFileId(string filename)
        {
            // Define parameters of request
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 10;
            listRequest.Q = "name = '" + filename + "'";
            listRequest.Fields = "nextPageToken, files(id, name)";

            // List files
            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute().Files;
            if (files.Count == 0)
                return "";
            else
                return files[0].Id;
        }

        public static string RenameFile(string folderId, string filename, string newFilename)
        {
            string fileId = Drive.GetFileId(filename);

            if (fileId == "")
            {
                Console.WriteLine("Tried to rename file {0}, but it was not found!", filename, newFilename);
                return fileId;
            }

            var fileMetadata = new Google.Apis.Drive.v3.Data.File();
            fileMetadata.Name = newFilename;
            var request = service.Files.Update(fileMetadata, fileId);
            request.Execute();

            Console.WriteLine("Renamed: {0} to {1}", filename, newFilename);

            return fileId;
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

                Console.WriteLine("Uploaded: {0}", filename);
            }

            var file = request.ResponseBody;
            return file.Id;
        }

        public static void DeleteFile(string filename)
        {
            string fileId = Drive.GetFileId(filename);
            var request = service.Files.Delete(fileId);
            request.Execute();

            Console.WriteLine("Deleted: {0}", filename);
        }

        public static string DownloadFile(string fileId, string filename = "")
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
                            //Console.WriteLine("Download completed: {0}", filename);
                            break;
                        }
                        case DownloadStatus.Failed:
                        {
                            Console.WriteLine("Download failed: {0}", filename);
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

            /*
            // Slack example
            // curl - X POST - H 'Content-type: application/json'--data '{"text":"Hello, World!"}' 
            // https://hooks.slack.com/services/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

            var webAddr = "https://hooks.slack.com/services/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";
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);
            httpWebRequest.ContentType = "application/json; charset=utf-8";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                string json = "{\"text\":\"Hello, World!\"}";

                streamWriter.Write(json);
                streamWriter.Flush();
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
            }
            */

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
