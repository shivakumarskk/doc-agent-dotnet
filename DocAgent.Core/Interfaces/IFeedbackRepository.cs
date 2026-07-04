using DocAgent.Core.Models;

namespace DocAgent.Core.Interfaces;

/// <summary>
/// Interface for feedback repository
/// </summary>
public interface IFeedbackRepository
{
    void Add(Feedback feedback);
    IEnumerable<Feedback> GetAll();
    IEnumerable<Feedback> GetByQueryId(string queryId);
}
