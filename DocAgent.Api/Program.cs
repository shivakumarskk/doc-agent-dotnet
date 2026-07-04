using DocAgent.Infrastructure;
using DocAgent.Application;

var builder = WebApplication.CreateBuilder(args);

// Add infrastructure and application services
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Add controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Initialize ChromaDB collections
using (var scope = app.Services.CreateScope())
{
    try
    {
        var vectorStore = scope.ServiceProvider.GetRequiredService<DocAgent.Core.Interfaces.IVectorStore>();

        // Ensure database exists
        if (!await vectorStore.DatabaseExistsAsync("default_database"))
        {
            await vectorStore.CreateDatabaseAsync("default_database");
            Console.WriteLine("Database 'default_database' created.");
        }

        var collections = new[] { "doc-agent", "documents", "users" };

        foreach (var name in collections)
        {
            if (!await vectorStore.CollectionExistsAsync(name))
            {
                await vectorStore.CreateCollectionAsync(name, new Dictionary<string, object>
                {
                    { "description", $"Default collection {name}" }
                });
                Console.WriteLine($"Collection '{name}' created.");
            }
            else
            {
                Console.WriteLine($"Collection '{name}' already exists.");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Warning: Could not initialize ChromaDB: {ex.Message}");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
