using FluentAssertions;
using Moq;
using SupportTicketingPlatform.Application.Commands.Tickets.CancelTicket;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Application.Tests.Helpers;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Tests.Commands;

public class CancelTicketCommandTests
{
    [Fact]
    public async Task Handle_WhenTicketIsNew_CancelsSuccessfully()
    {
        await using var context = TestDbContextFactory.Create();

        context.Tickets.Add(new Ticket
        {
            Id = 1,
            Title = "Test",
            Description = "Test ticket",
            CustomerId = "customer-1",
            CategoryId = 1,
            Status = TicketStatus.New,
            Priority = TicketPriority.Medium,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns("customer-1");

        var handler = new CancelTicketCommandHandler(context, currentUser.Object);
        var result = await handler.Handle(new CancelTicketCommand(1, "No longer needed"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        context.Tickets.Single().Status.Should().Be(TicketStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_WhenTicketIsInProgress_ReturnsConflict()
    {
        await using var context = TestDbContextFactory.Create();

        context.Tickets.Add(new Ticket
        {
            Id = 2,
            Title = "In progress",
            Description = "Test ticket",
            CustomerId = "customer-1",
            CategoryId = 1,
            Status = TicketStatus.InProgress,
            Priority = TicketPriority.Medium,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns("customer-1");

        var handler = new CancelTicketCommandHandler(context, currentUser.Object);
        var result = await handler.Handle(new CancelTicketCommand(2, "Try cancel"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ErrorType.Conflict);
    }
}
