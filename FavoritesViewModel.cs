using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Windows;
using lab4wpf5oop.Models;

namespace RouteBookingSystem
{
    public class FavoritesViewModel : INotifyPropertyChanged
    {
        private readonly string _connectionString = "Server=DESKTOP-DMJJTUE\\SQLEXPRESS;Database=XKKBUS;Trusted_Connection=True;";
        private readonly string _email;
        private ObservableCollection<FavoriteRoute> _favoriteRoutes;

        public FavoritesViewModel(string email)
        {
            _email = email;
            FavoriteRoutes = new ObservableCollection<FavoriteRoute>();
            LoadFavoriteRoutes();
        }

        public ObservableCollection<FavoriteRoute> FavoriteRoutes
        {
            get => _favoriteRoutes;
            set
            {
                _favoriteRoutes = value; // Исправлено: _favorite_routes -> _favoriteRoutes
                OnPropertyChanged();
            }
        }

        private void LoadFavoriteRoutes()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand(
                    "SELECT f.TicketId, t.[From], t.[To], t.[Date], t.[Time], t.[Type], t.Price, f.AddedDate, t.BoardingPoints, t.DropOffPoints " +
                    "FROM Favorites f JOIN Tickets t ON f.TicketId = t.Id WHERE f.EmailUser = @Email", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@Email", _email);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            FavoriteRoutes.Add(new FavoriteRoute
                            {
                                TicketId = reader.GetInt32(0),
                                From = reader.GetString(1),
                                To = reader.GetString(2),
                                Date = reader.GetDateTime(3),
                                Time = reader.GetTimeSpan(4).ToString(@"hh\:mm"),
                                Type = reader.GetString(5),
                                Price = reader.GetDouble(6),
                                AddedDate = reader.GetDateTime(7),
                                BoardingPoints = reader.GetString(8),
                                DropOffPoints = reader.GetString(9)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки избранных маршрутов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RemoveFavorite(int ticketId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("DELETE FROM Favorites WHERE EmailUser = @Email AND TicketId = @TicketId", conn))
                {
                    conn.Open();
                    cmd.Parameters.AddWithValue("@Email", _email);
                    cmd.Parameters.AddWithValue("@TicketId", ticketId);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        FavoriteRoutes.Remove(FavoriteRoutes.FirstOrDefault(r => r.TicketId == ticketId));
                        MessageBox.Show("Маршрут удалён из избранного!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Маршрут не найден в избранном!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class FavoriteRoute
    {
        public int TicketId { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public DateTime Date { get; set; }
        public string Time { get; set; }
        public string Type { get; set; }
        public double Price { get; set; }
        public DateTime AddedDate { get; set; }
        public string BoardingPoints { get; set; }
        public string DropOffPoints { get; set; }
    }
}