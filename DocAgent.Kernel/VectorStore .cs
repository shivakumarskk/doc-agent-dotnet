using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB;

public class VectorStore : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<VectorRecord> _collection;
    private readonly ILiteCollection<FeedbackRecord> _feedback;

    public VectorStore(string path = "DocAgentVectors.db")
    {
        _db = new LiteDatabase(path);
        _collection = _db.GetCollection<VectorRecord>("vectors");
        _feedback = _db.GetCollection<FeedbackRecord>("feedback");
        _collection.EnsureIndex(x => x.Id);
        _feedback.EnsureIndex(x => x.Id);
    }

    public void Add(string id, string documentId, float[] vector, string text)
    {
        _collection.Upsert(new VectorRecord
        {
            Id = id,
            DocumentId = documentId,
            Vector = vector,
            Text = text
        });
    }

    public void AddFeedback(FeedbackRecord feedback)
    {
        _feedback.Insert(feedback);
    }

    public IEnumerable<string> GetAllDocuments()
    {
        return _collection.FindAll()
                          .Select(r => r.DocumentId)
                          .Distinct()
                          .OrderBy(d => d);
    }

    public IEnumerable<VectorRecord> GetAllVectors()
    {
        return _collection.FindAll();
    }


    public IEnumerable<VectorRecord> Query(float[] queryVector, int topK = 5, string? documentId = null)
    {
        var all = documentId == null
            ? _collection.FindAll()
            : _collection.Find(x => x.DocumentId == documentId);

        // Load feedback
        var feedbacks = _feedback.FindAll().ToList();

        return all
            .Select(r =>
            {
                var score = CosineSimilarity(queryVector, r.Vector);

                // Step 4: adjust score based on feedback
                var relatedFeedback = feedbacks.Where(f => f.ContextChunkIds.Contains(r.Id));
                foreach (var fb in relatedFeedback)
                {
                    score += fb.IsPositive ? 0.1f : -0.1f;
                }

                return new { Record = r, Score = score };
            })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Record);
    }


    public void DeleteDocument(string documentId)
    {
        _collection.DeleteMany(x => x.DocumentId == documentId);
    }


    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) throw new ArgumentException("Vector length mismatch");
        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        return dot / (float)(Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    public void Dispose() => _db.Dispose();
}

public class VectorRecord
{
    public string Id { get; set; } = "";
    public string DocumentId { get; set; } = ""; 
    public float[] Vector { get; set; } = Array.Empty<float>();
    public string Text { get; set; } = "";
}

public class FeedbackRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Question { get; set; } = "";
    public string Answer { get; set; } = "";
    public List<string> ContextChunkIds { get; set; } = new();
    public bool IsPositive { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
