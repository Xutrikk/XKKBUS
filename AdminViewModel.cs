using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LiveCharts;
using LiveCharts.Wpf;
using lab4wpf5oop.Models;

namespace RouteBookingSystem
{
    public class AdminViewModel : INotifyPropertyChanged
    {
        private readonly string connectionString;
        private ObservableCollection<Ticket> tickets;
        private ObservableCollection<User> users;
        private ObservableCollection<PurchasedTicket> purchasedTickets;
        private User selectedUser;
        private Ticket selectedTicket;
        private PurchasedTicket selectedPurchasedTicket;
        private string _ticketSearchQuery;
        private double _minPrice;
        private double _maxPrice = 60;
        private string _userSearchQuery;
        private bool _showAdminsOnly;
        private string _selectedUserPassword;
        private readonly User currentAdmin;
        private bool _isNewUser;
        private string _routeStatistics;
        private SeriesCollection _routeStatisticsSeries;

        private readonly Dictionary<string, string> _transportTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Bus"] = "Автобус",
            ["Marshrutka"] = "Маршрутка",
            ["Автобус"] = "Автобус",
            ["Маршрутка"] = "Маршрутка",
        };

        public bool IsNewUser
        {
            get => _isNewUser;
            set
            {
                _isNewUser = value;
                OnPropertyChanged();
            }
        }

        public string SelectedUserPassword
        {
            get => _selectedUserPassword;
            set
            {
                _selectedUserPassword = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<PurchasedTicket> PurchasedTickets
        {
            get => purchasedTickets;
            set
            {
                purchasedTickets = value;
                OnPropertyChanged();
                UpdateRouteStatistics();
            }
        }

        public PurchasedTicket SelectedPurchasedTicket
        {
            get => selectedPurchasedTicket;
            set
            {
                selectedPurchasedTicket = value;
                if (value != null)
                {
                    Console.WriteLine($"SelectedPurchasedTicket updated: PurchaseId={value.PurchaseId}, Status={value.Status}, Number={value.Number}, Price={value.Price}");
                }
                else
                {
                    Console.WriteLine("SelectedPurchasedTicket is null");
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedPurchasedTicket.StatusText));
                OnPropertyChanged(nameof(SelectedPurchasedTicket.Number));
                OnPropertyChanged(nameof(SelectedPurchasedTicket.Price));
            }
        }

        public string RouteStatistics
        {
            get => _routeStatistics;
            set
            {
                _routeStatistics = value;
                OnPropertyChanged();
            }
        }

        public SeriesCollection RouteStatisticsSeries
        {
            get => _routeStatisticsSeries;
            set
            {
                _routeStatisticsSeries = value;
                OnPropertyChanged();
            }
        }

        public AdminViewModel(string connString, User currentAdmin)
        {
            connectionString = connString;
            this.currentAdmin = currentAdmin;
            LoadData();
            InitializeCommands();
            FilteredTickets = new ObservableCollection<Ticket>(Tickets);
            FilteredUsers = new ObservableCollection<User>(Users);
            FilteredPurchasedTickets = new ObservableCollection<PurchasedTicket>(PurchasedTickets);
            if (FilteredPurchasedTickets.Count > 0) SelectedPurchasedTicket = FilteredPurchasedTickets[0]; // Установка первой записи
            UpdateTransportTypes();
            UpdateRouteStatistics();
        }

        private void UpdateRouteStatistics()
        {
            var routeStats = PurchasedTickets
                .GroupBy(pt => $"{pt.From}-{pt.To}")
                .Select(g => new
                {
                    Route = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(g => g.Count)
                .ToList();

            int totalOrders = PurchasedTickets.Count;
            if (totalOrders == 0)
            {
                RouteStatistics = "Нет заказов для отображения статистики.";
                RouteStatisticsSeries = new SeriesCollection();
                return;
            }

            var pieSeries = new SeriesCollection();
            foreach (var stat in routeStats)
            {
                double percentage = (double)stat.Count / totalOrders * 100;
                pieSeries.Add(new PieSeries
                {
                    Title = stat.Route,
                    Values = new ChartValues<double> { stat.Count },
                    DataLabels = true
                });
            }

            RouteStatistics = "";
            RouteStatisticsSeries = pieSeries;
        }

        private string MapTransportType(string inputType)
        {
            return _transportTypeMap.TryGetValue(inputType, out var result)
                ? result
                : inputType;
        }

        private string MapTransportTypeForFilter(string inputKey)
        {
            if (inputKey == "AllTypes") return null;
            return _transportTypeMap.TryGetValue(inputKey, out var value) ? value : null;
        }

        private bool ValidateUser(User user, string password, bool requirePassword, out string errorMessage)
        {
            var errors = new StringBuilder();
            CheckField(user.FirstName, "Имя", errors);
            CheckField(user.LastName, "Фамилия", errors);
            CheckField(user.Surname, "Отчество", errors);
            CheckField(user.Email, "Email", errors);
            if (requirePassword)
            {
                CheckField(password, "Пароль", errors);
            }
            CheckField(user.PhoneNumber, "Номер телефона", errors);

            if (!string.IsNullOrEmpty(user.FirstName) && !Regex.IsMatch(user.FirstName, @"^[а-яА-ЯёЁa-zA-Z]+$"))
            {
                errors.AppendLine("• Имя должно содержать только русские или английские буквы");
            }
            if (!string.IsNullOrEmpty(user.LastName) && !Regex.IsMatch(user.LastName, @"^[а-яА-ЯёЁa-zA-Z]+$"))
            {
                errors.AppendLine("• Фамилия должно содержать только русские или английские буквы");
            }
            if (!string.IsNullOrEmpty(user.Surname) && !Regex.IsMatch(user.Surname, @"^[а-яА-ЯёЁa-zA-Z]+$"))
            {
                errors.AppendLine("• Отчество должно содержать только русские или английские буквы");
            }
            if (!IsValidEmail(user.Email))
            {
                errors.AppendLine("• Неверный формат Email (только английские буквы и цифры, без спецсимволов (кроме @ и .), без пробелов)");
            }
            if (!IsValidPhone(user.PhoneNumber))
            {
                errors.AppendLine("• Телефон должен быть в формате: +375XXXXXXXXX");
            }
            if (requirePassword && !string.IsNullOrEmpty(password) && !IsValidPassword(password))
            {
                errors.AppendLine("• Пароль должен содержать минимум 6 символов, только английские буквы, цифры и спецсимволы, без пробелов");
            }

            errorMessage = errors.ToString();
            return errorMessage.Length == 0;
        }

        private bool ValidateTicket(Ticket ticket, out string errorMessage)
        {
            var errors = new StringBuilder();
            bool isValid = true;

            CheckField(ticket.From, "Откуда", errors);
            CheckField(ticket.To, "Куда", errors);
            CheckField(ticket.Date.ToString(), "Дата", errors);
            CheckField(ticket.Time, "Время", errors);
            CheckField(ticket.Type, "Тип транспорта", errors);
            CheckField(ticket.Description, "Описание", errors);
            CheckField(ticket.BoardingPoints, "Место посадки", errors);
            CheckField(ticket.DropOffPoints, "Место высадки", errors);

            if (ticket.Date < DateTime.Today)
            {
                errors.AppendLine("• Дата маршрута не может быть раньше текущей даты");
                isValid = false;
            }

            if (ticket.Price <= 0 || !Regex.IsMatch(ticket.Price.ToString(CultureInfo.InvariantCulture), @"^\d+\.?\d*$"))
            {
                errors.AppendLine("• Цена должна быть положительным числом ");
                isValid = false;
            }

            if (ticket.Number <= 0)
            {
                errors.AppendLine("• Количество должно быть целым числом больше 0");
                isValid = false;
            }

            if (!TimeSpan.TryParseExact(ticket.Time, @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan ticketTime))
            {
                errors.AppendLine("• Время должно быть в формате HH:mm");
                isValid = false;
            }
            else
            {
                if (ticket.Date.Date == DateTime.Today)
                {
                    TimeSpan currentTime = DateTime.Now.TimeOfDay;
                    TimeSpan minAllowedTime = currentTime.Add(TimeSpan.FromHours(8));
                    if (minAllowedTime >= TimeSpan.FromHours(24))
                    {
                        errors.AppendLine("• Сегодня уже нельзя добавить маршрут — текущее время не позволяет!");
                        isValid = false;
                    }
                    else if (ticketTime < minAllowedTime)
                    {
                        errors.AppendLine($"• Время для сегодняшней даты должно быть не ранее {minAllowedTime.Hours:D2}:{minAllowedTime.Minutes:D2}");
                        isValid = false;
                    }
                }
            }

            if (!string.IsNullOrEmpty(ticket.From) && !Regex.IsMatch(ticket.From, @"^[а-яА-ЯёЁa-zA-Z\s-,]+$"))
            {
                errors.AppendLine("• Поле 'Откуда' должно содержать только русские или английские буквы, пробелы, дефисы, запятые");
                isValid = false;
            }
            if (!string.IsNullOrEmpty(ticket.To) && !Regex.IsMatch(ticket.To, @"^[а-яА-ЯёЁa-zA-Z\s-,]+$"))
            {
                errors.AppendLine("• Поле 'Куда' должно содержать только русские или английские буквы, пробелы, дефисы, запятые");
                isValid = false;
            }
            if (!string.IsNullOrEmpty(ticket.BoardingPoints) &&
                !Regex.IsMatch(ticket.BoardingPoints, @"^[а-яА-ЯёЁa-zA-Z\s-,]*(?:[а-яА-ЯёЁa-zA-Z]+[0-9]+|[0-9]+[а-яА-ЯёЁa-zA-Z]+)[а-яА-ЯёЁa-zA-Z\s-,]*$|^[а-яА-ЯёЁa-zA-Z\s-,]+$"))
            {
                errors.AppendLine("• Поле 'Место посадки' должно содержать только русские или английские буквы, числа (в сочетании с буквами), пробелы, дефисы, запятые");
                isValid = false;
            }
            if (!string.IsNullOrEmpty(ticket.DropOffPoints) &&
                !Regex.IsMatch(ticket.DropOffPoints, @"^[а-яА-ЯёЁa-zA-Z\s-,]*(?:[а-яА-ЯёЁa-zA-Z]+[0-9]+|[0-9]+[а-яА-ЯёЁa-zA-Z]+)[а-яА-ЯёЁa-zA-Z\s-,]*$|^[а-яА-ЯёЁa-zA-Z\s-,]+$"))
            {
                errors.AppendLine("• Поле 'Место высадки' должно содержать только русские или английские буквы, числа (в сочетании с буквами), пробелы, дефисы, запятые");
                isValid = false;
            }
            if (!string.IsNullOrEmpty(ticket.Description) && !Regex.IsMatch(ticket.Description, @"^[а-яА-ЯёЁa-zA-Z\s-,]+$"))
            {
                errors.AppendLine("• Поле 'Описание' должно содержать только русские или английские буквы, пробелы, дефисы, запятые");
                isValid = false;
            }
            if (!string.IsNullOrEmpty(ticket.Company) && !Regex.IsMatch(ticket.Company, @"^[а-яА-ЯёЁa-zA-Z\s-,]+$"))
            {
                errors.AppendLine("• Поле 'Компания' должно содержать только русские или английские буквы, пробелы, дефисы, запятые");
                isValid = false;
            }

            errorMessage = errors.ToString();
            return isValid && errors.Length == 0;
        }

        private void CheckField(string value, string fieldName, StringBuilder errors)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.AppendLine($"• Поле '{fieldName}' обязательно для заполнения");
            }
        }

        private bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[a-zA-Z][a-zA-Z0-9]*@[a-zA-Z][a-zA-Z0-9]*\.[a-zA-Z]+$");
        }

        private bool IsValidPhone(string phone)
        {
            return Regex.IsMatch(phone, @"^\+375(25|29|33|44)\d{7}$");
        }

        private bool IsValidPassword(string password)
        {
            return !string.IsNullOrEmpty(password) && password.Length >= 6 &&
                   Regex.IsMatch(password, @"^[a-zA-Z0-9!@#$%^&*()+\-=\[\]{};:'"",.<>?]+$") &&
                   !password.Contains(" ");
        }

        public ObservableCollection<Ticket> Tickets
        {
            get => tickets;
            set { tickets = value; OnPropertyChanged(); }
        }

        public ObservableCollection<User> Users
        {
            get => users;
            set { users = value; OnPropertyChanged(); }
        }

        public User SelectedUser
        {
            get => selectedUser;
            set
            {
                if (value != null)
                {
                    selectedUser = new User
                    {
                        FirstName = value.FirstName,
                        LastName = value.LastName,
                        Surname = value.Surname,
                        Email = value.Email,
                        PasswordHash = value.PasswordHash,
                        PhoneNumber = value.PhoneNumber,
                        IsAdmin = value.IsAdmin,
                        IsBlocked = value.IsBlocked
                    };
                }
                else
                {
                    selectedUser = null;
                }
                OnPropertyChanged();
                SelectedUserPassword = "";
                IsNewUser = selectedUser != null && !Users.Any(u => u.Email == selectedUser.Email);
            }
        }

        public Ticket SelectedTicket
        {
            get => selectedTicket;
            set { selectedTicket = value; OnPropertyChanged(); }
        }

        public string TicketSearchQuery
        {
            get => _ticketSearchQuery;
            set { _ticketSearchQuery = value; OnPropertyChanged(); UpdateFilteredTickets(); }
        }

        public double MinPrice
        {
            get => _minPrice;
            set { _minPrice = value; OnPropertyChanged(); UpdateFilteredTickets(); }
        }

        public double MaxPrice
        {
            get => _maxPrice;
            set { _maxPrice = value; OnPropertyChanged(); UpdateFilteredTickets(); }
        }

        public string UserSearchQuery
        {
            get => _userSearchQuery;
            set { _userSearchQuery = value; OnPropertyChanged(); UpdateFilteredUsers(); }
        }

        public bool ShowAdminsOnly
        {
            get => _showAdminsOnly;
            set { _showAdminsOnly = value; OnPropertyChanged(); UpdateFilteredUsers(); }
        }

        private string _selectedTransportType;
        public string SelectedTransportType
        {
            get => _selectedTransportType;
            set
            {
                _selectedTransportType = value;
                OnPropertyChanged();
                UpdateFilteredTickets();
            }
        }

        public ObservableCollection<Ticket> FilteredTickets { get; set; }
        public ObservableCollection<User> FilteredUsers { get; set; }
        public ObservableCollection<PurchasedTicket> FilteredPurchasedTickets { get; set; }

        private ObservableCollection<string> _transportTypesForFilter;
        public ObservableCollection<string> TransportTypesForFilter
        {
            get => _transportTypesForFilter;
            set { _transportTypesForFilter = value; OnPropertyChanged(); }
        }

        private ObservableCollection<string> _transportTypesForEdit;
        public ObservableCollection<string> TransportTypesForEdit
        {
            get => _transportTypesForEdit;
            set { _transportTypesForEdit = value; OnPropertyChanged(); }
        }

        public ICommand DeleteTicketCommand { get; private set; }
        public ICommand SaveTicketCommand { get; private set; }
        public ICommand AddTicketCommand { get; private set; }
        public ICommand DeleteUserCommand { get; private set; }
        public ICommand SaveUserCommand { get; private set; }
        public ICommand AddUserCommand { get; private set; }
        public ICommand DeletePurchasedTicketCommand { get; private set; }
        public ICommand UpdateUserAdminCommand { get; private set; }
        public ICommand UpdateUserBlockedCommand { get; private set; }

        private void InitializeCommands()
        {
            DeleteTicketCommand = new RelayCommand(_ => DeleteTicket());
            SaveTicketCommand = new RelayCommand(_ => SaveTicket());
            AddTicketCommand = new RelayCommand(_ => AddTicket());
            DeleteUserCommand = new RelayCommand(_ => DeleteUser());
            SaveUserCommand = new RelayCommand(_ => SaveUser());
            AddUserCommand = new RelayCommand(_ => AddUser());
            DeletePurchasedTicketCommand = new RelayCommand(_ => DeletePurchasedTicket());
            UpdateUserAdminCommand = new RelayCommand(_ => UpdateUserAdmin());
            UpdateUserBlockedCommand = new RelayCommand(_ => UpdateUserBlocked());
        }

        private void UpdateUserAdmin()
        {
            if (SelectedUser == null) return;
            if (SelectedUser.Email == currentAdmin.Email && !SelectedUser.IsAdmin)
            {
                MessageBox.Show("Вы не можете снять статус администратора с самого себя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                SelectedUser.IsAdmin = true;
                return;
            }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(
                        "UPDATE Users SET isAdmin = @IsAdmin WHERE Email = @Email", conn);
                    cmd.Parameters.AddWithValue("@IsAdmin", SelectedUser.IsAdmin);
                    cmd.Parameters.AddWithValue("@Email", SelectedUser.Email);
                    cmd.ExecuteNonQuery();

                    var existingUser = Users.FirstOrDefault(u => u.Email == SelectedUser.Email);
                    if (existingUser != null)
                    {
                        existingUser.IsAdmin = SelectedUser.IsAdmin;
                    }
                    UpdateFilteredUsers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления статуса администратора: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUserBlocked()
        {
            if (SelectedUser == null) return;
            if (SelectedUser.Email == currentAdmin.Email && SelectedUser.IsBlocked)
            {
                MessageBox.Show("Вы не можете заблокировать самого себя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                SelectedUser.IsBlocked = false;
                return;
            }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(
                        "UPDATE Users SET IsBlocked = @IsBlocked WHERE Email = @Email", conn);
                    cmd.Parameters.AddWithValue("@IsBlocked", SelectedUser.IsBlocked);
                    cmd.Parameters.AddWithValue("@Email", SelectedUser.Email);
                    cmd.ExecuteNonQuery();

                    var existingUser = Users.FirstOrDefault(u => u.Email == SelectedUser.Email);
                    if (existingUser != null)
                    {
                        existingUser.IsBlocked = SelectedUser.IsBlocked;
                    }
                    UpdateFilteredUsers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления статуса блокировки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadData()
        {
            Tickets = new ObservableCollection<Ticket>(LoadTickets());
            Users = new ObservableCollection<User>(LoadUsers());
            PurchasedTickets = new ObservableCollection<PurchasedTicket>(LoadPurchasedTickets());
            FilteredUsers = new ObservableCollection<User>(Users);
            FilteredPurchasedTickets = new ObservableCollection<PurchasedTicket>(PurchasedTickets);
            OnPropertyChanged(nameof(FilteredUsers));
            OnPropertyChanged(nameof(FilteredPurchasedTickets));
        }

        public void RefreshTransportTypes()
        {
            UpdateTransportTypes();
            UpdateFilteredTickets();
            OnPropertyChanged(nameof(TransportTypesForFilter));
        }

        private void UpdateTransportTypes()
        {
            TransportTypesForFilter = new ObservableCollection<string>
            {
                Application.Current.Resources["AllTypes"].ToString(),
                Application.Current.Resources["Bus"].ToString(),
                Application.Current.Resources["Marshrutka"].ToString()
            };

            TransportTypesForEdit = new ObservableCollection<string>(
                _transportTypeMap.Values.Distinct().ToList()
            );
            SelectedTransportType = Application.Current.Resources["AllTypes"].ToString();
        }

        public void UpdateFilteredTickets()
        {
            var dbType = MapTransportTypeForFilter(SelectedTransportType);

            var filtered = Tickets
                .Where(t => (string.IsNullOrEmpty(TicketSearchQuery) ||
                           t.From.Contains(TicketSearchQuery, StringComparison.OrdinalIgnoreCase) ||
                           t.To.Contains(TicketSearchQuery, StringComparison.OrdinalIgnoreCase)))
                .Where(t => t.Price >= MinPrice && t.Price <= MaxPrice)
                .Where(t => dbType == null || t.Type == dbType)
                .ToList();

            FilteredTickets = new ObservableCollection<Ticket>(filtered);
            OnPropertyChanged(nameof(FilteredTickets));
        }

        private void UpdateFilteredUsers()
        {
            var filtered = Users
                .Where(u => (string.IsNullOrEmpty(UserSearchQuery) ||
                            u.Email.Contains(UserSearchQuery, StringComparison.OrdinalIgnoreCase) ||
                            u.LastName.Contains(UserSearchQuery, StringComparison.OrdinalIgnoreCase)) &&
                            (!ShowAdminsOnly || u.IsAdmin))
                .ToList();

            FilteredUsers = new ObservableCollection<User>(filtered);
            OnPropertyChanged(nameof(FilteredUsers));
        }

        private void DeleteTicket()
        {
            if (SelectedTicket == null) return;
            if (MessageBox.Show("Удалить этот маршрут?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.No) return;
            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("DELETE FROM Tickets WHERE Id = @Id", conn))
                {
                    cmd.Parameters.AddWithValue("@Id", SelectedTicket.Id);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    Tickets.Remove(SelectedTicket);
                    UpdateFilteredTickets();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}");
            }
        }

        private void SaveTicket()
        {
            if (SelectedTicket == null) return;
            SelectedTicket.Type = MapTransportType(SelectedTicket.Type);
            if (!ValidateTicket(SelectedTicket, out var errorMessage))
            {
                MessageBox.Show(errorMessage, "Ошибки ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    if (SelectedTicket.Id == 0)
                    {
                        var cmd = new SqlCommand(
                            @"INSERT INTO Tickets ([From], [To], Date, Time, Price, Number, Description, Type, BoardingPoints, DropOffPoints, Company)
                            VALUES (@From, @To, @Date, @Time, @Price, @Number, @Description, @Type, @BoardingPoints, @DropOffPoints, @Company);
                            SELECT SCOPE_IDENTITY();", conn);
                        SetTicketParameters(cmd);
                        SelectedTicket.Id = Convert.ToInt32(cmd.ExecuteScalar());
                        Tickets.Add(SelectedTicket);
                    }
                    else
                    {
                        var cmd = new SqlCommand(
                            @"UPDATE Tickets SET [From] = @From, [To] = @To, Date = @Date, Time = @Time, Price = @Price,
                                Number = @Number, Description = @Description, Type = @Type, BoardingPoints = @BoardingPoints,
                                DropOffPoints = @DropOffPoints, Company = @Company WHERE Id = @Id", conn);
                        cmd.Parameters.AddWithValue("@Id", SelectedTicket.Id);
                        SetTicketParameters(cmd);
                        cmd.ExecuteNonQuery();
                    }
                    UpdateFilteredTickets();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
            MessageBox.Show("Маршрут успешно сохранён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SetTicketParameters(SqlCommand cmd)
        {
            cmd.Parameters.AddWithValue("@From", SelectedTicket.From ?? "");
            cmd.Parameters.AddWithValue("@To", SelectedTicket.To ?? "");
            cmd.Parameters.AddWithValue("@Date", SelectedTicket.Date);
            cmd.Parameters.AddWithValue("@Time", SelectedTicket.Time);
            cmd.Parameters.AddWithValue("@Price", SelectedTicket.Price);
            cmd.Parameters.AddWithValue("@Number", SelectedTicket.Number);
            cmd.Parameters.AddWithValue("@Description", SelectedTicket.Description ?? "");
            cmd.Parameters.AddWithValue("@Type", SelectedTicket.Type);
            cmd.Parameters.AddWithValue("@BoardingPoints", SelectedTicket.BoardingPoints ?? "");
            cmd.Parameters.AddWithValue("@DropOffPoints", SelectedTicket.DropOffPoints ?? "");
            cmd.Parameters.AddWithValue("@Company", SelectedTicket.Company ?? "");
        }

        private void AddTicket()
        {
            SelectedTicket = new Ticket
            {
                Date = DateTime.Today,
                Time = DateTime.Now.ToString("HH:mm"),
                Price = 0,
                Number = 0,
                Type = TransportTypesForEdit.FirstOrDefault(),
                BoardingPoints = "",
                DropOffPoints = "",
                Company = ""
            };
        }

        private void DeleteUser()
        {
            if (SelectedUser == null) return;
            if (SelectedUser.Email == currentAdmin.Email)
            {
                MessageBox.Show("Вы не можете удалить самого себя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show("Удалить этого пользователя?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.No) return;
            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand("DELETE FROM Users WHERE Email = @Email", conn))
                {
                    cmd.Parameters.AddWithValue("@Email", SelectedUser.Email);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                    Users.Remove(Users.FirstOrDefault(u => u.Email == SelectedUser.Email));
                    UpdateFilteredUsers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}");
            }
        }

        private void DeletePurchasedTicket()
        {
            if (SelectedPurchasedTicket == null) return;
            if (MessageBox.Show("Удалить этот заказ?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.No) return;
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var ticket = Tickets.FirstOrDefault(t => t.Id == SelectedPurchasedTicket.TicketId);
                    if (ticket != null)
                    {
                        ticket.Number += SelectedPurchasedTicket.Number;
                        var updateCmd = new SqlCommand("UPDATE Tickets SET Number = @Number WHERE Id = @Id", conn);
                        updateCmd.Parameters.AddWithValue("@Number", ticket.Number);
                        updateCmd.Parameters.AddWithValue("@Id", ticket.Id);
                        updateCmd.ExecuteNonQuery();
                    }
                    var deleteCmd = new SqlCommand("DELETE FROM PurchasedTickets WHERE PurchaseId = @PurchaseId", conn);
                    deleteCmd.Parameters.AddWithValue("@PurchaseId", SelectedPurchasedTicket.PurchaseId);
                    deleteCmd.ExecuteNonQuery();
                    PurchasedTickets.Remove(SelectedPurchasedTicket);
                    FilteredPurchasedTickets.Remove(SelectedPurchasedTicket);
                    SelectedPurchasedTicket = null;
                    UpdateRouteStatistics();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка удаления: {ex.Message}");
            }
        }

        private void SaveUser()
        {
            if (SelectedUser == null) return;
            var tempUser = new User
            {
                FirstName = SelectedUser.FirstName,
                LastName = SelectedUser.LastName,
                Surname = SelectedUser.Surname,
                Email = SelectedUser.Email,
                PasswordHash = SelectedUser.PasswordHash,
                PhoneNumber = SelectedUser.PhoneNumber,
                IsAdmin = SelectedUser.IsAdmin,
                IsBlocked = SelectedUser.IsBlocked
            };

            bool requirePassword = IsNewUser || !string.IsNullOrEmpty(SelectedUserPassword);
            if (!ValidateUser(tempUser, SelectedUserPassword, requirePassword, out var errorMessage))
            {
                MessageBox.Show(errorMessage, "Ошибки ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (tempUser.IsBlocked && tempUser.Email == currentAdmin.Email)
            {
                MessageBox.Show("Вы не можете заблокировать самого себя!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(SelectedUserPassword))
            {
                tempUser.PasswordHash = HashPassword(SelectedUserPassword);
            }
            else if (!IsNewUser)
            {
                var existingUser = Users.FirstOrDefault(u => u.Email == tempUser.Email);
                if (existingUser != null)
                {
                    tempUser.PasswordHash = existingUser.PasswordHash;
                }
            }

            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Email = @Email", conn);
                    checkCmd.Parameters.AddWithValue("@Email", tempUser.Email);
                    int existingCount = (int)checkCmd.ExecuteScalar();
                    if (existingCount > 0 && IsNewUser)
                    {
                        MessageBox.Show("Пользователь с таким email уже существует!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (IsNewUser)
                    {
                        var cmd = new SqlCommand(
                            @"INSERT INTO Users (FirstName, LastName, Surname, Email, PasswordHash, PhoneNumber, isAdmin, IsBlocked)
                            VALUES (@FirstName, @LastName, @Surname, @Email, @PasswordHash, @PhoneNumber, @IsAdmin, @IsBlocked)", conn);
                        SetUserParameters(cmd, tempUser);
                        cmd.ExecuteNonQuery();
                        Users.Add(tempUser);
                        MessageBox.Show("Пользователь успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        AddUser();
                    }
                    else
                    {
                        var cmd = new SqlCommand(
                            @"UPDATE Users SET FirstName = @FirstName, LastName = @LastName, Surname = @Surname,
                            PhoneNumber = @PhoneNumber, isAdmin = @IsAdmin, IsBlocked = @IsBlocked, PasswordHash = @PasswordHash
                            WHERE Email = @Email", conn);
                        SetUserParameters(cmd, tempUser);
                        cmd.ExecuteNonQuery();
                        var existingUser = Users.FirstOrDefault(u => u.Email == tempUser.Email);
                        if (existingUser != null)
                        {
                            existingUser.FirstName = tempUser.FirstName;
                            existingUser.LastName = tempUser.LastName;
                            existingUser.Surname = tempUser.Surname;
                            existingUser.PhoneNumber = tempUser.PhoneNumber;
                            existingUser.IsAdmin = tempUser.IsAdmin;
                            existingUser.IsBlocked = tempUser.IsBlocked;
                            existingUser.PasswordHash = tempUser.PasswordHash;
                        }
                        SelectedUser.FirstName = tempUser.FirstName;
                        SelectedUser.LastName = tempUser.LastName;
                        SelectedUser.Surname = tempUser.Surname;
                        SelectedUser.PhoneNumber = tempUser.PhoneNumber;
                        SelectedUser.IsAdmin = tempUser.IsAdmin;
                        SelectedUser.IsBlocked = tempUser.IsBlocked;
                        SelectedUser.PasswordHash = tempUser.PasswordHash;
                        MessageBox.Show("Пользователь успешно отредактирован!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    UpdateFilteredUsers();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void SetUserParameters(SqlCommand cmd, User user)
        {
            cmd.Parameters.AddWithValue("@FirstName", user.FirstName ?? "");
            cmd.Parameters.AddWithValue("@LastName", user.LastName ?? "");
            cmd.Parameters.AddWithValue("@Surname", user.Surname ?? "");
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber ?? "");
            cmd.Parameters.AddWithValue("@IsAdmin", user.IsAdmin);
            cmd.Parameters.AddWithValue("@IsBlocked", user.IsBlocked);
        }

        private void AddUser()
        {
            SelectedUser = new User
            {
                FirstName = "",
                LastName = "",
                Surname = "",
                Email = "",
                PasswordHash = "",
                PhoneNumber = "",
                IsAdmin = false,
                IsBlocked = false
            };
            SelectedUserPassword = "";
            IsNewUser = true;
        }

        private ObservableCollection<Ticket> LoadTickets()
        {
            var tickets = new ObservableCollection<Ticket>();
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("SELECT * FROM Tickets", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var dbType = reader["Type"].ToString();
                        var localizedType = _transportTypeMap.FirstOrDefault(x => x.Value == dbType).Key ?? dbType;
                        var displayType = Application.Current.Resources[localizedType]?.ToString() ?? dbType;
                        tickets.Add(new Ticket
                        {
                            Id = reader.GetInt32(0),
                            From = reader.GetString(1),
                            To = reader.GetString(2),
                            Date = reader.GetDateTime(3),
                            Time = reader.GetTimeSpan(4).ToString(@"hh\:mm"),
                            Price = reader.GetDouble(5),
                            Number = reader.GetInt32(6),
                            Description = reader.GetString(7),
                            Type = displayType,
                            BoardingPoints = reader.GetString(9),
                            DropOffPoints = reader.GetString(10),
                            Company = reader.IsDBNull(11) ? "" : reader.GetString(11)
                        });
                    }
                }
            }
            return tickets;
        }

        private ObservableCollection<User> LoadUsers()
        {
            var users = new ObservableCollection<User>();
            using (var conn = new SqlConnection(connectionString))
            using (var cmd = new SqlCommand("SELECT * FROM Users", conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        users.Add(new User
                        {
                            FirstName = reader.GetString(0),
                            LastName = reader.GetString(1),
                            Surname = reader.GetString(2),
                            Email = reader.GetString(3),
                            PasswordHash = reader.GetString(4),
                            PhoneNumber = reader.IsDBNull(5) ? null : reader.GetString(5),
                            IsAdmin = reader.GetBoolean(6),
                            IsBlocked = reader.GetBoolean(7)
                        });
                    }
                }
            }
            return users;
        }

        private ObservableCollection<PurchasedTicket> LoadPurchasedTickets()
        {
            var purchasedTickets = new ObservableCollection<PurchasedTicket>();
            try
            {
                using (var conn = new SqlConnection(connectionString))
                using (var cmd = new SqlCommand(
                    @"SELECT pt.PurchaseId, pt.EmailUser, pt.TicketId, pt.PurchaseDate, pt.PurchaseTime, pt.Price, pt.Number, pt.Status,
                     pt.[From], pt.[To], t.Date AS RouteDate, t.Time AS RouteTime, pt.Type, pt.BoardingPoints, pt.DropOffPoints
              FROM PurchasedTickets pt
              INNER JOIN Tickets t ON pt.TicketId = t.Id", conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var ticket = new PurchasedTicket
                            {
                                PurchaseId = reader.IsDBNull(reader.GetOrdinal("PurchaseId")) ? 0 : reader.GetInt32(reader.GetOrdinal("PurchaseId")),
                                EmailUser = reader.IsDBNull(reader.GetOrdinal("EmailUser")) ? "" : reader.GetString(reader.GetOrdinal("EmailUser")),
                                TicketId = reader.IsDBNull(reader.GetOrdinal("TicketId")) ? 0 : reader.GetInt32(reader.GetOrdinal("TicketId")),
                                PurchaseDate = reader.IsDBNull(reader.GetOrdinal("PurchaseDate")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("PurchaseDate")),
                                PurchaseTime = reader.IsDBNull(reader.GetOrdinal("PurchaseTime")) ? "00:00" : reader.GetTimeSpan(reader.GetOrdinal("PurchaseTime")).ToString(@"hh\:mm"),
                                Price = reader.IsDBNull(reader.GetOrdinal("Price")) ? 0.0 : reader.GetDouble(reader.GetOrdinal("Price")),
                                Number = reader.IsDBNull(reader.GetOrdinal("Number")) ? 0 : reader.GetInt32(reader.GetOrdinal("Number")),
                                Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? 0 : reader.GetInt32(reader.GetOrdinal("Status")),
                                From = reader.IsDBNull(reader.GetOrdinal("From")) ? "" : reader.GetString(reader.GetOrdinal("From")),
                                To = reader.IsDBNull(reader.GetOrdinal("To")) ? "" : reader.GetString(reader.GetOrdinal("To")),
                                RouteDate = reader.IsDBNull(reader.GetOrdinal("RouteDate")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("RouteDate")),
                                RouteTime = reader.IsDBNull(reader.GetOrdinal("RouteTime")) ? "00:00" : reader.GetTimeSpan(reader.GetOrdinal("RouteTime")).ToString(@"hh\:mm"),
                                Type = reader.IsDBNull(reader.GetOrdinal("Type")) ? "" : reader.GetString(reader.GetOrdinal("Type")),
                                BoardingPoints = reader.IsDBNull(reader.GetOrdinal("BoardingPoints")) ? "" : reader.GetString(reader.GetOrdinal("BoardingPoints")),
                                DropOffPoints = reader.IsDBNull(reader.GetOrdinal("DropOffPoints")) ? "" : reader.GetString(reader.GetOrdinal("DropOffPoints"))
                            };
                            purchasedTickets.Add(ticket);
                        }
                    }
                }
                foreach (var ticket in purchasedTickets)
                {
                    LoadRatingAndCommentForTicket(ticket);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return purchasedTickets;
        }

        private void LoadRatingAndCommentForTicket(PurchasedTicket ticket)
        {
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(
                        "SELECT Rating, Comment FROM TripRatings WHERE PurchaseId = @PurchaseId AND EmailUser = @EmailUser",
                        conn);
                    cmd.Parameters.AddWithValue("@PurchaseId", ticket.PurchaseId);
                    cmd.Parameters.AddWithValue("@EmailUser", ticket.EmailUser);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            ticket.Rating = reader.IsDBNull(reader.GetOrdinal("Rating")) ? null : (int?)reader.GetInt32(reader.GetOrdinal("Rating"));
                            ticket.Comment = reader.IsDBNull(reader.GetOrdinal("Comment")) ? null : reader.GetString(reader.GetOrdinal("Comment"));
                        }
                        else
                        {
                            ticket.Rating = null;
                            ticket.Comment = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки рейтинга для заказа {ticket.PurchaseId}: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class PurchasedTicket : INotifyPropertyChanged
        {
            private int _purchaseId;
            private string _emailUser;
            private int _ticketId;
            private DateTime _purchaseDate;
            private string _purchaseTime;
            private double _price;
            private int _number;
            private int _status;
            private string _from;
            private string _to;
            private DateTime _routeDate;
            private string _routeTime;
            private string _type;
            private string _boardingPoints;
            private string _dropOffPoints;
            private int? _rating;
            private string _comment;

            public int PurchaseId
            {
                get => _purchaseId;
                set { _purchaseId = value; OnPropertyChanged(); }
            }

            public string EmailUser
            {
                get => _emailUser;
                set { _emailUser = value; OnPropertyChanged(); }
            }

            public int TicketId
            {
                get => _ticketId;
                set { _ticketId = value; OnPropertyChanged(); }
            }

            public DateTime PurchaseDate
            {
                get => _purchaseDate;
                set { _purchaseDate = value; OnPropertyChanged(); }
            }

            public string PurchaseTime
            {
                get => _purchaseTime;
                set { _purchaseTime = value; OnPropertyChanged(); }
            }

            public double Price
            {
                get => _price;
                set
                {
                    _price = value;
                    OnPropertyChanged();
                }
            }

            public int Number
            {
                get => _number;
                set
                {
                    _number = value;
                    OnPropertyChanged();
                }
            }

            public int Status
            {
                get => _status;
                set
                {
                    _status = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText));
                }
            }

            public string StatusText
            {
                get => Status == 0 ? "Не оплачено" : "Оплачено";
                set {}
            }

            public string From
            {
                get => _from;
                set { _from = value; OnPropertyChanged(); }
            }

            public string To
            {
                get => _to;
                set { _to = value; OnPropertyChanged(); }
            }

            public DateTime RouteDate
            {
                get => _routeDate;
                set { _routeDate = value; OnPropertyChanged(); }
            }

            public string RouteTime
            {
                get => _routeTime;
                set { _routeTime = value; OnPropertyChanged(); }
            }

            public string Type
            {
                get => _type;
                set { _type = value; OnPropertyChanged(); }
            }

            public string BoardingPoints
            {
                get => _boardingPoints;
                set { _boardingPoints = value; OnPropertyChanged(); }
            }

            public string DropOffPoints
            {
                get => _dropOffPoints;
                set { _dropOffPoints = value; OnPropertyChanged(); }
            }

            public int? Rating
            {
                get => _rating;
                set { _rating = value; OnPropertyChanged(); }
            }

            public string Comment
            {
                get => _comment;
                set { _comment = value; OnPropertyChanged(); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}