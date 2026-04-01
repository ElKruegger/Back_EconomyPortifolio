using EconomyBackPortifolio.DTOs;
using EconomyBackPortifolio.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EconomyBackPortifolio.Controllers
{
    /// <summary>
    /// Controller responsável por toda a autenticação e gestão de identidade:
    /// registro, login com 2FA por e-mail, verificação de código,
    /// redefinição de senha e consulta do usuário autenticado.
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
        /// Retorna os dados do usuário autenticado (para persistência de sessão no front-end).
        /// Requer token JWT válido.
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
                    return NotFound(new { message = "Usuário não encontrado" });

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
        /// Registra um novo usuário e envia código de verificação por e-mail.
        /// Após o registro, o usuário ainda precisará verificar o e-mail via /verify-code.
        /// </summary>
        /// <param name="registerDto">Dados de cadastro: nome, e-mail e senha.</param>
        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<MessageResponseDto>> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

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
        /// Autentica as credenciais do usuário e envia um código 2FA por e-mail.
        /// O token JWT só é gerado após validar o código em /verify-code.
        /// </summary>
        /// <param name="loginDto">E-mail e senha do usuário.</param>
        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<MessageResponseDto>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

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
        /// Valida o código 2FA enviado por e-mail e retorna o JWT + RefreshToken de acesso.
        /// O tipo do código deve corresponder à operação (Login ou Registration).
        /// </summary>
        /// <param name="verifyCodeDto">E-mail, código recebido e tipo da operação.</param>
        [HttpPost("verify-code")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<AuthResponseDto>> VerifyCode([FromBody] VerifyCodeDto verifyCodeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

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
        /// Solicita a redefinição de senha. Envia um código de verificação por e-mail.
        /// A resposta é genérica (mesmo se o e-mail não existir) para evitar user enumeration.
        /// </summary>
        /// <param name="forgotPasswordDto">E-mail do usuário.</param>
        [HttpPost("forgot-password")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<MessageResponseDto>> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

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
        /// Redefine a senha usando o código de verificação recebido por e-mail.
        /// O código deve ser do tipo PasswordReset e ainda válido (não expirado/usado).
        /// </summary>
        /// <param name="resetPasswordDto">E-mail, código e nova senha.</param>
        [HttpPost("reset-password")]
        [EnableRateLimiting("auth")]
        public async Task<ActionResult<MessageResponseDto>> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

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
    }
}
