using FluentAssertions;
using LvApplication.Common.Exceptions;
using LvDomain.Enums;
using LvTest.Common;

namespace LvTest.Services.Notifications;

public class NotificationServiceTests
{
    [Fact]
    public async Task NotifyAsync_MultipleUserIds_CreatesOneUnreadNotificationPerUser()
    {
        using var context = TestDbContextFactory.Create();
        var userA = await TestUserFactory.CreateAsync(
            context,
            "notif-a@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var userB = await TestUserFactory.CreateAsync(
            context,
            "notif-b@example.com",
            roleId: TestUserFactory.OperationsDirectorRoleId
        );
        var service = ServiceFactory.CreateNotificationService(context);

        await service.NotifyAsync(
            new[] { userA.Id, userB.Id },
            NotificationType.StockBajo,
            "Stock bajo: SKU-1."
        );

        var resultA = await service.GetMyNotificationsAsync(userA.Id, 1, 20);
        var resultB = await service.GetMyNotificationsAsync(userB.Id, 1, 20);

        resultA.TotalCount.Should().Be(1);
        resultA.Items.Single().IsRead.Should().BeFalse();
        resultA.Items.Single().Type.Should().Be(NotificationType.StockBajo);
        resultB.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task MarkAsReadAsync_OwnNotification_SetsIsReadTrue()
    {
        using var context = TestDbContextFactory.Create();
        var user = await TestUserFactory.CreateAsync(
            context,
            "notif-c@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var service = ServiceFactory.CreateNotificationService(context);
        await service.NotifyAsync(
            new[] { user.Id },
            NotificationType.VentaRealizada,
            "Venta realizada."
        );
        var notification = (await service.GetMyNotificationsAsync(user.Id, 1, 20)).Items.Single();

        await service.MarkAsReadAsync(notification.Id, user.Id);

        var reloaded = (await service.GetMyNotificationsAsync(user.Id, 1, 20)).Items.Single();
        reloaded.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsReadAsync_NotificationBelongsToAnotherUser_ThrowsNotFoundException()
    {
        using var context = TestDbContextFactory.Create();
        var owner = await TestUserFactory.CreateAsync(
            context,
            "notif-owner@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var otherUser = await TestUserFactory.CreateAsync(
            context,
            "notif-other@example.com",
            roleId: TestUserFactory.GeneralManagerRoleId
        );
        var service = ServiceFactory.CreateNotificationService(context);
        await service.NotifyAsync(
            new[] { owner.Id },
            NotificationType.VentaRealizada,
            "Venta realizada."
        );
        var notification = (await service.GetMyNotificationsAsync(owner.Id, 1, 20)).Items.Single();

        var act = async () => await service.MarkAsReadAsync(notification.Id, otherUser.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
