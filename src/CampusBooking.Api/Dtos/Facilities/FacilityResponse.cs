namespace CampusBooking.Api.Dtos.Facilities;

public class FacilityResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int FacilityTypeId { get; set; }
    public string FacilityTypeName { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; }
    public int Capacity { get; set; }
    public string Location { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
