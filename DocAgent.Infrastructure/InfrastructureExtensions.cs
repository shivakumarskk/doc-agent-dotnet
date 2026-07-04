using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using DocAgent.Core.Interfaces;
using DocAgent.Infrastructure.Services;
using DocAgent.Infrastructure.VectorStores;
using DocAgent.Infrastructure.Repositories;
using DocAgent.Infrastructure.Skills;

namespace DocAgent.Infrastructure;

/// <summary>
/// Extension methods for infrastructure dependency injection
/// </summary>
public static class InfrastructureExtensions
{
    /// <summary>
    /// Register infrastructure services
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Ollama client
        services.AddHttpClient<ILlmClient, OllamaClient>(client =>
        {
            var baseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(configuration.GetValue<int>("Ollama:TimeoutSeconds", 120));
        });

        // Register embedding service with lazy initialization
        var modelPath = Path.Combine(AppContext.BaseDirectory, configuration["Embedding:ModelPath"] ?? "Models/model.onnx");
        var tokenizerPath = Path.Combine(AppContext.BaseDirectory, configuration["Embedding:TokenizerPath"] ?? "Models/tokenizer.json");

        services.AddSingleton<IEmbeddingService>(sp =>
        {
            // Check if model files exist before trying to load them
            if (!File.Exists(modelPath))
            {
                Console.WriteLine($"[WARNING] Embedding model not found at: {modelPath}");
                Console.WriteLine("[INFO] EmbeddingService will be unavailable until model files are provided.");
                return new NullEmbeddingService();
            }
            if (!File.Exists(tokenizerPath))
            {
                Console.WriteLine($"[WARNING] Tokenizer not found at: {tokenizerPath}");
                Console.WriteLine("[INFO] EmbeddingService will be unavailable until model files are provided.");
                return new NullEmbeddingService();
            }

            return new EmbeddingService(modelPath, tokenizerPath);
        });

        // Register vector store with error handling
        var chromaUrl = configuration["ChromaDB:Url"] ?? "http://localhost:8000";
        services.AddSingleton<IVectorStore>(sp => new ChromaVectorStore(chromaUrl));

        // Register text processor
        services.AddSingleton<ITextProcessor, TextProcessor>();

        // Register feedback repository
        var feedbackDbPath = configuration["Feedback:DatabasePath"] ?? "feedback.db";
        services.AddSingleton<IFeedbackRepository>(sp => new FeedbackRepository(feedbackDbPath));

        // Register skills
        services.AddSingleton<CalculatorSkill>();
        services.AddSingleton<ISkillService, SkillService>();

        return services;
    }
}
