using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/users")]
[SessionAuthorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class UsersController : ApiControllerBase
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db) => _db = db;

    /// <summary>View your own profile (Participant or Organiser).</summary>
    /// <response code="200">Your profile.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe()
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        return user is null ? NotFound() : Ok(AuthController.ToResponse(user, user.Role!.RoleName));
    }

    /// <summary>Update your own full name and phone number.</summary>
    /// <response code="200">Profile updated.</response>
    /// <response code="400">Validation failed.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
    {
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == CurrentUserId);
        if (user is null) return NotFound();

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        await _db.SaveChangesAsync();
        return Ok(AuthController.ToResponse(user, user.Role!.RoleName));
    }
}
