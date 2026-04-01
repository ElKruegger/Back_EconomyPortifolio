using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EconomyBackPortifolio.Controllers
{
    /// <summary>
    /// Handles all authentication and identity flows.
    /// The full auth lifecycle is:
    ///
    ///   REGISTRATION:
    ///   POST /register -> user receives a 6-digit code by email
    ///   POST /verify-code (type=1) -> code validated, email marked as verified, JWT returned
    ///
    ///   LOGIN (2FA):
    ///   POST /login -> credentials validated, user receives a 6-digit code by email
    ///   POST /verify-code (type=0) -> code validated, JWT returned
    ///
    ///   PASSWORD RESET:
    ///   POST /forgot-password -> user receives a 6-digit reset code by email
    ///   POST /reset-password -> code validated, password updated
    ///
    /// All verification codes expire after 10 minutes and are single-use.
    /// The /register, /login, /verify-code, /forgot-password and /reset-password endpoints
    /// are rate-limited to 10 requests per IP per minute to prevent brute-force attacks.
    /// </summary>
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

        /// <summary>
        /// Returns the profile data of the currently authenticated user.
        /// Used by the frontend to restore session state on page load (e.g. persist login across refreshes).
        ///
        /// Requires: Bearer token in the Authorization header.
        /// Returns 401 if the token is missing, expired, or invalid.
        /// Returns 404 if the user ID from the token no longer exists in the database.
        /// </summary>
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

        /// <summary>
        /// Registers a new user account and sends an email verification code.
        /// The account is created immediately but remains unverified until
        /// the code is submitted to POST /verify-code with type=1 (Registration).
        /// An unverified account cannot log in.
        ///
        /// Side effects on success:
        /// - User record created in the database.
        /// - A BRL wallet is automatically created for the new user.
        /// - A 6-digit verification code is emailed (expires in 10 minutes).
        ///
        /// Returns 409 Conflict if the email is already registered.
        ///
        /// Example body: { "name": "Erick", "email": "user@email.com", "password": "Senha123" }
        /// Password rules: 8-100 chars, at least one uppercase, one lowercase, one digit.
        /// </summary>
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

        /// <summary>
        /// Authenticates the user's credentials and sends a 2FA code by email.
        /// The JWT is NOT returned here — it is only returned after the code is validated
        /// via POST /verify-code with type=0 (Login).
        ///
        /// Returns 401 if the email/password combination is incorrect.
        /// Returns 401 if the account email has not been verified yet (user must complete registration first).
        ///
        /// Example body: { "email": "user@email.com", "password": "Senha123" }
        /// </summary>
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

        /// <summary>
        /// Validates the 6-digit code sent by email and returns the JWT access token.
        /// This is the second step for both the registration and login flows.
        ///
        /// The 'type' field tells the API which kind of code to look up:
        ///   0 = Login       -> call this after POST /login
        ///   1 = Registration -> call this after POST /register
        ///   2 = PasswordReset -> use POST /reset-password instead for this type
        ///
        /// On success for Registration (type=1): marks the user's email as verified.
        /// On success for Login (type=0): returns JWT + RefreshToken immediately.
        ///
        /// Returns 401 if the code is wrong, already used, or expired (10-minute window).
        ///
        /// Example body: { "email": "user@email.com", "code": "482910", "type": 1 }
        /// </summary>
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

        /// <summary>
        /// Sends a password reset code to the given email address.
        /// If the email is not registered, the response is identical to the success case —
        /// this prevents attackers from discovering which emails are registered (user enumeration).
        ///
        /// After calling this, the user should submit the code to POST /reset-password.
        /// The code expires in 10 minutes.
        ///
        /// Example body: { "email": "user@email.com" }
        /// </summary>
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

        /// <summary>
        /// Resets the user's password using the code received by email via POST /forgot-password.
        /// The code must be of type PasswordReset (type=2), unused, and not expired.
        ///
        /// On success: the password is updated and the code is marked as used.
        /// The user can then log in normally with the new password via POST /login.
        ///
        /// Returns 401 if the code is wrong, already used, or expired.
        ///
        /// Example body: { "email": "user@email.com", "code": "193847", "newPassword": "NovaSenha456" }
        /// Password rules: 8-100 chars, at least one uppercase, one lowercase, one digit.
        /// </summary>
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
