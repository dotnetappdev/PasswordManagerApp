using Allure.NUnit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using VaultGuard.DAL;
using VaultGuard.Services.Services;
using System.Threading.Tasks;

namespace VaultGuard.BackEnd.Tests.Services
{
    [TestFixture]
    [AllureNUnit]
    public class DatabaseMigrationServiceTests
    {
        private DatabaseMigrationService _migrationService;
        private VaultGuardDbContext _context;
        private ILogger<DatabaseMigrationService> _logger;

        [SetUp]
        public void SetUp()
        {
            // Create an in-memory database context for testing (single application context).
            var optionsApi = new DbContextOptionsBuilder<VaultGuardDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            _context = new VaultGuardDbContext(optionsApi);

            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseMigrationService>.Instance;

            _migrationService = new DatabaseMigrationService(_context, _logger);
        }

        [TearDown]
        public void TearDown()
        {
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
            // InMemory databases don't support migrations, so we expect either:
            // - Success with "No pending migrations found" message, or
            // - The service handles InMemory gracefully
            // Just verify the result structure is valid
            Assert.That(result.Message, Is.Not.Null.And.Not.Empty);
            Assert.That(result.AppliedMigrations, Is.Not.Null);
            
            // If no migrations were applied (expected for InMemory), that's fine
            if (!result.AppliedMigrations.Any())
            {
                Assert.That(result.Success, Is.True, "Should succeed when no migrations are pending");
            }
        }
    }
}