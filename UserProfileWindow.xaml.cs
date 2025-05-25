using System.Windows;

namespace RouteBookingSystem
{
    public partial class UserProfileWindow : Window
    {
        private readonly string _email;
        private UserProfileViewModel _viewModel;

        public UserProfileWindow(string email)
        {
            InitializeComponent();
            _email = email;
            _viewModel = new UserProfileViewModel(email, PasswordBox, ConfirmPasswordBox);
            DataContext = _viewModel;
            this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("C:/BSTU/SEM4/OOP/lab4wpf5oop/lab4wpf5oop/Images/bus_icon-icons.com_76529.ico"));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            new UserDashboardWindow(_email).Show();
            this.Close();
        }
    }
}