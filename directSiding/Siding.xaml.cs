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
using Microsoft.Live;

namespace directSiding
{
    public partial class Siding : PhoneApplicationPage
    {
        Uri _uriLogin = new Uri("https://intrawww.ing.puc.cl/siding/index.phtml", UriKind.Absolute);
        Uri _uriCursos = new Uri("https://intrawww.ing.puc.cl/siding/dirdes/ingcursos/cursos/vista.phtml", UriKind.Absolute);
        string _skyDriveFolderName = "directSIDING";
        string _skyDriveFolderID = string.Empty;

        IsolatedStorageSettings settings;
        LiveConnectClient client;
        string filename;

        Stack<Uri> _history;

        // The Uri of the current file that is trying to load
        Uri _fileToDownload;
        // The browser is navigating
        bool _navigating;

        // BackgroundWorker for downloading files in the background
        private BackgroundWorker _bw;
        private WebClient _webClient;

        public Siding()
        {
            InitializeComponent();

            settings = IsolatedStorageSettings.ApplicationSettings;

            browser.IsScriptEnabled = true;
            _history = new Stack<Uri>();
            _navigating = false;

            _bw = new BackgroundWorker();
            _bw.DoWork += _bw_DoWork;
            _webClient = new WebClient();
            
            _webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(_webClient_DownloadProgressChanged);
            _webClient.OpenReadCompleted += new OpenReadCompletedEventHandler(_webClient_OpenReadCompleted);

            var encoding = System.Text.Encoding.GetEncoding("iso-8859-1");
            string postData = "login="+ settings["username"]+"&passwd="+ settings["password"]+"&sw=&sh=&cd=";
            string headers = "Content-Type: application/x-www-form-urlencoded\r\n"+
                "Connection: keep-alive\r\n" +
                "Referer: http://www.ing.puc.cl/\r\n"+
                "Accept-Language: es-cl,es;q=0.8,en-us;q=0.5,en;q=0.3\r\n"+
                "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            browser.Navigated += new EventHandler<System.Windows.Navigation.NavigationEventArgs>(browser_firstTime);
            browser.Navigate(_uriLogin, encoding.GetBytes(postData), headers);
        }

        private void _bw_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;

            Dispatcher.BeginInvoke(() => 
            {
                progressBar.Visibility = System.Windows.Visibility.Visible;
                ApplicationTitle.Text = "Descargando...";
            });
            _webClient.OpenReadAsync(_fileToDownload, e.Argument);
        }

        void _webClient_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            try
            {
                if (e.Result != null)
                {
                    // Download file
                    var contentDisposition = (sender as WebClient).ResponseHeaders["Content-Disposition"];
                    filename = "filename=\"";
                    var i = contentDisposition.IndexOf(filename) + filename.Length;
                    var j = contentDisposition.IndexOf("\"", i);
                    filename = "/shared/transfers/" + contentDisposition.Substring(i, j - i);
                    using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                    using (var f = new IsolatedStorageFileStream(filename, System.IO.FileMode.Create, storage))
                    {
                        long fileNameLength = (long)e.Result.Length;
                        byte[] byteImage = new byte[fileNameLength];
                        e.Result.Read(byteImage, 0, byteImage.Length);
                        f.Write(byteImage, 0, byteImage.Length);
                    }

                    // Check if the folder exists
                    client = new LiveConnectClient(App.Session);
                    client.GetCompleted += client_GetFolderInfoCompleted;
                    client.GetAsync("me/skydrive/files?filter=folders,albums");
                }
            }
            catch (Exception) { }
        }

        private void client_GetFolderInfoCompleted(object sender, LiveOperationCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                var folders = e.Result["data"] as List<object>;

                foreach (IDictionary<string, object> folder in folders)
                {
                    if (folder["name"].ToString() == _skyDriveFolderName)
                    {
                        _skyDriveFolderID = folder["id"].ToString();
                        break;
                    }
                }

                if (_skyDriveFolderID == string.Empty)
                {
                    var skyDriveFolderData = new Dictionary<string, object>();
                    skyDriveFolderData.Add("name", _skyDriveFolderName);
                    client.PostCompleted += client_CreateFolderCompleted;
                    client.PostAsync("me/skydrive", skyDriveFolderData);
                }
                else
                    uploadFile();
            }
            else
            {
                MessageBox.Show(e.Error.Message);
            } 
        }

        private void client_CreateFolderCompleted(object sender, LiveOperationCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                var folder = e.Result;
                _skyDriveFolderID = folder["id"].ToString();
                uploadFile();
            }
            else
            {
                MessageBox.Show(e.Error.Message, "No pudimos usar Skydrive :(", MessageBoxButton.OK);
            } 
        }

        private void uploadFile()
        {
            if (String.IsNullOrEmpty(_skyDriveFolderID))
                return;

            Dispatcher.BeginInvoke(() =>
            {
                ApplicationTitle.Text = "Subiendo a Skydrive...";
            });

            this.client.BackgroundUploadCompleted += client_BackgroundUploadCompleted;

            // Upload file
            try
            {
                client.BackgroundUploadAsync(_skyDriveFolderID, new Uri(filename, UriKind.Relative), OverwriteOption.Overwrite);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        void client_BackgroundUploadCompleted(object sender, LiveOperationCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    ApplicationTitle.Text = "Listo :)";
                });

                // Remove the file from the IS
                using (var storage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    storage.DeleteFile(filename);
                }

                ThreadPool.QueueUserWorkItem(updateTitle);

                var file = e.Result;
                var browser = new Microsoft.Phone.Tasks.WebBrowserTask();
                browser.Uri = new Uri(file["source"] as string, UriKind.Absolute);
                browser.Show();
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
                browser.Navigate(_uriCursos);
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
            NavigationService.Navigate(new Uri("/Config.xaml", UriKind.Relative));
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
            var g = new Microsoft.Xna.Framework.Game();
            var title = "Ups!";
            var message = "Algo salió mal.\r\n¿Estás seguro tienes conexión a Internet?";
            var ans = MessageBox.Show(message, title, MessageBoxButton.OKCancel);
            if (ans == MessageBoxResult.OK)
            {
                title = "¿Quieres instalar el certificado en tu teléfono?";
                message = "El SIDING tiene problemas con el certificado de seguridad.\r\n" +
                    "Si no has seguido las instrucciones, pon Ok para que te expliquemos qué debes hacer.\r\n\r\n" +
                    "Si ya hiciste el trámite pon cancelar e intenta más tarde. Saldrás de la app.";
                ans = MessageBox.Show(message, title, MessageBoxButton.OKCancel);
                if (ans != MessageBoxResult.OK)
                    g.Exit();
                else
                {
                    title = "Instrucciones 1/2";
                    message = "1. Desde un computador baja el archivo http://bit.ly/dsCertf\r\n" +
                        "2. Adjunta el archivo y envíalo por email a la cuenta que tienes configurada en la app Mail de tu equipo Windows Phone.\r\n\r\n" +
                        "Cuando estes listo pon Ok para seguir con los pasos.";
                    ans = MessageBox.Show(message, title, MessageBoxButton.OK);

                    if (ans != MessageBoxResult.OK)
                        g.Exit();

                    title = "Instrucciones 2/2";
                    message = "1. Desde tu smartphone ve a la app Mail y revisa el correo que te acabas de enviar.\r\n" +
                        "2. Descarga el archivo adjunto .pem haciendo tap en el nombre.\r\n" +
                        "3. Abre el archivo adjunto. Si todo va bien debe aparecer un \"escudo\" de ícono.\r\n" +
                        "4. Acepta instalar el certificado de seguridad.\r\n\r\n" +
                        "Una vez hecho todo esto, reinicia directSiding. Tap Ok para salir.";
                    MessageBox.Show(message, title, MessageBoxButton.OK);
                    g.Exit();
                }
            }
            else
            {
                title = "¡Conéctate a Internet antes!";
                message = "Es necesario que actives tu conexión de datos o Wi-Fi para continuar.\r\n" +
                    "Tap Ok para salir";
                MessageBox.Show(message, title, MessageBoxButton.OK);
                g.Exit();
            }
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

                if (App.Session != null)
                {
                    _bw.RunWorkerAsync(String.Format("{0}-{1}", idCurso, idArchivo));
                }
                else
                {
                    var res = MessageBox.Show("Requieres iniciar sesión en Skydrive. Ve a la página de configuración", "No puedes descargar archivos", MessageBoxButton.OKCancel);
                    if(res == MessageBoxResult.OK)
                        NavigationService.Navigate(new Uri("/Config.xaml", UriKind.Relative));
                }
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