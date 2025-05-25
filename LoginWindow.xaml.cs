using lab4wpf5oop;
using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using lab4wpf5oop.Models;

namespace RouteBookingSystem
{
    public partial class LoginWindow : Window
    {
        private readonly string connectionString = "Server=DESKTOP-DMJJTUE\\SQLEXPRESS;Database=XKKBUS;Trusted_Connection=True;";

        public LoginWindow()
        {
            InitializeComponent();
            this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("C:/BSTU/SEM4/OOP/lab4wpf5oop/lab4wpf5oop/Images/bus_icon-icons.com_76529.ico"));
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите email и пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsValidEmail(email))
            {
                MessageBox.Show("Неверный формат Email (только английские буквы и цифры, без спецсимволов (кроме @ и .), без пробелов)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!IsValidPassword(password))
            {
                MessageBox.Show("Неверный формат пароля (минимум 6 символов, только английские буквы, цифры и спецсимволы, без пробелов)", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT FirstName, LastName, Surname, Email, PasswordHash, PhoneNumber, isAdmin, IsBlocked FROM Users WHERE Email = @Email";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedHash = reader.GetString(4); // PasswordHash
                                bool isAdmin = reader.GetBoolean(6); // isAdmin
                                bool isBlocked = reader.GetBoolean(7); // IsBlocked

                                if (isBlocked)
                                {
                                    MessageBox.Show("Ваш аккаунт заблокирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                if (VerifyPassword(password, storedHash))
                                {
                                    // Создаем объект User с данными из базы
                                    User currentUser = new User
                                    {
                                        FirstName = reader.GetString(0),
                                        LastName = reader.GetString(1),
                                        Surname = reader.GetString(2),
                                        Email = reader.GetString(3),
                                        PasswordHash = storedHash,
                                        PhoneNumber = reader.IsDBNull(5) ? null : reader.GetString(5),
                                        IsAdmin = isAdmin,
                                        IsBlocked = isBlocked // Добавляем новое поле
                                    };

                                    MessageBox.Show("Вход выполнен успешно!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                                    OpenUserWindow(isAdmin, currentUser);
                                }
                                else
                                {
                                    MessageBox.Show("Неверный пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Пользователь не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenUserWindow(bool isAdmin, User currentUser)
        {
            Window nextWindow = isAdmin ? new AdminWindow(currentUser) : new UserDashboardWindow(currentUser.Email);
            nextWindow.Show();
            this.Close();
        }

        private bool VerifyPassword(string inputPassword, string storedHash)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(inputPassword);
                byte[] hashedBytes = sha256.ComputeHash(inputBytes);
                string hashedInput = Convert.ToBase64String(hashedBytes);

                return hashedInput == storedHash;
            }
        }

        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[a-zA-Z][a-zA-Z0-9]*@[a-zA-Z][a-zA-Z0-9]*\.[a-zA-Z]+$");
        }

        private bool IsValidPassword(string password)
        {
            return !string.IsNullOrEmpty(password) && password.Length >= 6 &&
                   Regex.IsMatch(password, @"^[a-zA-Z0-9!@#$%^&*()+\-=\[\]{};:'"",.<>?]+$") &&
                   !password.Contains(" ");
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            var registrationWindow = new RegistrationWindow();
            registrationWindow.Show();
            this.Close();
        }

        private void txtEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtEmailPlaceholder != null)
            {
                txtEmailPlaceholder.Visibility = string.IsNullOrEmpty(txtEmail.Text) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (txtPasswordPlaceholder != null)
            {
                txtPasswordPlaceholder.Visibility = string.IsNullOrEmpty(txtPassword.Password) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}