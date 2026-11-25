using System.Linq.Expressions;
using MongoDB.Driver;
using StarTalkSessions.Models;

namespace StarTalkSessions.Data
{
    public class LiveStreamDatabaseAccess
    {
        private readonly IMongoCollection<LiveStream> _collection;
        private readonly ILogger<LiveStreamDatabaseAccess> _logger;

        public LiveStreamDatabaseAccess(IMongoDatabase mongodb, ILogger<LiveStreamDatabaseAccess> logger)
        {
            _collection = mongodb.GetCollection<LiveStream>("livestreams");
            _logger = logger;
        }

        public async Task<LiveStream?> QueryOneAsync(Expression<Func<LiveStream, bool>> query, CancellationToken ctl)
        {
            try { ctl.ThrowIfCancellationRequested(); return await _collection.Find(query).FirstOrDefaultAsync(ctl); }
            catch (Exception ex) { _logger.LogError(ex, "Error in QueryOne"); return null; }
        }

        public async Task<List<LiveStream>> QueryManyAsync(Expression<Func<LiveStream, bool>> query, CancellationToken ctl)
        {
            try { ctl.ThrowIfCancellationRequested(); return await _collection.Find(query).ToListAsync(ctl); }
            catch (Exception ex) { _logger.LogError(ex, "Error in QueryMany"); return new List<LiveStream>(); }
        }

        public async Task<bool> InsertAsync(LiveStream item, CancellationToken ctl)
        {
            try { ctl.ThrowIfCancellationRequested(); await _collection.InsertOneAsync(item, cancellationToken: ctl); return true; }
            catch (Exception ex) { _logger.LogError(ex, "Error in Insert"); return false; }
        }

        public async Task<bool> UpdateAsync(LiveStream item, CancellationToken ctl)
        {
            try { var result = await _collection.ReplaceOneAsync(x => x.Id == item.Id, item, cancellationToken: ctl); return result.ModifiedCount > 0; }
            catch (Exception ex) { _logger.LogError(ex, "Error in Update"); return false; }
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ctl)
        {
            try { ctl.ThrowIfCancellationRequested(); var result = await _collection.DeleteOneAsync(x => x.Id == id, ctl); return result.DeletedCount > 0; }
            catch (Exception ex) { _logger.LogError(ex, "Error in Delete"); return false; }
        }
    }
}
