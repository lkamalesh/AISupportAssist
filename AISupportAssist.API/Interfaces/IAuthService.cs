using Microsoft.AspNetCore.Identity;

namespace AISupportAssist.API.Interfaces
{
    public interface IAuthService
    {
        string GenerateJwtToken(IdentityUser user, IList<string> roles);
    }
}
