using System;
using System.Collections.Generic;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.XPath;

namespace yakupov41
{
    /// <summary>
    /// Логика взаимодействия для AuthPage.xaml
    /// </summary>
    public partial class AuthPage : Page
    {
        private string _currentCaptcha = "";
        private int _failedAttempts = 0;
        private DispatcherTimer _timer;
        private int _blockSecondsLeft = 0;
        public AuthPage()
        {
            InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        private void BtnGuest_Click(object sender, RoutedEventArgs e)
        {
            ServicePage page = new ServicePage(null);
            page.FIOTB.Text = "Гость";
            page.RoleTB.Text = "Гость";
            NavigationService.Navigate(page);
            ResetAuthState();

        }
        private void GenerateCaptcha()
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            _currentCaptcha = "";
            for(int i =0; i <4; i++)
            {
                _currentCaptcha+= chars[random.Next(chars.Length)];
            }
            captchaOneWord.Text = _currentCaptcha[0].ToString();
            captchaTwoWord.Text = _currentCaptcha[1].ToString();
            captchaThreeWord.Text = _currentCaptcha[2].ToString();
            captchaFourWord.Text = _currentCaptcha[3].ToString();
            TBoxCaptchaInput.Text = "";
            CaptchaPanel.Visibility = Visibility.Visible;

        }
        private void StartBlockTimer()
        {
            _blockSecondsLeft = 10;
            BtnLogin.IsEnabled = false;
            BtnGuest.IsEnabled = false;
            BtnLogin.Content = $"Ждите {_blockSecondsLeft}с";
            _timer.Start();
        }
        private void Timer_Tick(object sender,EventArgs e)
        {
            _blockSecondsLeft--;
            if(_blockSecondsLeft > 0)
            {
                BtnLogin.Content = $"Ждите {_blockSecondsLeft}c";
            }
            else
            {
                _timer.Stop();
                BtnLogin.IsEnabled = true;
                BtnGuest.IsEnabled = true;
                BtnLogin.Content = "Войти";

                GenerateCaptcha();
            }
        }
        private void ResetAuthState()
        {
            TBoxLogin.Text = "";
            TBoxPassword.Text = "";
            TBoxCaptchaInput.Text = "";
            CaptchaPanel.Visibility = Visibility.Collapsed;
            _failedAttempts = 0;
            _currentCaptcha = "";
            captchaOneWord.Text = "";
            captchaTwoWord.Text = "";
            captchaThreeWord.Text = "";
            captchaFourWord.Text = "";
            if(_timer != null && _timer.IsEnabled)
            {
                _timer.Stop();
            }
            BtnLogin.IsEnabled = true;
            BtnGuest.IsEnabled = true;
            BtnLogin.Content = "Войти";
        }
        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            string login = TBoxLogin.Text;
            string password = TBoxPassword.Text;
            if (login == "" || password == "")
            {
                MessageBox.Show("Есть пустые поля");
                return;
            }
            User user = yakupov41Entities.GetContext().User.ToList().Find(p => p.UserLogin == login && p.UserPassword == password);
            if(user != null)
            {
                ResetAuthState();
                Manager.MainFrame.Navigate(new ServicePage(user));
                TBoxLogin.Text = "";
                TBoxPassword.Text = "";
            }
            else
            {
                MessageBox.Show("Введены неверные данные");
                _failedAttempts++;
                GenerateCaptcha();
                if(_failedAttempts > 1)
                {
                    StartBlockTimer();
                }
            }
        }
        

    }
}
