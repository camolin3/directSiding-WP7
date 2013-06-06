﻿using System;
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
            // Fix the case that the user wrote an email instead of the username
            if (txtUsername.Text.Contains('@'))
                txtUsername.Text = txtUsername.Text.Split('@')[0];

            // Save user config
            settings["username"] = txtUsername.Text;
            settings["password"] = pswPassword.Password;
            settings["redirect"] = cbCursos.IsChecked;

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
    }
}