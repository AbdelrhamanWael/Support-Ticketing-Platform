using FluentAssertions;
using Moq;
using SupportTicketingPlatform.Application.Commands.AssignTicket;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Application.Tests.Helpers;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;

namespace SupportTicketingPlatform.Application.Tests.Commands;

public class AssignTicketCommandTests
{
    [Fact]
    public async Task Handle_WhenAgentInactive_ReturnsFailure()
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

        context.AgentProfiles.Add(new AgentProfile
        {
            Id = 5,
            UserId = "agent-inactive",
            DisplayName = "Inactive Agent",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns("lead-1");

        var handler = new AssignTicketCommandHandler(context, currentUser.Object);
        var result = await handler.Handle(new AssignTicketCommand(1, 5, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not active");
    }

    [Fact]
    public async Task Handle_WhenAgentActive_CreatesAssignment()
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

        context.AgentProfiles.Add(new AgentProfile
        {
            Id = 7,
            UserId = "agent-active",
            DisplayName = "Active Agent",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns("lead-1");

        var handler = new AssignTicketCommandHandler(context, currentUser.Object);
        var result = await handler.Handle(new AssignTicketCommand(1, 7, "Assigned"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AssignedAgent.Should().Be("Active Agent");
        context.TicketAssignments.Should().ContainSingle(a => a.IsActive && a.AgentId == "agent-active");
    }
}
