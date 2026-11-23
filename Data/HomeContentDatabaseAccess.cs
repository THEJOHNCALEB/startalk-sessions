using System.Linq.Expressions;
using MongoDB.Driver;
using StarTalkSessions.Models;

namespace StarTalkSessions.Data
{
    public class HomeContentDatabaseAccess
    {
        private readonly IMongoCollection<HomeContent> _collection;
        private readonly ILogger<HomeContentDatabaseAccess> _logger;

        public HomeContentDatabaseAccess(IMongoDatabase mongodb, ILogger<HomeContentDatabaseAccess> logger)
        {
            _collection = mongodb.GetCollection<HomeContent>("homecontent");
            _logger = logger;
        }

        public async Task<HomeContent?> QueryOneAsync(Expression<Func<HomeContent, bool>> query, CancellationToken ctl)
        {
            try { ctl.ThrowIfCancellationRequested(); return await _collection.Find(query).FirstOrDefaultAsync(ctl); }
            catch (Exception ex) { _logger.LogError(ex, "Error in QueryOne"); return null; }
        }

        public async Task<List<HomeContent>> QueryManyAsync(Expression<Func<HomeContent, bool>> query, CancellationToken ctl)
        {
            try { ctl.ThrowIfCancellationRequested(); return await _collection.Find(query).ToListAsync(ctl); }
            catch (Exception ex) { _logger.LogError(ex, "Error in QueryMany"); return new List<HomeContent>(); }
        }

        public async Task<bool> InsertAsync(HomeContent item, CancellationToken ctl)
        {
            try { ctl.ThrowIfCancellationRequested(); await _collection.InsertOneAsync(item, cancellationToken: ctl); return true; }
            catch (Exception ex) { _logger.LogError(ex, "Error in Insert"); return false; }
        }

        public async Task<bool> UpdateAsync(HomeContent item, CancellationToken ctl)
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
