using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CalorieTracker
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string LoginGiven = LoginTextBox.Text;
            string PasswordGiven = PasswordTextBox.Password;

            using (var db = new CalorieTrackerDBEntities())
            {
                var user = db.Users.FirstOrDefault(u => u.Username == LoginGiven && u.Password == PasswordGiven);

                if (user != null)
                {
                    MainWindow main = new MainWindow();
                    main.Show();

                    this.Close();
                }
                else
                {
                    MessageBox.Show("Wrong login or password, how did you forget already", "Error");
                }
            }
        }
    }
}
