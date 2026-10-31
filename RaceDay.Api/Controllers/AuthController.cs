using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>Register a new Participant or Organiser account.</summary>
    /// <remarks>
    /// Organisers must supply the correct <c>organiserInviteCode</c>, so that people cannot simply
    /// give themselves the Organiser role. Passwords are hashed with BCrypt before they are stored.
    /// </remarks>
    /// <response code="201">Account created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="403">Organiser invite code missing or wrong.</response>
    /// <response code="409">Email already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var roleName = request.Role.ToString();
        var inviteCode = _config["Organiser:InviteCode"];

        if (request.Role == UserRole.Organiser &&
            (string.IsNullOrEmpty(inviteCode) || request.OrganiserInviteCode != inviteCode))
            return Forbidden("A valid organiser invite code is required to register as an Organiser.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "That email address is already registered." });

        var role = await _db.Roles.FirstAsync(r => r.RoleName == roleName);
        var user = new User
        {
            RoleId = role.RoleId,
            FullName = request.FullName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            PhoneNumber = request.PhoneNumber
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, ToResponse(user, roleName));
    }

    /// <summary>Log in with email and password. Starts a session that stores your id and role.</summary>
    /// <response code="200">Logged in; session cookie issued.</response>
    /// <response code="401">Wrong email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var roleName = user.Role!.RoleName;
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, roleName);
        return Ok(ToResponse(user, roleName));
    }

    /// <summary>Log out and clear the session.</summary>
    /// <response code="204">Session cleared.</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return NoContent();
    }

    internal static UserResponse ToResponse(User u, string role) =>
        new(u.UserId, u.FullName, u.Email, u.PhoneNumber, role, u.CreatedAt, u.ProfilePictureUrl);
}
