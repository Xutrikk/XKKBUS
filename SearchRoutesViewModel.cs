using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Collections.Generic;
using lab4wpf5oop.Models;

namespace RouteBookingSystem
{
    public class SearchRoutesViewModel : INotifyPropertyChanged
    {
        private readonly string _connectionString = "Server=DESKTOP-DMJJTUE\\SQLEXPRESS;Database=XKKBUS;Trusted_Connection=True;";
        private readonly string _email;
        private string _from;
        private string _to;
        private DateTime _date = DateTime.Today;
        private int _numberOfSeats = 1; 
        private string _selectedTransportType;
        private ObservableCollection<string> _transportTypes;
        private ObservableCollection<Ticket> _filteredTickets;
        private Ticket _selectedTicket;
        private ObservableCollection<string> _fromOptions;
        private ObservableCollection<string> _toOptions;
        private ObservableCollection<int> _seatOptions; 

        public SearchRoutesViewModel(string email)
        {
            _email = email;
            LoadTransportTypes();
            LoadRouteOptions();
            LoadSeatOptions();
            FilteredTickets = new ObservableCollection<Ticket>();
            SearchRoutesCommand = new RelayCommand(_ => SearchRoutes());
            SelectRouteCommand = new RelayCommand(_ => SelectRoute());
        }

        public string From
        {
            get => _from;
            set
            {
                if (_from != value)
                {
                    _from = value;
                    OnPropertyChosen();
                    UpdateToOptions();
                }
            }
        }

        public string To
        {
            get => _to;
            set
            {
                if (_to != value)
                {
                    _to = value;
                    OnPropertyChosen();
                }
            }
        }

        public DateTime Date
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChosen();
            }
        }

        public int NumberOfSeats
        {
            get => _numberOfSeats;
            set
            {
                if (value < 1) value = 1;
                if (value > 3) value = 3;
                _numberOfSeats = value;
                OnPropertyChosen();
            }
        }

        public ObservableCollection<int> SeatOptions
        {
            get => _seatOptions;
            set
            {
                _seatOptions = value;
                OnPropertyChosen();
            }
        }

        public string SelectedTransportType
        {
            get => _selectedTransportType;
            set
            {
                _selectedTransportType = value;
                OnPropertyChosen();
            }
        }

        public ObservableCollection<string> TransportTypes
        {
            get => _transportTypes;
            set
            {
                _transportTypes = value;
                OnPropertyChosen();
            }
        }

        public ObservableCollection<Ticket> FilteredTickets
        {
            get => _filteredTickets;
            set
            {
                _filteredTickets = value;
                OnPropertyChosen();
            }
        }

        public Ticket SelectedTicket
        {
            get => _selectedTicket;
            set
            {
                _selectedTicket = value;
                OnPropertyChosen();
            }
        }

        public ObservableCollection<string> FromOptions
        {
            get => _fromOptions;
            set
            {
                _fromOptions = value;
                OnPropertyChosen();
            }
        }

        public ObservableCollection<string> ToOptions
        {
            get => _toOptions;
            set
            {
                _toOptions = value;
                OnPropertyChosen();
            }
        }

        public RelayCommand SearchRoutesCommand { get; }
        public RelayCommand SelectRouteCommand { get; }

        private void LoadTransportTypes()
        {
            TransportTypes = new ObservableCollection<string>
            {
                Application.Current.Resources["AllTypes"]?.ToString() ?? "All Types",
                Application.Current.Resources["Bus"]?.ToString() ?? "Bus",
                Application.Current.Resources["Marshrutka"]?.ToString() ?? "Marshrutka"
            };
            SelectedTransportType = TransportTypes[0];
        }

        private void LoadSeatOptions()
        {
            SeatOptions = new ObservableCollection<int> { 1, 2, 3 };
            NumberOfSeats = 1; 
        }

        private void LoadRouteOptions()
        {
            try
            {
                var fromSet = new HashSet<string>();
                var toSet = new HashSet<string>();
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("SELECT DISTINCT [From], [To] FROM Tickets", conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            fromSet.Add(reader.GetString(0));
                            toSet.Add(reader.GetString(1));
                        }
                    }
                }
                FromOptions = new ObservableCollection<string>(fromSet.OrderBy(x => x));
                ToOptions = new ObservableCollection<string>(toSet.OrderBy(x => x));
                From = FromOptions.FirstOrDefault();
                UpdateToOptions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки маршрутов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateToOptions()
        {
            if (string.IsNullOrEmpty(From))
            {
                ToOptions = new ObservableCollection<string>();
                To = null;
                return;
            }

            try
            {
                var toSet = new HashSet<string>();
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("SELECT DISTINCT [To] FROM Tickets WHERE [From] = @From", conn))
                {
                    cmd.Parameters.AddWithValue("@From", From);
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            toSet.Add(reader.GetString(0));
                        }
                    }
                }
                ToOptions = new ObservableCollection<string>(toSet.OrderBy(x => x));

                if (!string.IsNullOrEmpty(To) && !ToOptions.Contains(To))
                {
                    To = null;
                }

                if (ToOptions.Any() && string.IsNullOrEmpty(To))
                {
                    To = ToOptions.First();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления пункта назначения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchRoutes()
        {
            try
            {
                DateTime currentDate = DateTime.Now;
                if (Date < currentDate.Date)
                {
                    MessageBox.Show("Нельзя выбрать прошедшую дату!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(From) || string.IsNullOrEmpty(To))
                {
                    MessageBox.Show("Пожалуйста, выберите 'Откуда' и 'Куда'!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var tickets = new ObservableCollection<Ticket>();
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("SELECT * FROM Tickets", conn))
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
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
                                Type = reader.GetString(8),
                                BoardingPoints = reader.GetString(9),
                                DropOffPoints = reader.GetString(10)
                            });
                        }
                    }
                }

                var filtered = tickets
                    .Where(t => t.From == From && t.To == To)
                    .Where(t => t.Date.Date == Date.Date)
                    .Where(t => SelectedTransportType == TransportTypes[0] || t.Type == SelectedTransportType)
                    .ToList();

                if (Date.Date == currentDate.Date)
                {
                    TimeSpan currentTime = currentDate.TimeOfDay;
                    TimeSpan minAllowedTime = currentTime.Add(TimeSpan.FromMinutes(30));

                    filtered = filtered
                        .Where(t =>
                        {
                            if (TimeSpan.TryParse(t.Time, out TimeSpan ticketTime))
                            {
                                return ticketTime >= minAllowedTime;
                            }
                            return false;
                        })
                        .ToList();
                }

                if (!filtered.Any())
                {
                    MessageBox.Show("Маршрут с указанными параметрами не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    FilteredTickets = new ObservableCollection<Ticket>();
                    return;
                }

                FilteredTickets = new ObservableCollection<Ticket>(filtered);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SelectRoute()
        {
            if (SelectedTicket == null)
            {
                MessageBox.Show("Пожалуйста, выберите маршрут!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedTicket.Number < NumberOfSeats)
            {
                MessageBox.Show("Недостаточно доступных мест!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var routeDetailsWindow = new RouteDetailsWindow(_email, SelectedTicket, NumberOfSeats);
            routeDetailsWindow.Show();
            (Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w is SearchRoutesWindow))?.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChosen([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}