using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using StudetCouncilPlannerAPI.Models.Entities;
using StudetCouncilPlannerAPI.Models.DTOs;
using StudetCouncilPlannerAPI.Services;
using StudetCouncilPlannerAPI.Data;

namespace StudetCouncilPlannerAPI.Tests
{
    public class TaskServiceTests
    {
        private ApplicationDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private User CreateUser(short role = 1)
        {
            return new User
            {
                UserId = Guid.NewGuid(),
                Login = Guid.NewGuid().ToString(),
                PasswordHash = "hash",
                Surname = "User",
                Name = "Name",
                Role = role,
                Group = "Test",
                Phone = 1234567890,
                Contacts = "user@site",
                Archive = false
            };
        }

        private Event CreateEvent()
        {
            return new Event
            {
                EventId = Guid.NewGuid(),
                Title = "Test Event",
                Status = 0,
                Description = "Event Desc",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                EventTime = TimeSpan.FromHours(10),
                Location = "Room 100",
                NumberOfParticipants = 0
            };
        }

        private void PrepareEventUsers(ApplicationDbContext db, Guid eventId, Guid creatorId, Guid executorId)
        {
            db.EventUsers.Add(new EventUser
            {
                EventId = eventId,
                UserId = creatorId,
                Role = 2 // Главный организатор
            });
            db.EventUsers.Add(new EventUser
            {
                EventId = eventId,
                UserId = executorId,
                Role = 1 // Организатор
            });
            db.SaveChanges();
        }

        [Fact]
        public async System.Threading.Tasks.Task CreateTaskAsync_CreatesTask()
        {
            var db = GetDbContext(nameof(CreateTaskAsync_CreatesTask));
            var creator = CreateUser(2);
            var executor = CreateUser(1);
            var ev = CreateEvent();

            db.Users.AddRange(creator, executor);
            db.Events.Add(ev);
            db.SaveChanges();
            PrepareEventUsers(db, ev.EventId, creator.UserId, executor.UserId);

            var service = new TaskService(db);

            var dto = new TaskCreateDto
            {
                Title = "New Task",
                EventId = ev.EventId,
                ExecutorUserId = executor.UserId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2))
            };

            var taskId = await service.CreateTaskAsync(dto, creator.UserId);

            var task = db.Tasks.Include(t => t.TaskUsers).FirstOrDefault(t => t.TaskId == taskId);
            Assert.NotNull(task);
            Assert.Equal(dto.Title, task.Title);
            Assert.Equal(ev.EventId, task.EventId);
            Assert.Equal(2, task.TaskUsers.Count);
            Assert.Contains(task.TaskUsers, tu => tu.UserId == creator.UserId && tu.Role == 0);
            Assert.Contains(task.TaskUsers, tu => tu.UserId == executor.UserId && tu.Role == 1);
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateTaskAsync_CreatorCanUpdate()
        {
            var db = GetDbContext(nameof(UpdateTaskAsync_CreatorCanUpdate));
            var creator = CreateUser(2);
            var executor = CreateUser(1);
            var ev = CreateEvent();

            db.Users.AddRange(creator, executor);
            db.Events.Add(ev);
            db.SaveChanges();
            PrepareEventUsers(db, ev.EventId, creator.UserId, executor.UserId);

            var service = new TaskService(db);

            var taskId = await service.CreateTaskAsync(new TaskCreateDto
            {
                Title = "To Update",
                EventId = ev.EventId,
                ExecutorUserId = executor.UserId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
            }, creator.UserId);

            var updateDto = new TaskUpdateDto
            {
                Title = "Updated Title",
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
                Status = 1,
                ExecutorUserId = executor.UserId
            };

            var result = await service.UpdateTaskAsync(taskId, updateDto, creator.UserId);

            Assert.True(result);
            var task = db.Tasks.Find(taskId);
            Assert.NotNull(task);
            Assert.Equal("Updated Title", task.Title);
            Assert.Equal(1, task.Status);
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateTaskStatusAsync_ExecutorCanUpdateStatus()
        {
            var db = GetDbContext(nameof(UpdateTaskStatusAsync_ExecutorCanUpdateStatus));
            var creator = CreateUser(2);
            var executor = CreateUser(1);
            var ev = CreateEvent();

            db.Users.AddRange(creator, executor);
            db.Events.Add(ev);
            db.SaveChanges();
            PrepareEventUsers(db, ev.EventId, creator.UserId, executor.UserId);

            var service = new TaskService(db);

            var taskId = await service.CreateTaskAsync(new TaskCreateDto
            {
                Title = "Status Task",
                EventId = ev.EventId,
                ExecutorUserId = executor.UserId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
            }, creator.UserId);

            var statusDto = new TaskStatusUpdateDto { Status = 2 };
            var result = await service.UpdateTaskStatusAsync(taskId, statusDto, executor.UserId);

            Assert.True(result);
            var task = db.Tasks.Find(taskId);
            Assert.NotNull(task);
            Assert.Equal(2, task.Status);
        }

        [Fact]
        public async System.Threading.Tasks.Task SetPartnerAsync_CreatorCanSetPartner()
        {
            var db = GetDbContext(nameof(SetPartnerAsync_CreatorCanSetPartner));
            var creator = CreateUser(2);
            var executor = CreateUser(1);
            var partner = CreateUser(1);
            var ev = CreateEvent();

            db.Users.AddRange(creator, executor, partner);
            db.Events.Add(ev);
            db.SaveChanges();
            PrepareEventUsers(db, ev.EventId, creator.UserId, executor.UserId);

            var service = new TaskService(db);

            var taskId = await service.CreateTaskAsync(new TaskCreateDto
            {
                Title = "Partner Task",
                EventId = ev.EventId,
                ExecutorUserId = executor.UserId,
                StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
            }, creator.UserId);

            var result = await service.SetPartnerAsync(taskId, partner.UserId, creator.UserId);

            Assert.True(result);
            var task = db.Tasks.Find(taskId);
            Assert.NotNull(task);
            Assert.Equal(partner.UserId, task.PartnerId);
        }
    }
}