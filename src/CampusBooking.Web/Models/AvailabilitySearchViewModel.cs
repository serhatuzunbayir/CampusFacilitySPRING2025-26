using System.ComponentModel.DataAnnotations;

namespace CampusBooking.Web.Models{
    public class AvailabilitySearchViewModel{
        [Required]
        public string Date { get; set; } = "";

        [Required]
        public int TimeSlot { get; set; }

        public int? FacilityTypeId { get; set; }

        public int? MinCapacity { get; set; }

        public List<Facility> Results { get; set; } = new();
    }
}