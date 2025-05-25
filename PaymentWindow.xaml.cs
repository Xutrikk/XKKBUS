using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RouteBookingSystem
{
    public class CardDetails
    {
        public string CardNumber { get; set; }
        public string CardHolder { get; set; }
        public string ExpiryDate { get; set; }
        public string Cvv { get; set; }
    }

    public partial class PaymentWindow : Window
    {
        public decimal Amount { get; }
        public CardDetails CardDetails { get; private set; }

        public PaymentWindow(decimal amount)
        {
            InitializeComponent();
            Amount = amount;
            CardDetails = new CardDetails();
            DataContext = this;
            this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("C:/BSTU/SEM4/OOP/lab4wpf5oop/lab4wpf5oop/Images/bus_icon-icons.com_76529.ico"));
        }

        private void CardNumberBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
            }
        }

        private void CardNumberBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            string text = textBox.Text.Replace(" ", "");
            if (text.Length > 16)
            {
                text = text.Substring(0, 16);
            }

            string formattedText = "";
            for (int i = 0; i < text.Length; i++)
            {
                if (i > 0 && i % 4 == 0)
                {
                    formattedText += " ";
                }
                formattedText += text[i];
            }

            textBox.TextChanged -= CardNumberBox_TextChanged;
            textBox.Text = formattedText;
            textBox.CaretIndex = formattedText.Length;
            textBox.TextChanged += CardNumberBox_TextChanged;
        }

        private void CardHolderBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!Regex.IsMatch(e.Text, @"[a-zA-Z\s]"))
            {
                e.Handled = true;
            }
        }

        private void ExpiryDateBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
            }
        }

        private void CvvBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
            }
        }

        private void CardHolderBox_GotFocus(object sender, RoutedEventArgs e)
        {
            CardHolderBox.SelectAll();
        }

        private void Pay_Click(object sender, RoutedEventArgs e)
        {
            // Валидация номера карты
            string cardNumber = CardNumberBox.Text.Replace(" ", "");
            if (!Regex.IsMatch(cardNumber, @"^\d{16}$"))
            {
                CardNumberErrorTextBlock.Text = "Номер карты должен содержать ровно 16 цифр.";
                CardNumberErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }
            CardNumberErrorTextBlock.Visibility = Visibility.Collapsed;

            // Валидация имени держателя
            string cardHolder = CardHolderBox.Text.Trim();
            if (cardHolder.Length < 2 || !Regex.IsMatch(cardHolder, @"^[a-zA-Z\s]+$"))
            {
                CardHolderErrorTextBlock.Text = "Введите корректное имя держателя (латинские буквы, минимум 2 символа).";
                CardHolderErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }
            if (cardHolder.Length > 50)
            {
                CardHolderErrorTextBlock.Text = "Имя держателя не должно превышать 50 символов.";
                CardHolderErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }
            CardHolderErrorTextBlock.Visibility = Visibility.Collapsed;

            // Валидация срока действия
            string expiryDate = ExpiryDateBox.Text;
            if (!Regex.IsMatch(expiryDate, @"^(0[1-9]|1[0-2])\d{2}$"))
            {
                ExpiryDateErrorTextBlock.Text = "Введите срок действия в формате MMYY (например, 1225).";
                ExpiryDateErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }
            string formattedExpiry = expiryDate.Insert(2, "/");
            if (!IsValidExpiryDate(formattedExpiry))
            {
                ExpiryDateErrorTextBlock.Text = "Срок действия карты истек или неверный.";
                ExpiryDateErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }
            ExpiryDateErrorTextBlock.Visibility = Visibility.Collapsed;

            // Валидация CVV
            string cvv = CvvBox.Text;
            if (!Regex.IsMatch(cvv, @"^\d{3,4}$"))
            {
                CvvErrorTextBlock.Text = "CVV должен содержать 3 или 4 цифры.";
                CvvErrorTextBlock.Visibility = Visibility.Visible;
                return;
            }
            CvvErrorTextBlock.Visibility = Visibility.Collapsed;

            // Сохранение данных
            CardDetails.CardNumber = cardNumber;
            CardDetails.CardHolder = cardHolder;
            CardDetails.ExpiryDate = formattedExpiry;
            CardDetails.Cvv = cvv;

            DialogResult = true;
            Close();
        }

        private bool IsValidExpiryDate(string expiryDate)
        {
            if (!Regex.IsMatch(expiryDate, @"^(0[1-9]|1[0-2])/\d{2}$"))
            {
                return false;
            }

            string[] parts = expiryDate.Split('/');
            int month = int.Parse(parts[0]);
            int year = int.Parse(parts[1]) + 2000;

            DateTime now = DateTime.Now;
            DateTime expiry = new DateTime(year, month, 1).AddMonths(1).AddDays(-1);

            return expiry >= now;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}