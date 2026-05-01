using CampusBooking.Api.Data;
using CampusBooking.Api.Data.Entities;
using CampusBooking.Api.Dtos.Facilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusBooking.Api.Controllers;

[ApiController]
[Route("api/facility-types")]
[Authorize]
public class FacilityTypesController : ControllerBase
{
    private readonly AppDbContext _db;

    public FacilityTypesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<FacilityTypeResponse>>> GetAll()
    {
        var types = await _db.FacilityTypes
            .OrderBy(t => t.Name)
            .Select(t => new FacilityTypeResponse
            {
                Id = t.Id,
                Name = t.Name,
                RequiresApproval = t.RequiresApproval,
                IsActive = t.IsActive
            })
            .ToListAsync();

        return Ok(types);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FacilityTypeResponse>> GetById(int id)
    {
        var t = await _db.FacilityTypes.FindAsync(id);
        if (t is null) return NotFound();

        return Ok(new FacilityTypeResponse
        {
            Id = t.Id,
            Name = t.Name,
            RequiresApproval = t.RequiresApproval,
            IsActive = t.IsActive
        });
    }

    [HttpPost]
    [Authorize(Roles = "FacilityManager")]
    public async Task<ActionResult<FacilityTypeResponse>> Create([FromBody] CreateFacilityTypeRequest request)
    {
        if (await _db.FacilityTypes.AnyAsync(t => t.Name == request.Name))
            return Conflict(new { message = $"Facility type '{request.Name}' already exists." });

        var entity = new FacilityType
        {
            Name = request.Name,
            RequiresApproval = request.RequiresApproval
        };

        _db.FacilityTypes.Add(entity);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new FacilityTypeResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            RequiresApproval = entity.RequiresApproval,
            IsActive = entity.IsActive
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "FacilityManager")]
    public async Task<ActionResult<FacilityTypeResponse>> Update(int id, [FromBody] UpdateFacilityTypeRequest request)
    {
        var entity = await _db.FacilityTypes.FindAsync(id);
        if (entity is null) return NotFound();

        if (await _db.FacilityTypes.AnyAsync(t => t.Name == request.Name && t.Id != id))
            return Conflict(new { message = $"Facility type '{request.Name}' already exists." });

        entity.Name = request.Name;
        entity.RequiresApproval = request.RequiresApproval;
        entity.IsActive = request.IsActive;

        await _db.SaveChangesAsync();

        return Ok(new FacilityTypeResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            RequiresApproval = entity.RequiresApproval,
            IsActive = entity.IsActive
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "FacilityManager")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var entity = await _db.FacilityTypes.FindAsync(id);
        if (entity is null) return NotFound();

        entity.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
