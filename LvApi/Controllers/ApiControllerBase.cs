using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
        return int.Parse(userIdClaim!.Value);
    }

    protected List<string> GetCurrentRoles() =>
        User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
}
