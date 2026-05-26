namespace CampusBooking.Web.Models{
    public class BookingCreateViewModel{
        public int FacilityId { get; set; }
        public string Date { get; set; } = "";
        public int TimeSlot { get; set; }
        public string? Message { get; set; }

        public List<Facility> Facilities { get; set; } = new();
    }
}