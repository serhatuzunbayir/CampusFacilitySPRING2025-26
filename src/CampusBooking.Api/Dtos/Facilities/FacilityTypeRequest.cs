using System.ComponentModel.DataAnnotations;

namespace CampusBooking.Api.Dtos.Facilities;

public class CreateFacilityTypeRequest
{
    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    public bool RequiresApproval { get; set; }
}

public class UpdateFacilityTypeRequest
{
    [Required, MaxLength(64)]
    public string Name { get; set; } = string.Empty;

    public bool RequiresApproval { get; set; }
    public bool IsActive { get; set; }
}
