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

        private void ShowRegisterPanel_Click(object sender, RoutedEventArgs e)
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            RegisterPanel.Visibility = Visibility.Visible;
        }

        private void ShowLoginPanel_Click(object sender, RoutedEventArgs e)
        {
            RegisterPanel.Visibility = Visibility.Collapsed;
            LoginPanel.Visibility = Visibility.Visible;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string username = LoginUsernameTextBox.Text;
            string password = LoginPasswordBox.Password;

            using (var db = new CalorieTrackerDBEntities())
            {
                var user = db.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

                if (user != null)
                {
                    MainWindow main = new MainWindow(user.Id); 
                    main.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Wrong login or password, how did you forget already", "Error");
                }
            }
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            string newUsername = RegisterUsernameTextBox.Text;
            string newPassword = RegisterPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(newUsername) || string.IsNullOrWhiteSpace(newPassword))
            {
                MessageBox.Show("Username and password cannot be empty.", "Validation Error");
                return;
            }

            using (var db = new CalorieTrackerDBEntities())
            {
                bool userExists = db.Users.Any(u => u.Username == newUsername);
                
                if (userExists)
                {
                    MessageBox.Show("This username is already taken. Choose another one.", "Registration Error");
                    return;
                }

                User newUser = new User();
                newUser.Username = newUsername;
                newUser.Password = newPassword;
                newUser.DailyCalorieGoal = 2000;
                
                db.Users.Add(newUser);
                db.SaveChanges();

                MessageBox.Show("Account created successfully! You can now log in.", "Success");
                
                ShowLoginPanel_Click(null, null);
                RegisterUsernameTextBox.Clear();
                RegisterPasswordBox.Clear();
            }
        }
    }
}
