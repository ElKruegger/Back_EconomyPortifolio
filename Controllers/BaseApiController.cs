using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EconomyBackPortifolio.Controllers
{
    /// <summary>
    /// Controller base com utilitários comuns para todos os controllers autenticados.
    /// Centraliza a extração do UserId do token JWT, eliminando duplicação de código.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>
        /// Extrai o UserId do token JWT autenticado.
        /// </summary>
        /// <returns>O <see cref="Guid"/> do usuário autenticado.</returns>
        /// <exception cref="UnauthorizedAccessException">Lançado se o claim não existir ou for inválido.</exception>
        protected Guid GetUserId()
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
