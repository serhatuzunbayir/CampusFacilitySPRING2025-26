using System.ComponentModel.DataAnnotations;

namespace CampusBooking.Api.Dtos.Facilities;

public class CreateFacilityRequest
{
    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    public int FacilityTypeId { get; set; }

    [Range(1, 10000)]
    public int Capacity { get; set; }

    [Required, MaxLength(256)]
    public string Location { get; set; } = string.Empty;
}

public class UpdateFacilityRequest
{
    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    public int FacilityTypeId { get; set; }

    [Range(1, 10000)]
    public int Capacity { get; set; }

    [Required, MaxLength(256)]
    public string Location { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
