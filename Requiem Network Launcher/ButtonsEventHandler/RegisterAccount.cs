using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Net.Http;

namespace Requiem_Network_Launcher
{
    public partial class MainWindow
    {
        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            Register();
        }

        private async void Register()
        {
            HttpClient _client = new HttpClient();

            try
            {
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
                        WarningBox.Foreground = new SolidColorBrush(Colors.Red);
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
                        WarningBox.Text = "Register failed!\nPlease contact staff for more help.";
                        WarningBox.Foreground = new SolidColorBrush(Colors.Red);
                    }));
                }
                else
                {
                    System.Windows.MessageBox.Show(e.Message, "Connection error");
                    Dispatcher.Invoke((Action)(() =>
                    {
                        WarningBox.Text = "An unexpected error happened!\nPlease contact staff for more help.";
                        WarningBox.Foreground = new SolidColorBrush(Colors.Red);
                    }));
                }
            }
        }
    }
}

