using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using PasswordManager.DAL;
using PasswordManager.Services.Services;
using System.Threading.Tasks;

namespace PasswordManager.BackEnd.Tests.Services
{
    [TestFixture]
    public class DatabaseMigrationServiceTests
    {
        private DatabaseMigrationService _migrationService;
        private PasswordManagerDbContextApp _contextApp;
        private PasswordManagerDbContext _context;
        private ILogger<DatabaseMigrationService> _logger;

        [SetUp]
        public void SetUp()
        {
            // Create in-memory database contexts for testing
            var optionsApp = new DbContextOptionsBuilder<PasswordManagerDbContextApp>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            
            var optionsApi = new DbContextOptionsBuilder<PasswordManagerDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            _contextApp = new PasswordManagerDbContextApp(optionsApp);
            _context = new PasswordManagerDbContext(optionsApi);
            
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseMigrationService>.Instance;
            
            _migrationService = new DatabaseMigrationService(_contextApp, _context, _logger);
        }

        [TearDown]
        public void TearDown()
        {
            _contextApp?.Dispose();
            _context?.Dispose();
        }

        [Test]
        public async Task CreateDatabaseAsync_ShouldReturnSuccess()
        {
            // Act
            var result = await _migrationService.CreateDatabaseAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.Not.Null.And.Not.Empty);
            Assert.That(result.AppliedMigrations, Is.Not.Null);
        }

        [Test]
        public async Task GetMigrationStatusAsync_ShouldReturnValidStatus()
        {
            // Act
            var result = await _migrationService.GetMigrationStatusAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.PendingMigrations, Is.Not.Null);
            Assert.That(result.AppliedMigrations, Is.Not.Null);
        }

        [Test]
        public async Task GetAppliedMigrationsAsync_ShouldReturnCollection()
        {
            // Act
            var result = await _migrationService.GetAppliedMigrationsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetPendingMigrationsAsync_ShouldReturnCollection()
        {
            // Act
            var result = await _migrationService.GetPendingMigrationsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task ApplyPendingMigrationsAsync_ShouldReturnValidResult()
        {
            // Act
            var result = await _migrationService.ApplyPendingMigrationsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.Not.Null.And.Not.Empty);
            Assert.That(result.AppliedMigrations, Is.Not.Null);
        }
    }
}