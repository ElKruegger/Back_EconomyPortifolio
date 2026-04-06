using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EconomyBackPortifolio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : BaseApiController
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserInfoDto>> GetMe()
        {
            try
            {
                var userId = GetUserId();
                var user = await _authService.GetUserByIdAsync(userId);

                if (user == null)
                    return NotFound(new { message = "User not found" });

                return Ok(user);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching authenticated user data");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<MessageResponseDto>> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _authService.RegisterAsync(registerDto);
                _logger.LogInformation("Verification code sent for new registration: {Email}", registerDto.Email);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Registration attempt with existing email: {Email}", registerDto.Email);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration: {Email}", registerDto.Email);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<MessageResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _authService.LoginAsync(loginDto);
                _logger.LogInformation("2FA code sent for login: {Email}", loginDto.Email);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Failed login attempt: {Email}", loginDto.Email);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login: {Email}", loginDto.Email);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("verify-code")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<AuthResponseDto>> VerifyCode([FromBody] VerifyCodeDto verifyCodeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _authService.VerifyCodeAsync(verifyCodeDto);
                _logger.LogInformation("Code verified successfully: {Email}", verifyCodeDto.Email);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Invalid verification code attempt: {Email}", verifyCodeDto.Email);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying code: {Email}", verifyCodeDto.Email);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<MessageResponseDto>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _authService.ForgotPasswordAsync(forgotPasswordDto);
                _logger.LogInformation("Password reset requested: {Email}", forgotPasswordDto.Email);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing password reset request: {Email}", forgotPasswordDto.Email);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<MessageResponseDto>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _authService.ResetPasswordAsync(resetPasswordDto);
                _logger.LogInformation("Password reset successfully: {Email}", resetPasswordDto.Email);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Invalid reset code attempt: {Email}", resetPasswordDto.Email);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password: {Email}", resetPasswordDto.Email);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
