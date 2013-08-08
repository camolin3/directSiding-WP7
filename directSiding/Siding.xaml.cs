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
using Microsoft.Phone.Shell;
using System.ComponentModel;
using System.IO.IsolatedStorage;

namespace directSiding
{
    public partial class Siding : PhoneApplicationPage
    {
        Uri UriLogin = new Uri("https://intrawww.ing.puc.cl/siding/index.phtml", UriKind.Absolute);
        Uri UriCursos = new Uri("https://intrawww.ing.puc.cl/siding/dirdes/ingcursos/cursos/vista.phtml", UriKind.Absolute);

        Stack<Uri> _history;

        // The Uri of the current file that is trying to load
        Uri _fileToDownload;
        // The browser is navigating
        bool _navigating;

        // BackgroundWorker for downloading files in the background
        private BackgroundWorker _bw;
        private WebClient _webClient;
        // Storage
        IsolatedStorageFile _storage;

        public Siding()
        {
            InitializeComponent();

            browser.IsScriptEnabled = true;
            _history = new Stack<Uri>();
            _navigating = false;

            _bw = new BackgroundWorker();
            _bw.DoWork += _bw_DoWork;
            _webClient = new WebClient();
            
            _webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(_webClient_DownloadProgressChanged);
            _webClient.OpenReadCompleted += new OpenReadCompletedEventHandler(_webClient_OpenReadCompleted);
            _storage = IsolatedStorageFile.GetUserStoreForApplication();

            var encoding = System.Text.Encoding.GetEncoding("iso-8859-1");
            string postData = "login="+ (Application.Current as App).settings["username"]+"&passwd="+ (Application.Current as App).settings["password"]+"&sw=&sh=&cd=";
            string headers = "Content-Type: application/x-www-form-urlencoded\r\n"+
                "Connection: keep-alive\r\n" +
                "Referer: http://www.ing.puc.cl/\r\n"+
                "Accept-Language: es-cl,es;q=0.8,en-us;q=0.5,en;q=0.3\r\n"+
                "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            browser.Navigated += new EventHandler<System.Windows.Navigation.NavigationEventArgs>(browser_firstTime);
            browser.Navigate(UriLogin, encoding.GetBytes(postData), headers);
        }

        private void _bw_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            
            _webClient.OpenReadAsync(_fileToDownload, e.Argument);
        }

        void _webClient_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            try
            {
                if (e.Result != null)
                {
                    var contentDisposition = (sender as WebClient).ResponseHeaders["Content-Disposition"];
                    var filename = "filename=\"";
                    var i = contentDisposition.IndexOf(filename) + filename.Length;
                    var j = contentDisposition.IndexOf("\"", i);
                    filename = contentDisposition.Substring(i, j - i);
                    var f = new IsolatedStorageFileStream(filename, System.IO.FileMode.Create, _storage);
                    long fileNameLength = (long)e.Result.Length;
                    byte[] byteImage = new byte[fileNameLength];
                    e.Result.Read(byteImage, 0, byteImage.Length);
                    f.Write(byteImage, 0, byteImage.Length);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }
        }

        void _webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            try
            {
                progressBar.Value = (double)e.ProgressPercentage;
            }
            catch (Exception)
            {
            }
        }

        void browser_firstTime(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            var cookies = browser.GetCookies();
            var cookiesStr = "";
            foreach (var cookie in cookies)
            {
                cookiesStr = cookie.ToString();
            }
            _webClient.Headers[HttpRequestHeader.Cookie] = cookiesStr;
            browser.Navigated -= browser_firstTime;
            if ((bool)(Application.Current as App).settings["redirect"])
            {
                _history.Pop();
                browser.Navigate(UriCursos);
            }
        }

        private void btnRefreshOrStop_Click(object sender, EventArgs e)
        {
            if (_navigating)
            {
                // Cancel navigation
                try
                {
                    browser.InvokeScript("eval", "document.execCommand('Stop');");
                }
                catch (Exception) { }
                _navigating = false;
                progressBar.Visibility = System.Windows.Visibility.Collapsed;
                // Restore refresh btn
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IconUri = new Uri("/Images/appbar.refresh.rest.png", UriKind.Relative);
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).Text = "Actualizar";
            }
            else
            {
                // Reload the Uri
                browser.Navigate(_history.Peek());
            }
        }

        private void btnConfig_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
        }

        private void PhoneApplicationPage_BackKeyPress(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_history.Count > 1)
            {
                _history.Pop();
                browser.Navigate(_history.Pop());
                e.Cancel = true;
            }
        }

        private void browser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(updateTitle);
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            NavigationService.RemoveBackEntry();
        }

        private void browser_NavigationFailed(object sender, System.Windows.Navigation.NavigationFailedEventArgs e)
        {
            var message = "Algo salió mal.\r\nRevisa que tengas conexión a Internet.\r\nSi usas WP7, asegurate de tener instalado el certificado correspondiente.\r\n";
            if (e.Exception != null)
                message += e.Exception.Message;
            MessageBox.Show(message, "¡Ups!", MessageBoxButton.OK);
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
                    _navigating = false;
                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IconUri = new Uri("/Images/appbar.refresh.rest.png", UriKind.Relative);
                    ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).Text = "Actualizar";
                    //ApplicationBar.Mode = ApplicationBarMode.Minimized;
                }
                catch (SystemException)
                {
                    ThreadPool.QueueUserWorkItem(updateTitle);
                }
            });
        }

        private void browser_Navigating(object sender, NavigatingEventArgs e)
        {
            if (e.Uri.AbsoluteUri.Contains("id_archivo="))
            {
                var uri = e.Uri.AbsoluteUri;
                var curso = "id_curso_ic=";
                var i = uri.IndexOf(curso) + curso.Length;
                var archivo = "&id_archivo=";
                var j = uri.IndexOf(archivo);
                var idCurso = uri.Substring(i, j - i);
                var idArchivo = uri.Substring(j + archivo.Length);
                _fileToDownload = e.Uri;

                _bw.RunWorkerAsync(String.Format("{0}-{1}", idCurso, idArchivo));
                e.Cancel = true;
            }
            else
            {
                _history.Push(e.Uri);
                _navigating = true;
                progressBar.Visibility = System.Windows.Visibility.Visible;
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).IconUri = new Uri("/Images/appbar.cancel.rest.png", UriKind.Relative);
                ((ApplicationBarIconButton)ApplicationBar.Buttons[0]).Text = "Detener";
                //ApplicationBar.Mode = ApplicationBarMode.Default;
            }
        }
    }
}