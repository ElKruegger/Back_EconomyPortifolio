using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EconomyBackPortifolio.Controllers
{
    /// <summary>
    /// Abstract base controller inherited by all authenticated controllers.
    /// Provides shared utilities so each controller doesn't repeat the same boilerplate.
    ///
    /// Currently provides:
    /// - GetUserId(): extracts the authenticated user's ID from the JWT token claims.
    ///
    /// Every controller that needs to identify the calling user should extend this class
    /// instead of ControllerBase directly.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
        /// <summary>
        /// Extracts the authenticated user's ID from the JWT token claims.
        /// The ID is stored in the NameIdentifier claim when the token is generated at login.
        ///
        /// Throws UnauthorizedAccessException if the claim is missing or cannot be parsed as a GUID.
        /// This would only happen if the token is malformed — valid tokens always contain this claim.
        /// </summary>
        /// <returns>The GUID of the currently authenticated user.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when the NameIdentifier claim is absent or not a valid GUID.
        /// </exception>
        protected Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                throw new UnauthorizedAccessException("User is not authenticated");

            return userId;
        }
    }
}
