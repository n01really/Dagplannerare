using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;
using PlannerApp.Models;

namespace PlannerApp.DB
{
    internal class dbContext
    {
        private SQLiteAsyncConnection _connection;

        public dbContext(string dbPath)
        {
            _connection = new SQLiteAsyncConnection(dbPath);
            InitializeDatabaseAsync().Wait();
        }

        private async Task InitializeDatabaseAsync()
        {
            await _connection.CreateTableAsync<WeatherLoggingModel>();
            await _connection.CreateTableAsync<SchedualModel>();
            await _connection.CreateTableAsync<ProcessLoggingModel>();
        }

        // ============ WeatherLoggingModel CRUD ============
        public Task<List<WeatherLoggingModel>> GetWeatherLogsAsync()
        {
            return _connection.Table<WeatherLoggingModel>().ToListAsync();
        }

        public Task<WeatherLoggingModel> GetWeatherLogAsync(int id)
        {
            return _connection.Table<WeatherLoggingModel>()
                .Where(w => w.Id == id)
                .FirstOrDefaultAsync();
        }

        public Task<int> SaveWeatherLogAsync(WeatherLoggingModel weatherLog)
        {
            if (weatherLog.Id != 0)
            {
                return _connection.UpdateAsync(weatherLog);
            }
            else
            {
                return _connection.InsertAsync(weatherLog);
            }
        }

        public Task<int> DeleteWeatherLogAsync(WeatherLoggingModel weatherLog)
        {
            return _connection.DeleteAsync(weatherLog);
        }

        // ============ SchedualModel CRUD ============
        public Task<List<SchedualModel>> GetSchedualsAsync()
        {
            return _connection.Table<SchedualModel>().ToListAsync();
        }

        public Task<SchedualModel> GetSchedualAsync(int id)
        {
            return _connection.Table<SchedualModel>()
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync();
        }

        public Task<List<SchedualModel>> GetSchedualsByDateAsync(DateTime date)
        {
            return _connection.Table<SchedualModel>()
                .Where(s => s.DateTime.Date == date.Date)
                .ToListAsync();
        }

        public Task<int> SaveSchedualAsync(SchedualModel schedual)
        {
            if (schedual.Id != 0)
            {
                return _connection.UpdateAsync(schedual);
            }
            else
            {
                return _connection.InsertAsync(schedual);
            }
        }

        public Task<int> DeleteSchedualAsync(SchedualModel schedual)
        {
            return _connection.DeleteAsync(schedual);
        }

        // ============ ProcessLoggingModel CRUD ============
        public Task<List<ProcessLoggingModel>> GetProcessLogsAsync()
        {
            return _connection.Table<ProcessLoggingModel>().ToListAsync();
        }

        public Task<ProcessLoggingModel> GetProcessLogAsync(int id)
        {
            return _connection.Table<ProcessLoggingModel>()
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync();
        }

        public Task<List<ProcessLoggingModel>> GetProcessLogsByAppNameAsync(string appName)
        {
            return _connection.Table<ProcessLoggingModel>()
                .Where(p => p.AppName == appName)
                .ToListAsync();
        }

        public Task<int> SaveProcessLogAsync(ProcessLoggingModel processLog)
        {
            if (processLog.Id != 0)
            {
                return _connection.UpdateAsync(processLog);
            }
            else
            {
                return _connection.InsertAsync(processLog);
            }
        }

        public Task<int> DeleteProcessLogAsync(ProcessLoggingModel processLog)
        {
            return _connection.DeleteAsync(processLog);
        }
    }
}
