using System;
using System.Collections.Generic;
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
    public class UserServiceTests
    {
        private ApplicationDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private User CreateAdmin(string login = "admin", bool archive = false)
        {
            return new User
            {
                UserId = Guid.NewGuid(),
                Login = login,
                PasswordHash = "hash",
                Surname = "Admin",
                Name = "User",
                Role = 2,
                Group = "A",
                Phone = 1234567890,
                Contacts = "admin@site",
                Archive = archive
            };
        }

        private User CreateStudent(string login = "student", bool archive = false)
        {
            return new User
            {
                UserId = Guid.NewGuid(),
                Login = login,
                PasswordHash = "hash",
                Surname = "Student",
                Name = "User",
                Role = 1,
                Group = "B",
                Phone = 9876543210,
                Contacts = "student@site",
                Archive = archive
            };
        }

        [Fact]
        public async System.Threading.Tasks.Task GetUsersAsync_WithFilters_ReturnsFilteredUsers()
        {
            var db = GetDbContext(nameof(GetUsersAsync_WithFilters_ReturnsFilteredUsers));
            db.Users.Add(CreateAdmin());
            db.Users.Add(CreateStudent());
            db.SaveChanges();

            var service = new UserService(db);
            var query = new UserListQueryDto { Role = 2 };

            var result = await service.GetUsersAsync(query);

            Assert.Single(result);
            Assert.Equal(2, result[0].Role);
        }

        [Fact]
        public async System.Threading.Tasks.Task GetUserByIdAsync_ReturnsUserDto_IfExists()
        {
            var db = GetDbContext(nameof(GetUserByIdAsync_ReturnsUserDto_IfExists));
            var user = CreateAdmin();
            db.Users.Add(user);
            db.SaveChanges();

            var service = new UserService(db);
            var result = await service.GetUserByIdAsync(user.UserId);

            Assert.NotNull(result);
            Assert.Equal(user.UserId, result!.UserId);
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateUserAsync_AdminCanEditOtherUser()
        {
            var db = GetDbContext(nameof(UpdateUserAsync_AdminCanEditOtherUser));
            var admin = CreateAdmin();
            var student = CreateStudent();
            db.Users.Add(admin);
            db.Users.Add(student);
            db.SaveChanges();

            var service = new UserService(db);
            var updateDto = new UserUpdateDto { Name = "Changed" };

            var updated = await service.UpdateUserAsync(student.UserId, updateDto, admin);

            Assert.True(updated);
            Assert.Equal("Changed", db.Users.Find(student.UserId)!.Name);
        }

        [Fact]
        public async System.Threading.Tasks.Task UpdateUserAsync_UserCannotEditOtherUser()
        {
            var db = GetDbContext(nameof(UpdateUserAsync_UserCannotEditOtherUser));
            var user1 = CreateStudent("user1");
            var user2 = CreateStudent("user2");
            db.Users.Add(user1);
            db.Users.Add(user2);
            db.SaveChanges();

            var service = new UserService(db);
            var updateDto = new UserUpdateDto { Name = "Changed" };

            var updated = await service.UpdateUserAsync(user2.UserId, updateDto, user1);

            Assert.False(updated);
        }

        [Fact]
        public async System.Threading.Tasks.Task ArchiveUserAsync_AdminCanArchiveUser()
        {
            var db = GetDbContext(nameof(ArchiveUserAsync_AdminCanArchiveUser));
            var admin = CreateAdmin();
            var student = CreateStudent();
            db.Users.Add(admin);
            db.Users.Add(student);
            db.SaveChanges();

            var service = new UserService(db);
            var result = await service.ArchiveUserAsync(student.UserId, admin);

            Assert.True(result);
            Assert.True(db.Users.Find(student.UserId)!.Archive);
        }

        [Fact]
        public async System.Threading.Tasks.Task ArchiveUserAsync_AdminCannotArchiveLastActiveAdmin()
        {
            var db = GetDbContext(nameof(ArchiveUserAsync_AdminCannotArchiveLastActiveAdmin));
            var admin = CreateAdmin();
            db.Users.Add(admin);
            db.SaveChanges();

            var service = new UserService(db);
            var result = await service.ArchiveUserAsync(admin.UserId, admin);

            Assert.False(result);
            Assert.False(db.Users.Find(admin.UserId)!.Archive);
        }

        [Fact]
        public async System.Threading.Tasks.Task ArchiveUserAsync_UserCannotArchive()
        {
            var db = GetDbContext(nameof(ArchiveUserAsync_UserCannotArchive));
            var admin = CreateAdmin();
            var student = CreateStudent();
            db.Users.Add(admin);
            db.Users.Add(student);
            db.SaveChanges();

            var service = new UserService(db);

            var result = await service.ArchiveUserAsync(admin.UserId, student);

            Assert.False(result);
        }

        [Fact]
        public async System.Threading.Tasks.Task RestoreUserAsync_AdminCanRestoreArchivedUser()
        {
            var db = GetDbContext(nameof(RestoreUserAsync_AdminCanRestoreArchivedUser));
            var admin = CreateAdmin();
            var student = CreateStudent(archive: true);
            db.Users.Add(admin);
            db.Users.Add(student);
            db.SaveChanges();

            var service = new UserService(db);

            var result = await service.RestoreUserAsync(student.UserId, admin);

            Assert.True(result);
            Assert.False(db.Users.Find(student.UserId)!.Archive);
        }

        [Fact]
        public async System.Threading.Tasks.Task RestoreUserAsync_UserCannotRestore()
        {
            var db = GetDbContext(nameof(RestoreUserAsync_UserCannotRestore));
            var admin = CreateAdmin(archive: true);
            var student = CreateStudent();
            db.Users.Add(admin);
            db.Users.Add(student);
            db.SaveChanges();

            var service = new UserService(db);

            var result = await service.RestoreUserAsync(admin.UserId, student);

            Assert.False(result);
        }

        [Fact]
        public async System.Threading.Tasks.Task ChangeUserRoleAsync_AdminCanChangeRole()
        {
            var db = GetDbContext(nameof(ChangeUserRoleAsync_AdminCanChangeRole));
            var admin = CreateAdmin();
            var student = CreateStudent();
            db.Users.Add(admin);
            db.Users.Add(student);
            db.SaveChanges();

            var service = new UserService(db);

            var result = await service.ChangeUserRoleAsync(student.UserId, 2, admin);

            Assert.True(result);
            Assert.Equal(2, db.Users.Find(student.UserId)!.Role);
        }

        [Fact]
        public async System.Threading.Tasks.Task ChangeUserRoleAsync_UserCannotChangeRole()
        {
            var db = GetDbContext(nameof(ChangeUserRoleAsync_UserCannotChangeRole));
            var admin = CreateAdmin();
            var student = CreateStudent();
            db.Users.Add(admin);
            db.Users.Add(student);
            db.SaveChanges();

            var service = new UserService(db);

            var result = await service.ChangeUserRoleAsync(admin.UserId, 1, student);

            Assert.False(result);
            Assert.Equal(2, db.Users.Find(admin.UserId)!.Role);
        }
    }
}