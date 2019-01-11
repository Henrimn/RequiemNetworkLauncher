using System;
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
using System.Runtime.InteropServices;
using Microsoft.Shell;

namespace Requiem_Network_Launcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        private const string Unique = "My_Unique_Application_String";
        
        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance(Unique))
            {
                
                var application = new App();

                application.InitializeComponent();
                application.Run();

                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();
            }
        }

        #region ISingleInstanceApp Members

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            // Bring window to foreground
            if (this.MainWindow.WindowState == WindowState.Minimized)
            {
                this.MainWindow.Show();
                this.MainWindow.WindowState = WindowState.Normal;
            }
            
            return true;
        }

        #endregion

    }
}
