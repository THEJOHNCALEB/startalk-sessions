using MongoDB.Driver;
using StarTalkSessions.Models;

namespace StarTalkSessions.Data
{
    public class SiteSettingDatabaseAccess
    {
        private readonly IMongoCollection<SiteSetting> _collection;
        private readonly ILogger<SiteSettingDatabaseAccess> _logger;

        public SiteSettingDatabaseAccess(IMongoDatabase mongodb, ILogger<SiteSettingDatabaseAccess> logger)
        {
            _collection = mongodb.GetCollection<SiteSetting>("sitesettings");
            _logger = logger;
        }

        public async Task<Dictionary<string, string>> QueryAllAsync(CancellationToken ctl)
        {
            try
            {
                ctl.ThrowIfCancellationRequested();
                var all = await _collection.Find(_ => true).ToListAsync(ctl);
                return all.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error in QueryAll"); return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); }
        }

        public async Task<string?> GetAsync(string key, CancellationToken ctl)
        {
            try
            {
                ctl.ThrowIfCancellationRequested();
                var item = await _collection.Find(x => x.Key == key).FirstOrDefaultAsync(ctl);
                return item?.Value;
            }
            catch (Exception ex) { _logger.LogError(ex, "Error in Get"); return null; }
        }

        public async Task<bool> SetAsync(string key, string value, CancellationToken ctl)
        {
            try
            {
                ctl.ThrowIfCancellationRequested();
                var item = new SiteSetting { Key = key, Value = value, UpdatedAt = DateTime.Now };
                await _collection.ReplaceOneAsync(x => x.Key == key, item, new ReplaceOptions { IsUpsert = true }, ctl);
                return true;
            }
            catch (Exception ex) { _logger.LogError(ex, "Error in Set"); return false; }
        }

        public async Task<bool> SetIfMissingAsync(string key, string value, CancellationToken ctl)
        {
            var existing = await GetAsync(key, ctl);
            return existing == null && await SetAsync(key, value, ctl);
        }
    }
}
