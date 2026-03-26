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

        [Fact]
        public async Task SaveHourlyWeatherLogsForOneDay()
        {
            // Arrange
            var startDate = DateTime.Today;
            var weatherLogs = new System.Collections.Generic.List<WeatherLoggingModel>();

            
            for (int hour = 0; hour < 24; hour++)
            {
                var weatherLog = new WeatherLoggingModel
                {
                    WeatherCondition = GetWeatherConditionForHour(hour),
                    Temperature = GetTemperatureForHour(hour),
                    DateTime = startDate.AddHours(hour)
                };
                weatherLogs.Add(weatherLog);
            }

            // Act
            foreach (var log in weatherLogs)
            {
                var result = await _dbContext.SaveWeatherLogAsync(log);
                Assert.True(result > 0, $"Failed to save weather log for {log.DateTime}");
                Assert.True(log.Id > 0, $"Weather log ID should be set for {log.DateTime}");
            }

            // Assert
            var allWeatherLogs = await _dbContext.GetWeatherLogsAsync();
            Assert.Equal(24, allWeatherLogs.Count);

            
            for (int hour = 0; hour < 24; hour++)
            {
                var expectedDateTime = startDate.AddHours(hour);
                var logForHour = allWeatherLogs.Find(w => w.DateTime == expectedDateTime);
                
                Assert.NotNull(logForHour);
                Assert.Equal(expectedDateTime, logForHour.DateTime);
                Assert.True(logForHour.Temperature >= -30 && logForHour.Temperature <= 40, 
                    $"Temperature should be realistic for hour {hour}");
                Assert.False(string.IsNullOrEmpty(logForHour.WeatherCondition), 
                    $"Weather condition should not be empty for hour {hour}");
            }
        }

        
        private string GetWeatherConditionForHour(int hour)
        {
            return hour switch
            {
                >= 0 and < 6 => "Klart",
                >= 6 and < 9 => "Lätt molnighet",
                >= 9 and < 15 => "Soligt",
                >= 15 and < 18 => "Lätt molnighet",
                >= 18 and < 21 => "Mulet",
                _ => "Klart"
            };
        }

        
        private double GetTemperatureForHour(int hour)
        {
           
            return hour switch
            {
                >= 0 and < 6 => 5.0 + hour * 0.5,    // Kallast på natten
                >= 6 and < 12 => 8.0 + (hour - 6) * 2.0,  // Värms upp på morgonen
                >= 12 and < 15 => 18.0 + (hour - 12) * 0.5,  // Varmast på eftermiddagen
                >= 15 and < 21 => 20.0 - (hour - 15) * 1.5,  // Svalnar på kvällen
                _ => 10.0 - (hour - 21) * 1.0  // Blir kallare på natten
            };
        }

        public void Dispose()
        {
            // Stäng databasanslutningen först
            _dbContext.CloseAsync().GetAwaiter().GetResult();
            
            // Vänta en kort stund för att säkerställa att filen frigörs
            System.Threading.Thread.Sleep(100);
            
            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }
        }
    }
}