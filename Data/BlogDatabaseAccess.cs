using System.Linq.Expressions;
using StarTalkSessions.Models;
using MongoDB.Driver;

namespace StarTalkSessions.Data
{
    public class BlogDatabaseAccess
    {
        private readonly IMongoCollection<BlogPost> _collection;
        private readonly ILogger<BlogDatabaseAccess> _logger;

        public BlogDatabaseAccess(IMongoDatabase mongodb, ILogger<BlogDatabaseAccess> logger)
        {
            _collection = mongodb.GetCollection<BlogPost>("blogPosts");
            _logger = logger;
        }

        public async Task<BlogPost?> QueryOneAsync(Expression<Func<BlogPost, bool>> query, CancellationToken ctl)
        {
            try { ctl.ThrowIfCancellationRequested(); return await _collection.Find(query).FirstOrDefaultAsync(ctl); }
            catch (Exception ex) { _logger.LogError(ex, "Error in QueryOne"); return null; }
        }

        public async Task<List<BlogPost>> QueryManyAsync(Expression<Func<BlogPost, bool>> query, CancellationToken ctl)
        {
            try { ctl.ThrowIfCancellationRequested(); return await _collection.Find(query).ToListAsync(ctl); }
            catch (Exception ex) { _logger.LogError(ex, "Error in QueryMany"); return new List<BlogPost>(); }
        }

        public async Task<bool> InsertAsync(BlogPost item, CancellationToken ctl)
        {
            try { ctl.ThrowIfCancellationRequested(); await _collection.InsertOneAsync(item, cancellationToken: ctl); return true; }
            catch (Exception ex) { _logger.LogError(ex, "Error in Insert"); return false; }
        }

        public async Task<bool> UpdateAsync(BlogPost item, CancellationToken ctl)
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