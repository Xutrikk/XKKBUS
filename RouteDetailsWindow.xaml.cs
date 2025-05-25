using lab4wpf5oop.Models;
using System.Windows;

namespace RouteBookingSystem
{
    public partial class RouteDetailsWindow : Window
    {
        private readonly string _email;

        public RouteDetailsWindow(string email, Ticket ticket, int numberOfSeats)
        {
            InitializeComponent();
            _email = email;
            DataContext = new RouteDetailsViewModel(email, ticket, numberOfSeats);
            this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("C:/BSTU/SEM4/OOP/lab4wpf5oop/lab4wpf5oop/Images/bus_icon-icons.com_76529.ico"));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            new SearchRoutesWindow(_email).Show();
            this.Close();
        }
    }
}