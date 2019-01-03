using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.IO;
using System.Net.Http;
using System.Net;
using Ionic.Zip;
using System.Text.RegularExpressions;


namespace Requiem_Network_Launcher
{
    public partial class MainWindow
    {
        private double _updateFileSize;
        private string _continueSign = "continue";

        private void UpdateGameButton_Click(object sender, RoutedEventArgs e)
        {
            Update();
        }

        private async void Update()
        {
            // update ui for downloading
            Dispatcher.Invoke((Action)(() =>
            {
                LoginWarningBox.Content = "Gathering information...";
                LoginWarningBox.Foreground = new SolidColorBrush(Colors.LawnGreen);

                // disable update button while updating
                UpdateGameButton.IsEnabled = false;
                UpdateGameButton.Foreground = new SolidColorBrush(Colors.Silver);
            }));

            HttpClient _client = new HttpClient();

            // get download links from server
            var updateDownload = await _client.GetStringAsync("http://requiemnetwork.com/update.txt");
            var updateDownloadSplit = updateDownload.Split(',');

            // download information for people who update their game regularly
            var updateDowndloadOld = updateDownloadSplit[0].Split('"');

            // download information for people who has not updated their game for a while
            var updateDowndloadNew = updateDownloadSplit[1].Split('"');

            // set default download link
            var updateDownloadLink = "https://drive.google.com/uc?export=download&id=";

            // get download link based on their game's current version
            if (_currentVersionLocal == updateDowndloadOld[1]) // if player has the latest patch
            {
                updateDownloadLink = updateDownloadLink + updateDowndloadOld[3];
                GetDownloadFileSize(updateDowndloadOld[5]);
            }
            else // if player has an outdate patch
            {
                updateDownloadLink = updateDownloadLink + updateDowndloadNew[3];
                GetDownloadFileSize(updateDowndloadNew[5]);
            }

            //_updatePath = @"D:\test\UpdateTemp.zip";

            // create temporary zip file from download
            _updatePath = System.IO.Path.Combine(rootDirectory, "UpdateTemporary.zip");


            // download update (zip) 
            using (CookieAwareWebClient webClient = new CookieAwareWebClient())
            {
                // sometimes google drive returns an NID cookie instead of a download warning cookie at first attempt
                // it will works in the second attempt
                for (int i = 0; i < 2; i++)
                {
                    // download page content
                    string DownloadString = await webClient.DownloadStringTaskAsync(updateDownloadLink);

                    // get confirm code from page content
                    Match match = Regex.Match(DownloadString, @"confirm=([0-9A-Za-z]+)");

                    if (_continueSign == "continue")
                    {
                        if (match.Value == "") // in case it is a direct google drive download link
                        {
                            // download the update
                            var uri = new Uri(updateDownloadLink);
                            webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                            webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
                            await webClient.DownloadFileTaskAsync(uri, _updatePath);
                        }
                        else // in case it is not a direct google drive download link
                        {
                            // construct new download link with confirm code
                            //string updateDownloadLinkNew = "https://drive.google.com/uc?export=download&" + match.Value + "&id=" + "15wrcgMB8AyRk30x5p7DoXvdZSrCrkL70";
                            string updateDownloadLinkNew = "https://drive.google.com/uc?export=download&" + match.Value + "&id=" + updateDowndloadNew[3];
                            var uri = new Uri(updateDownloadLinkNew);
                            webClient.DownloadProgressChanged += WebClient_DownloadProgressChanged;
                            webClient.DownloadFileCompleted += WebClient_DownloadFileCompleted;
                            await webClient.DownloadFileTaskAsync(uri, _updatePath);
                        }
                    }
                    else if (_continueSign == "stop")
                    {
                        break;
                    }
                }
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
                    LoginWarningBox.Content = "Extracting files...";
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
                        // hide progress bar and detail
                        DownloadProgressBar.Visibility = Visibility.Hidden;
                        DownloadDetails.Visibility = Visibility.Hidden;

                        // display updating status notice
                        LoginWarningBox.Content = "Updating finished!\nPlease restart the launcher to play.";
                    }));
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
                    DownloadProgressBar.Value = Convert.ToInt32(100 * e.BytesTransferred / e.TotalBytesToTransfer);
                    DownloadDetails.Content = "Extracting: " + e.CurrentEntry.FileName;
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
                    DownloadDetails.Visibility = Visibility.Visible;
                    DownloadProgressBar.Visibility = Visibility.Visible;

                    LoginWarningBox.Content = "Dowloading update... " + total.ToString("F2") + "%";
                    DownloadProgressBar.Value = total;
                    DownloadDetails.Content = FileSizeType(e.BytesReceived) + " / " + FileSizeType(_updateFileSize);
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