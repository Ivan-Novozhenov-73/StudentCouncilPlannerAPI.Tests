using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StudetCouncilPlannerAPI.Data;
using StudetCouncilPlannerAPI.Models.Entities;
using StudetCouncilPlannerAPI.Models.DTOs;
using StudetCouncilPlannerAPI.Services;

namespace StudetCouncilPlannerAPI.Tests
{
    public class AuthServiceTests
    {
        private ApplicationDbContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private IConfiguration GetConfig()
        {
            var settings = new Dictionary<string, string>
            {
                {"Jwt:Key", "test_key_12345678901234567890123456789012"},
                {"Jwt:Issuer", "test_issuer"},
                {"Jwt:Audience", "test_audience"}
            };
            return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        }

        [Fact]
        public async System.Threading.Tasks.Task RegisterAsync_RegistersNewUser()
        {
            var db = GetDbContext(nameof(RegisterAsync_RegistersNewUser));
            var service = new AuthService(db, GetConfig());

            var dto = new UserRegisterDto
            {
                Login = "user1",
                Password = "Password123",
                Surname = "Ivanov",
                Name = "Ivan",
                Patronymic = "Ivanovich",
                Group = "A1",
                Phone = 1234567890,
                Contacts = "tg"
            };

            var result = await service.RegisterAsync(dto);

            Assert.NotNull(result);
            Assert.Equal("user1", result.Login);
        }

        [Fact]
        public async System.Threading.Tasks.Task RegisterAsync_DoesNotRegisterDuplicateLogin()
        {
            var db = GetDbContext(nameof(RegisterAsync_DoesNotRegisterDuplicateLogin));
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Login = "user1",
                PasswordHash = "hash",
                Surname = "Ivanov",
                Name = "Ivan",
                Patronymic = "Ivanovich",
                Group = "A1",
                Phone = 1234567890,
                Contacts = "tg",
                Role = 0
            };
            db.Users.Add(user);
            db.SaveChanges();

            var service = new AuthService(db, GetConfig());

            var dto = new UserRegisterDto
            {
                Login = "user1",
                Password = "Password123",
                Surname = "Petrov",
                Name = "Petr",
                Patronymic = "Petrovich",
                Group = "B2",
                Phone = 9876543210,
                Contacts = "tg"
            };

            var result = await service.RegisterAsync(dto);

            Assert.Null(result);
        }

        [Fact]
        public async System.Threading.Tasks.Task LoginAsync_ReturnsTokenForValidCredentials()
        {
            var db = GetDbContext(nameof(LoginAsync_ReturnsTokenForValidCredentials));
            var service = new AuthService(db, GetConfig());

            var password = "Password123";
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Login = "user1",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Surname = "Ivanov",
                Name = "Ivan",
                Patronymic = "Ivanovich",
                Group = "A1",
                Phone = 1234567890,
                Contacts = "tg",
                Role = 0
            };
            db.Users.Add(user);
            db.SaveChanges();

            var dto = new UserLoginDto
            {
                Login = "user1",
                Password = password
            };

            var result = await service.LoginAsync(dto);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrWhiteSpace(result.Token));
            Assert.NotNull(result.User);
            Assert.Equal("user1", result.User.Login);
        }

        [Fact]
        public async System.Threading.Tasks.Task LoginAsync_ReturnsNullForInvalidPassword()
        {
            var db = GetDbContext(nameof(LoginAsync_ReturnsNullForInvalidPassword));
            var service = new AuthService(db, GetConfig());

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Login = "user1",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                Surname = "Ivanov",
                Name = "Ivan",
                Patronymic = "Ivanovich",
                Group = "A1",
                Phone = 1234567890,
                Contacts = "tg",
                Role = 0
            };
            db.Users.Add(user);
            db.SaveChanges();

            var dto = new UserLoginDto
            {
                Login = "user1",
                Password = "WrongPassword"
            };

            var result = await service.LoginAsync(dto);

            Assert.Null(result);
        }
    }
}