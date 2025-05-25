using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using lab4wpf5oop.Models;

namespace RouteBookingSystem
{
    public partial class AdminWindow : Window
    {
        private readonly string connectionString = "Server=DESKTOP-DMJJTUE\\SQLEXPRESS;Database=XKKBUS;Trusted_Connection=True;";

        public AdminWindow(User currentAdmin)
        {
            InitializeComponent();
            this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("C:/BSTU/SEM4/OOP/lab4wpf5oop/lab4wpf5oop/Images/bus_icon-icons.com_76529.ico"));
            DataContext = new AdminViewModel(connectionString, currentAdmin);
        }

        private void txtNewPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminViewModel vm)
            {
                var passwordBox = (PasswordBox)sender;
                vm.SelectedUserPassword = passwordBox.Password;
            }
        }
        private void PriceValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var textBox = sender as TextBox;
            string newText = textBox.Text.Insert(textBox.CaretIndex, e.Text);

            bool isAllowed = Regex.IsMatch(newText, @"^[0-9]*\.?[0-9]*$");

            if (newText.StartsWith(".") || newText.Count(c => c == '.') > 1)
            {
                isAllowed = false;
            }

            e.Handled = !isAllowed;
        }

        private void IntegerValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^\d$");
        }

        private void PricePasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!Regex.IsMatch(text, @"^[0-9]*\.?[0-9]*$"))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}