# DocAgent - AI-Powered Document Retrieval and Generation System

A sophisticated .NET 10-based REST API application that combines **Retrieval-Augmented Generation (RAG)** with **Large Language Models (LLM)** to enable intelligent document ingestion, searching, and AI-powered question answering.

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
- `Models/model.onnx` - The ONNX runtime model used to convert tokenized text into dense numeric vectors
- `Models/tokenizer.json` - The HuggingFace tokenizer definition that converts raw text into model-ready token IDs and attention masks

**Why the application needs both files:**
- The **tokenizer** is required first because the ONNX model cannot read raw strings directly. It expects structured numeric inputs such as `input_ids`, `attention_mask`, and `token_type_ids`.
- The **ONNX model** is required next because it transforms those tokenized inputs into semantic vector representations called embeddings.
- Without the tokenizer, the application cannot prepare valid model input.
- Without the ONNX model, the application cannot generate embeddings for document chunks or user questions.

**How they work together in DocAgent:**
1. A document chunk or question enters `EmbeddingService`.
2. The HuggingFace tokenizer reads the text and breaks it into tokens/subwords.
3. Those tokens are converted into numeric IDs and masks.
4. The ONNX transformer model runs inference over those IDs.
5. The model produces token-level hidden states.
6. The application performs mean pooling to reduce token-level output into one fixed-size embedding vector.
7. That final vector is stored in ChromaDB for document chunks or used as the query vector for search.

**Why ONNX is used here:**
- ONNX allows local inference inside the .NET application.
- It avoids calling an external embedding API for every chunk and question.
- It gives stable and repeatable embeddings as long as the same model and tokenizer are used.
- It integrates efficiently with `Microsoft.ML.OnnxRuntime` in the current implementation.

**Why HuggingFace tokenizer is used here:**
- Transformer embedding models require the exact tokenizer they were trained with.
- The tokenizer preserves how words, punctuation, and subwords are split for the model.
- Using a mismatched tokenizer would produce token IDs that do not align with the model and would degrade or break retrieval quality.

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

## 🧠 Special Section: How Embeddings, Vector Search, and Context Generation Work

This section explains the full Retrieval-Augmented Generation (RAG) flow used by DocAgent when answering a question through `/api/rag/ask`.

### 1. What is an embedding?
An embedding is a numeric representation of text. Instead of storing meaning as plain words only, the system converts a sentence or document chunk into a floating-point vector such as:

```text
[0.1058, 0.4946, 0.1805, ...]
```

The purpose of the embedding is to place text into a high-dimensional vector space where semantically similar text is closer together than unrelated text.

Examples of semantically related text:
- "What is the score of Nikhila?"
- "How many marks did Nikhila get?"

The wording is different, but the meaning is close, so the embeddings should also be close.

### 2. How a question is turned into an embedding in this application
DocAgent uses `DocAgent.Infrastructure.Services.EmbeddingService` for embedding generation.

The detailed flow is:
1. The incoming question text is validated.
2. The HuggingFace tokenizer converts the raw question into token IDs.
3. Attention masks and token type IDs are prepared.
4. Those arrays are passed into the ONNX transformer model through `Microsoft.ML.OnnxRuntime`.
5. The model returns token-level hidden states with shape similar to `[batch, sequence_length, hidden_size]`.
6. The application performs mean pooling over the valid tokens.
7. The result is one fixed-size embedding vector for the entire question.

The same process is used during document ingestion for every chunk of every uploaded file.

### 3. How document embeddings are generated and stored
During document ingestion:
1. The uploaded PDF or text is read.
2. The extracted text is split into chunks.
3. Each chunk is passed to the same embedding service.
4. Each chunk receives its own embedding vector.
5. The system stores the following in ChromaDB:
   - chunk ID
   - chunk text
   - chunk embedding
   - metadata such as `document_id`

This means a single uploaded document may produce hundreds or thousands of searchable chunk vectors.

### 4. How vector search works in DocAgent
When `/api/rag/ask` or `/api/rag/query` is called:
1. The user question is converted into an embedding.
2. That embedding is sent to `ChromaVectorStore.QueryAsync(...)`.
3. ChromaDB compares the query vector against the stored chunk embeddings.
4. ChromaDB returns the nearest vectors according to vector similarity.
5. The application receives the matching chunk text and metadata.

This is called **semantic retrieval** because the system searches by meaning, not only by exact word overlap.

### 5. How relevant chunks are chosen
ChromaDB uses the query embedding and compares it with all candidate chunk embeddings in the selected collection. The nearest vectors are treated as the most relevant chunks.

With the current implementation, DocAgent can optionally narrow the search to a single uploaded document by using `DocumentFilter`. In that case, the application sends a Chroma metadata filter like:

```json
{ "document_id": "NEET_UG_2026" }
```

That prevents chunks from unrelated uploaded files from participating in retrieval.

### 6. How context is generated for the LLM
After vector retrieval, DocAgent builds context for the language model:
1. The returned chunks are converted into `ContextChunkDto` objects for API output.
2. The raw chunk text is assembled into a prompt context block.
3. Each chunk is prefixed with its document ID.
4. All selected chunks are joined together and inserted into the final LLM prompt.

For `/api/rag/ask`, the logical flow is:
- question → embedding → vector search → context assembly → LLM answer generation

### 7. Why this context generation step is important
The LLM is not expected to answer from memory alone. It is given retrieved evidence first. This makes the answer:
- grounded in uploaded documents
- more explainable
- less likely to hallucinate unrelated content

Without context generation, the endpoint would behave like plain text generation instead of RAG.

### 8. Why retrieval quality depends on consistent embeddings
The same embedding model and tokenizer must be used for:
- document chunk embeddings
- user question embeddings

If ingestion uses one embedding space and questions use another, vector similarity becomes unreliable and ChromaDB may return poor or empty matches.

### 9. Why semi-structured PDFs can still be hard
Rank lists, tables, and scanned PDF-style layouts are harder than natural prose because:
- extracted text can be noisy
- rows may be split across chunks
- names and scores may appear in dense compressed text
- semantic retrieval is strong for meaning, but exact row lookup may still need structured parsing or post-processing

That is why rank-list-style documents often benefit from both:
- embedding-based retrieval for narrowing context
- exact text parsing or record extraction for final precision

---

## 📦 Solution Structure

```
DocAgent Solution (5 Projects)
│
├── 📁 DocAgent.Core
│   ├── Interfaces/
│   │   ├── IDocumentService.cs         ← Core business logic contracts
│   │   ├── ICollectionAdminService.cs  ← Collection/database administration contracts
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
│   │   ├── CollectionAdminDtos.cs      ← Collection/database admin requests & responses
│   │   └── ResponseDtos.cs             ← Other response models
│   │
│   └── Models/
│       ├── Document.cs                 ← Document entity
│       ├── DocumentChunk.cs            ← Text chunk entity
│       ├── Feedback.cs                 ← Feedback entity
│       ├── QueryResult.cs              ← Query result entity
│       └── VectorCollectionInfo.cs     ← Collection metadata model
│
├── 📁 DocAgent.Application
│   └── Services/
│       ├── DocumentService.cs          ← Business logic implementation
│       │                                   (orchestrates: embed, search, ask)
│       └── CollectionAdminService.cs   ← Chroma collection/database management rules
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
│   │   ├── CollectionsController.cs    ← Chroma collection/database administration
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

### **🗂️ Collections Controller** (`/api/collections`)
Manage Chroma collections, collection records, databases, and diagnostics.

| Method | Endpoint | Purpose | Why Needed |
|--------|----------|---------|-----------|
| `GET` | `/api/collections` | List collections | Inspect available Chroma collections |
| `GET` | `/api/collections/count` | Count collections | Quick administrative visibility |
| `GET` | `/api/collections/{name}` | Get collection metadata | Inspect one collection in detail |
| `POST` | `/api/collections` | Create collection | Prepare isolated storage for a new use case |
| `POST` | `/api/collections/get-or-create` | Get or create collection | Idempotent collection setup |
| `PUT` | `/api/collections/{name}` | Update collection metadata or rename | Maintain collection configuration |
| `DELETE` | `/api/collections/{name}` | Delete collection | Remove unused collections |
| `GET` | `/api/collections/{name}/records/count` | Count records in a collection | Validate ingestion volume |
| `POST` | `/api/collections/{name}/records/upsert` | Upsert records | Insert or replace collection records |
| `POST` | `/api/collections/{name}/records/delete-by-filter` | Delete records by metadata filter | Targeted cleanup inside a collection |
| `GET` | `/api/collections/databases/{name}/exists` | Check database existence | Verify Chroma database availability |
| `POST` | `/api/collections/databases` | Create database | Initialize a new Chroma database |
| `GET` | `/api/collections/diagnostics/health` | Get diagnostics/health | Confirm readiness of database and default collections |

**Important guardrail:** protected default collections such as `doc-agent`, `documents`, and `users` are intentionally blocked from destructive rename/delete operations by the application service layer.

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
    "maxTokens": 1024,
    "documentFilter": "NEET_UG_2026"
  }'
```

`documentFilter` is optional. When supplied, retrieval is restricted to chunks whose metadata contains the matching `document_id`, which is useful when multiple uploaded files should be queried independently.

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

**Built with ❤️ using .NET 10, ChromaDB, Ollama, and Open Source AI**
