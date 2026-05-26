namespace CampusBooking.Web.Models{
    public class NotificationViewModel{
        public int Id { get; set; }
        public string Message { get; set; } = "";
        public string Kind { get; set; } = "";
        public bool IsRead { get; set; }
        public string CreatedAtUtc { get; set; } = "";
    }
}