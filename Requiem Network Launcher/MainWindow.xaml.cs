﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Threading;
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
        private string rootDirectory;
        private string _dllPath;
        private string _processPath;
        private string _versionPath;
        private string _updatePath;
        private string _currentVersionLocal;
        private NotifyIcon _nIcon;

        public MainWindow()
        {
            InitializeComponent();

            // load user settings
            LoadUserSettings();
            this.SourceInitialized += Window_SourceInitialized;

            // setup system tray icon for launcher
            NotifyIconSetup();

            // setup files path and perform version checking everytime launcher starts
            SetFilesPath();

            // get artwork credit at the bottom corner
            GetArtWorkCredit();

            // hide ui 
            ProgressDetails.Visibility = Visibility.Hidden;
            ProgressBar.Visibility = Visibility.Hidden;

        }
        
        private void SetFilesPath()
        {
            // get current directory of the Launcher
            rootDirectory = System.AppDomain.CurrentDomain.BaseDirectory;

            // set path for winnsi.dll and Vindictus.exe, version.txt and LauncherUpdater.exe
            _dllPath = System.IO.Path.Combine(rootDirectory, "winnsi.dll");
            _processPath = System.IO.Path.Combine(rootDirectory, "Vindictus.exe");
            _versionPath = System.IO.Path.Combine(rootDirectory, "version.txt");
            var ngclientFilePath = System.IO.Path.Combine(rootDirectory, "NGClient.aes");

            /*_dllPath = @"C:\Requiem\ko-KR\winnsi.dll";
            _processPath = @"C:\Requiem\ko-KR\Vindictus.exe";
            _versionPath = @"D:\test\version.txt";*/

            if (!File.Exists(_versionPath))
            {
                // update small version info at bottom left corner
                Dispatcher.Invoke((Action)(() =>
                {
                    WarningBox.Text = "You are missing version.txt file!\nMake sure launcher is in main game folder!\nTry update your game first.\nContact staff for more help.";
                    WarningBox.Foreground = new SolidColorBrush(Colors.Red);

                    StartGameButton.IsEnabled = false;
                    StartGameButton.Foreground = new SolidColorBrush(Colors.Silver);

                    GetCurrentLocalVersion("other");
                }));
            }
            else if (!File.Exists(_dllPath))
            {
                // update small version info at bottom left corner
                Dispatcher.Invoke((Action)(() =>
                {
                    WarningBox.Text = "You are missing winnsi.dll file!\nMake sure launcher is in main game folder!\nTry update your game first.\nContact staff for more help.";
                    WarningBox.Foreground = new SolidColorBrush(Colors.Red);

                    StartGameButton.IsEnabled = false;
                    StartGameButton.Foreground = new SolidColorBrush(Colors.Silver);

                    GetCurrentLocalVersion("other");
                }));
            }
            else if (!File.Exists(_processPath))
            {
                // update small version info at bottom left corner
                Dispatcher.Invoke((Action)(() =>
                {
                    WarningBox.Text = "You are missing Vindictus.exe file!\nMake sure launcher is in main game folder!\nContact staff for more help.";
                    WarningBox.Foreground = new SolidColorBrush(Colors.Red);

                    StartGameButton.IsEnabled = false;
                    StartGameButton.Foreground = new SolidColorBrush(Colors.Silver);

                    GetCurrentLocalVersion("other");
                }));
            }
            else if (!File.Exists(ngclientFilePath))
            {
                // update small version info at bottom left corner
                Dispatcher.Invoke((Action)(() =>
                {
                    WarningBox.Text = "You are missing NGClient.aes file!\nMake sure launcher is in your main game folder!\nContact staff for more help.";
                    WarningBox.Foreground = new SolidColorBrush(Colors.Red);

                    StartGameButton.IsEnabled = false;
                    StartGameButton.Foreground = new SolidColorBrush(Colors.Silver);

                    GetCurrentLocalVersion("other");
                }));
            }
            else
            {
                CheckGameVersion();
            }
        }

        private async void CheckGameVersion()
        {
            // update small version info at bottom left corner
            Dispatcher.Invoke((Action)(() =>
            {
                WarningBox.Text = "Checking game version...";
                WarningBox.Foreground = new SolidColorBrush(Colors.LawnGreen);
            }));

            var versionPath = _versionPath;

            // temporary disable all button before finishing checking game version
            DisableAllButtons();

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

            // work with .net framework 4.5 and above
            HttpClient _client = new HttpClient();

            try
            {
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
                        WarningBox.Text = "Your game is not up to date yet.\nPlease update your game!";
                        WarningBox.Foreground = new SolidColorBrush(Colors.Red);

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

                        WarningBox.Text = "Your game is up to date!";
                        WarningBox.Foreground = new SolidColorBrush(Colors.LawnGreen);

                        // re-enable start game button for player
                        StartGameButton.IsEnabled = true;
                        StartGameButton.Foreground = new SolidColorBrush(Colors.Black);

                        UpdateLauncherButton.IsEnabled = true;
                        UpdateLauncherButton.Foreground = new SolidColorBrush(Colors.Black);
                    }));

                }
            }
            catch (Exception e)
            {
                if (e is HttpRequestException)
                {
                    System.Windows.MessageBox.Show(e.Message, "Connection error");

                    Dispatcher.Invoke((Action)(() =>
                    {
                        WarningBox.Text = "Cannot check game version!\nServer is currently down or your internet is down!\nContact staff for more help.";
                        WarningBox.Foreground = new SolidColorBrush(Colors.Red);
                    }));
                }
                else if (e is NotSupportedException)
                {
                    System.Windows.MessageBox.Show(e.Message, "Error");

                    Dispatcher.Invoke((Action)(() =>
                    {
                        WarningBox.Text = e.Message;
                        WarningBox.Foreground = new SolidColorBrush(Colors.Red);
                    }));
                }
            }

        }

        private async void GetArtWorkCredit()
        {
            HttpClient _client = new HttpClient();

            try
            {
                var artWorkCredit = await _client.GetStringAsync("http://requiemnetwork.com/artwork.txt");
                Dispatcher.Invoke((Action)(() =>
                {
                    ArtCreditText.Content = artWorkCredit;
                }));
            }
            catch (Exception)
            {

            }
            
        }

        private void GetCurrentLocalVersion(string version)
        {
            _currentVersionLocal = version;
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

        private void MenuExit_Click(object sender, EventArgs e)
        {
            _nIcon.Visible = false;
            this.Close();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                _nIcon.BalloonTipText = "Launcher has been minimized to system tray";
                _nIcon.BalloonTipTitle = "Requiem Network";
                _nIcon.ShowBalloonTip(1000);
                this.Hide();
            }
            base.OnStateChanged(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _nIcon.Visible = false;
            if (this.WindowState == WindowState.Normal)
            {
                Properties.Settings.Default.Height = this.Height;
                Properties.Settings.Default.Width = this.Width;

            }
            else
            {
                Properties.Settings.Default.Height = this.RestoreBounds.Height;
                Properties.Settings.Default.Width = this.RestoreBounds.Width;
            }
            Properties.Settings.Default.Save();
            Environment.Exit(0);
            base.OnClosing(e);
        }

        private void DisableAllButtons()
        {
            Dispatcher.Invoke((Action)(() =>
            {
                StartGameButton.IsEnabled = false;
                StartGameButton.Foreground = new SolidColorBrush(Colors.Silver);

                UpdateGameButton.IsEnabled = false;
                UpdateGameButton.Foreground = new SolidColorBrush(Colors.Silver);

                UpdateLauncherButton.IsEnabled = false;
                UpdateLauncherButton.Foreground = new SolidColorBrush(Colors.Silver);
            }));
        }

        private void LoadUserSettings()
        {
            this.Height = Properties.Settings.Default.Height;
            this.Width = Properties.Settings.Default.Width;
        }

    }
}
