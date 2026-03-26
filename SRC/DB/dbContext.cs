using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SQLite;
using PlannerApp.SRC.Models;

namespace PlannerApp.SRC.DB
{
    public class dbContext
    {
        private SQLiteAsyncConnection _connection;
        private bool _isInitialized = false;
        private readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1); // Låsmekanism för att begränsa hur många trådar som kan initiera samtidigt, med (1, 1) kan bara en tråd initiera åt gången

        public dbContext(string dbPath)
        {
            _connection = new SQLiteAsyncConnection(dbPath);
        }

        private async Task InitializeDatabaseAsync()
        {
            if (_isInitialized)
                return;

            await _initLock.WaitAsync();
            try
            {
                if (_isInitialized)
                    return;

                await _connection.CreateTableAsync<WeatherLoggingModel>();
                await _connection.CreateTableAsync<SchedualModel>();
                await _connection.CreateTableAsync<ProcessLoggingModel>();

                _isInitialized = true;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task CloseAsync()
        {
            if (_connection != null)
            {
                await _connection.CloseAsync();
                _connection = null;
                _isInitialized = false;
            }
        }

        // ============ WeatherLoggingModel CRUD ============
        public async Task<List<WeatherLoggingModel>> GetWeatherLogsAsync()
        {
            await InitializeDatabaseAsync();
            return await _connection.Table<WeatherLoggingModel>().ToListAsync();
        }

        public async Task<WeatherLoggingModel> GetWeatherLogAsync(int id)
        {
            await InitializeDatabaseAsync();
            return await _connection.Table<WeatherLoggingModel>()
                .Where(w => w.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<int> SaveWeatherLogAsync(WeatherLoggingModel weatherLog)
        {
            await InitializeDatabaseAsync();
            if (weatherLog.Id != 0)
            {
                return await _connection.UpdateAsync(weatherLog);
            }
            else
            {
                return await _connection.InsertAsync(weatherLog);
            }
        }

        public async Task<int> DeleteWeatherLogAsync(WeatherLoggingModel weatherLog)
        {
            await InitializeDatabaseAsync();
            return await _connection.DeleteAsync(weatherLog);
        }

        // ============ SchedualModel CRUD ============
        public async Task<List<SchedualModel>> GetSchedualsAsync()
        {
            await InitializeDatabaseAsync();
            return await _connection.Table<SchedualModel>().ToListAsync();
        }

        public async Task<SchedualModel> GetSchedualAsync(int id)
        {
            await InitializeDatabaseAsync();
            return await _connection.Table<SchedualModel>()
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<List<SchedualModel>> GetSchedualsByDateAsync(DateTime date)
        {
            await InitializeDatabaseAsync();
            return await _connection.Table<SchedualModel>()
                .Where(s => s.DateTime.Date == date.Date)
                .ToListAsync();
        }

        public async Task<int> SaveSchedualAsync(SchedualModel schedual)
        {
            await InitializeDatabaseAsync();
            if (schedual.Id != 0)
            {
                return await _connection.UpdateAsync(schedual);
            }
            else
            {
                return await _connection.InsertAsync(schedual);
            }
        }

        public async Task<int> DeleteSchedualAsync(SchedualModel schedual)
        {
            await InitializeDatabaseAsync();
            return await _connection.DeleteAsync(schedual);
        }

        // ============ ProcessLoggingModel CRUD ============
        public async Task<List<ProcessLoggingModel>> GetProcessLogsAsync()
        {
            await InitializeDatabaseAsync();
            return await _connection.Table<ProcessLoggingModel>().ToListAsync();
        }

        public async Task<ProcessLoggingModel> GetProcessLogAsync(int id)
        {
            await InitializeDatabaseAsync();
            return await _connection.Table<ProcessLoggingModel>()
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ProcessLoggingModel>> GetProcessLogsByAppNameAsync(string appName)
        {
            await InitializeDatabaseAsync();
            return await _connection.Table<ProcessLoggingModel>()
                .Where(p => p.AppName == appName)
                .ToListAsync();
        }

        public async Task<int> SaveProcessLogAsync(ProcessLoggingModel processLog)
        {
            await InitializeDatabaseAsync();
            if (processLog.Id != 0)
            {
                return await _connection.UpdateAsync(processLog);
            }
            else
            {
                return await _connection.InsertAsync(processLog);
            }
        }

        public async Task<int> DeleteProcessLogAsync(ProcessLoggingModel processLog)
        {
            await InitializeDatabaseAsync();
            return await _connection.DeleteAsync(processLog);
        }
    }
}
