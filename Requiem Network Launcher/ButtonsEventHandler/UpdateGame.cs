using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.IO;
using System.Net.Http;
using System.Net;
using Ionic.Zip;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Requiem_Network_Launcher
{
    public partial class MainWindow
    {
        private double _updateFileSize;
        private string _continueSign = "continue";
        private string _updateDownloadID;

        private void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            CheckGameVersion();
        }

        private async void Update()
        {
            // update ui for downloading
            Dispatcher.Invoke((Action)(() =>
            {
                WarningBox.Text = "Preparing update...";
                WarningBox.Foreground = new SolidColorBrush(Colors.LawnGreen);
                DisableAllButtons();

            }));

            if (_versionTxtCheck == "not found")
            {
                HttpClient _client = new HttpClient();

                // get download links from server
                var updateDownload = await _client.GetStringAsync("http://requiemnetwork.com/launcher/update_02.txt");
                var updateDownloadSplit = updateDownload.Split(',');

                // download information for people who update their game regularly
                var updateDowndloadLink = updateDownloadSplit[0].Split('"');

                _updateDownloadID = updateDowndloadLink[3];
                GetDownloadFileSize(updateDowndloadLink[5]);
            }
            else if (_versionTxtCheck == "yes")
            {

                HttpClient _client = new HttpClient();

                // get download links from server
                var updateDownload = await _client.GetStringAsync("http://requiemnetwork.com/launcher/update_01.txt");
                var updateDownloadSplit = updateDownload.Split(',');

                // download information for people who update their game regularly
                var updateDowndloadOld = updateDownloadSplit[0].Split('"');

                // download information for people who has not updated their game for a while
                var updateDowndloadNew = updateDownloadSplit[1].Split('"');

                // get download link based on their game's current version
                if (_currentVersionLocal == updateDowndloadOld[1]) // if player has the latest patch
                {
                    _updateDownloadID = updateDowndloadOld[3];
                    GetDownloadFileSize(updateDowndloadOld[5]);
                }
                else // if player has an outdate patch 
                {
                    _updateDownloadID = updateDowndloadNew[3];
                    GetDownloadFileSize(updateDowndloadNew[5]);
                }
            }

            // create temporary zip file from download
            _updatePath = System.IO.Path.Combine(rootDirectory, "UpdateTemporary.zip");
            //_updatePath = @"D:\test\UpdateTemp.zip";


            // download update (zip) 
            try
            {
                using (CookieAwareWebClient webClient = new CookieAwareWebClient())
                {
                    if (_updateFileSize <= 100000000) // if file is < 100 MB -> no virus scan page -> can download directly
                    {
                        // download the update
                        var uri = new Uri("https://drive.google.com/uc?export=download&id=" + _updateDownloadID);
                        webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(WebClient_DownloadProgressChanged);
                        webClient.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(WebClient_DownloadFileCompleted);
                        await webClient.DownloadFileTaskAsync(uri, _updatePath);
                    }
                    else // more than 100 MB -> have to bypass virus scanning page
                    {
                        // sometimes google drive returns an NID cookie instead of a download warning cookie at first attempt
                        // it will works in the second attempt
                        for (int i = 0; i < 2; i++)
                        {
                            // download page content
                            string DownloadString = await webClient.DownloadStringTaskAsync("https://drive.google.com/uc?export=download&id=" + _updateDownloadID);
                            Console.WriteLine("downloading page contents...");

                            // get confirm code from page content
                            Match match = Regex.Match(DownloadString, @"confirm=([0-9A-Za-z]+)");

                            if (_continueSign == "stop")
                            {
                                break;
                                
                            }
                            else
                            {
                                // construct new download link with confirm code
                                string updateDownloadLinkNew = "https://drive.google.com/uc?export=download&" + match.Value + "&id=" + _updateDownloadID;
                                var uri = new Uri(updateDownloadLinkNew);
                                webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(WebClient_DownloadProgressChanged);
                                webClient.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(WebClient_DownloadFileCompleted);
                                await webClient.DownloadFileTaskAsync(uri, _updatePath);
                                
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message, "Error");
            }
        }

        private void WebClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            // get the size of the downloaded file
            long length = new System.IO.FileInfo(_updatePath).Length;

            if (length < 60000) // if the downloaded file is a web page content (usually has size smaller than 60KB)
            {
                // delete the file and re-send the request
                File.Delete(_updatePath);
            }
            else // if the file is valid, extract it
            {
                _continueSign = "stop";

                Dispatcher.Invoke((Action)(() =>
                {
                    // display updating status notice
                    WarningBox.Text = "Extracting files...";
                }));

                // perform unzip in a new thread to prevent UI from freezing
                new Thread(delegate ()
                {
                    // extract update to main game folder and overwrite all existing files
                    using (ZipFile zip = ZipFile.Read(_updatePath))
                    {
                        zip.ExtractProgress += Zip_ExtractProgress;
                        //zip.ExtractAll(@"D:\test\", ExtractExistingFileAction.OverwriteSilently);
                        zip.ExtractAll(rootDirectory, ExtractExistingFileAction.OverwriteSilently);
                    }

                    // delete the temporary zip file after finish extracting
                    File.Delete(_updatePath);

                    Dispatcher.Invoke((Action)(() =>
                    {
                        // display updating status notice
                        ProgressDetails.Visibility = Visibility.Hidden;
                        ProgressBar.Visibility = Visibility.Hidden;
                    }));

                    _versionTxtCheck = "yes";

                    CheckGameVersion();

                }).Start();
            }
        }

        private void Zip_ExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            if (e.TotalBytesToTransfer > 0)
            {
                Dispatcher.Invoke((Action)(() =>
                {
                    // display updating status notice
                    ProgressBar.Value = Convert.ToInt32(100 * e.BytesTransferred / e.TotalBytesToTransfer);
                    ProgressDetails.Content = "Extracting: " + e.CurrentEntry.FileName;
                }));
            }
        }

        private void WebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double total = ((double)e.BytesReceived / (double)_updateFileSize) * 100;
            if (_continueSign == "continue")
            {
                Dispatcher.Invoke((Action)(() =>
                {
                    // display updating status notice
                    ProgressDetails.Visibility = Visibility.Visible;
                    ProgressBar.Visibility = Visibility.Visible;

                    WarningBox.Text = "Dowloading update... " + total.ToString("F2") + "%";
                    ProgressBar.Value = total;
                    ProgressDetails.Content = FileSizeType(e.BytesReceived) + " / " + FileSizeType(_updateFileSize);
                }));
            }
        }

        private void GetDownloadFileSize(string fileSize)
        {
            _updateFileSize = Convert.ToInt64(fileSize);
        }

        private string FileSizeType(double bytes)
        {
            string fileSizeType;
            if ((bytes / 1024d / 1024d / 1024d) > 1)
            {
                fileSizeType = string.Format("{0} GB", (bytes / 1024d / 1024d / 1024d).ToString("0.00"));
            }
            else
            {
                fileSizeType = string.Format("{0} MB", (bytes / 1024d / 1024d).ToString("0.00"));
            }

            return fileSizeType;
        }

    }

}