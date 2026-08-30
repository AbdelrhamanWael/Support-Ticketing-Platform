using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SupportTicketingPlatform.Application.Common;
using SupportTicketingPlatform.Application.Interfaces;
using SupportTicketingPlatform.Domain.Entities;
using SupportTicketingPlatform.Domain.Enums;
using SupportTicketingPlatform.Application.Tests.Helpers;
using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SupportTicketingPlatform.Application.Tests.Commands
{
    public class AddInternalNoteCommandTests
    {
        [Fact]
        public async Task Handle_ShouldAddNote_WhenTicketExists()
        {
            // Arrange
            var dbContext = TestDbContextFactory.Create();

            var ticket = new Ticket { Title = "Test", Description = "Test", CustomerId = "cust1", CategoryId = 1, Status = TicketStatus.New };
            dbContext.Tickets.Add(ticket);
            await dbContext.SaveChangesAsync();

            var handler = new AddInternalNoteCommandHandler(dbContext);
            
            var command = new AddInternalNoteCommand 
            { 
                TicketId = ticket.Id, 
                Content = "Secret Agent Note",
                StaffId = "agent1"
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeGreaterThan(0);
            var note = await dbContext.TicketComments.FirstAsync();
            note.Content.Should().Be("Secret Agent Note");
            note.Visibility.Should().Be(CommentVisibility.Internal);
        }
        
        [Fact]
        public async Task Handle_ShouldThrowException_WhenTicketNotFound()
        {
            var dbContext = TestDbContextFactory.Create();
            var handler = new AddInternalNoteCommandHandler(dbContext);
            var command = new AddInternalNoteCommand { TicketId = 999, Content = "Note", StaffId = "agent1" };

            // Act & Assert
            await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<Exception>().WithMessage("Ticket not found");
        }
    }
}
