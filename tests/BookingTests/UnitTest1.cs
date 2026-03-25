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

        [Fact]
        public async Task SaveProcessLogAsync()
        {
            // Arrange
            var processLog = new ProcessLoggingModel
            {
                AppName = "Test Application",
                AppId = 123,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddMinutes(30) 
            };

            // Act 
            var result = await _dbContext.SaveProcessLogAsync(processLog);

            // Assert 
            Assert.True(result > 0, "Process log should be saved and return a valid ID.");
            Assert.True(processLog.Id > 0, "Process log ID should be set after saving.");

            
            var savedProcessLog = await _dbContext.GetProcessLogAsync(processLog.Id);
            Assert.NotNull(savedProcessLog);
            Assert.Equal("Test Application", savedProcessLog.AppName);
            Assert.Equal(123, savedProcessLog.AppId);
            Assert.Equal(processLog.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                         savedProcessLog.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            Assert.Equal(processLog.EndTime.ToString("yyyy-MM-dd HH:mm:ss"),
                         savedProcessLog.EndTime.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        [Fact]
        public async Task SaveMultipleProcessLogs()
        {
            // Arrange 
            var processLogs = new[]
            {
                new ProcessLoggingModel
                {
                    AppName = "Chrome",
                    AppId = 1,
                    StartTime = DateTime.Now.AddHours(-2),
                    EndTime = DateTime.Now.AddHours(-1)
                },
                new ProcessLoggingModel
                {
                    AppName = "Visual Studio",
                    AppId = 2,
                    StartTime = DateTime.Now.AddHours(-3),
                    EndTime = DateTime.Now
                },
                new ProcessLoggingModel
                {
                    AppName = "Chrome",
                    AppId = 1,
                    StartTime = DateTime.Now.AddMinutes(-30),
                    EndTime = DateTime.Now
                }
            };

            // Act 
            foreach (var log in processLogs)
            {
                var result = await _dbContext.SaveProcessLogAsync(log);
                Assert.True(result > 0, $"Failed to save process log for {log.AppName}");
            }

            // Assert 
            var allLogs = await _dbContext.GetProcessLogsAsync();
            Assert.Equal(3, allLogs.Count);

            
            var chromeLogs = await _dbContext.GetProcessLogsByAppNameAsync("Chrome");
            Assert.Equal(2, chromeLogs.Count);
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