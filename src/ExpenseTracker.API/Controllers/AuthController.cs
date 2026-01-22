using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Application.Common.Models;
using ExpenseTracker.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IIdentityService _identityService;
        private readonly ICurrentUserService _currentUserService;

        public AuthController(
            IIdentityService identityService,
            ICurrentUserService currentUserService)
        {
            _identityService = identityService;
            _currentUserService = currentUserService;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<AuthResult>> Register([FromBody] RegisterRequest request)
        {
            var result = await _identityService.CreateUserAsync(
                request.UserName,
                request.Email,
                request.Password);

            if (!result.Succeeded)
            {
                return BadRequest(new { errors = result.Errors });
            }

            var authResult = await _identityService.AuthenticateAsync(request.Email, request.Password);

            if (!authResult.Succeeded)
            {
                return BadRequest(new { errors = authResult.Errors });
            }

            return Ok(authResult.Value);
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResult>> Login([FromBody] LoginRequest request)
        {
            var result = await _identityService.AuthenticateAsync(request.Email, request.Password);

            if (!result.Succeeded)
            {
                return Unauthorized(new { error = result.FirstError });
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Refresh access token using refresh token
        /// </summary>
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResult>> Refresh([FromBody] RefreshTokenRequest request)
        {
            var result = await _identityService.RefreshTokenAsync(request.RefreshToken);

            if (!result.Succeeded)
            {
                return Unauthorized(new { error = result.FirstError });
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Logout (revoke refresh token)
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (_currentUserService.UserId != null)
            {
                await _identityService.RevokeRefreshTokenAsync(_currentUserService.UserId);
            }

            return NoContent();
        }

        /// <summary>
        /// Get current user info
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            if (_currentUserService.UserId == null)
            {
                return Unauthorized();
            }

            var user = await _identityService.GetUserAsync(_currentUserService.UserId);

            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }
    }

    public record RegisterRequest(string UserName, string Email, string Password);
    public record LoginRequest(string Email, string Password);
    public record RefreshTokenRequest(string RefreshToken);
}
