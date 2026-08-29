using FluentAssertions;
using Moq;
using SupportTicketingPlatform.Application.Commands.Tickets.CreateTicket;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Application.Tests.Helpers;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Tests.Commands;

public class CreateTicketCommandTests
{
    [Fact]
    public async Task Handle_WhenAuthenticated_CreatesTicket()
    {
        await using var context = TestDbContextFactory.Create();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns("customer-1");

        var handler = new CreateTicketCommandHandler(context, currentUser.Object);
        var command = new CreateTicketCommand(
            "Cannot login",
            "Getting 401 on the login page after reset.",
            TicketPriority.High,
            1);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
        context.Tickets.Should().ContainSingle(t => t.CustomerId == "customer-1");
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsForbidden()
    {
        await using var context = TestDbContextFactory.Create();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns((string?)null);

        var handler = new CreateTicketCommandHandler(context, currentUser.Object);
        var command = new CreateTicketCommand(
            "Cannot login",
            "Getting 401 on the login page after reset.",
            TicketPriority.High,
            1);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ErrorType.Forbidden);
    }
}
