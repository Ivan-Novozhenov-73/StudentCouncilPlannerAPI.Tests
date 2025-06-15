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

        [Fact]
        public async System.Threading.Tasks.Task CreateEventAsync_CreatesEvent()
        {
            var db = GetDbContext(nameof(CreateEventAsync_CreatesEvent));
            var service = new EventService(db);

            var dto = new EventCreateDto
            {
                Title = "New Event",
                Description = "Description",
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                EventTime = TimeSpan.FromHours(12),
                Location = "Main Hall",
                NumberOfParticipants = 20
            };

            var creatorId = Guid.NewGuid();

            var eventId = await service.CreateEventAsync(dto, creatorId);

            var ev = db.Events.Include(e => e.EventUsers).FirstOrDefault(e => e.EventId == eventId);
            Assert.NotNull(ev);
            Assert.Equal("New Event", ev.Title);
            Assert.Single(ev.EventUsers.Where(eu => eu.UserId == creatorId));
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateEventAsync_UpdatesEvent()
        {
            var db = GetDbContext(nameof(UpdateEventAsync_UpdatesEvent));
            var ev = CreateEvent();
            db.Events.Add(ev);
            db.SaveChanges();

            var service = new EventService(db);

            var updateDto = new EventUpdateDto
            {
                Title = "Updated",
                Description = "Updated Desc",
                Status = 1,
                StartDate = ev.StartDate,
                EndDate = ev.EndDate,
                EventTime = ev.EventTime,
                Location = ev.Location,
                NumberOfParticipants = 42
            };

            var result = await service.UpdateEventAsync(ev.EventId, updateDto);

            Assert.True(result);
            var updated = db.Events.Find(ev.EventId);
            Assert.Equal("Updated", updated.Title);
            Assert.Equal(1, updated.Status);
        }

        [Fact]
        public async System.Threading.Tasks.Task AddParticipantAsync_AddsParticipant()
        {
            var db = GetDbContext(nameof(AddParticipantAsync_AddsParticipant));
            var ev = CreateEvent();
            db.Events.Add(ev);
            db.SaveChanges();

            var service = new EventService(db);

            var userId = Guid.NewGuid();

            var result = await service.AddParticipantAsync(ev.EventId, userId);

            Assert.True(result);
            Assert.Contains(db.EventUsers, eu => eu.EventId == ev.EventId && eu.UserId == userId && eu.Role == 0);
        }

        [Fact]
        public async System.Threading.Tasks.Task AddParticipantAsync_DoesNotAddDuplicate()
        {
            var db = GetDbContext(nameof(AddParticipantAsync_DoesNotAddDuplicate));
            var ev = CreateEvent();
            var userId = Guid.NewGuid();
            db.Events.Add(ev);
            db.EventUsers.Add(new EventUser { EventId = ev.EventId, UserId = userId, Role = 0 });
            db.SaveChanges();

            var service = new EventService(db);

            var result = await service.AddParticipantAsync(ev.EventId, userId);

            Assert.False(result);
        }

        [Fact]
        public async System.Threading.Tasks.Task RemoveParticipantAsync_RemovesParticipant()
        {
            var db = GetDbContext(nameof(RemoveParticipantAsync_RemovesParticipant));
            var ev = CreateEvent();
            var userId = Guid.NewGuid();
            db.Events.Add(ev);
            db.EventUsers.Add(new EventUser { EventId = ev.EventId, UserId = userId, Role = 0 });
            db.SaveChanges();

            var service = new EventService(db);

            var result = await service.RemoveParticipantAsync(ev.EventId, userId);

            Assert.True(result);
            Assert.DoesNotContain(db.EventUsers, eu => eu.EventId == ev.EventId && eu.UserId == userId && eu.Role == 0);
        }

        [Fact]
        public async System.Threading.Tasks.Task RemoveParticipantAsync_NoSuchParticipant()
        {
            var db = GetDbContext(nameof(RemoveParticipantAsync_NoSuchParticipant));
            var ev = CreateEvent();
            db.Events.Add(ev);
            db.SaveChanges();

            var service = new EventService(db);

            var result = await service.RemoveParticipantAsync(ev.EventId, Guid.NewGuid());

            Assert.False(result);
        }

        [Fact]
        public async System.Threading.Tasks.Task AddOrganizerAsync_AddsOrganizer()
        {
            var db = GetDbContext(nameof(AddOrganizerAsync_AddsOrganizer));
            var ev = CreateEvent();
            db.Events.Add(ev);
            db.SaveChanges();

            var service = new EventService(db);

            var userId = Guid.NewGuid();

            var result = await service.AddOrganizerAsync(ev.EventId, userId);

            Assert.True(result);
            Assert.Contains(db.EventUsers, eu => eu.EventId == ev.EventId && eu.UserId == userId && eu.Role == 1);
        }

        [Fact]
        public async System.Threading.Tasks.Task AddOrganizerAsync_DoesNotAddDuplicate()
        {
            var db = GetDbContext(nameof(AddOrganizerAsync_DoesNotAddDuplicate));
            var ev = CreateEvent();
            var userId = Guid.NewGuid();
            db.Events.Add(ev);
            db.EventUsers.Add(new EventUser { EventId = ev.EventId, UserId = userId, Role = 1 });
            db.SaveChanges();

            var service = new EventService(db);

            var result = await service.AddOrganizerAsync(ev.EventId, userId);

            Assert.False(result);
        }

        [Fact]
        public async System.Threading.Tasks.Task RemoveOrganizerAsync_RemovesOrganizer()
        {
            var db = GetDbContext(nameof(RemoveOrganizerAsync_RemovesOrganizer));
            var ev = CreateEvent();
            var userId = Guid.NewGuid();
            db.Events.Add(ev);
            db.EventUsers.Add(new EventUser { EventId = ev.EventId, UserId = userId, Role = 1 });
            db.SaveChanges();

            var service = new EventService(db);

            var result = await service.RemoveOrganizerAsync(ev.EventId, userId);

            Assert.True(result);
            Assert.DoesNotContain(db.EventUsers, eu => eu.EventId == ev.EventId && eu.UserId == userId && eu.Role == 1);
        }

        [Fact]
        public async System.Threading.Tasks.Task RemoveOrganizerAsync_NoSuchOrganizer()
        {
            var db = GetDbContext(nameof(RemoveOrganizerAsync_NoSuchOrganizer));
            var ev = CreateEvent();
            db.Events.Add(ev);
            db.SaveChanges();

            var service = new EventService(db);

            var result = await service.RemoveOrganizerAsync(ev.EventId, Guid.NewGuid());

            Assert.False(result);
        }
    }
}