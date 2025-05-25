using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RouteBookingSystem
{
    public partial class RatingWindow : Window
    {
        private readonly string _email;
        private readonly int _purchaseId;
        private int _selectedRating = 0; 
        private bool _isHovering = false; 

        public RatingWindow(string email, int purchaseId)
        {
            InitializeComponent();
            _email = email;
            _purchaseId = purchaseId;
            this.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("C:/BSTU/SEM4/OOP/lab4wpf5oop/lab4wpf5oop/Images/bus_icon-icons.com_76529.ico"));
        }

        private void Star_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            _selectedRating = int.Parse(button.Tag.ToString());

            UpdateStarColors();
            _isHovering = false; 
        }

        private void Star_MouseEnter(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            _isHovering = true;
            int hoverRating = int.Parse(button.Tag.ToString());

            Star1.Foreground = hoverRating >= 1 ? Brushes.Gold : Brushes.Gray;
            Star2.Foreground = hoverRating >= 2 ? Brushes.Gold : Brushes.Gray;
            Star3.Foreground = hoverRating >= 3 ? Brushes.Gold : Brushes.Gray;
            Star4.Foreground = hoverRating >= 4 ? Brushes.Gold : Brushes.Gray;
            Star5.Foreground = hoverRating >= 5 ? Brushes.Gold : Brushes.Gray;
        }

        private void Star_MouseLeave(object sender, RoutedEventArgs e)
        {
            _isHovering = false;
            UpdateStarColors();
        }

        private void UpdateStarColors()
        {
            if (_isHovering) return; 

            Star1.Foreground = Brushes.Gray;
            Star2.Foreground = Brushes.Gray;
            Star3.Foreground = Brushes.Gray;
            Star4.Foreground = Brushes.Gray;
            Star5.Foreground = Brushes.Gray;

            if (_selectedRating >= 1) Star1.Foreground = Brushes.Gold;
            if (_selectedRating >= 2) Star2.Foreground = Brushes.Gold;
            if (_selectedRating >= 3) Star3.Foreground = Brushes.Gold;
            if (_selectedRating >= 4) Star4.Foreground = Brushes.Gold;
            if (_selectedRating >= 5) Star5.Foreground = Brushes.Gold;
        }

        private void SubmitRating_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRating == 0)
            {
                MessageBox.Show("Пожалуйста, выберите рейтинг!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string comment = CommentTextBox.Text;
            var viewModel = new PurchaseHistoryViewModel(_email);
            viewModel.SaveRating(_purchaseId, _selectedRating, comment);
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}