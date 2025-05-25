using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using lab4wpf5oop.Models;

namespace RouteBookingSystem
{
    public partial class SearchRoutesWindow : Window
    {
        private readonly string _email;

        public SearchRoutesWindow(string email)
        {
            InitializeComponent();
            _email = email;
            DataContext = new SearchRoutesViewModel(email);
            this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("C:/BSTU/SEM4/OOP/lab4wpf5oop/lab4wpf5oop/Images/bus_icon-icons.com_76529.ico"));
        }
        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Ticket ticket)
            {
                if (DataContext is SearchRoutesViewModel viewModel)
                {
                    viewModel.SelectedTicket = ticket;
                    viewModel.SelectRoute();
                }
            }
        }
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            new UserDashboardWindow(_email).Show();
            this.Close();
        }

        private void ComboBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                if (!comboBox.IsDropDownOpen)
                {
                    e.Handled = true;
                }
            }
        }
    }
}