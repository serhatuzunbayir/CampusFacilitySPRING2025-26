namespace CampusBooking.Web.Models{
    public class MaintenanceCreateViewModel{
        public int FacilityId { get; set; }
        public string Description { get; set; } = "";
        public string? Message { get; set; }

        public List<Facility> Facilities { get; set; } = new();
    }
}