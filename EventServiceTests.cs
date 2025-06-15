using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using StudetCouncilPlannerAPI.Models.Entities;
using StudetCouncilPlannerAPI.Models.Dtos;
using StudetCouncilPlannerAPI.Services;
using StudetCouncilPlannerAPI.Data;

namespace StudetCouncilPlannerAPI.Tests
{
    public class EventServiceTests
    {
        private ApplicationDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private Event CreateEvent()
        {
            return new Event
            {
                EventId = Guid.NewGuid(),
                Title = "Event",
                Status = 0,
                Description = "Desc",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                EventTime = TimeSpan.FromHours(10),
                Location = "Room 1",
                NumberOfParticipants = 0
            };
        }

        [Fact]
        public async System.Threading.Tasks.Task GetEventsAsync_ReturnsEvents()
        {
            var db = GetDbContext(nameof(GetEventsAsync_ReturnsEvents));
            db.Events.Add(CreateEvent());
            db.Events.Add(CreateEvent());
            db.SaveChanges();

            var service = new EventService(db);

            var result = await service.GetEventsAsync(new EventListQueryDto());

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetEventByIdAsync_ReturnsEvent()
        {
            var db = GetDbContext(nameof(GetEventByIdAsync_ReturnsEvent));
            var ev = CreateEvent();
            db.Events.Add(ev);
            db.SaveChanges();

            var service = new EventService(db);

            var result = await service.GetEventByIdAsync(ev.EventId);

            Assert.NotNull(result);
            Assert.Equal(ev.EventId, result.EventId);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetEventByIdAsync_ReturnsNullIfNotFound()
        {
            var db = GetDbContext(nameof(GetEventByIdAsync_ReturnsNullIfNotFound));
            var service = new EventService(db);

            var result = await service.GetEventByIdAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        // Дополнительно: тесты на фильтрацию, добавление участника, организатора и т.д. (если требуется)
    }
}