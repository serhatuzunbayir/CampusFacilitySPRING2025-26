namespace CampusBooking.Api.Dtos.Facilities;

public class FacilityTypeResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; }
    public bool IsActive { get; set; }
}
