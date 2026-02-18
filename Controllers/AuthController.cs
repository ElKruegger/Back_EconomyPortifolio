using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EconomyBackPortifolio.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Retorna os dados do usuário autenticado (para persistência de sessão)
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
                {
                    return NotFound(new { message = "Usuário não encontrado" });
                }

                return Ok(user);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter dados do usuário autenticado");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Registra um novo usuário e envia código de verificação por e-mail
        /// </summary>
        [HttpPost("register")]
        public async Task<ActionResult<MessageResponseDto>> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RegisterAsync(registerDto);
                _logger.LogInformation("Código de verificação enviado para novo registro: {Email}", registerDto.Email);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Tentativa de registro com email já existente: {Email}", registerDto.Email);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar usuário: {Email}", registerDto.Email);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Autentica credenciais e envia código de verificação por e-mail
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<MessageResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.LoginAsync(loginDto);
                _logger.LogInformation("Código de verificação enviado para login: {Email}", loginDto.Email);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Tentativa de login inválida: {Email}", loginDto.Email);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao realizar login: {Email}", loginDto.Email);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Valida o código de verificação e retorna tokens de acesso (JWT + RefreshToken)
        /// </summary>
        [HttpPost("verify-code")]
        public async Task<ActionResult<AuthResponseDto>> VerifyCode([FromBody] VerifyCodeDto verifyCodeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.VerifyCodeAsync(verifyCodeDto);
                _logger.LogInformation("Código verificado com sucesso: {Email}", verifyCodeDto.Email);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Código de verificação inválido: {Email}", verifyCodeDto.Email);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar código: {Email}", verifyCodeDto.Email);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Solicita redefinição de senha enviando código de verificação por e-mail
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<ActionResult<MessageResponseDto>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.ForgotPasswordAsync(forgotPasswordDto);
                _logger.LogInformation("Solicitação de redefinição de senha: {Email}", forgotPasswordDto.Email);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar redefinição de senha: {Email}", forgotPasswordDto.Email);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        /// <summary>
        /// Redefine a senha do usuário usando o código de verificação
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<ActionResult<MessageResponseDto>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.ResetPasswordAsync(resetPasswordDto);
                _logger.LogInformation("Senha redefinida com sucesso: {Email}", resetPasswordDto.Email);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Tentativa de redefinição de senha com código inválido: {Email}", resetPasswordDto.Email);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao redefinir senha: {Email}", resetPasswordDto.Email);
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("Usuário não autenticado");
            }
            return userId;
        }
    }
}
