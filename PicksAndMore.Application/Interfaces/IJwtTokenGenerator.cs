using PicksAndMore.Domain.Entities;

namespace PicksAndMore.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateJwtToken(ApplicationUser user, string roleName, List<string> permissions);
}
