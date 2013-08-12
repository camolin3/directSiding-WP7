using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.IO.IsolatedStorage;
using System.Reflection;
using Microsoft.Live;

namespace directSiding
{
    public partial class MainPage : PhoneApplicationPage
    {
        // User settings
        IsolatedStorageSettings settings;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            try
            {
                settings = IsolatedStorageSettings.ApplicationSettings;
                if(settings.Contains("username"))
                    txtUsername.Text = (string)settings["username"];
                if (settings.Contains("password"))
                    pswPassword.Password = (string)settings["password"];
                cbRedirect.IsChecked = (bool)settings["redirect"];
                cbAutologin.IsChecked = (bool)settings["autologin"];
            }
            catch (Exception)
            {
                //
            }
        }

        private void btn_Click(object sender, RoutedEventArgs e)
        {
            // Fix the case that the user wrote an email instead of the username
            if (txtUsername.Text.Contains('@'))
                txtUsername.Text = txtUsername.Text.Split('@')[0];

            // Save user config
            settings["username"] = txtUsername.Text;
            settings["password"] = pswPassword.Password;
            settings["redirect"] = cbRedirect.IsChecked;
            settings["autologin"] = cbAutologin.IsChecked;

            NavigationService.Navigate(new Uri("/Siding.xaml", UriKind.Relative));
        }

        private void lnkEgg_Click(object sender, RoutedEventArgs e)
        {
            var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);

            var name = nameHelper.Name + " v" + nameHelper.Version.ToString();
            var content = "Ninguna de las contraseñas es guardada, robada ni nada de esas cosas, trust me. \r\nY si no confías, siempre puedes consultar el código fuente, es Open Source ;)";
            
            var result = System.Windows.MessageBox.Show(content, name, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                settings["clippy-js"] = "Link";
            }
            else if (result == MessageBoxResult.Cancel)
            {
                settings["clippy-js"] = "Clippy";
            }
        }

        private void btnSignin_SessionChanged(object sender, Microsoft.Live.Controls.LiveConnectSessionChangedEventArgs e)
        {
            if (e != null && e.Status == LiveConnectSessionStatus.Connected)
            {
                App.Session = e.Session;
                var client = new LiveConnectClient(App.Session);
                client.GetCompleted += client_Login;
                client.GetAsync("me");
            }
            else
            {
                blockSdkStatus.Text = "Inicie sesión en Skydrive para descargar archivos";
            }
        }

        void client_Login(object sender, LiveOperationCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                string firstName = e.Result.ContainsKey("first_name") ? e.Result["first_name"] as string : string.Empty;
                string lastName = e.Result.ContainsKey("last_name") ? e.Result["last_name"] as string : string.Empty;
                blockSdkStatus.Text = String.Format("{0} {1}, ya puedes descargar archivos", firstName, lastName);
            }
            else
                blockSdkStatus.Text = e.Error.Message;
        }
    }
}