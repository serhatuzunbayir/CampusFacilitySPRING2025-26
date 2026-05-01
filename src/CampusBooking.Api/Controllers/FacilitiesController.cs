using CampusBooking.Api.Data;
using CampusBooking.Api.Data.Entities;
using CampusBooking.Api.Dtos.Facilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusBooking.Api.Controllers;

[ApiController]
[Route("api/facilities")]
[Authorize]
public class FacilitiesController : ControllerBase
{
    private readonly AppDbContext _db;

    public FacilitiesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<FacilityResponse>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var query = _db.Facilities.Include(f => f.FacilityType).AsQueryable();

        if (!includeInactive)
            query = query.Where(f => f.IsActive);

        var list = await query
            .OrderBy(f => f.Name)
            .Select(f => new FacilityResponse
            {
                Id = f.Id,
                Name = f.Name,
                FacilityTypeId = f.FacilityTypeId,
                FacilityTypeName = f.FacilityType.Name,
                RequiresApproval = f.FacilityType.RequiresApproval,
                Capacity = f.Capacity,
                Location = f.Location,
                IsActive = f.IsActive
            })
            .ToListAsync();

        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FacilityResponse>> GetById(int id)
    {
        var f = await _db.Facilities
            .Include(f => f.FacilityType)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (f is null) return NotFound();

        return Ok(new FacilityResponse
        {
            Id = f.Id,
            Name = f.Name,
            FacilityTypeId = f.FacilityTypeId,
            FacilityTypeName = f.FacilityType.Name,
            RequiresApproval = f.FacilityType.RequiresApproval,
            Capacity = f.Capacity,
            Location = f.Location,
            IsActive = f.IsActive
        });
    }

    [HttpPost]
    [Authorize(Roles = "FacilityManager")]
    public async Task<ActionResult<FacilityResponse>> Create([FromBody] CreateFacilityRequest request)
    {
        var facilityType = await _db.FacilityTypes.FindAsync(request.FacilityTypeId);
        if (facilityType is null)
            return BadRequest(new { message = "Invalid FacilityTypeId." });

        var entity = new Facility
        {
            Name = request.Name,
            FacilityTypeId = request.FacilityTypeId,
            Capacity = request.Capacity,
            Location = request.Location
        };

        _db.Facilities.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new FacilityResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            FacilityTypeId = entity.FacilityTypeId,
            FacilityTypeName = facilityType.Name,
            RequiresApproval = facilityType.RequiresApproval,
            Capacity = entity.Capacity,
            Location = entity.Location,
            IsActive = entity.IsActive
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "FacilityManager")]
    public async Task<ActionResult<FacilityResponse>> Update(int id, [FromBody] UpdateFacilityRequest request)
    {
        var entity = await _db.Facilities
            .Include(f => f.FacilityType)
            .FirstOrDefaultAsync(f => f.Id == id);

        if (entity is null) return NotFound();

        var facilityType = await _db.FacilityTypes.FindAsync(request.FacilityTypeId);
        if (facilityType is null)
            return BadRequest(new { message = "Invalid FacilityTypeId." });

        entity.Name = request.Name;
        entity.FacilityTypeId = request.FacilityTypeId;
        entity.Capacity = request.Capacity;
        entity.Location = request.Location;
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync();

        return Ok(new FacilityResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            FacilityTypeId = entity.FacilityTypeId,
            FacilityTypeName = facilityType.Name,
            RequiresApproval = facilityType.RequiresApproval,
            Capacity = entity.Capacity,
            Location = entity.Location,
            IsActive = entity.IsActive
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "FacilityManager")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var entity = await _db.Facilities.FindAsync(id);
        if (entity is null) return NotFound();

        entity.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
