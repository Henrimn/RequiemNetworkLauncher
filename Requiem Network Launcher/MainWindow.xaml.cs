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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Injector _injector = new Injector();
        private string rootDirectory;
        private string _dllPath;
        private string _processPath;
        private string _versionPath;
        private string _updatePath;
        private string _currentVersionLocal;
        private NotifyIcon _nIcon;
        private double _updateFileSize;
        private string _continueSign = "continue";
        private Process _process;

        public MainWindow()
        {
            InitializeComponent();

            NotifyIconSetup();

            // perform version checking everytime launcher starts
            SetFilesPath();
            CheckGameVersion();
            GetArtWorkCredit();

            // hide ui 
            DownloadDetails.Visibility = Visibility.Hidden;
            DownloadProgressBar.Visibility = Visibility.Hidden;

        }

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            StartGame();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            Register();
        }

        private void UpdateGameButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateGame();
        }

        private void SetFilesPath()
        {
            // get current directory of the Launcher
            rootDirectory = System.AppDomain.CurrentDomain.BaseDirectory;

            // set path for winnsi.dll and Vindictus.exe and version.txt
            _dllPath = System.IO.Path.Combine(rootDirectory, "winnsi.dll");
            _processPath = System.IO.Path.Combine(rootDirectory, "Vindictus.exe");
            _versionPath = System.IO.Path.Combine(rootDirectory, "Version.txt");


            /*_dllPath = @"C:\Requiem\ko-KR\winnsi.dll";
            _processPath = @"C:\Requiem\ko-KR\Vindictus.exe";
            _versionPath = @"D:\test\version.txt";*/
        }

        private async void CheckGameVersion()
        {
            // update small version info at bottom left corner
            Dispatcher.Invoke((Action)(() =>
            {
                LoginWarningBox.Content = "Checking game version...";
                LoginWarningBox.Foreground = new SolidColorBrush(Colors.LawnGreen);
            }));

            var versionPath = _versionPath;

            HttpClient _client = new HttpClient();

            // temporary disable start game button before finishing checking game version
            StartGameButton.IsEnabled = false;
            StartGameButton.Foreground = new SolidColorBrush(Colors.Silver);
            UpdateGameButton.IsEnabled = false;
            UpdateGameButton.Foreground = new SolidColorBrush(Colors.Silver);

            // read info from version.txt file in main game folder
            var versionTextLocal = System.IO.File.ReadAllText(versionPath);
            var versionTextLocalSplit = versionTextLocal.Split(',');
            var currentVersionLocal = versionTextLocalSplit[0].Split('"')[3];
            var currentVersionDate = versionTextLocalSplit[1].Split('"')[3];

            // update small version info at bottom left corner
            Dispatcher.Invoke((Action)(() =>
            {
                VersionDisplayLabel.Content = "Version: " + currentVersionLocal + " - Release Date: " + currentVersionDate;
            }));

            // read info from version.txt on the server
            var versionTextServer = await _client.GetStringAsync("http://requiemnetwork.com/version.txt");
            var versionTextServerSplit = versionTextServer.Split(',');
            var currentVersionServer = versionTextServerSplit[0].Split('"')[3];

            // check if player has updated their game yet or not
            if (currentVersionLocal != currentVersionServer)
            {
                // display warning
                Dispatcher.Invoke((Action)(() =>
                {
                    LoginWarningBox.Content = "Your game is not up to date yet.\nPlease update your game!";
                    LoginWarningBox.Foreground = new SolidColorBrush(Colors.Red);

                    // enable update button only -> force player to update the game
                    UpdateGameButton.IsEnabled = true;
                    UpdateGameButton.Foreground = new SolidColorBrush(Colors.Black);
                }));

                // store variable so that it can be accessed outside of async method
                GetCurrentLocalVersion(currentVersionLocal);
            }
            else
            {
                // display notice
                Dispatcher.Invoke((Action)(() =>
                {
                    // set focus on username box once the launcher starts
                    UsernameBox.Focus();

                    LoginWarningBox.Content = "Your game is up to date!";
                    LoginWarningBox.Foreground = new SolidColorBrush(Colors.LawnGreen);

                    // re-enable start game button for player
                    StartGameButton.IsEnabled = true;
                    StartGameButton.Foreground = new SolidColorBrush(Colors.Black);
                }));

            }

        }

        private async void StartGame()
        {
            HttpClient _client = new HttpClient();

            // set base address for request
            _client.BaseAddress = new Uri("http://142.44.142.178");

            var values = new Dictionary<string, string>
            {
                    { "username", UsernameBox.Text },
                    { "password", PasswordBox.Password }
            };
            var content = new FormUrlEncodedContent(values);

            // send POST request with username and password and get the response from server
            var response = await _client.PostAsync("/api/auth.php?username=" + UsernameBox.Text
                                                              + "&password=" + PasswordBox.Password, content);

            // convert response from server to string 
            var responseString = await response.Content.ReadAsStringAsync();
            var responseStringSplit = responseString.Split(',');
            var responseCode = responseStringSplit[0].Split(':')[1]; // split string to "code" and code number
            var responseMess = responseStringSplit[1].Split('"')[3]; // split string to "message", ":", and message content

            // reponse code 200 = OK, username + password are correct
            if (responseCode.ToString() == "200")
            {
                // Update login status on UI thread (main thread)
                Dispatcher.Invoke((Action)(() =>
                {
                    LoginWarningBox.Content = "Login successfully!";
                    LoginWarningBox.Foreground = new SolidColorBrush(Colors.LawnGreen);
                }));

                // split string to "token", ":", and token value
                string responseToken = responseStringSplit[2].Split('"')[3];
                responseToken = responseToken.Replace(@"\", ""); // "\/" is not valid. Correct format should be "/" only, "\" acts as an escape character

                // start Vindictus.exe 
                _process = new Process();
                try
                {
                    _process.EnableRaisingEvents = true;
                    _process.Exited += _process_Exited;
                    _process.StartInfo.FileName = _processPath;
                    _process.StartInfo.Arguments = " -lang zh-TW -token " + responseToken; // if token is null -> server under maintenance error
                    _process.Start();
                }
                catch (Exception)
                {
                }

                // inject winnsi.dll to Vindictus.exe 
                _injector.CreateRemoteThread(_dllPath, _process.Id);

                Dispatcher.Invoke((Action)(() =>
                {
                    // shift focus to start game button to get rid of "|" on password box
                    StartGameButton.Focus();

                    // clear password box
                    PasswordBox.Password = "";

                    // disable start game button after the game start
                    StartGameButton.Content = "The game is running...";
                    StartGameButton.IsEnabled = false;
                    StartGameButton.Foreground = new SolidColorBrush(Colors.Silver);
                }));

                // close the launcher if user has logged in successfully
                await Task.Delay(2000);
                this.WindowState = WindowState.Minimized;

                //this.Close();
            }

            // response code 500 = wrong username or password
            else if (responseCode == "500")
            {
                // Update login status on UI thread (main thread)
                Dispatcher.Invoke((Action)(() =>
                {
                    PasswordBox.Password = "";
                    LoginWarningBox.Content = "Wrong username or password.\nPlease try again!";
                    LoginWarningBox.Foreground = new SolidColorBrush(Colors.Red);
                }));
            }

            // for any other response code that is not 200 or 500
            else
            {
                // Update login status on UI thread(main thread)
                Dispatcher.Invoke((Action)(() =>
                {
                    LoginWarningBox.Content = "Login failed!\nError code: " + responseCode + "\nPlease contact staff for more help.";
                    LoginWarningBox.Foreground = new SolidColorBrush(Colors.Red);
                }));
            }
        }

        private void _process_Exited(object sender, EventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                // clear login status
                LoginWarningBox.Content = "";

                // re-enable start game button after the game is closed
                StartGameButton.Content = "Start Game";
                StartGameButton.IsEnabled = true;
                StartGameButton.Foreground = new SolidColorBrush(Colors.Black);
            }));
        }

        private async void Register()
        {
            HttpClient _client = new HttpClient();

            // set base address for request
            _client.BaseAddress = new Uri("http://142.44.142.178");

            var values = new Dictionary<string, string>
                {
                    { "username", UsernameRegisterBox.Text },
                    { "password", PasswordRegisterBox.Text },
                    { "email"   , EmailRegisterBox.Text    },
                };
            var content = new FormUrlEncodedContent(values);
            var response = await _client.PostAsync("/api/registration.php?username=" + UsernameRegisterBox.Text
                                                                     + "&password=" + PasswordRegisterBox.Text
                                                                     + "&email=" + EmailRegisterBox.Text, content);
            if (response.StatusCode.ToString() == "OK")
            {
                Dispatcher.Invoke((Action)(() =>
                {
                    RegisterWarningBox.Content = "Register successfully!\nPlease return to login tab to login.";
                    RegisterWarningBox.Foreground = new SolidColorBrush(Colors.LawnGreen);
                }));
            }
            else
            {
                Dispatcher.Invoke((Action)(() =>
                {
                    RegisterWarningBox.Content = "Error: " + response.StatusCode.ToString();
                    LoginWarningBox.Foreground = new SolidColorBrush(Colors.Red);
                }));
            }
        }

        private async void UpdateGame()
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
                    // hide progress bar and detail
                    DownloadDetails.Visibility = Visibility.Hidden;
                    DownloadProgressBar.Visibility = Visibility.Hidden;
                }));

                // perform unzip in a new thread to prevent UI from freezing
                new Thread(delegate ()
                {
                    // extract update to main game folder and overwrite all existing files
                    using (ZipFile zip = ZipFile.Read(_updatePath))
                    {
                        //zip.ExtractAll(@"D:\test\", ExtractExistingFileAction.OverwriteSilently);
                        zip.ExtractAll(rootDirectory, ExtractExistingFileAction.OverwriteSilently);
                    }

                    // delete the temporary zip file after finish extracting
                    File.Delete(_updatePath);

                    Dispatcher.Invoke((Action)(() =>
                    {
                        // set focus on username box once the launcher starts
                        UsernameBox.Focus();

                        // display updating status notice
                        LoginWarningBox.Content = "Updating finished! You can play now!";
                    }));

                    // read info from version.txt file in main game folder
                    var versionTextLocal = System.IO.File.ReadAllText(_versionPath);
                    var versionTextLocalSplit = versionTextLocal.Split(',');
                    var currentVersionLocal = versionTextLocalSplit[0].Split('"')[3];
                    var currentVersionDate = versionTextLocalSplit[1].Split('"')[3];

                    // update new version info at the bottom corner of the launcher
                    Dispatcher.Invoke((Action)(() =>
                    {
                        VersionDisplayLabel.Content = "Version: " + currentVersionLocal + " - Release Date: " + currentVersionDate;
                        // re-enable start game button for player
                        StartGameButton.IsEnabled = true;
                        StartGameButton.Foreground = new SolidColorBrush(Colors.Black);

                        // disable update game button after finishing updating
                        UpdateGameButton.IsEnabled = false;
                        UpdateGameButton.Foreground = new SolidColorBrush(Colors.Silver);
                    }));
                }).Start();
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

                    DownloadProgressBar.Value = total;
                    LoginWarningBox.Content = "Dowloading update... " + total.ToString("F2") + "%";
                    DownloadDetails.Content = FileSizeType(e.BytesReceived) + " / " + FileSizeType(_updateFileSize);
                    //LoginWarningBox.Content = "Your game is updating...\nDownloaded:   " + string.Format("{0} GB", (e.BytesReceived / 1024d / 1024d / 1024d).ToString("0.00")) + " / " + _updateFileSize;
                }));
            }
        }

        // these 2 methods handle enter key press on both username and password field
        private void UsernameBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                StartGame();
            }
        }
        private void PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                StartGame();
            }
        }

        private void GetCurrentLocalVersion(string version)
        {
            _currentVersionLocal = version;
        }

        private void GetDownloadFileSize(string fileSize)
        {
            _updateFileSize = Convert.ToInt64(fileSize);
        }

        private async void GetArtWorkCredit()
        {
            HttpClient _client = new HttpClient();
            var artWorkCredit = await _client.GetStringAsync("http://requiemnetwork.com/artwork.txt");
            Dispatcher.Invoke((Action)(() =>
            {
                ArtCreditText.Content = artWorkCredit;
            }));
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

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                _nIcon.BalloonTipText = "Launcher has been minimized to system tray";
                _nIcon.BalloonTipTitle = "Requiem Network Launcher";
                _nIcon.ShowBalloonTip(1000);
                this.Hide();
            }
            base.OnStateChanged(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _nIcon.Visible = false;
            System.Windows.Forms.Application.Exit();
            base.OnClosing(e);
        }

        private void MenuExit_Click(object sender, EventArgs e)
        {
            _nIcon.Visible = false;
            System.Windows.Forms.Application.Exit();
        }
        
        private void NotifyIconSetup()
        {
            _nIcon = new NotifyIcon();
            _nIcon.Icon = Properties.Resources.macha_icon;
            _nIcon.Visible = true;
            _nIcon.DoubleClick += delegate (object sender, EventArgs e) { this.Show(); this.WindowState = WindowState.Normal; };
            _nIcon.ContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
            _nIcon.ContextMenuStrip.Items.Add("Exit", null, this.MenuExit_Click);
            _nIcon.Text = "Requiem Network Launcher";
        }
    }
}
