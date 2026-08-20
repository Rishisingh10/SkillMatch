using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SkillMatch.API.Models;

namespace SkillMatch.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly SkillMatchDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthController(
        SkillMatchDbContext context,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Email and password are required.");
        }

        var normalizedRole = request.Role?.ToUpperInvariant() ?? "CANDIDATE";
        var allowedRoles = new[] { "CANDIDATE", "RECRUITER", "ADMIN" };
        if (!allowedRoles.Contains(normalizedRole))
        {
            return BadRequest("Invalid role. Allowed roles: CANDIDATE, RECRUITER, ADMIN.");
        }

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (existingUser != null)
        {
            return BadRequest("A user with this email already exists.");
        }

        var user = new User
        {
            Email = request.Email.Trim(),
            Role = normalizedRole,
            IsActive = true,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Create corresponding candidate or recruiter profile
        if (normalizedRole == "CANDIDATE")
        {
            _context.CandidateProfiles.Add(new CandidateProfile
            {
                UserId = user.Id,
                FullName = request.FullName ?? request.Email.Split('@')[0],
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else if (normalizedRole == "RECRUITER")
        {
            _context.RecruiterProfiles.Add(new RecruiterProfile
            {
                UserId = user.Id,
                CompanyName = request.CompanyName ?? "Default Company",
                IsApprovedByAdmin = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = "User registered successfully.",
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Email and password are required.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null)
        {
            return Unauthorized("Invalid credentials.");
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid credentials.");
        }

        if (user.IsActive == false)
        {
            return Unauthorized("Account is inactive.");
        }

        var token = GenerateJwtToken(user);

        return Ok(new
        {
            Token = token,
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role
        });
    }

    private string GenerateJwtToken(User user)
    {
        var secret = _configuration["Jwt:Secret"] ?? "SkillMatch_Super_Secret_Key_For_JWT_Signing_2026_Minimum_256_Bits!";
        var issuer = _configuration["Jwt:Issuer"] ?? "SkillMatch.API";
        var audience = _configuration["Jwt:Audience"] ?? "SkillMatchApp";
        var expiryDays = int.TryParse(_configuration["Jwt:ExpiryInDays"], out var days) ? days : 7;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expiryDays),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class RegisterRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "CANDIDATE";
    public string? FullName { get; set; }
    public string? CompanyName { get; set; }
}

public class LoginRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
