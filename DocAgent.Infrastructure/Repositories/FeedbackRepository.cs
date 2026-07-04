using LiteDB;
using DocAgent.Core.Models;
using DocAgent.Core.Interfaces;

namespace DocAgent.Infrastructure.Repositories;

/// <summary>
/// LiteDB-based feedback repository
/// </summary>
public class FeedbackRepository : IFeedbackRepository, IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<Feedback> _collection;

    public FeedbackRepository(string dbPath = "feedback.db")
    {
        _db = new LiteDatabase(dbPath);
        _collection = _db.GetCollection<Feedback>("feedback");
        _collection.EnsureIndex(x => x.Id);
        _collection.EnsureIndex(x => x.QueryId);
    }

    public void Add(Feedback feedback)
    {
        _collection.Insert(feedback);
    }

    public IEnumerable<Feedback> GetAll()
    {
        return _collection.FindAll();
    }

    public IEnumerable<Feedback> GetByQueryId(string queryId)
    {
        return _collection.Find(x => x.QueryId == queryId);
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}
