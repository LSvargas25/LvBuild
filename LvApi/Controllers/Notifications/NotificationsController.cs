using LvApi.Controllers;
using LvApplication.Common;
using LvApplication.DTOs.Notifications;
using LvApplication.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LvApi.Controllers.Notifications;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetMine([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _notificationService.GetMyNotificationsAsync(GetCurrentUserId(), pageNumber, pageSize);
        return Ok(result);
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await _notificationService.MarkAsReadAsync(id, GetCurrentUserId());
        return NoContent();
    }
}
