using Xunit;
using PlannerApp.SRC.Models;
using PlannerApp.SRC.DB;
using System;
using System.IO;
using System.Threading.Tasks;

namespace BookingTests
{
    public class UnitTest1 : IDisposable
    {
        private readonly String _testDbPath;
        private readonly dbContext _dbContext;

        public UnitTest1()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"testdb_{Guid.NewGuid()}.db");
            _dbContext = new dbContext(_testDbPath);
        }

        [Fact]
        private async Task SaveSchedualAsync()
        {
            // Arrange
            var schedual = new SchedualModel
            {
                AppName = "Test App",
                AppId = 1,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddHours(1),
                Description = "Test Description",
                DateTime = DateTime.Now
            };

            // Act
            var result = await _dbContext.SaveSchedualAsync(schedual);

            // Assert
            Assert.True(result > 0, "Schedual should be saved and return a valid ID.");
            Assert.True(schedual.Id > 0, "Schedual ID should be set after saving.");
        }

        public void Dispose()
        {
            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }
        }
    }
}
