# DocAgent - AI-Powered Document Retrieval and Generation System

A sophisticated .NET 10-based REST API application that combines **Retrieval-Augmented Generation (RAG)** with **Large Language Models (LLM)** to enable intelligent document ingestion, searching, and AI-powered question answering.

---

## 📋 Table of Contents

- [What is DocAgent?](#what-is-docagent)
- [System Architecture](#system-architecture)
- [External Dependencies & Setup](#external-dependencies--setup)
- [Solution Structure](#solution-structure)
- [API Endpoints](#api-endpoints)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Project Layers](#project-layers)

---

## 🎯 What is DocAgent?

**DocAgent** is an intelligent document analysis platform that:

1. **Ingests Documents** - Accepts PDF files or plain text and intelligently chunks them
2. **Generates Embeddings** - Converts text chunks into vector embeddings for semantic search
3. **Stores in Vector Database** - Uses ChromaDB to store and retrieve semantically similar documents
4. **Searches Intelligently** - Finds relevant documents using embedding similarity (without LLM)
5. **Generates Answers with AI** - Uses Ollama LLM to synthesize answers from retrieved documents (RAG pattern)
6. **Collects Feedback** - Tracks user feedback to improve document relevance
7. **Executes Skills** - Runs custom skills like calculators on user requests

### Key Features
✅ **RAG (Retrieval Augmented Generation)** - Combines document search with LLM reasoning  
✅ **Semantic Search** - Finds documents by meaning, not just keywords  
✅ **PDF Processing** - Automatically extracts and chunks PDF documents  
✅ **Streaming Responses** - Supports real-time streaming of LLM-generated answers  
✅ **Feedback Loop** - Collects user feedback for system improvement  
✅ **Extensible Skills** - Add custom skills for specialized tasks  

---

## 🏗️ System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Client / Frontend                        │
└──────────────────────────┬──────────────────────────────────┘
                           │ REST API Requests
                           ▼
┌─────────────────────────────────────────────────────────────┐
│              DocAgent.Api (ASP.NET Core)                     │
│  ┌──────────────┬──────────────┬──────────────┐             │
│  │ Controllers  │ Controllers  │ Controllers  │             │
│  │ (Documents)  │ (RAG)        │ (LLM/Skills) │             │
│  └──────────────┴──────────────┴──────────────┘             │
└──────────────────────────┬──────────────────────────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        ▼                  ▼                  ▼
┌──────────────┐   ┌──────────────┐   ┌──────────────┐
│  Application │   │ Infrastructure│   │   Core       │
│  Services    │   │  Services    │   │  Models/DTOs │
└──────────────┘   └──────────────┘   └──────────────┘
        │                  │
        ▼                  ▼
    ┌─────────────────────────────────┐
    │ External Dependencies            │
    ├─────────────────────────────────┤
    │ • Ollama LLM (Port 11434)       │
    │ • ChromaDB Vector DB (Port 8000)│
    │ • ONNX Embedding Model          │
    │ • PDF Processing Library        │
    └─────────────────────────────────┘
```

---

## 🔧 External Dependencies & Setup

### 1. **Ollama (Large Language Model Server)**

**What it is:** A local LLM inference server that runs open-source language models.

**Installation:**
```bash
# Download from https://ollama.ai
# macOS: brew install ollama
# Windows: Download installer from ollama.ai
# Linux: curl https://ollama.ai/install.sh | sh
```

**Setup:**
```bash
# Start Ollama service
ollama serve

# In another terminal, pull a model
ollama pull llama2  # or: ollama pull mistral, neural-chat, etc.
```

**Configuration (appsettings.json):**
```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama2",
    "TimeoutSeconds": 120
  }
}
```

**Verify it's running:**
```bash
curl http://localhost:11434/api/tags
```

---

### 2. **ChromaDB (Vector Database)**

**What it is:** A vector database specifically designed for storing and querying document embeddings.

**Installation using Docker:**
```bash
# Pull ChromaDB image
docker pull chromadb/chroma:latest

# Run ChromaDB container
docker run -d \
  --name chroma \
  -p 8000:8000 \
  chromadb/chroma:latest
```

**Installation using Python:**
```bash
# Install ChromaDB locally
pip install chromadb

# Run ChromaDB server
chroma run --host 0.0.0.0 --port 8000
```

**Configuration (appsettings.json):**
```json
{
  "ChromaDB": {
    "Url": "http://localhost:8000/api/v2/"
  }
}
```

**Verify it's running:**
```bash
curl http://localhost:8000/api/v1/heartbeat
```

---

### 3. **ONNX Embedding Models**

**What it is:** Lightweight embedding models that convert text to vectors for semantic search.

**Model Files Required:**
- `Models/model.onnx` - The ONNX runtime model
- `Models/tokenizer.json` - HuggingFace tokenizer for the model

**Setup:**
Place model files in the project root under `Models/` directory:
```
DocAgent.Api/
  Models/
    ├── model.onnx
    └── tokenizer.json
```

**Configuration (appsettings.json):**
```json
{
  "Embedding": {
    "ModelPath": "Models/model.onnx",
    "TokenizerPath": "Models/tokenizer.json"
  }
}
```

**Note:** If models are missing, the system gracefully falls back to `NullEmbeddingService`.

---

## 📦 Solution Structure

```
DocAgent Solution (5 Projects)
│
├── 📁 DocAgent.Core
│   ├── Interfaces/
│   │   ├── IDocumentService.cs         ← Core business logic contracts
│   │   ├── IEmbeddingService.cs        ← Embedding generation
│   │   ├── IVectorStore.cs             ← Vector DB operations
│   │   ├── ILlmClient.cs               ← LLM interactions
│   │   ├── ITextProcessor.cs           ← PDF & text processing
│   │   ├── ISkillService.cs            ← Skill execution
│   │   └── IFeedbackRepository.cs      ← Feedback storage
│   │
│   ├── DTOs/
│   │   ├── SearchRequestDto.cs         ← Search query input
│   │   ├── SearchResponseDto.cs        ← Search results
│   │   ├── QueryRequestDto.cs          ← Document query
│   │   ├── AskRequestDto.cs            ← RAG question
│   │   ├── AskResponseDto.cs           ← RAG answer
│   │   ├── IngestDocumentDto.cs        ← Document upload
│   │   ├── IngestResponseDto.cs        ← Upload confirmation
│   │   ├── GenerateRequestDto.cs       ← LLM generation input
│   │   ├── FeedbackDto.cs              ← Feedback data
│   │   └── ResponseDtos.cs             ← Other response models
│   │
│   └── Models/
│       ├── Document.cs                 ← Document entity
│       ├── DocumentChunk.cs            ← Text chunk entity
│       ├── Feedback.cs                 ← Feedback entity
│       └── QueryResult.cs              ← Query result entity
│
├── 📁 DocAgent.Application
│   └── Services/
│       └── DocumentService.cs          ← Business logic implementation
│                                           (orchestrates: embed, search, ask)
│
├── 📁 DocAgent.Infrastructure
│   ├── Services/
│   │   ├── EmbeddingService.cs         ← ONNX embedding generation
│   │   ├── OllamaClient.cs             ← Ollama LLM integration
│   │   ├── TextProcessor.cs            ← PDF extraction & chunking
│   │   └── SkillService.cs             ← Skill management
│   │
│   ├── VectorStores/
│   │   └── ChromaVectorStore.cs        ← ChromaDB integration
│   │
│   ├── Repositories/
│   │   └── FeedbackRepository.cs       ← SQLite feedback storage
│   │
│   ├── Skills/
│   │   └── CalculatorSkill.cs          ← Example skill
│   │
│   └── InfrastructureExtensions.cs     ← DI registration
│
├── 📁 DocAgent.Api
│   ├── Controllers/
│   │   ├── DocumentsController.cs      ← Document ingestion & management
│   │   ├── RagController.cs            ← Search & RAG operations
│   │   ├── LlmController.cs            ← Direct LLM access
│   │   ├── FeedbackController.cs       ← Feedback collection
│   │   └── SkillsController.cs         ← Skill invocation
│   │
│   ├── Program.cs                      ← App startup & config
│   ├── appsettings.json                ← Configuration
│   └── launchSettings.json             ← Launch profiles
│
└── 📁 DocAgent.Kernel
    ├── Models/                         ← Pre-bundled ONNX models
    │   ├── model.onnx
    │   └── tokenizer.json
    └── EmbeddingService.cs             ← Fallback implementation
```

---

## 🔌 API Endpoints

### **📄 Documents Controller** (`/api/documents`)
Manage document ingestion and retrieval.

| Method | Endpoint | Purpose | Why Needed |
|--------|----------|---------|-----------|
| `GET` | `/api/documents` | List all ingested documents | Know what's available in the system |
| `POST` | `/api/documents/ingest` | Upload & process a PDF or text | Add new knowledge to the system |
| `DELETE` | `/api/documents/{id}` | Remove a document | Clean up old/irrelevant data |
| `PUT` | `/api/documents/{id}` | Update existing document | Refresh document content |

**Example: Ingest Document**
```bash
curl -X POST http://localhost:5088/api/documents/ingest \
  -H "Content-Type: application/json" \
  -d '{
    "source": "/path/to/document.pdf",
    "chunkSize": 1000,
    "chunkOverlap": 200
  }'
```

---

### **🔍 RAG Controller** (`/api/rag`)
Search documents and generate AI answers (Retrieval-Augmented Generation).

| Method | Endpoint | Purpose | Why Needed |
|--------|----------|---------|-----------|
| `POST` | `/api/rag/query` | Find relevant documents | Get raw search results without LLM |
| `POST` | `/api/rag/ask` | Ask a question & get AI answer | Get intelligent answers synthesized from documents |
| `POST` | `/api/rag/ask-stream` | Stream LLM response in real-time | Show responses as they're generated |
| `POST` | `/api/rag/search` | Semantic search by embedding | Find documents using vector similarity |
| `POST` | `/api/rag/search-with-summary` | Search + LLM summary | Get concise AI-generated summary of results |

**Workflow Comparison:**

```
┌─────────────────────────────────────────────────────────┐
│ Query (/api/rag/query)                                  │
│ ────────────────────────────────────────────────────────│
│ Embed Query → Search Vector DB → Return Raw Chunks    │
│ Use Case: Need the exact document text                  │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ Ask (/api/rag/ask)                                      │
│ ────────────────────────────────────────────────────────│
│ Embed Query → Search → Build Context → LLM Generate   │
│ Use Case: Need an intelligent synthesized answer        │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ Search (/api/rag/search)                                │
│ ────────────────────────────────────────────────────────│
│ Embed Query → Search Vector DB → Return Matches       │
│ Use Case: Pure semantic search without LLM              │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ Search with Summary (/api/rag/search-with-summary)      │
│ ────────────────────────────────────────────────────────│
│ Embed Query → Search → LLM Summarize Results           │
│ Use Case: Quick AI summary of search results            │
└─────────────────────────────────────────────────────────┘
```

**Example: Ask a Question**
```bash
curl -X POST http://localhost:5088/api/rag/ask \
  -H "Content-Type: application/json" \
  -d '{
    "question": "What is the main topic of the document?",
    "topK": 5,
    "maxTokens": 1024
  }'
```

---

### **🤖 LLM Controller** (`/api/llm`)
Direct access to language model generation.

| Method | Endpoint | Purpose | Why Needed |
|--------|----------|---------|-----------|
| `POST` | `/api/llm/generate` | Generate text from prompt | Use LLM for any text generation task |

**Example: Generate Text**
```bash
curl -X POST http://localhost:5088/api/llm/generate \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama2",
    "prompt": "Write a haiku about coding",
    "maxTokens": 100
  }'
```

---

### **💬 Feedback Controller** (`/api/feedback`)
Collect user feedback for system improvement.

| Method | Endpoint | Purpose | Why Needed |
|--------|----------|---------|-----------|
| `POST` | `/api/feedback` | Submit feedback | Track whether answers were helpful |

**Example: Submit Feedback**
```bash
curl -X POST http://localhost:5088/api/feedback \
  -H "Content-Type: application/json" \
  -d '{
    "queryId": "query-123",
    "contextChunkIds": ["chunk-1", "chunk-2"],
    "isPositive": true,
    "comment": "Great answer! Very helpful."
  }'
```

---

### **🛠️ Skills Controller** (`/api/skills`)
Execute custom skills for specialized tasks.

| Method | Endpoint | Purpose | Why Needed |
|--------|----------|---------|-----------|
| `POST` | `/api/skills/invoke` | Run a named skill | Execute custom logic (e.g., calculator) |

**Example: Invoke Calculator Skill**
```bash
curl -X POST http://localhost:5088/api/skills/invoke \
  -H "Content-Type: application/json" \
  -d '{
    "skillName": "calculator",
    "input": "2 + 2 * 3"
  }'
```

---

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK
- Ollama (running on port 11434)
- ChromaDB (running on port 8000)
- ONNX model files in `Models/` directory

### Installation

1. **Clone the repository**
```bash
git clone https://github.com/shivakumarskk/doc-agent-dotnet.git
cd doc-agent-dotnet
```

2. **Build the solution**
```bash
dotnet build
```

3. **Configure external services** (see [External Dependencies](#external-dependencies--setup))

4. **Run the application**
```bash
cd DocAgent.Api
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5088`
- HTTPS: `https://localhost:7048`
- Swagger/OpenAPI: `http://localhost:5088/openapi/v1.json`

### Quick Test

```bash
# Check if services are running
curl http://localhost:11434/api/tags          # Ollama
curl http://localhost:8000/api/v1/heartbeat   # ChromaDB

# Ingest a document
curl -X POST http://localhost:5088/api/documents/ingest \
  -H "Content-Type: application/json" \
  -d '{"source": "sample.pdf", "chunkSize": 1000}'

# Ask a question
curl -X POST http://localhost:5088/api/rag/ask \
  -H "Content-Type: application/json" \
  -d '{"question": "What is this document about?", "topK": 5}'
```

---

## ⚙️ Configuration

### `appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",

  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama2",
    "TimeoutSeconds": 120
  },

  "Embedding": {
    "ModelPath": "Models/model.onnx",
    "TokenizerPath": "Models/tokenizer.json"
  },

  "ChromaDB": {
    "Url": "http://localhost:8000/api/v2/"
  },

  "Feedback": {
    "DatabasePath": "feedback.db"
  }
}
```

### Available LLM Models

You can use any model available in Ollama:

```bash
ollama pull llama2          # General purpose
ollama pull mistral         # Fast & efficient
ollama pull neural-chat     # Optimized for chat
ollama pull dolphin-mixtral # High quality
ollama pull openchat        # Good context window
```

Update `appsettings.json` to use a different model:
```json
"Ollama": {
  "Model": "mistral"
}
```

---

## 🏛️ Project Layers

### **Layer 1: Core (DocAgent.Core)**
**Responsibility:** Domain logic and contracts

- **Interfaces** - Contracts for all services
- **DTOs** - Data transfer objects for API requests/responses
- **Models** - Domain entities (Document, Feedback, etc.)

**Principle:** Framework-agnostic, reusable contracts

---

### **Layer 2: Application (DocAgent.Application)**
**Responsibility:** Business logic orchestration

- **DocumentService** - Main business logic
  - Ingests documents (PDF extraction, chunking)
  - Generates embeddings
  - Searches vector store
  - Calls LLM for answer generation
  - Handles RAG workflow

**Principle:** Orchestrates infrastructure services, no HTTP concerns

---

### **Layer 3: Infrastructure (DocAgent.Infrastructure)**
**Responsibility:** External service integration

- **EmbeddingService** - ONNX embedding generation
- **OllamaClient** - Ollama LLM API calls
- **ChromaVectorStore** - ChromaDB integration
- **TextProcessor** - PDF extraction & text chunking
- **FeedbackRepository** - SQLite storage
- **SkillService** - Custom skill execution

**Principle:** Implements interfaces, handles external APIs

---

### **Layer 4: API (DocAgent.Api)**
**Responsibility:** HTTP request handling

- **Controllers** - REST endpoints
  - DocumentsController
  - RagController
  - LlmController
  - FeedbackController
  - SkillsController
- **Program.cs** - App startup, DI registration, middleware

**Principle:** Handles HTTP, delegates to services

---

## 🔄 Request/Response Cycle Example

**Scenario: User asks "What is the main topic?"**

```
1. HTTP Request
   POST /api/rag/ask
   {
     "question": "What is the main topic?",
     "topK": 5,
     "maxTokens": 1024
   }

2. RagController
   → Receives request
   → Validates input
   → Calls IDocumentService.AskAsync()

3. DocumentService (Business Logic)
   → Step 1: Embed question using IEmbeddingService
   → Step 2: Query vector store using IVectorStore (get top 5 chunks)
   → Step 3: Build prompt with context
   → Step 4: Call ILlmClient.GenerateAsync()
   → Step 5: Return AskResponseDto

4. Infrastructure Services
   → EmbeddingService: ONNX model generates embedding
   → ChromaVectorStore: Queries ChromaDB, returns matching chunks
   → OllamaClient: Sends prompt to Ollama, gets answer

5. HTTP Response
   {
     "question": "What is the main topic?",
     "answer": "The main topic is...",
     "contextChunks": [...]
   }
```

---

## 📊 Data Flow Diagram

```
Document Upload
├─ PDF Extraction (TextProcessor)
├─ Text Chunking (TextProcessor)
├─ Embedding Generation (EmbeddingService - ONNX)
└─ Store in ChromaDB (ChromaVectorStore)

User Query
├─ Embedding Generation (same model)
├─ Vector Search in ChromaDB (semantic matching)
└─ Return Top-K Results

RAG (Ask)
├─ Vector Search (as above)
├─ Build LLM Prompt with Context
├─ LLM Generation (Ollama)
└─ Return Synthesized Answer

Search
├─ Vector Search (as above)
└─ Return Raw Chunks + Metadata

Search with Summary
├─ Vector Search
├─ LLM Summarization
└─ Return Summary + Chunks
```

---

## 🎓 Why Each Component?

| Component | Why |
|-----------|-----|
| **ONNX Embeddings** | Fast, offline embedding generation without network latency |
| **ChromaDB** | Purpose-built for storing & querying vector embeddings efficiently |
| **Ollama** | Local LLM inference - privacy, control, no API keys needed |
| **Text Chunking** | Large documents must be split for better embedding & retrieval |
| **Vector Search** | Semantic search finds meaning-based matches, not just keywords |
| **RAG Pattern** | Combines retrieval (accurate facts) with generation (natural language) |
| **Feedback System** | Collects data to identify which documents/answers are helpful |
| **Skills System** | Extensible architecture for custom logic (calculators, formatters, etc.) |

---

## 📝 Common Use Cases

### **Use Case 1: Question Answering from Documents**
```
Endpoint: POST /api/rag/ask
Flow: Ingest Docs → User Question → Semantic Search → LLM Synthesis
Result: AI-generated answer citing the documents
```

### **Use Case 2: Document Search**
```
Endpoint: POST /api/rag/search
Flow: Ingest Docs → User Query → Semantic Search
Result: List of relevant document chunks with similarity scores
```

### **Use Case 3: Content Summarization**
```
Endpoint: POST /api/rag/search-with-summary
Flow: Ingest Docs → User Query → Search → LLM Summarize
Result: Quick AI summary of relevant content
```

### **Use Case 4: Real-time Streaming Answers**
```
Endpoint: POST /api/rag/ask-stream
Flow: Ingest Docs → User Question → ... → Stream LLM Output
Result: Real-time token streaming for UI updates
```

### **Use Case 5: Custom Skill Execution**
```
Endpoint: POST /api/skills/invoke
Flow: User Input → Skill Logic → Result
Result: Task-specific output (e.g., calculator result)
```

---

## 🐛 Troubleshooting

### Ollama Connection Error
```
Error: Could not connect to Ollama at http://localhost:11434
```
**Solution:**
```bash
# Verify Ollama is running
ps aux | grep ollama
# Or restart it
ollama serve
```

### ChromaDB Connection Error
```
Error: Could not connect to ChromaDB at http://localhost:8000
```
**Solution:**
```bash
# Check Docker container
docker ps
# Restart if needed
docker restart chroma
```

### Embedding Model Not Found
```
Warning: Embedding model not found at Models/model.onnx
```
**Solution:** Place model files in `Models/` directory or download from HuggingFace

### No Collections Initialized
```
Warning: Could not initialize ChromaDB
```
**Solution:** Program.cs automatically creates collections. Check ChromaDB logs.

---

## 📚 Related Documentation

- [Ollama Docs](https://github.com/ollama/ollama)
- [ChromaDB Docs](https://docs.trychroma.com)
- [ONNX Runtime](https://onnxruntime.ai)
- [ASP.NET Core Docs](https://learn.microsoft.com/aspnet)

---

## 📄 License

[Add your license here]

---

## 🤝 Contributing

[Add contribution guidelines here]

---

**Built with ❤️ using .NET 10, ChromaDB, Ollama, and Open Source AI**
