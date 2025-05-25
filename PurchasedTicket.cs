namespace lab4wpf5oop.Models
{
    public class PurchasedTicket
    {
        public int PurchaseId { get; set; }
        public string EmailUser { get; set; }
        public int TicketId { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string PurchaseTime { get; set; }
        public double Price { get; set; }
        public int Number { get; set; }
        public int Status { get; set; }
        public string StatusText => Status == 0 ? "Не оплачено" : (Status == 1 ? "Оплачено" : "Неизвестно");
        public int? Rating { get; set; }
        public string Comment { get; set; }
        public DateTime Date { get; set; }
        public string Time { get; set; }
        public DateTime RouteDate { get; set; }
        public string RouteTime { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Type { get; set; }
        public string BoardingPoints { get; set; }
        public string DropOffPoints { get; set; }
    }
}