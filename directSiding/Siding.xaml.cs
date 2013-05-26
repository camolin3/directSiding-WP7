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
using System.Threading;

namespace directSiding
{
    public partial class Siding : PhoneApplicationPage
    {
        int _browserHistoryLenght;
        bool _goingBack;

        public Siding()
        {
            InitializeComponent();

            browser.IsScriptEnabled = true;
            _browserHistoryLenght = -1;
            _goingBack = false;

            string url = "https://intrawww.ing.puc.cl/siding/index.phtml";
            System.Text.Encoding a = System.Text.Encoding.GetEncoding("iso-8859-1");
            string postData = "login="+ (Application.Current as App).settings["username"]+"&passwd="+ (Application.Current as App).settings["password"]+"&sw=&sh=&cd=";
            string headers = "Content-Type: application/x-www-form-urlencoded\r\n"+
                "Connection: keep-alive\r\n" +
                "Referer: http://www.ing.puc.cl/\r\n"+
                "Accept-Language: es-cl,es;q=0.8,en-us;q=0.5,en;q=0.3\r\n"+
                "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            browser.Navigated += new EventHandler<System.Windows.Navigation.NavigationEventArgs>(browser_firstTime);
            browser.Navigate(new Uri(url, UriKind.Absolute), a.GetBytes(postData), headers);
        }

        void browser_firstTime(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            browser.Navigated -= browser_firstTime;
            if ((bool)(Application.Current as App).settings["redirect"])
            {
                browser.Navigate(new Uri("https://intrawww.ing.puc.cl/siding/dirdes/ingcursos/cursos/vista.phtml", UriKind.Absolute));
                _browserHistoryLenght--;
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            browser.Navigate(new Uri(browser.Source.AbsoluteUri, UriKind.Absolute));
            _browserHistoryLenght--;
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_goingBack = _browserHistoryLenght > 0)
            {
                _browserHistoryLenght--;
                browser.InvokeScript("eval", "history.go(-1)");

                e.Cancel = true;
            }
        }

        private void browser_Navigated_1(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if(!_goingBack)
                _browserHistoryLenght++;
            ThreadPool.QueueUserWorkItem(updateTitle);
            
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            NavigationService.RemoveBackEntry();
        }

        private void browser_NavigationFailed(object sender, System.Windows.Navigation.NavigationFailedEventArgs e)
        {
            MessageBox.Show("Algo salió mal.\r\nRevisa que tengas conexión a Internet o vuelve a intentar más tarde.", "Ups!", MessageBoxButton.OK);
            throw new Exception();
        }

        private void updateTitle(Object o)
        {
            Thread.Sleep(500);
            Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    string title = (string)browser.InvokeScript("eval", "document.querySelector('html body table tbody tr td.ColorFondoZonaTrabajo table tbody tr td table tbody tr td.ColorFondoResaltado b').innerHTML");
                    ApplicationTitle.Text = "DIRECTSIDING - " + title;
                }
                catch (SystemException)
                {
                    ThreadPool.QueueUserWorkItem(updateTitle);
                }
            });
        }
    }
}