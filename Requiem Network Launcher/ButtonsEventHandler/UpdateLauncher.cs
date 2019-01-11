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

        private string _launcherUpdaterPath;
        private string _newLauncherPath;

        private async void UpdateLauncherButton_Click(object sender, RoutedEventArgs e)
        {

            _launcherUpdaterPath = System.IO.Path.Combine(rootDirectory, "update.bat");

            Dispatcher.Invoke((Action)(() =>
            {
                // display updating status notice
                ProgressBar.Visibility = Visibility.Visible;
                WarningBox.Text = "Downloading new launcher...";
                WarningBox.Foreground = new SolidColorBrush(Colors.LawnGreen);

                DisableAllButtons();

            }));

            if (!File.Exists(_launcherUpdaterPath))
            {
                // download the batch file if it was not in the folder yet
                try
                {
                    using (WebClient webClient = new WebClient())
                    {
                        var uri = new Uri("http://requiemnetwork.com/downloads/update.bat");
                        Console.WriteLine(uri);
                        webClient.DownloadProgressChanged += NewWebClient_DownloadProgressChanged;
                        webClient.DownloadFileCompleted += NewWebClient_DownloadFileCompleted;
                        await webClient.DownloadFileTaskAsync(uri, _launcherUpdaterPath);
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
            else
            {
                // download new launcher
                _newLauncherPath = _versionPath = System.IO.Path.Combine(rootDirectory, "update.exe");
                try
                {
                    using (WebClient webClient = new WebClient())
                    {
                        var uri = new Uri("http://requiemnetwork.com/downloads/update.exe");
                        Console.WriteLine(uri);
                        webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged1;
                        webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted1;
                        await webClient.DownloadFileTaskAsync(uri, _newLauncherPath);
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
        }
        
        private async void NewWebClient_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            // download new launcher after finish downloading batch file
            _newLauncherPath = _versionPath = System.IO.Path.Combine(rootDirectory, "update.exe");
            using (WebClient webClient = new WebClient())
            {
                var uri = new Uri("http://requiemnetwork.com/downloads/update.exe");
                Console.WriteLine(uri);
                webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged1; 
                webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted1;
                await webClient.DownloadFileTaskAsync(uri, _newLauncherPath);
            }
        }
        private void NewWebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                // display updating status notice
                ProgressBar.Visibility = Visibility.Visible;
                WarningBox.Text = "Downloading necessary files...";
                WarningBox.Foreground = new SolidColorBrush(Colors.LawnGreen);
                ProgressBar.Value = e.ProgressPercentage;
            }));
        }

        private void WebClient_DownloadFileCompleted1(object sender, AsyncCompletedEventArgs e)
        {
            Process launcherUpdater = new Process();
            launcherUpdater.StartInfo.FileName = _launcherUpdaterPath;
            launcherUpdater.Start();
            this.Close();
        }
        
        private void WebClient_DownloadProgressChanged1(object sender, DownloadProgressChangedEventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                // display updating status notice
                ProgressBar.Visibility = Visibility.Visible;
                WarningBox.Text = "Downloading new launcher...\nPlease be patient.";
                WarningBox.Foreground = new SolidColorBrush(Colors.LawnGreen);
                ProgressBar.Value = e.ProgressPercentage;
            }));
        }
    }
    
}