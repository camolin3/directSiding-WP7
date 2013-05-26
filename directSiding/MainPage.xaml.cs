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
                settings = (Application.Current as App).settings = IsolatedStorageSettings.ApplicationSettings;
                if(settings.Contains("username"))
                    txtUsername.Text = (string)settings["username"];
                if (settings.Contains("password"))
                    pswPassword.Password = (string)settings["password"];
                if(settings.Contains("redirect"))
                    cbCursos.IsChecked = (bool)settings["redirect"];
            }
            catch (Exception)
            {
                //
            }
        }

        private void btn_Click(object sender, RoutedEventArgs e)
        {
            // Save user config

            settings["username"] = txtUsername.Text;
            settings["password"] = pswPassword.Password;
            settings["redirect"] = cbCursos.IsChecked;

            NavigationService.Navigate(new Uri("/Siding.xaml", UriKind.Relative));
        }
    }
}