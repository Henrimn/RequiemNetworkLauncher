using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Bleak;
using System.Diagnostics;
using System.Net.Http;

namespace Requiem_Network_Launcher
{
    public partial class MainWindow
    {
        private Injector _injector = new Injector();
        private Process _vindictus;

        private void StartGameButton_Click(object sender, RoutedEventArgs e)
        {
            LoginAndStart();
        }

        private async void LoginAndStart()
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
                    WarningBox.Text = "Login successfully!";
                    WarningBox.Foreground = new SolidColorBrush(Colors.LawnGreen);
                }));

                // split string to "token", ":", and token value
                string responseToken = responseStringSplit[2].Split('"')[3];
                responseToken = responseToken.Replace(@"\", ""); // "\/" is not valid. Correct format should be "/" only, "\" acts as an escape character

                // start Vindictus.exe 
                _vindictus = new Process();
                try
                {
                    _vindictus.EnableRaisingEvents = true;
                    _vindictus.Exited += _process_Exited;
                    _vindictus.StartInfo.FileName = _processPath;
                    _vindictus.StartInfo.Arguments = " -lang zh-TW -token " + responseToken; // if token is null -> server under maintenance error
                    _vindictus.Start();
                }
                catch (Exception)
                {

                }

                // inject winnsi.dll to Vindictus.exe 
                _injector.CreateRemoteThread(_dllPath, _vindictus.Id);

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
                    WarningBox.Text = "Wrong username or password.\nPlease try again!";
                    WarningBox.Foreground = new SolidColorBrush(Colors.Red);
                }));
            }

            // for any other response code that is not 200 or 500
            else
            {
                // Update login status on UI thread(main thread)
                Dispatcher.Invoke((Action)(() =>
                {
                    WarningBox.Text = "Login failed!\nError code: " + responseCode + "\nPlease contact staff for more help.";
                    WarningBox.Foreground = new SolidColorBrush(Colors.Red);
                }));
            }
        }

        private void _process_Exited(object sender, EventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                // clear login status
                WarningBox.Text = "";

                // re-enable start game button after the game is closed
                StartGameButton.Content = "Start Game";
                StartGameButton.IsEnabled = true;
                StartGameButton.Foreground = new SolidColorBrush(Colors.Black);
            }));
        }

        // these 2 methods handle enter key press on both username and password field
        private void UsernameBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                LoginAndStart();
            }
        }
        private void PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                LoginAndStart();
            }
        }
    }
}

