using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TaskMaster.API.DTOs;
using TaskMaster.Core.Entities;
using TaskMaster.Infrastructure.Data;

namespace TaskMaster.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly TaskMasterDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(TaskMasterDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDTO)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Email == registerDTO.Email);

            if (userExists)
            {
                return BadRequest("User with this email already exist");
            }

            var user = new User
            {
                Name = registerDTO.Name,
                Email = registerDTO.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDTO.password)
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registered successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDTO)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDTO.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDTO.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid credentials");
            }
            var accessToken = GenerateJWTToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new {
                accessToken = accessToken,
                refreshToken = refreshToken
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(TokenRequestDto tokenRequest)
        {
            if (tokenRequest is null)
            {
                return BadRequest("Invalid client request");
            }

            string accessToken = tokenRequest.AccessToken;
            string refreshToken = tokenRequest.RefreshToken;

            var principal = GetPrincipalFromExpiredToken(accessToken);

            if (principal == null)
            {
                return BadRequest("Invalid access token or refresh token");
            }

            int userId = int.Parse(principal.Identity!.Name!);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || user.RefreshToken != refreshToken || user.RefreshExpiryTime <= DateTime.UtcNow)
            {
                return BadRequest("Invalid access token or refresh token");
            }

            var newAccessToken = GenerateJWTToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshExpiryTime = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdString, out var userId))
            {
                return BadRequest("Invalid user ID in token");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            user.RefreshToken = null;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Token successfully revoked" });
        }

        private string GenerateJWTToken(User user) {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.Name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!))
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token");
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        [HttpGet("external-login")]
        public IActionResult ExternalLogin( string provider, string? returnUrl = null)
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action(nameof(ExternalLoginCallback)) };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("signin-google")]
        public async Task<IActionResult> ExternalLoginCallback()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            if (!result.Succeeded)
            {
                return BadRequest("Error authenticating with Google.");
            }

            var claims = result.Principal.Identities
                .FirstOrDefault()?.Claims.Select(claim => new
                {
                    claim.Issuer,
                    claim.OriginalIssuer,
                    claim.Type,
                    claim.Value
                });

            var emailClaim = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email);
            var nameClaim = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name);
            var identifierClaim = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (emailClaim == null || nameClaim == null || identifierClaim == null)
            {
                return BadRequest("Could not retrieve required information from Google.");
            }

            var userEmail = emailClaim.Value;
            var userName = nameClaim.Value;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user == null)
            {
                user = new User
                {
                    Email = userEmail,
                    Name = userName,
                    PasswordHash = "EXT_LOGIN"
                };
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
            }

            var accessToken = GenerateJWTToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            var frontendCallbackUrl = $"https://localhost:4200/external-login-callback?accessToken={accessToken}&refreshToken={refreshToken}";
            return Redirect(frontendCallbackUrl);
        }

        //[Httppost("change-password")]
        //public async Task<IActionResult> ChangePassword(ChangePasswordDTO changePasswordDTO)
        //{
        //    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == changePasswordDTO.UserId);
        //    if (user == null)
        //    {
        //        return NotFound("User not found");
        //    }
        //    if (!BCrypt.Net.BCrypt.Verify(changePasswordDTO.CurrentPassword, user.PasswordHash))
        //    {
        //        return BadRequest("Current password is incorrect");
        //    }
        //    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDTO.NewPassword);
        //    await _context.SaveChangesAsync();
        //    return Ok(new { message = "Password changed successfully" });
        //}
        //[HttpDelete("delete-account/{userId}")]
        //public async Task<IActionResult> DeleteAccount(int userId)
        //{
        //    var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        //    if (user == null)
        //    {
        //        return NotFound("User not found");
        //    }
        //    _context.Users.Remove(user);
        //    await _context.SaveChangesAsync();
        //    return Ok(new { message = "User account deleted successfully" });
        //}


    }
}
