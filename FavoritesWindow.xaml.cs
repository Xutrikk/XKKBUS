using System.Windows;
using System.Windows.Controls;

namespace RouteBookingSystem
{
    public partial class FavoritesWindow : Window
    {
        private readonly string _email;

        public FavoritesWindow(string email)
        {
            InitializeComponent();
            _email = email;
            DataContext = new FavoritesViewModel(email);
            this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("C:/BSTU/SEM4/OOP/lab4wpf5oop/lab4wpf5oop/Images/bus_icon-icons.com_76529.ico"));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            new UserDashboardWindow(_email).Show();
            this.Close();
        }

        private void RemoveFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int ticketId)
            {
                var viewModel = (FavoritesViewModel)DataContext;
                viewModel.RemoveFavorite(ticketId);
            }
        }
    }
}