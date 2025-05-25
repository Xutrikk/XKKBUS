namespace lab4wpf5oop.Models
{
    public class TripRating
    {
        public int Id { get; set; }
        public int PurchaseId { get; set; }
        public string EmailUser { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime RatingDate { get; set; }
    }
}