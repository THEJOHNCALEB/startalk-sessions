using System.Linq.Expressions;
using StarTalkSessions.Models;
using MongoDB.Driver;

namespace StarTalkSessions.Data
{
    public class NewsletterDatabaseAccess
    {
        private readonly IMongoCollection<NewsletterSubscription> _collection;
        private readonly ILogger<NewsletterDatabaseAccess> _logger;

        public NewsletterDatabaseAccess(IMongoDatabase mongodb, ILogger<NewsletterDatabaseAccess> logger)
        {
            _collection = mongodb.GetCollection<NewsletterSubscription>("newsletters");
            _logger = logger;
        }

        public async Task<NewsletterSubscription?> QueryOneAsync(Expression<Func<NewsletterSubscription, bool>> query, CancellationToken ctl)
        {
            try { ctl.ThrowIfCancellationRequested(); return await _collection.Find(query).FirstOrDefaultAsync(ctl); }
            catch (Exception ex) { _logger.LogError(ex, "Error in QueryOne"); return null; }
        }

        public async Task<List<NewsletterSubscription>> QueryManyAsync(Expression<Func<NewsletterSubscription, bool>> query, CancellationToken ctl)
        {
            try { ctl.ThrowIfCancellationRequested(); return await _collection.Find(query).ToListAsync(ctl); }
            catch (Exception ex) { _logger.LogError(ex, "Error in QueryMany"); return new List<NewsletterSubscription>(); }
        }

        public async Task<bool> InsertAsync(NewsletterSubscription item, CancellationToken ctl)
        {
            try { ctl.ThrowIfCancellationRequested(); await _collection.InsertOneAsync(item, cancellationToken: ctl); return true; }
            catch (Exception ex) { _logger.LogError(ex, "Error in Insert"); return false; }
        }

        public async Task<bool> UpdateAsync(NewsletterSubscription item, CancellationToken ctl)
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