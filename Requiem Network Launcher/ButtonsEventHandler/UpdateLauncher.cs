using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Bleak;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net;
using Ionic.Zip;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Drawing;


namespace Requiem_Network_Launcher
{
    public partial class MainWindow
    {

        private string _launcherUpdatePath;

        private async void UpdateLauncherButton_Click(object sender, RoutedEventArgs e)
        {

            _launcherUpdatePath = System.IO.Path.Combine(rootDirectory, "LauncherUpdate.zip");

            Dispatcher.Invoke((Action)(() =>
            {
                // display updating status notice
                ProgressBar.Visibility = Visibility.Visible;
                WarningBox.Text = "Downloading new launcher...";
                WarningBox.Foreground = new SolidColorBrush(Colors.LawnGreen);

                DisableAllButtons();

            }));
            
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    var uri = new Uri("http://requiemnetwork.com/launcher/update.zip");
                    Console.WriteLine(uri);
                    webClient.DownloadProgressChanged += NewWebClient_DownloadProgressChanged;
                    webClient.DownloadFileCompleted += NewWebClient_DownloadFileCompleted;
                    await webClient.DownloadFileTaskAsync(uri, _launcherUpdatePath);
                }
            }
            catch (Exception e1)
            {
                System.Windows.MessageBox.Show(e1.Message, "Connection error");

                Dispatcher.Invoke((Action)(() =>
                {
                    WarningBox.Text = e1.Message;
                    WarningBox.Foreground = new SolidColorBrush(Colors.Red);
                }));
            }
        }

        private void NewWebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {

            new Thread(delegate ()
            {
                // extract update to main game folder and overwrite all existing files
                using (ZipFile zip = ZipFile.Read(_launcherUpdatePath))
                {
                    //zip.ExtractAll(@"D:\test\", ExtractExistingFileAction.OverwriteSilently);
                    zip.ExtractAll(rootDirectory, ExtractExistingFileAction.OverwriteSilently);
                }

                // delete the temporary zip file after finish extracting
                File.Delete(_launcherUpdatePath);

                string launcherUpdaterPath = System.IO.Path.Combine(rootDirectory, "update.bat");
                Process launcherUpdater = new Process();
                launcherUpdater.StartInfo.FileName = launcherUpdaterPath;
                launcherUpdater.Start();
                this.Close();

            }).Start();
        }

        private void NewWebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                // display updating status notice
                ProgressBar.Visibility = Visibility.Visible;
                ProgressBar.Value = e.ProgressPercentage;
            }));
        }
        
    }

}