using System.Windows;
using System.Windows.Controls;

namespace RouteBookingSystem
{
    public partial class PurchaseHistoryWindow : Window
    {
        private readonly string _email;

        public PurchaseHistoryWindow(string email)
        {
            InitializeComponent();
            _email = email;
            DataContext = new PurchaseHistoryViewModel(email);
            this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("C:/BSTU/SEM4/OOP/lab4wpf5oop/lab4wpf5oop/Images/bus_icon-icons.com_76529.ico"));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            new UserDashboardWindow(_email).Show();
            this.Close();
        }

        private void RateTicket_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int purchaseId)
            {
                var viewModel = (PurchaseHistoryViewModel)DataContext;
                viewModel.ShowRatingDialog(purchaseId);
            }
        }

        private void CancelTicket_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int purchaseId)
            {
                var viewModel = (PurchaseHistoryViewModel)DataContext;
                viewModel.CancelTicket(purchaseId);
            }
        }
    }
}