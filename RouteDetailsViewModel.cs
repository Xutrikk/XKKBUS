using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using lab4wpf5oop.Models;

namespace RouteBookingSystem
{
    public class RouteDetailsViewModel : INotifyPropertyChanged
    {
        private readonly string _connectionString = "Server=DESKTOP-DMJJTUE\\SQLEXPRESS;Database=XKKBUS;Trusted_Connection=True;";
        private readonly string _email;
        private readonly int _numberOfSeats;
        private string _selectedBoardingPoint;
        private string _selectedDropOffPoint;
        private string _purchaseStatus;
        private ObservableCollection<string> _boardingPoints;
        private ObservableCollection<string> _dropOffPoints;

        public Ticket Ticket { get; }
        public RelayCommand AddToFavoritesCommand { get; }
        public RelayCommand BookTicketCommand { get; }
        public RelayCommand PayTicketCommand { get; }
        public int NumberOfSeats => _numberOfSeats;
        public decimal CalculatedPrice => (decimal)(Ticket.Price * _numberOfSeats);

        public string SelectedBoardingPoint
        {
            get => _selectedBoardingPoint;
            set
            {
                _selectedBoardingPoint = value;
                OnPropertyChanged();
            }
        }

        public string SelectedDropOffPoint
        {
            get => _selectedDropOffPoint;
            set
            {
                _selectedDropOffPoint = value;
                OnPropertyChanged();
            }
        }

        public string PurchaseStatus
        {
            get => _purchaseStatus;
            set
            {
                _purchaseStatus = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> BoardingPoints
        {
            get => _boardingPoints;
            set
            {
                _boardingPoints = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<string> DropOffPoints
        {
            get => _dropOffPoints;
            set
            {
                _dropOffPoints = value;
                OnPropertyChanged();
            }
        }

        public RouteDetailsViewModel(string email, Ticket ticket, int numberOfSeats)
        {
            _email = email;
            _numberOfSeats = numberOfSeats;
            Ticket = ticket;
            AddToFavoritesCommand = new RelayCommand(_ => AddToFavorites());
            BookTicketCommand = new RelayCommand(_ => BookTicket());
            PayTicketCommand = new RelayCommand(_ => PayTicket());
            LoadBoardingAndDropOffPoints();
            CheckPurchaseStatus();
        }

        private void LoadBoardingAndDropOffPoints()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT BoardingPoints, DropOffPoints FROM Tickets WHERE Id = @Id", conn);
                    cmd.Parameters.AddWithValue("@Id", Ticket.Id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            BoardingPoints = new ObservableCollection<string>(reader.GetString(0).Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries));
                            DropOffPoints = new ObservableCollection<string>(reader.GetString(1).Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries));
                        }
                    }
                }
                SelectedBoardingPoint = BoardingPoints.FirstOrDefault();
                SelectedDropOffPoint = DropOffPoints.FirstOrDefault();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки точек посадки/высадки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckPurchaseStatus()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(
                        "SELECT Status FROM PurchasedTickets WHERE EmailUser = @Email AND TicketId = @TicketId",
                        conn);
                    cmd.Parameters.AddWithValue("@Email", _email);
                    cmd.Parameters.AddWithValue("@TicketId", Ticket.Id);
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        PurchaseStatus = (int)result == 0 ? "Не оплачено" : "Оплачено";
                    }
                    else
                    {
                        PurchaseStatus = "Не забронировано";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка проверки статуса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                PurchaseStatus = "Неизвестно";
            }
        }

        private void AddToFavorites()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    // Проверка существования маршрута в избранном
                    var checkCmd = new SqlCommand(
                        "SELECT COUNT(*) FROM Favorites WHERE EmailUser = @Email AND TicketId = @TicketId",
                        conn);
                    checkCmd.Parameters.AddWithValue("@Email", _email);
                    checkCmd.Parameters.AddWithValue("@TicketId", Ticket.Id);
                    int count = (int)checkCmd.ExecuteScalar();

                    if (count > 0)
                    {
                        MessageBox.Show("Этот маршрут уже добавлен в избранное!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var cmd = new SqlCommand(
                        @"INSERT INTO Favorites (EmailUser, TicketId, AddedDate) 
                          VALUES (@EmailUser, @TicketId, @AddedDate)",
                        conn);
                    cmd.Parameters.AddWithValue("@EmailUser", _email);
                    cmd.Parameters.AddWithValue("@TicketId", Ticket.Id);
                    cmd.Parameters.AddWithValue("@AddedDate", DateTime.Now);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Маршрут добавлен в избранное!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BookTicket()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Проверяем, не забронирован ли маршрут уже
                    var checkDuplicateCmd = new SqlCommand(
                        "SELECT COUNT(*) FROM PurchasedTickets WHERE EmailUser = @Email AND TicketId = @TicketId",
                        conn);
                    checkDuplicateCmd.Parameters.AddWithValue("@Email", _email);
                    checkDuplicateCmd.Parameters.AddWithValue("@TicketId", Ticket.Id);
                    int duplicateCount = (int)checkDuplicateCmd.ExecuteScalar();

                    if (duplicateCount > 0)
                    {
                        MessageBox.Show("Этот маршрут уже забронирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Проверяем, достаточно ли билетов
                    var checkCmd = new SqlCommand(
                        "SELECT Number FROM Tickets WHERE Id = @TicketId", conn);
                    checkCmd.Parameters.AddWithValue("@TicketId", Ticket.Id);
                    int availableTickets = (int)checkCmd.ExecuteScalar();

                    if (availableTickets < _numberOfSeats)
                    {
                        MessageBox.Show("Недостаточно доступных мест!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Уменьшаем количество доступных мест
                    var updateCmd = new SqlCommand(
                        "UPDATE Tickets SET Number = Number - @NumberOfSeats WHERE Id = @TicketId",
                        conn);
                    updateCmd.Parameters.AddWithValue("@NumberOfSeats", _numberOfSeats);
                    updateCmd.Parameters.AddWithValue("@TicketId", Ticket.Id);
                    updateCmd.ExecuteNonQuery();

                    // Сохраняем бронирование
                    var insertCmd = new SqlCommand(
                        @"INSERT INTO PurchasedTickets 
                          (PurchaseId, EmailUser, TicketId, [From], [To], PurchaseDate, PurchaseTime, Price, Number, Status, [Type], BoardingPoints, DropOffPoints) 
                          VALUES 
                          ((SELECT ISNULL(MAX(PurchaseId), 0) + 1 FROM PurchasedTickets), @EmailUser, @TicketId, @From, @To, @PurchaseDate, @PurchaseTime, @Price, @Number, @Status, @Type, @BoardingPoints, @DropOffPoints)",
                        conn);
                    insertCmd.Parameters.AddWithValue("@EmailUser", _email);
                    insertCmd.Parameters.AddWithValue("@TicketId", Ticket.Id);
                    insertCmd.Parameters.AddWithValue("@From", Ticket.From);
                    insertCmd.Parameters.AddWithValue("@To", Ticket.To);
                    insertCmd.Parameters.AddWithValue("@PurchaseDate", DateTime.Now.Date);
                    insertCmd.Parameters.AddWithValue("@PurchaseTime", DateTime.Now.ToString("HH:mm"));
                    insertCmd.Parameters.AddWithValue("@Price", (decimal)(Ticket.Price * _numberOfSeats));
                    insertCmd.Parameters.AddWithValue("@Number", _numberOfSeats);
                    insertCmd.Parameters.AddWithValue("@Status", 0);
                    insertCmd.Parameters.AddWithValue("@Type", Ticket.Type);
                    insertCmd.Parameters.AddWithValue("@BoardingPoints", SelectedBoardingPoint);
                    insertCmd.Parameters.AddWithValue("@DropOffPoints", SelectedDropOffPoint);
                    insertCmd.ExecuteNonQuery();
                }
                MessageBox.Show($"Маршрут успешно забронирован! Количество мест: {_numberOfSeats}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                CheckPurchaseStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка бронирования: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PayTicket()
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    // Проверяем, забронирован ли маршрут
                    var checkCmd = new SqlCommand(
                        "SELECT Status, Number, BoardingPoints, DropOffPoints FROM PurchasedTickets WHERE EmailUser = @Email AND TicketId = @TicketId",
                        conn);
                    checkCmd.Parameters.AddWithValue("@Email", _email);
                    checkCmd.Parameters.AddWithValue("@TicketId", Ticket.Id);
                    using (var reader = checkCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int status = reader.GetInt32(0);
                            int bookedSeats = reader.GetInt32(1);
                            string bookedBoardingPoint = reader.GetString(2);
                            string bookedDropOffPoint = reader.GetString(3);

                            if (status == 1)
                            {
                                MessageBox.Show("Маршрут уже оплачен!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            if (bookedSeats != _numberOfSeats)
                            {
                                MessageBox.Show($"Вы уже забронировали этот маршрут на другое кол-во мест!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            if (bookedBoardingPoint != SelectedBoardingPoint || bookedDropOffPoint != SelectedDropOffPoint)
                            {
                                MessageBox.Show("Вы забронировали этот маршрут с другими местами посадки и высадки!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                        else
                        {
                            MessageBox.Show("Сначала забронируйте маршрут!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    var paymentWindow = new PaymentWindow((decimal)(Ticket.Price * _numberOfSeats));
                    if (paymentWindow.ShowDialog() == true)
                    {

                        var updateCmd = new SqlCommand(
                            "UPDATE PurchasedTickets SET Status = 1 WHERE EmailUser = @Email AND TicketId = @TicketId",
                            conn);
                        updateCmd.Parameters.AddWithValue("@Email", _email);
                        updateCmd.Parameters.AddWithValue("@TicketId", Ticket.Id);
                        updateCmd.ExecuteNonQuery();

                        MessageBox.Show("Оплата успешно выполнена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        CheckPurchaseStatus();

                        var dashboardWindow = new UserDashboardWindow(_email);
                        dashboardWindow.Show();
                        (Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is RouteDetailsWindow))?.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка оплаты: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}