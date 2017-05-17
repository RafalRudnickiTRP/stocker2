using Google.Apis.Auth.OAuth2;
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

namespace WpfApplication3
{
    public class Drive
    {
        // If modifying these scopes, delete your previously saved credentials
        // at ~/.credentials/drive-dotnet-quickstart.json
        static string[] scopes = { DriveService.Scope.Drive };
        static string applicationName = "Stocker";
        private static string currentPath = "";

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

        public static void Main2()
        {
            UserCredential credential;

            using (var stream =
                new FileStream("../../client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/drive-dotnet-quickstart.json");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Drive API service
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });

            // Define parameters of request
            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.PageSize = 10;
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
