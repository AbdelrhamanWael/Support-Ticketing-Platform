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
    public class AddPublicCommentCommandTests
    {
        [Fact]
        public async Task Handle_ShouldFail_WhenTicketIsClosed()
        {
            var dbContext = TestDbContextFactory.Create();

            var ticket = new Ticket { Title = "Test", Description = "Test", CustomerId = "cust1", CategoryId = 1, Status = TicketStatus.Closed };
            dbContext.Tickets.Add(ticket);
            await dbContext.SaveChangesAsync();

            var handler = new AddPublicCommentCommandHandler(dbContext);
            var command = new AddPublicCommentCommand 
            { 
                TicketId = ticket.Id, 
                CommentText = "Hello",
                UserId = "cust1"
            };

            await FluentActions.Invoking(() => handler.Handle(command, CancellationToken.None))
                .Should().ThrowAsync<InvalidOperationException>().WithMessage("*Cannot add a comment to a closed ticket*");
        }
    }
}
