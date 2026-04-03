using SQLite;
using System.Text.Json;
using BinokelTracker.Models;

namespace BinokelTracker.Services;

public class GameStorageService : IGameStorageService
{
    private SQLiteAsyncConnection? _db;
    private const string DbName = "binokel.db3";

    private async Task<SQLiteAsyncConnection> GetDb()
    {
        if (_db is not null) return _db;

        var path = Path.Combine(FileSystem.AppDataDirectory, DbName);
        _db = new SQLiteAsyncConnection(path);
        await _db.CreateTableAsync<AppStateRecord>();
        return _db;
    }

    public async Task<AppState> LoadAsync()
    {
        try
        {
            var db = await GetDb();
            var record = await db.Table<AppStateRecord>().FirstOrDefaultAsync();
            if (record?.Json is null) return new AppState();
            return JsonSerializer.Deserialize<AppState>(record.Json) ?? new AppState();
        }
        catch
        {
            return new AppState();
        }
    }

    public async Task SaveAsync(AppState state)
    {
        try
        {
            var db = await GetDb();
            var json = JsonSerializer.Serialize(state);
            var existing = await db.Table<AppStateRecord>().FirstOrDefaultAsync();
            if (existing is null)
                await db.InsertAsync(new AppStateRecord { Id = 1, Json = json });
            else
            {
                existing.Json = json;
                await db.UpdateAsync(existing);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Save failed: {ex.Message}");
        }
    }

    [Table("AppState")]
    private class AppStateRecord
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string Json { get; set; } = "";
    }
}