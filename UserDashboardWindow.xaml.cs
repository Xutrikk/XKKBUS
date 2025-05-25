using System.Windows;

namespace RouteBookingSystem
{
    public partial class UserDashboardWindow : Window
    {
        private readonly string _email;

        public UserDashboardWindow(string email)
        {
            InitializeComponent();
            _email = email;
            this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("C:/BSTU/SEM4/OOP/lab4wpf5oop/lab4wpf5oop/Images/bus_icon-icons.com_76529.ico"));
        }

        private void SearchRoutesButton_Click(object sender, RoutedEventArgs e)
        {
            new SearchRoutesWindow(_email).Show();
            this.Close();
        }

        private void ViewProfileButton_Click(object sender, RoutedEventArgs e)
        {
            new UserProfileWindow(_email).Show();
            this.Close();
        }

        private void OrderHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            new PurchaseHistoryWindow(_email).Show();
            this.Close();
        }

        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            new FavoritesWindow(_email).Show();
            this.Close();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            this.Close();
        }
    }
}