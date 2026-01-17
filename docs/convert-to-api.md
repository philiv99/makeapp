# MakeApp: PowerShell CLI to C#.NET Web API Conversion Plan

## Executive Summary

This document outlines a comprehensive phased plan for converting the MakeApp PowerShell CLI tool into a C#.NET Web API service layer. The new architecture will expose MakeApp's feature development workflow capabilities through RESTful endpoints, enabling client applications to manage artifacts, orchestrate workflows, and integrate with GitHub Copilot programmatically.

## Current Architecture Analysis

### Existing PowerShell Modules

| Module | Primary Responsibility | Key Functions |
|--------|----------------------|---------------|
| `BranchManager.ps1` | Git branch operations | `Get-AvailableRepos`, `Get-RepoBranches`, `New-FeatureBranch`, `Switch-ToBranch` |
| `CopilotConfig.ps1` | Copilot configuration management | `Test-CopilotInstructions`, `New-CopilotInstructions`, `Test-McpConfig`, `New-McpConfig` |
| `Executor.ps1` | Command orchestration | `Invoke-CopilotCommand`, `Invoke-ShellCommand`, `Start-AgentOrchestration` |
| `FeaturePrompt.ps1` | Feature requirements capture | `Get-FeatureRequirements`, `Import-FeatureFromFile`, `Format-CopilotPrompt` |
| `GitAutomation.ps1` | Git operations | `Add-AllChanges`, `New-FeatureCommit`, `Push-FeatureBranch`, `New-PullRequest` |
| `Sandbox.ps1` | Sandbox environment management | `New-Sandbox`, `Remove-Sandbox`, `Reset-Sandbox`, `Get-SandboxInfo` |

### Current Workflow

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│  Select Repo    │───▶│ Feature Input    │───▶│ Verify Config   │
│  & Branch       │    │ (Interactive/    │    │ (Copilot,MCP)   │
│                 │    │  File-based)     │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                                        │
                                                        ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│  Create PR      │◀───│ Git Operations   │◀───│ Agent           │
│  (optional)     │    │ (stage/commit/   │    │ Orchestration   │
│                 │    │  push)           │    │ Loop            │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

---

## Target Architecture

### Technology Stack

| Component | Technology | Rationale |
|-----------|------------|-----------|
| **Framework** | .NET 8.0 | LTS support, performance, native AOT compilation |
| **API Style** | RESTful with OpenAPI | Industry standard, excellent tooling |
| **Copilot Integration** | [GitHub.Copilot.SDK](https://github.com/github/copilot-sdk) | Official SDK for programmatic Copilot control |
| **Agentic Memory** | [Copilot Memory](https://docs.github.com/en/copilot/concepts/agents/copilot-memory) | Cross-workflow learning with citation validation |
| **Git Operations** | LibGit2Sharp | Native .NET Git library |
| **GitHub API** | Octokit.NET | Official GitHub API client |
| **Background Jobs** | Hangfire or .NET Channels | Long-running workflow orchestration, memory maintenance |
| **Configuration** | Options Pattern + JSON | Flexible, environment-aware config |
| **Logging** | Serilog | Structured logging with multiple sinks |
| **Authentication** | JWT Bearer + GitHub OAuth | Secure API access |

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CLIENT APPLICATIONS                             │
│  (Web Dashboard, CLI Client, VS Code Extension, CI/CD Integrations)         │
└────────────────────────────────────────┬────────────────────────────────────┘
                                         │ HTTPS/REST
                                         ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           API GATEWAY / LOAD BALANCER                        │
└────────────────────────────────────────┬────────────────────────────────────┘
                                         │
                                         ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          MAKEAPP WEB API SERVICE                            │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                         CONTROLLER LAYER                             │   │
│  │  ┌────────────┐ ┌──────────────┐ ┌──────────┐ ┌──────────────────┐  │   │
│  │  │ Repos      │ │ Features     │ │ Branches │ │ Workflows        │  │   │
│  │  │ Controller │ │ Controller   │ │ Ctrl     │ │ Controller       │  │   │
│  │  └────────────┘ └──────────────┘ └──────────┘ └──────────────────┘  │   │
│  │  ┌────────────┐ ┌──────────────┐ ┌──────────┐ ┌──────────────────┐  │   │
│  │  │ Config     │ │ Sandbox      │ │ Sessions │ │ Health           │  │   │
│  │  │ Controller │ │ Controller   │ │ Ctrl     │ │ Controller       │  │   │
│  │  └────────────┘ └──────────────┘ └──────────┘ └──────────────────┘  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                         SERVICE LAYER                                │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                 │   │
│  │  │ Repository   │ │ Branch       │ │ Feature      │                 │   │
│  │  │ Service      │ │ Service      │ │ Service      │                 │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘                 │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                 │   │
│  │  │ Copilot      │ │ Orchestration│ │ Git          │                 │   │
│  │  │ Service      │ │ Service      │ │ Service      │                 │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘                 │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                 │   │
│  │  │ Sandbox      │ │ Config       │ │ Notification │                 │   │
│  │  │ Service      │ │ Service      │ │ Service      │                 │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘                 │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                      INFRASTRUCTURE LAYER                            │   │
│  │  ┌────────────────────────────────────────────────────────────────┐ │   │
│  │  │              GITHUB COPILOT SDK INTEGRATION                     │ │   │
│  │  │  ┌──────────────┐  ┌──────────────┐  ┌───────────────────────┐ │ │   │
│  │  │  │ CopilotClient│  │CopilotSession│  │ Event Handler         │ │ │   │
│  │  │  │ (Singleton)  │  │ Pool         │  │ (Streaming Support)   │ │ │   │
│  │  │  └──────────────┘  └──────────────┘  └───────────────────────┘ │ │   │
│  │  └────────────────────────────────────────────────────────────────┘ │   │
│  │  ┌───────────────┐ ┌───────────────┐ ┌───────────────────────────┐ │   │
│  │  │ LibGit2Sharp  │ │ Octokit.NET   │ │ File System Abstraction  │ │   │
│  │  │ (Git Ops)     │ │ (GitHub API)  │ │ (Testability)            │ │   │
│  │  └───────────────┘ └───────────────┘ └───────────────────────────┘ │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
                                         │
                   ┌─────────────────────┼─────────────────────┐
                   │                     │                     │
                   ▼                     ▼                     ▼
          ┌───────────────┐    ┌───────────────┐    ┌───────────────┐
          │  Git          │    │  GitHub       │    │  Copilot CLI  │
          │  Repositories │    │  API          │    │  (via SDK)    │
          └───────────────┘    └───────────────┘    └───────────────┘
```

---

## API Endpoint Design

### Repository Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/repos` | List available repositories |
| GET | `/api/v1/repos/{owner}/{name}` | Get repository details |
| POST | `/api/v1/repos/scan` | Scan a folder for repositories |
| GET | `/api/v1/repos/{owner}/{name}/config` | Get repository configuration status |
| PUT | `/api/v1/repos/{owner}/{name}/config/copilot-instructions` | Update Copilot instructions |
| PUT | `/api/v1/repos/{owner}/{name}/config/mcp` | Update MCP configuration |

### Branch Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/repos/{owner}/{name}/branches` | List branches |
| GET | `/api/v1/repos/{owner}/{name}/branches/{branch}` | Get branch details |
| POST | `/api/v1/repos/{owner}/{name}/branches` | Create feature branch |
| DELETE | `/api/v1/repos/{owner}/{name}/branches/{branch}` | Delete branch |
| POST | `/api/v1/repos/{owner}/{name}/branches/{branch}/checkout` | Switch to branch |

### Feature Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/features` | List all features |
| GET | `/api/v1/features/{id}` | Get feature details |
| POST | `/api/v1/features` | Create new feature |
| PUT | `/api/v1/features/{id}` | Update feature |
| DELETE | `/api/v1/features/{id}` | Delete feature |
| POST | `/api/v1/features/import` | Import feature from file/markdown |
| GET | `/api/v1/features/{id}/prompt` | Get formatted Copilot prompt |

### Workflow Orchestration

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/workflows` | Start new workflow |
| GET | `/api/v1/workflows/{id}` | Get workflow status |
| GET | `/api/v1/workflows/{id}/events` | Get workflow events (SSE) |
| POST | `/api/v1/workflows/{id}/abort` | Abort workflow |
| POST | `/api/v1/workflows/{id}/retry` | Retry failed step |
| POST | `/api/v1/workflows/{id}/skip` | Skip current step |
| GET | `/api/v1/workflows/{id}/plan` | Get implementation plan |

### Git Operations

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/repos/{owner}/{name}/status` | Get git status |
| POST | `/api/v1/repos/{owner}/{name}/stage` | Stage changes |
| POST | `/api/v1/repos/{owner}/{name}/commit` | Create commit |
| POST | `/api/v1/repos/{owner}/{name}/push` | Push to remote |
| POST | `/api/v1/repos/{owner}/{name}/pull-requests` | Create pull request |

### Copilot Sessions

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/copilot/sessions` | Create Copilot session |
| GET | `/api/v1/copilot/sessions` | List active sessions |
| GET | `/api/v1/copilot/sessions/{id}` | Get session details |
| DELETE | `/api/v1/copilot/sessions/{id}` | Close session |
| POST | `/api/v1/copilot/sessions/{id}/messages` | Send message |
| GET | `/api/v1/copilot/sessions/{id}/messages` | Get messages |
| GET | `/api/v1/copilot/sessions/{id}/stream` | Stream responses (SSE) |

### Sandbox Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/sandbox` | Create sandbox |
| GET | `/api/v1/sandbox` | Get sandbox status |
| DELETE | `/api/v1/sandbox` | Delete sandbox |
| POST | `/api/v1/sandbox/reset` | Reset sandbox |

### Configuration

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/config` | Get current configuration |
| PUT | `/api/v1/config` | Update configuration |
| GET | `/api/v1/config/defaults` | Get default configuration |

---

## GitHub Copilot SDK Integration

### SDK Overview

The [GitHub Copilot SDK for .NET](https://github.com/github/copilot-sdk/blob/main/dotnet/README.md) provides programmatic control over GitHub Copilot CLI:

```csharp
// Installation
// dotnet add package GitHub.Copilot.SDK

using GitHub.Copilot.SDK;

// Create and start client
await using var client = new CopilotClient();
await client.StartAsync();

// Create a session
await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "gpt-5",
    Streaming = true
});

// Handle events
session.On(evt =>
{
    switch (evt)
    {
        case AssistantMessageDeltaEvent delta:
            // Stream response chunks
            break;
        case AssistantMessageEvent msg:
            // Final response
            break;
        case SessionIdleEvent:
            // Session finished
            break;
    }
});

// Send a message
await session.SendAsync(new MessageOptions { Prompt = "Implement the feature" });
```

### Key Integration Points

1. **CopilotClient as Singleton**: Manage a single CopilotClient instance for the application lifecycle
2. **Session Pooling**: Reuse sessions where appropriate for performance
3. **Streaming Support**: Use SSE (Server-Sent Events) to stream responses to clients
4. **Custom Tools**: Register custom tools for domain-specific operations
5. **File Attachments**: Attach repository files to provide context

### Custom Tool Registration

```csharp
var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "gpt-5",
    Tools = [
        AIFunctionFactory.Create(
            async ([Description("Repository path")] string repoPath) => {
                return await _gitService.GetStatusAsync(repoPath);
            },
            "get_repo_status",
            "Get the current git status of a repository"),
        
        AIFunctionFactory.Create(
            async ([Description("File path to read")] string filePath) => {
                return await File.ReadAllTextAsync(filePath);
            },
            "read_file",
            "Read contents of a file in the repository"),
    ]
});
```

---

## GitHub Copilot Agentic Memory Integration

### Overview

[Copilot's Agentic Memory](https://docs.github.com/en/copilot/concepts/agents/copilot-memory) is a cross-agent memory system that enables Copilot agents to learn and improve across your development workflow. This is a **critical feature** for MakeApp as it allows the system to build cumulative knowledge about repositories, coding conventions, and patterns over time.

> **Key Insight**: Memory allows Copilot to become increasingly effective over time without requiring users to repeatedly provide the same context or maintain detailed custom instruction files.

### How Memory Works

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        COPILOT AGENTIC MEMORY FLOW                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────────┐         ┌──────────────────┐                         │
│  │  MakeApp API     │         │  Memory Store    │                         │
│  │  Workflow        │◀───────▶│  (Per Repository)│                         │
│  └────────┬─────────┘         └────────┬─────────┘                         │
│           │                            │                                    │
│           │ 1. Start Task              │                                    │
│           ▼                            │                                    │
│  ┌──────────────────┐                  │                                    │
│  │ Retrieve Recent  │◀─────────────────┘                                    │
│  │ Memories         │  2. Fetch memories with citations                     │
│  └────────┬─────────┘                                                       │
│           │                                                                  │
│           │ 3. Include in prompt                                            │
│           ▼                                                                  │
│  ┌──────────────────┐                                                       │
│  │ Copilot Session  │                                                       │
│  │ (with context)   │                                                       │
│  └────────┬─────────┘                                                       │
│           │                                                                  │
│           │ 4. Verify citations against current codebase                    │
│           ▼                                                                  │
│  ┌──────────────────┐     ┌──────────────────┐                             │
│  │ Execute Task     │────▶│ Store New Memory │ 5. Learn from task          │
│  │ (if valid)       │     │ (with citations) │                              │
│  └──────────────────┘     └──────────────────┘                             │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Memory Structure

Each memory stored by Copilot includes:

```json
{
  "subject": "API version synchronization",
  "fact": "API version must match between client SDK, server routes, and documentation.",
  "citations": [
    "src/client/sdk/constants.ts:12",
    "server/routes/api.go:8",
    "docs/api-reference.md:37"
  ],
  "reason": "If the API version is not kept properly synchronized, the integration can fail or exhibit subtle bugs.",
  "createdAt": "2026-01-15T10:30:00Z",
  "validatedAt": "2026-01-17T14:22:00Z"
}
```

### Memory Characteristics

| Characteristic | Description |
|---------------|-------------|
| **Repository-Scoped** | Memories are tied to a specific repository, not user-scoped |
| **Citation-Based** | Each memory includes references to specific code locations |
| **Just-in-Time Verified** | Citations are validated against current codebase before use |
| **Self-Healing** | Invalid memories are automatically corrected or expired |
| **28-Day TTL** | Memories auto-expire after 28 days unless refreshed by use |
| **Cross-Agent** | Memories created by one agent can be used by another |

### MakeApp Memory Integration Points

#### 1. Memory-Aware Workflow Orchestration

When MakeApp starts a workflow, it should leverage repository memories:

```csharp
// MakeApp.Application/Services/MemoryAwareOrchestrationService.cs
public class MemoryAwareOrchestrationService : IOrchestrationService
{
    private readonly ICopilotService _copilotService;
    private readonly IMemoryService _memoryService;
    
    public async Task<Workflow> StartWorkflowAsync(StartWorkflowDto dto)
    {
        // 1. Retrieve relevant memories for the repository
        var memories = await _memoryService.GetRepositoryMemoriesAsync(
            dto.RepositoryPath, 
            dto.FeatureContext);
        
        // 2. Create session with memory context
        var sessionConfig = new SessionConfig
        {
            Model = "gpt-5",
            Streaming = true,
            SystemMessage = new SystemMessageConfig
            {
                Mode = SystemMessageMode.Append,
                Content = FormatMemoriesAsContext(memories)
            }
        };
        
        // 3. Register memory storage tool
        sessionConfig.Tools = [
            .. GetStandardTools(),
            CreateMemoryStorageTool(dto.RepositoryPath)
        ];
        
        var session = await _copilotService.CreateSessionAsync(sessionConfig);
        
        // Continue with workflow...
    }
    
    private AIFunction CreateMemoryStorageTool(string repoPath)
    {
        return AIFunctionFactory.Create(
            async (
                [Description("Subject of the memory")] string subject,
                [Description("The fact or convention learned")] string fact,
                [Description("File paths and line numbers that support this fact")] string[] citations,
                [Description("Why this is worth remembering")] string reason) =>
            {
                await _memoryService.StoreMemoryAsync(new Memory
                {
                    RepositoryPath = repoPath,
                    Subject = subject,
                    Fact = fact,
                    Citations = citations.ToList(),
                    Reason = reason,
                    CreatedAt = DateTime.UtcNow
                });
                return "Memory stored successfully";
            },
            "store_memory",
            "Store a learning about the codebase for future reference. " +
            "Use this when discovering conventions, patterns, or important " +
            "relationships between files that should be remembered.");
    }
}
```

#### 2. Memory Service Implementation

```csharp
// MakeApp.Core/Entities/Memory.cs
public class Memory
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RepositoryPath { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Fact { get; set; } = "";
    public List<string> Citations { get; set; } = new();
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public DateTime ExpiresAt => CreatedAt.AddDays(28);
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}

// MakeApp.Application/Services/MemoryService.cs
public interface IMemoryService
{
    Task<IEnumerable<Memory>> GetRepositoryMemoriesAsync(string repoPath, string? contextFilter = null);
    Task<Memory> StoreMemoryAsync(Memory memory);
    Task<MemoryValidationResult> ValidateMemoryAsync(Memory memory);
    Task<bool> RefreshMemoryAsync(string memoryId);
    Task<bool> DeleteMemoryAsync(string memoryId);
    Task<IEnumerable<Memory>> GetMemoriesByCitationAsync(string filePath);
    Task PruneExpiredMemoriesAsync();
}

public class MemoryService : IMemoryService
{
    private readonly IMemoryRepository _repository;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<MemoryService> _logger;
    
    public async Task<MemoryValidationResult> ValidateMemoryAsync(Memory memory)
    {
        var result = new MemoryValidationResult { MemoryId = memory.Id };
        
        foreach (var citation in memory.Citations)
        {
            var (filePath, lineNumber) = ParseCitation(citation);
            
            if (!_fileSystem.File.Exists(filePath))
            {
                result.InvalidCitations.Add(citation);
                result.Issues.Add($"File not found: {filePath}");
                continue;
            }
            
            // Read file and verify the citation still makes sense
            var lines = await _fileSystem.File.ReadAllLinesAsync(filePath);
            if (lineNumber > lines.Length)
            {
                result.InvalidCitations.Add(citation);
                result.Issues.Add($"Line {lineNumber} exceeds file length in {filePath}");
                continue;
            }
            
            result.ValidCitations.Add(citation);
        }
        
        result.IsValid = result.InvalidCitations.Count == 0;
        result.Confidence = (double)result.ValidCitations.Count / memory.Citations.Count;
        
        return result;
    }
    
    public async Task<IEnumerable<Memory>> GetRepositoryMemoriesAsync(
        string repoPath, 
        string? contextFilter = null)
    {
        var memories = await _repository.GetByRepositoryAsync(repoPath);
        
        // Filter out expired memories
        memories = memories.Where(m => !m.IsExpired);
        
        // Optionally filter by context relevance
        if (!string.IsNullOrEmpty(contextFilter))
        {
            memories = memories.Where(m => 
                m.Subject.Contains(contextFilter, StringComparison.OrdinalIgnoreCase) ||
                m.Fact.Contains(contextFilter, StringComparison.OrdinalIgnoreCase) ||
                m.Citations.Any(c => c.Contains(contextFilter, StringComparison.OrdinalIgnoreCase)));
        }
        
        // Return most recent first
        return memories.OrderByDescending(m => m.ValidatedAt ?? m.CreatedAt);
    }
}
```

#### 3. Memory API Endpoints

Add new endpoints for memory management:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/repos/{owner}/{name}/memories` | List repository memories |
| GET | `/api/v1/repos/{owner}/{name}/memories/{id}` | Get specific memory |
| POST | `/api/v1/repos/{owner}/{name}/memories` | Manually create memory |
| DELETE | `/api/v1/repos/{owner}/{name}/memories/{id}` | Delete memory |
| POST | `/api/v1/repos/{owner}/{name}/memories/{id}/validate` | Validate memory citations |
| POST | `/api/v1/repos/{owner}/{name}/memories/{id}/refresh` | Refresh memory timestamp |
| GET | `/api/v1/repos/{owner}/{name}/memories/by-file` | Get memories citing a file |

```csharp
// MakeApp.Api/Controllers/MemoriesController.cs
[ApiController]
[Route("api/v1/repos/{owner}/{name}/memories")]
public class MemoriesController : ControllerBase
{
    private readonly IMemoryService _memoryService;
    private readonly IRepositoryService _repositoryService;
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemoryDto>>> GetMemories(
        string owner, 
        string name,
        [FromQuery] string? filter = null,
        [FromQuery] bool includeExpired = false)
    {
        var repoPath = await _repositoryService.GetRepositoryPathAsync(owner, name);
        var memories = await _memoryService.GetRepositoryMemoriesAsync(repoPath, filter);
        
        if (!includeExpired)
        {
            memories = memories.Where(m => !m.IsExpired);
        }
        
        return Ok(memories.Select(m => m.ToDto()));
    }
    
    [HttpPost("{id}/validate")]
    public async Task<ActionResult<MemoryValidationResultDto>> ValidateMemory(
        string owner, 
        string name, 
        string id)
    {
        var memory = await _memoryService.GetMemoryAsync(id);
        if (memory == null) return NotFound();
        
        var result = await _memoryService.ValidateMemoryAsync(memory);
        
        // If valid, refresh the timestamp
        if (result.IsValid)
        {
            await _memoryService.RefreshMemoryAsync(id);
        }
        
        return Ok(result.ToDto());
    }
    
    [HttpGet("by-file")]
    public async Task<ActionResult<IEnumerable<MemoryDto>>> GetMemoriesByFile(
        string owner, 
        string name,
        [FromQuery] string filePath)
    {
        var memories = await _memoryService.GetMemoriesByCitationAsync(filePath);
        return Ok(memories.Select(m => m.ToDto()));
    }
}
```

#### 4. Cross-Agent Memory Scenarios for MakeApp

MakeApp can leverage memory across different workflow stages:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    MAKEAPP CROSS-WORKFLOW MEMORY BENEFITS                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  SCENARIO 1: Learning from Code Review                                      │
│  ───────────────────────────────────────                                    │
│  • Copilot Code Review discovers: "Tests must be in __tests__ folder"       │
│  • Memory stored with citation to test file structure                       │
│  • Next MakeApp workflow automatically places new tests correctly           │
│                                                                              │
│  SCENARIO 2: Learning from Feature Implementation                           │
│  ───────────────────────────────────────────────────                        │
│  • MakeApp implements feature, discovers logging pattern                    │
│  • Memory: "Use Winston logger with format: timestamp, errorCode, userId"   │
│  • Future features automatically use correct logging format                 │
│                                                                              │
│  SCENARIO 3: Learning from Bug Fixes                                        │
│  ────────────────────────────────────                                       │
│  • MakeApp fixes bug, learns files must stay synchronized                   │
│  • Memory: "config.ts and config.json must have matching values"            │
│  • Future changes to either file trigger update to both                     │
│                                                                              │
│  SCENARIO 4: Learning from PR Feedback                                      │
│  ─────────────────────────────────────                                      │
│  • Developer provides feedback on MakeApp-generated PR                      │
│  • Memory: "This project prefers async/await over .then() chains"           │
│  • Future code generation follows async/await pattern                       │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### 5. Memory-Enhanced Prompt Engineering

Incorporate memories into the system prompt:

```csharp
// MakeApp.Application/Services/MemoryAwarePromptFormatter.cs
public class MemoryAwarePromptFormatter : IPromptFormatterService
{
    private readonly IMemoryService _memoryService;
    
    public async Task<string> FormatFeaturePromptWithMemoriesAsync(
        Feature feature, 
        string repoPath,
        PromptStyle style)
    {
        var basePrompt = FormatFeaturePrompt(feature, style);
        var memories = await _memoryService.GetRepositoryMemoriesAsync(repoPath);
        
        if (!memories.Any())
        {
            return basePrompt;
        }
        
        var sb = new StringBuilder();
        sb.AppendLine(basePrompt);
        sb.AppendLine();
        sb.AppendLine("## Repository Knowledge (from previous work)");
        sb.AppendLine();
        sb.AppendLine("The following conventions and patterns have been learned from previous work in this repository. ");
        sb.AppendLine("**Verify each against the current codebase before applying.**");
        sb.AppendLine();
        
        foreach (var memory in memories.Take(10)) // Limit to most relevant
        {
            sb.AppendLine($"### {memory.Subject}");
            sb.AppendLine($"- **Fact**: {memory.Fact}");
            sb.AppendLine($"- **Evidence**: {string.Join(", ", memory.Citations)}");
            sb.AppendLine($"- **Reason**: {memory.Reason}");
            sb.AppendLine();
        }
        
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine("If you discover new conventions or patterns during this task, use the `store_memory` tool to record them for future reference.");
        
        return sb.ToString();
    }
}
```

#### 6. Memory Verification During Workflow

```csharp
// MakeApp.Application/Services/MemoryVerificationService.cs
public class MemoryVerificationService
{
    private readonly IMemoryService _memoryService;
    private readonly IGitService _gitService;
    
    /// <summary>
    /// Verifies all memories before a workflow starts and handles stale memories
    /// </summary>
    public async Task<MemoryVerificationReport> VerifyAndPrepareMemoriesAsync(string repoPath)
    {
        var report = new MemoryVerificationReport();
        var memories = await _memoryService.GetRepositoryMemoriesAsync(repoPath);
        
        foreach (var memory in memories)
        {
            var validation = await _memoryService.ValidateMemoryAsync(memory);
            
            if (validation.IsValid)
            {
                report.ValidMemories.Add(memory);
                await _memoryService.RefreshMemoryAsync(memory.Id);
            }
            else if (validation.Confidence > 0.5)
            {
                // Partially valid - include with warning
                report.PartialMemories.Add((memory, validation));
            }
            else
            {
                // Invalid - mark for potential deletion
                report.InvalidMemories.Add((memory, validation));
            }
        }
        
        return report;
    }
}
```

### Enabling Memory in MakeApp Workflows

For users to benefit from Copilot Memory with MakeApp:

1. **Enable Memory in GitHub Settings**
   - Enterprise/Organization: Admin enables in Copilot settings
   - Individual: Enable in personal Copilot settings on GitHub
   
2. **MakeApp Configuration**
   ```json
   {
     "copilot": {
       "memory": {
         "enabled": true,
         "maxMemoriesPerPrompt": 10,
         "verifyBeforeUse": true,
         "autoStoreDiscoveries": true
       }
     }
   }
   ```

3. **API Request with Memory**
   ```http
   POST /api/v1/workflows
   Content-Type: application/json
   
   {
     "featureId": "abc123",
     "repositoryPath": "/repos/myproject",
     "options": {
       "useMemory": true,
       "storeNewMemories": true
     }
   }
   ```

### Benefits for MakeApp Users

| Benefit | Description |
|---------|-------------|
| **Reduced Context Setup** | No need to repeatedly explain conventions in prompts |
| **Automatic Learning** | MakeApp learns from each workflow execution |
| **Consistency Enforcement** | Patterns discovered are automatically applied |
| **Knowledge Transfer** | Team conventions spread through shared memories |
| **Self-Improving** | Quality increases over time as memory pool grows |
| **Cross-Workflow Intelligence** | Code review findings improve code generation |

### Security & Privacy Considerations

1. **Repository Isolation**: Memories are strictly scoped to their repository
2. **Write Permission Required**: Only contributors with write access can create memories
3. **Read Permission Required**: Only users with read access can use memories
4. **No Cross-Repository Leakage**: Memories cannot be accessed from other repositories
5. **Audit Trail**: All memory operations can be logged for compliance

---

## Phased Implementation Plan

### Phase 1: Foundation (Weeks 1-2)

**Objective**: Establish the core project structure, configuration system, and basic API scaffolding.

#### 1.1 Project Setup

- [ ] Create new .NET 8 Web API solution structure:
  ```
  MakeApp.Api/
  ├── MakeApp.Api.sln
  ├── src/
  │   ├── MakeApp.Api/                    # Main API project
  │   │   ├── Controllers/
  │   │   ├── Program.cs
  │   │   └── appsettings.json
  │   ├── MakeApp.Core/                   # Core domain models
  │   │   ├── Entities/
  │   │   ├── Interfaces/
  │   │   └── Enums/
  │   ├── MakeApp.Application/            # Application services
  │   │   ├── Services/
  │   │   ├── DTOs/
  │   │   └── Mappings/
  │   └── MakeApp.Infrastructure/         # External integrations
  │       ├── Git/
  │       ├── GitHub/
  │       ├── Copilot/
  │       └── FileSystem/
  └── tests/
      ├── MakeApp.Api.Tests/
      ├── MakeApp.Application.Tests/
      └── MakeApp.Infrastructure.Tests/
  ```

- [ ] Set up dependency injection container
- [ ] Configure Serilog for structured logging
- [ ] Set up Options pattern for configuration

#### 1.2 Configuration System

**PowerShell Equivalent**: `Get-MakeAppConfig`, `Set-MakeAppConfig`, `Merge-Hashtable`

```csharp
// MakeApp.Core/Configuration/MakeAppOptions.cs
public class MakeAppOptions
{
    public FolderOptions Folders { get; set; } = new();
    public GitHubOptions GitHub { get; set; } = new();
    public LlmOptions Llm { get; set; } = new();
    public AgentOptions Agent { get; set; } = new();
    public GitOptions Git { get; set; } = new();
    public UiOptions Ui { get; set; } = new();
    public TimeoutOptions Timeouts { get; set; } = new();
    public LimitOptions Limits { get; set; } = new();
}

public class FolderOptions
{
    public string Repos { get; set; } = "";
    public string Workspace { get; set; } = "";
    public string Temp { get; set; } = "";
    public string Logs { get; set; } = "";
    public string Cache { get; set; } = "";
}

// etc...
```

- [ ] Define configuration DTOs mirroring defaults.json structure
- [ ] Implement configuration service with layered loading:
  - defaults.json → user config → environment variables → API parameters
- [ ] Add configuration validation
- [ ] Create configuration endpoints

#### 1.3 Health & Diagnostics

- [ ] Implement health check endpoints
- [ ] Add OpenAPI/Swagger documentation
- [ ] Set up API versioning

**Deliverables**:
- Working API with `/health`, `/api/v1/config` endpoints
- OpenAPI documentation at `/swagger`
- Comprehensive configuration system

---

### Phase 2: Repository & Branch Services (Weeks 3-4)

**Objective**: Implement repository scanning, branch management, and Git operations.

#### 2.1 Repository Service

**PowerShell Equivalent**: `Get-AvailableRepos` from `BranchManager.ps1`

```csharp
// MakeApp.Application/Services/RepositoryService.cs
public interface IRepositoryService
{
    Task<IEnumerable<RepositoryInfo>> GetAvailableRepositoriesAsync(string? reposPath = null);
    Task<RepositoryInfo?> GetRepositoryInfoAsync(string owner, string name);
    Task<RepositoryConfigStatus> GetConfigurationStatusAsync(string repoPath);
    Task<bool> ValidateRepositoryAsync(string repoPath);
}

public class RepositoryService : IRepositoryService
{
    private readonly MakeAppOptions _options;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<RepositoryService> _logger;

    public async Task<IEnumerable<RepositoryInfo>> GetAvailableRepositoriesAsync(string? reposPath = null)
    {
        var path = reposPath ?? _options.Folders.Repos;
        var directories = _fileSystem.Directory.GetDirectories(path);
        
        var repos = new List<RepositoryInfo>();
        foreach (var dir in directories)
        {
            if (_fileSystem.Directory.Exists(Path.Combine(dir, ".git")))
            {
                repos.Add(new RepositoryInfo
                {
                    Name = Path.GetFileName(dir),
                    Path = dir,
                    LastModified = _fileSystem.Directory.GetLastWriteTime(dir)
                });
            }
        }
        return repos;
    }
}
```

- [ ] Implement `IRepositoryService` interface
- [ ] Add LibGit2Sharp integration for Git operations
- [ ] Create repository controller with endpoints
- [ ] Add unit tests with mocked file system

#### 2.2 Branch Service

**PowerShell Equivalent**: `Get-RepoBranches`, `New-FeatureBranch`, `Switch-ToBranch`

```csharp
// MakeApp.Application/Services/BranchService.cs
public interface IBranchService
{
    Task<IEnumerable<BranchInfo>> GetBranchesAsync(string repoPath, bool includeRemote = false);
    Task<BranchInfo> CreateFeatureBranchAsync(string repoPath, string branchName, string baseBranch = "main");
    Task<bool> SwitchToBranchAsync(string repoPath, string branchName);
    Task<string> GetCurrentBranchAsync(string repoPath);
    Task<bool> DeleteBranchAsync(string repoPath, string branchName);
}
```

- [ ] Implement branch operations using LibGit2Sharp
- [ ] Add branch naming convention validation
- [ ] Create branch controller with endpoints
- [ ] Handle remote tracking branches

#### 2.3 Git Automation Service

**PowerShell Equivalent**: `Add-AllChanges`, `New-FeatureCommit`, `Push-FeatureBranch`

```csharp
// MakeApp.Application/Services/GitService.cs
public interface IGitService
{
    Task<GitStatus> GetStatusAsync(string repoPath);
    Task<bool> StageChangesAsync(string repoPath, string pathSpec = ".");
    Task<CommitResult> CommitAsync(string repoPath, string message, CommitOptions? options = null);
    Task<PushResult> PushAsync(string repoPath, PushOptions? options = null);
    Task<PullRequestResult> CreatePullRequestAsync(string repoPath, PullRequestOptions options);
}
```

- [ ] Implement Git status, stage, commit, push operations
- [ ] Integrate Octokit.NET for pull request creation
- [ ] Add commit message formatting (conventional commits support)
- [ ] Implement signed commit support

**Deliverables**:
- Working repository, branch, and git operation endpoints
- Integration with LibGit2Sharp and Octokit.NET
- Comprehensive unit tests

---

### Phase 3: Feature Management (Weeks 5-6)

**Objective**: Implement feature requirements capture, storage, and formatting.

#### 3.1 Feature Service

**PowerShell Equivalent**: `Get-FeatureRequirements`, `Import-FeatureFromFile`, `Save-FeatureInstructions`

```csharp
// MakeApp.Core/Entities/Feature.cs
public class Feature
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> AcceptanceCriteria { get; set; } = new();
    public List<string> TechnicalNotes { get; set; } = new();
    public List<string> AffectedAreas { get; set; } = new();
    public FeaturePriority Priority { get; set; } = FeaturePriority.Medium;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public FeatureStatus Status { get; set; } = FeatureStatus.Draft;
}

// MakeApp.Application/Services/FeatureService.cs
public interface IFeatureService
{
    Task<Feature> CreateFeatureAsync(CreateFeatureDto dto);
    Task<Feature?> GetFeatureAsync(string id);
    Task<IEnumerable<Feature>> GetAllFeaturesAsync();
    Task<Feature> UpdateFeatureAsync(string id, UpdateFeatureDto dto);
    Task<bool> DeleteFeatureAsync(string id);
    Task<Feature> ImportFromFileAsync(Stream fileStream, string fileName);
    Task<Feature> ImportFromMarkdownAsync(string markdown);
    Task<string> FormatAsPromptAsync(string featureId, PromptStyle style = PromptStyle.Structured);
}
```

- [ ] Define Feature entity and DTOs
- [ ] Implement feature storage (file-based or database)
- [ ] Add Markdown parser for feature import
- [ ] Implement JSON import/export

#### 3.2 Prompt Formatter Service

**PowerShell Equivalent**: `Format-CopilotPrompt` from `FeaturePrompt.ps1`

```csharp
// MakeApp.Application/Services/PromptFormatterService.cs
public interface IPromptFormatterService
{
    string FormatFeaturePrompt(Feature feature, PromptStyle style);
    string FormatImplementationPlanPrompt(Feature feature);
    string FormatValidationPrompt(Feature feature);
}

public class PromptFormatterService : IPromptFormatterService
{
    public string FormatFeaturePrompt(Feature feature, PromptStyle style)
    {
        return style switch
        {
            PromptStyle.Structured => FormatStructuredPrompt(feature),
            PromptStyle.Conversational => FormatConversationalPrompt(feature),
            PromptStyle.Minimal => FormatMinimalPrompt(feature),
            _ => FormatStructuredPrompt(feature)
        };
    }

    private string FormatStructuredPrompt(Feature feature)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"## Feature: {feature.Title}");
        sb.AppendLine();
        sb.AppendLine("### Description");
        sb.AppendLine(feature.Description);
        // ... etc
        return sb.ToString();
    }
}
```

- [ ] Implement multiple prompt formatting styles
- [ ] Add template customization support
- [ ] Create prompt preview endpoint

**Deliverables**:
- Feature CRUD endpoints
- Markdown/JSON import support
- Prompt formatting service

---

### Phase 4: Copilot SDK Integration (Weeks 7-9)

**Objective**: Integrate the GitHub Copilot SDK for AI-powered code generation.

#### 4.1 Copilot Client Manager

```csharp
// MakeApp.Infrastructure/Copilot/CopilotClientManager.cs
public interface ICopilotClientManager
{
    Task<CopilotClient> GetClientAsync();
    Task<CopilotSession> CreateSessionAsync(SessionConfig? config = null);
    Task<CopilotSession> GetOrCreateSessionAsync(string sessionId);
    Task CloseSessionAsync(string sessionId);
    Task<IReadOnlyList<SessionMetadata>> ListSessionsAsync();
}

public class CopilotClientManager : ICopilotClientManager, IAsyncDisposable
{
    private readonly CopilotClient _client;
    private readonly ConcurrentDictionary<string, CopilotSession> _sessions = new();
    private readonly MakeAppOptions _options;
    private readonly ILogger<CopilotClientManager> _logger;

    public CopilotClientManager(IOptions<MakeAppOptions> options, ILogger<CopilotClientManager> logger)
    {
        _options = options.Value;
        _logger = logger;
        _client = new CopilotClient(new CopilotClientOptions
        {
            AutoStart = true,
            AutoRestart = true,
            LogLevel = "info"
        });
    }

    public async Task<CopilotClient> GetClientAsync()
    {
        if (_client.State != ConnectionState.Connected)
        {
            await _client.StartAsync();
        }
        return _client;
    }

    public async Task<CopilotSession> CreateSessionAsync(SessionConfig? config = null)
    {
        var client = await GetClientAsync();
        var session = await client.CreateSessionAsync(config ?? new SessionConfig
        {
            Model = _options.Llm.Providers["copilot"].Model,
            Streaming = true
        });
        _sessions[session.SessionId] = session;
        return session;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var session in _sessions.Values)
        {
            await session.DisposeAsync();
        }
        await _client.StopAsync();
    }
}
```

- [ ] Implement CopilotClient singleton management
- [ ] Add session pooling for performance
- [ ] Configure custom tools for domain operations
- [ ] Handle connection lifecycle

#### 4.2 Copilot Service

**PowerShell Equivalent**: `Invoke-CopilotCommand`

```csharp
// MakeApp.Application/Services/CopilotService.cs
public interface ICopilotService
{
    Task<CopilotResponse> SendMessageAsync(string sessionId, string prompt, MessageOptions? options = null);
    IAsyncEnumerable<CopilotStreamEvent> StreamMessageAsync(string sessionId, string prompt, CancellationToken cancellationToken = default);
    Task<string> CreateSessionAsync(CreateSessionDto dto);
    Task<SessionInfo> GetSessionInfoAsync(string sessionId);
    Task<IEnumerable<SessionEvent>> GetSessionMessagesAsync(string sessionId);
    Task AbortSessionAsync(string sessionId);
    Task CloseSessionAsync(string sessionId);
}

public class CopilotService : ICopilotService
{
    private readonly ICopilotClientManager _clientManager;
    private readonly ILogger<CopilotService> _logger;

    public async IAsyncEnumerable<CopilotStreamEvent> StreamMessageAsync(
        string sessionId, 
        string prompt, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var session = await _clientManager.GetOrCreateSessionAsync(sessionId);
        var channel = Channel.CreateUnbounded<CopilotStreamEvent>();

        var subscription = session.On(evt =>
        {
            var streamEvent = evt switch
            {
                AssistantMessageDeltaEvent delta => new CopilotStreamEvent 
                { 
                    Type = "delta", 
                    Content = delta.Data.DeltaContent 
                },
                AssistantMessageEvent msg => new CopilotStreamEvent 
                { 
                    Type = "message", 
                    Content = msg.Data.Content 
                },
                SessionIdleEvent => new CopilotStreamEvent 
                { 
                    Type = "idle" 
                },
                SessionErrorEvent err => new CopilotStreamEvent 
                { 
                    Type = "error", 
                    Error = err.Data.Message 
                },
                _ => null
            };

            if (streamEvent != null)
            {
                channel.Writer.TryWrite(streamEvent);
            }

            if (evt is SessionIdleEvent or SessionErrorEvent)
            {
                channel.Writer.Complete();
            }
        });

        await session.SendAsync(new MessageOptions { Prompt = prompt });

        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return evt;
        }

        subscription.Dispose();
    }
}
```

- [ ] Implement message sending and streaming
- [ ] Add SSE endpoint for real-time streaming
- [ ] Handle session events and errors
- [ ] Add file attachment support

#### 4.3 Custom Tools Integration

```csharp
// MakeApp.Infrastructure/Copilot/MakeAppTools.cs
public static class MakeAppTools
{
    public static AIFunction[] CreateTools(IServiceProvider services)
    {
        var gitService = services.GetRequiredService<IGitService>();
        var fileSystem = services.GetRequiredService<IFileSystem>();

        return [
            AIFunctionFactory.Create(
                async ([Description("Repository path")] string repoPath) => 
                    await gitService.GetStatusAsync(repoPath),
                "get_git_status",
                "Get the current git status of a repository"),

            AIFunctionFactory.Create(
                async ([Description("File path to read")] string filePath) => 
                    await fileSystem.File.ReadAllTextAsync(filePath),
                "read_file",
                "Read the contents of a file"),

            AIFunctionFactory.Create(
                async ([Description("File path")] string filePath, 
                       [Description("Content to write")] string content) =>
                {
                    await fileSystem.File.WriteAllTextAsync(filePath, content);
                    return "File written successfully";
                },
                "write_file",
                "Write content to a file"),

            AIFunctionFactory.Create(
                async ([Description("Directory path")] string dirPath) =>
                    fileSystem.Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories),
                "list_files",
                "List all files in a directory"),
        ];
    }
}
```

- [ ] Define custom tools for MakeApp operations
- [ ] Register tools with Copilot sessions
- [ ] Add tool execution logging

**Deliverables**:
- Copilot session management
- Streaming response support via SSE
- Custom tools for file/git operations

---

### Phase 5: Workflow Orchestration (Weeks 10-12)

**Objective**: Implement the agent orchestration loop with state management.

#### 5.1 Orchestration Service

**PowerShell Equivalent**: `Start-AgentOrchestration`, `Get-ImplementationPlan`, `Invoke-ImplementationStep`

```csharp
// MakeApp.Core/Entities/Workflow.cs
public class Workflow
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FeatureId { get; set; } = "";
    public string RepositoryPath { get; set; } = "";
    public WorkflowPhase Phase { get; set; } = WorkflowPhase.Pending;
    public List<WorkflowStep> Steps { get; set; } = new();
    public List<WorkflowStep> CompletedSteps { get; set; } = new();
    public List<WorkflowError> Errors { get; set; } = new();
    public int CurrentIteration { get; set; }
    public int MaxIterations { get; set; } = 50;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CopilotSessionId { get; set; }
}

public enum WorkflowPhase
{
    Pending,
    Planning,
    Implementation,
    Validation,
    Complete,
    Failed,
    Aborted
}

// MakeApp.Application/Services/OrchestrationService.cs
public interface IOrchestrationService
{
    Task<Workflow> StartWorkflowAsync(StartWorkflowDto dto);
    Task<Workflow?> GetWorkflowAsync(string id);
    IAsyncEnumerable<WorkflowEvent> StreamWorkflowEventsAsync(string id, CancellationToken cancellationToken);
    Task<Workflow> AbortWorkflowAsync(string id);
    Task<Workflow> RetryStepAsync(string id);
    Task<Workflow> SkipStepAsync(string id);
    Task<IEnumerable<WorkflowStep>> GetImplementationPlanAsync(string id);
}

public class OrchestrationService : IOrchestrationService
{
    private readonly ICopilotService _copilotService;
    private readonly IGitService _gitService;
    private readonly IFeatureService _featureService;
    private readonly IPromptFormatterService _promptFormatter;
    private readonly ConcurrentDictionary<string, Workflow> _activeWorkflows = new();
    private readonly ILogger<OrchestrationService> _logger;

    public async Task<Workflow> StartWorkflowAsync(StartWorkflowDto dto)
    {
        var feature = await _featureService.GetFeatureAsync(dto.FeatureId)
            ?? throw new NotFoundException($"Feature {dto.FeatureId} not found");

        var workflow = new Workflow
        {
            FeatureId = dto.FeatureId,
            RepositoryPath = dto.RepositoryPath,
            MaxIterations = dto.MaxIterations ?? 50,
            StartedAt = DateTime.UtcNow
        };

        _activeWorkflows[workflow.Id] = workflow;

        // Start workflow in background
        _ = ExecuteWorkflowAsync(workflow, feature);

        return workflow;
    }

    private async Task ExecuteWorkflowAsync(Workflow workflow, Feature feature)
    {
        try
        {
            // Phase 1: Planning
            workflow.Phase = WorkflowPhase.Planning;
            await OnWorkflowPhaseChanged(workflow);

            var planPrompt = _promptFormatter.FormatImplementationPlanPrompt(feature);
            var sessionId = await _copilotService.CreateSessionAsync(new CreateSessionDto
            {
                Model = "gpt-5",
                Streaming = true
            });
            workflow.CopilotSessionId = sessionId;

            // Get implementation plan
            var planResponse = await _copilotService.SendMessageAsync(sessionId, planPrompt);
            workflow.Steps = ParseImplementationSteps(planResponse.Content);

            // Phase 2: Implementation
            workflow.Phase = WorkflowPhase.Implementation;
            await OnWorkflowPhaseChanged(workflow);

            while (workflow.CurrentIteration < workflow.MaxIterations && workflow.Steps.Count > 0)
            {
                workflow.CurrentIteration++;
                var currentStep = workflow.Steps[0];

                var stepResult = await ExecuteStepAsync(workflow, currentStep);

                if (stepResult.Success)
                {
                    workflow.CompletedSteps.Add(currentStep);
                    workflow.Steps.RemoveAt(0);
                }
                else
                {
                    workflow.Errors.Add(new WorkflowError
                    {
                        Step = currentStep,
                        Message = stepResult.Error,
                        Iteration = workflow.CurrentIteration
                    });
                    // Wait for user action (retry/skip/abort)
                    // This is handled via API calls
                }
            }

            // Phase 3: Validation
            workflow.Phase = WorkflowPhase.Validation;
            await OnWorkflowPhaseChanged(workflow);

            // ... validation logic

            workflow.Phase = WorkflowPhase.Complete;
            workflow.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            workflow.Phase = WorkflowPhase.Failed;
            workflow.Errors.Add(new WorkflowError { Message = ex.Message });
            _logger.LogError(ex, "Workflow {WorkflowId} failed", workflow.Id);
        }
    }
}
```

- [ ] Implement workflow state machine
- [ ] Add background task execution with Hangfire or Channels
- [ ] Create workflow event streaming via SSE
- [ ] Handle user interactions (retry/skip/abort)

#### 5.2 Workflow Controller with SSE

```csharp
// MakeApp.Api/Controllers/WorkflowsController.cs
[ApiController]
[Route("api/v1/workflows")]
public class WorkflowsController : ControllerBase
{
    private readonly IOrchestrationService _orchestrationService;

    [HttpPost]
    public async Task<ActionResult<WorkflowDto>> StartWorkflow([FromBody] StartWorkflowDto dto)
    {
        var workflow = await _orchestrationService.StartWorkflowAsync(dto);
        return CreatedAtAction(nameof(GetWorkflow), new { id = workflow.Id }, workflow.ToDto());
    }

    [HttpGet("{id}/events")]
    public async Task StreamEvents(string id, CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        await foreach (var evt in _orchestrationService.StreamWorkflowEventsAsync(id, cancellationToken))
        {
            var json = JsonSerializer.Serialize(evt);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    [HttpPost("{id}/abort")]
    public async Task<ActionResult<WorkflowDto>> AbortWorkflow(string id)
    {
        var workflow = await _orchestrationService.AbortWorkflowAsync(id);
        return Ok(workflow.ToDto());
    }

    [HttpPost("{id}/retry")]
    public async Task<ActionResult<WorkflowDto>> RetryStep(string id)
    {
        var workflow = await _orchestrationService.RetryStepAsync(id);
        return Ok(workflow.ToDto());
    }

    [HttpPost("{id}/skip")]
    public async Task<ActionResult<WorkflowDto>> SkipStep(string id)
    {
        var workflow = await _orchestrationService.SkipStepAsync(id);
        return Ok(workflow.ToDto());
    }
}
```

- [ ] Implement SSE streaming for workflow events
- [ ] Add workflow control endpoints
- [ ] Implement workflow persistence

**Deliverables**:
- Complete workflow orchestration engine
- Real-time event streaming
- Workflow control API

---

### Phase 5.5: Agentic Memory System (Weeks 12-13)

**Objective**: Implement the Copilot Agentic Memory system for cross-workflow learning and knowledge persistence.

> **Reference**: [GitHub Copilot Memory Documentation](https://docs.github.com/en/copilot/concepts/agents/copilot-memory) | [Memory System Architecture Blog](https://github.blog/ai-and-ml/github-copilot/building-an-agentic-memory-system-for-github-copilot/)

#### 5.5.1 Memory Entity and Repository

```csharp
// MakeApp.Core/Entities/Memory.cs
public class Memory
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string RepositoryPath { get; set; } = "";
    public string RepositoryOwner { get; set; } = "";
    public string RepositoryName { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Fact { get; set; } = "";
    public List<MemoryCitation> Citations { get; set; } = new();
    public string Reason { get; set; } = "";
    public string CreatedByWorkflowId { get; set; } = "";
    public string CreatedByUserId { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastValidatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int UseCount { get; set; }
    public MemoryStatus Status { get; set; } = MemoryStatus.Active;
    
    public DateTime ExpiresAt => (LastValidatedAt ?? CreatedAt).AddDays(28);
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}

public class MemoryCitation
{
    public string FilePath { get; set; } = "";
    public int? LineNumber { get; set; }
    public string? CodeSnippet { get; set; }
    public DateTime LastVerified { get; set; }
    public bool IsValid { get; set; } = true;
}

public enum MemoryStatus
{
    Active,
    Stale,
    Invalid,
    Superseded,
    Archived
}

// MakeApp.Infrastructure/Persistence/MemoryRepository.cs
public interface IMemoryRepository
{
    Task<Memory?> GetByIdAsync(string id);
    Task<IEnumerable<Memory>> GetByRepositoryAsync(string owner, string name);
    Task<IEnumerable<Memory>> GetByCitationFileAsync(string filePath);
    Task<IEnumerable<Memory>> SearchAsync(string repoPath, string query);
    Task<Memory> CreateAsync(Memory memory);
    Task<Memory> UpdateAsync(Memory memory);
    Task<bool> DeleteAsync(string id);
    Task<int> PruneExpiredAsync(string? repoPath = null);
}
```

- [ ] Define Memory entity with citations
- [ ] Implement memory repository (file-based or database)
- [ ] Add memory indexing for efficient search
- [ ] Create memory expiration background job

#### 5.5.2 Memory Service

```csharp
// MakeApp.Application/Services/MemoryService.cs
public interface IMemoryService
{
    // Retrieval
    Task<IEnumerable<Memory>> GetRepositoryMemoriesAsync(
        string owner, string name, MemoryFilterOptions? filter = null);
    Task<Memory?> GetMemoryAsync(string id);
    Task<IEnumerable<Memory>> SearchMemoriesAsync(string repoPath, string query);
    Task<IEnumerable<Memory>> GetMemoriesByCitationAsync(string filePath);
    
    // Storage
    Task<Memory> StoreMemoryAsync(CreateMemoryDto dto);
    Task<Memory> UpdateMemoryAsync(string id, UpdateMemoryDto dto);
    Task<bool> DeleteMemoryAsync(string id);
    
    // Validation
    Task<MemoryValidationResult> ValidateMemoryAsync(string id);
    Task<MemoryValidationReport> ValidateAllMemoriesAsync(string owner, string name);
    Task<bool> RefreshMemoryAsync(string id);
    
    // Maintenance
    Task<int> PruneExpiredMemoriesAsync(string? owner = null, string? name = null);
    Task<MemoryStatistics> GetStatisticsAsync(string owner, string name);
}

public class MemoryFilterOptions
{
    public string? SubjectContains { get; set; }
    public string? FactContains { get; set; }
    public string? AffectsFile { get; set; }
    public bool IncludeExpired { get; set; }
    public bool IncludeInvalid { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public int? MaxResults { get; set; } = 20;
    public MemorySortBy SortBy { get; set; } = MemorySortBy.LastUsed;
}

public enum MemorySortBy
{
    LastUsed,
    LastValidated,
    Created,
    UseCount,
    Relevance
}
```

- [ ] Implement memory CRUD operations
- [ ] Add citation-based retrieval
- [ ] Implement relevance-based search
- [ ] Create memory statistics tracking

#### 5.5.3 Memory Validation Service

```csharp
// MakeApp.Application/Services/MemoryValidationService.cs
public class MemoryValidationService : IMemoryValidationService
{
    private readonly IFileSystem _fileSystem;
    private readonly IGitService _gitService;
    private readonly ILogger<MemoryValidationService> _logger;
    
    public async Task<MemoryValidationResult> ValidateAsync(Memory memory)
    {
        var result = new MemoryValidationResult
        {
            MemoryId = memory.Id,
            ValidatedAt = DateTime.UtcNow
        };
        
        foreach (var citation in memory.Citations)
        {
            var citationResult = await ValidateCitationAsync(
                memory.RepositoryPath, citation);
            
            result.CitationResults.Add(citationResult);
        }
        
        // Calculate overall validity
        var validCount = result.CitationResults.Count(c => c.IsValid);
        var totalCount = result.CitationResults.Count;
        
        result.Confidence = totalCount > 0 
            ? (double)validCount / totalCount 
            : 0;
        
        result.IsValid = result.Confidence >= 0.5; // At least half must be valid
        result.RecommendedAction = DetermineAction(result);
        
        return result;
    }
    
    private async Task<CitationValidationResult> ValidateCitationAsync(
        string repoPath, MemoryCitation citation)
    {
        var fullPath = Path.Combine(repoPath, citation.FilePath);
        
        if (!_fileSystem.File.Exists(fullPath))
        {
            return new CitationValidationResult
            {
                Citation = citation,
                IsValid = false,
                Issue = $"File not found: {citation.FilePath}"
            };
        }
        
        var lines = await _fileSystem.File.ReadAllLinesAsync(fullPath);
        
        if (citation.LineNumber.HasValue)
        {
            if (citation.LineNumber > lines.Length)
            {
                return new CitationValidationResult
                {
                    Citation = citation,
                    IsValid = false,
                    Issue = $"Line {citation.LineNumber} exceeds file length ({lines.Length} lines)"
                };
            }
            
            // Optionally verify code snippet still matches
            if (!string.IsNullOrEmpty(citation.CodeSnippet))
            {
                var currentLine = lines[citation.LineNumber.Value - 1];
                if (!currentLine.Contains(citation.CodeSnippet))
                {
                    return new CitationValidationResult
                    {
                        Citation = citation,
                        IsValid = false,
                        Issue = "Code snippet no longer matches at specified line",
                        CurrentContent = currentLine
                    };
                }
            }
        }
        
        return new CitationValidationResult
        {
            Citation = citation,
            IsValid = true,
            VerifiedAt = DateTime.UtcNow
        };
    }
    
    private MemoryAction DetermineAction(MemoryValidationResult result)
    {
        if (result.Confidence >= 0.8)
            return MemoryAction.Refresh;
        if (result.Confidence >= 0.5)
            return MemoryAction.UpdateCitations;
        if (result.Confidence > 0)
            return MemoryAction.ReviewManually;
        return MemoryAction.Delete;
    }
}
```

- [ ] Implement just-in-time citation validation
- [ ] Add code snippet matching for precise verification
- [ ] Create confidence scoring algorithm
- [ ] Implement recommended action logic

#### 5.5.4 Memory Store Tool for Copilot

```csharp
// MakeApp.Infrastructure/Copilot/MemoryStoreTool.cs
public static class MemoryStoreTool
{
    public static AIFunction Create(
        IMemoryService memoryService, 
        string repositoryPath,
        string workflowId,
        string userId)
    {
        return AIFunctionFactory.Create(
            async (
                [Description("Brief subject/title of what was learned (e.g., 'API version synchronization')")] 
                string subject,
                
                [Description("The specific fact, convention, or pattern discovered")] 
                string fact,
                
                [Description("File paths with optional line numbers that support this fact (e.g., 'src/config.ts:42', 'README.md')")] 
                string[] citations,
                
                [Description("Explanation of why this is worth remembering for future work")] 
                string reason) =>
            {
                var memory = await memoryService.StoreMemoryAsync(new CreateMemoryDto
                {
                    RepositoryPath = repositoryPath,
                    Subject = subject,
                    Fact = fact,
                    Citations = ParseCitations(citations),
                    Reason = reason,
                    CreatedByWorkflowId = workflowId,
                    CreatedByUserId = userId
                });
                
                return new
                {
                    success = true,
                    memoryId = memory.Id,
                    message = $"Memory stored: '{subject}' with {citations.Length} citations"
                };
            },
            "store_memory",
            description: @"Store a learning about this codebase for future reference.
Use this tool when you discover:
- Coding conventions (naming, formatting, patterns)
- File relationships (files that must stay synchronized)
- Configuration requirements
- Testing patterns
- Build/deployment requirements
- Important domain knowledge

Good memories are:
- Specific and actionable
- Supported by concrete code locations (citations)
- Likely to be relevant for future tasks

Example: If you notice API versions must match in 3 files, store that as a memory
so future changes will know to update all locations.");
    }
    
    private static List<CreateMemoryCitationDto> ParseCitations(string[] citations)
    {
        return citations.Select(c =>
        {
            var parts = c.Split(':');
            return new CreateMemoryCitationDto
            {
                FilePath = parts[0],
                LineNumber = parts.Length > 1 && int.TryParse(parts[1], out var line) 
                    ? line 
                    : null
            };
        }).ToList();
    }
}
```

- [ ] Implement store_memory tool for Copilot sessions
- [ ] Add citation parsing from various formats
- [ ] Include helpful description for Copilot guidance
- [ ] Add validation before storage

#### 5.5.5 Memory-Enhanced Workflow Integration

```csharp
// MakeApp.Application/Services/MemoryAwareWorkflowService.cs
public class MemoryAwareWorkflowService
{
    private readonly IOrchestrationService _orchestration;
    private readonly IMemoryService _memoryService;
    private readonly IMemoryValidationService _validationService;
    private readonly ICopilotService _copilotService;
    
    public async Task<Workflow> StartMemoryAwareWorkflowAsync(StartWorkflowDto dto)
    {
        // 1. Verify and prepare memories
        var memoryReport = await PrepareMemoriesAsync(dto.Owner, dto.Name);
        
        // 2. Build memory context for prompt
        var memoryContext = BuildMemoryContext(memoryReport.ValidMemories);
        
        // 3. Create session with memory tools
        var sessionConfig = BuildSessionConfig(dto, memoryContext);
        
        // 4. Start workflow with memory awareness
        var workflow = await _orchestration.StartWorkflowAsync(dto with
        {
            AdditionalContext = memoryContext,
            SessionConfig = sessionConfig
        });
        
        // 5. Track memory usage for this workflow
        await TrackMemoryUsageAsync(workflow.Id, memoryReport.ValidMemories);
        
        return workflow;
    }
    
    private async Task<MemoryPreparationReport> PrepareMemoriesAsync(
        string owner, string name)
    {
        var report = new MemoryPreparationReport();
        var memories = await _memoryService.GetRepositoryMemoriesAsync(owner, name);
        
        // Validate in parallel for performance
        var validationTasks = memories.Select(async m =>
        {
            var result = await _validationService.ValidateAsync(m);
            return (Memory: m, Validation: result);
        });
        
        var results = await Task.WhenAll(validationTasks);
        
        foreach (var (memory, validation) in results)
        {
            if (validation.IsValid)
            {
                report.ValidMemories.Add(memory);
                await _memoryService.RefreshMemoryAsync(memory.Id);
            }
            else if (validation.Confidence > 0.3)
            {
                report.StaleMemories.Add((memory, validation));
            }
            else
            {
                report.InvalidMemories.Add((memory, validation));
            }
        }
        
        return report;
    }
    
    private string BuildMemoryContext(IEnumerable<Memory> memories)
    {
        if (!memories.Any()) return "";
        
        var sb = new StringBuilder();
        sb.AppendLine("## Repository Learnings (Verified)");
        sb.AppendLine();
        sb.AppendLine("These conventions have been learned from previous work. " +
                      "Verify citations before applying.");
        sb.AppendLine();
        
        foreach (var memory in memories.OrderByDescending(m => m.UseCount).Take(10))
        {
            sb.AppendLine($"### {memory.Subject}");
            sb.AppendLine($"**Fact:** {memory.Fact}");
            sb.AppendLine($"**Evidence:** {string.Join(", ", memory.Citations.Select(c => FormatCitation(c)))}");
            sb.AppendLine($"**Why it matters:** {memory.Reason}");
            sb.AppendLine();
        }
        
        return sb.ToString();
    }
}
```

- [ ] Implement memory preparation before workflow
- [ ] Add parallel validation for performance
- [ ] Create memory context formatting
- [ ] Track memory usage per workflow

#### 5.5.6 Memory API Controller

```csharp
// MakeApp.Api/Controllers/MemoriesController.cs
[ApiController]
[Route("api/v1/repos/{owner}/{name}/memories")]
[Authorize]
public class MemoriesController : ControllerBase
{
    private readonly IMemoryService _memoryService;
    
    /// <summary>
    /// List all memories for a repository
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<MemoryDto>), 200)]
    public async Task<ActionResult<PaginatedResult<MemoryDto>>> GetMemories(
        string owner,
        string name,
        [FromQuery] MemoryFilterOptions? filter = null)
    {
        var memories = await _memoryService.GetRepositoryMemoriesAsync(owner, name, filter);
        return Ok(memories.ToPaginatedResult());
    }
    
    /// <summary>
    /// Get a specific memory
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MemoryDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<MemoryDto>> GetMemory(
        string owner, string name, string id)
    {
        var memory = await _memoryService.GetMemoryAsync(id);
        if (memory == null) return NotFound();
        return Ok(memory.ToDto());
    }
    
    /// <summary>
    /// Manually create a memory
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MemoryDto), 201)]
    public async Task<ActionResult<MemoryDto>> CreateMemory(
        string owner, string name,
        [FromBody] CreateMemoryDto dto)
    {
        dto.RepositoryOwner = owner;
        dto.RepositoryName = name;
        var memory = await _memoryService.StoreMemoryAsync(dto);
        return CreatedAtAction(nameof(GetMemory), new { owner, name, id = memory.Id }, memory.ToDto());
    }
    
    /// <summary>
    /// Validate a memory's citations
    /// </summary>
    [HttpPost("{id}/validate")]
    [ProducesResponseType(typeof(MemoryValidationResultDto), 200)]
    public async Task<ActionResult<MemoryValidationResultDto>> ValidateMemory(
        string owner, string name, string id)
    {
        var result = await _memoryService.ValidateMemoryAsync(id);
        return Ok(result.ToDto());
    }
    
    /// <summary>
    /// Validate all memories for a repository
    /// </summary>
    [HttpPost("validate-all")]
    [ProducesResponseType(typeof(MemoryValidationReportDto), 200)]
    public async Task<ActionResult<MemoryValidationReportDto>> ValidateAllMemories(
        string owner, string name)
    {
        var report = await _memoryService.ValidateAllMemoriesAsync(owner, name);
        return Ok(report.ToDto());
    }
    
    /// <summary>
    /// Refresh a memory's timestamp (extend TTL)
    /// </summary>
    [HttpPost("{id}/refresh")]
    [ProducesResponseType(typeof(MemoryDto), 200)]
    public async Task<ActionResult<MemoryDto>> RefreshMemory(
        string owner, string name, string id)
    {
        await _memoryService.RefreshMemoryAsync(id);
        var memory = await _memoryService.GetMemoryAsync(id);
        return Ok(memory!.ToDto());
    }
    
    /// <summary>
    /// Delete a memory
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteMemory(
        string owner, string name, string id)
    {
        await _memoryService.DeleteMemoryAsync(id);
        return NoContent();
    }
    
    /// <summary>
    /// Get memories that cite a specific file
    /// </summary>
    [HttpGet("by-file")]
    [ProducesResponseType(typeof(IEnumerable<MemoryDto>), 200)]
    public async Task<ActionResult<IEnumerable<MemoryDto>>> GetMemoriesByFile(
        string owner, string name,
        [FromQuery] string filePath)
    {
        var memories = await _memoryService.GetMemoriesByCitationAsync(filePath);
        return Ok(memories.Select(m => m.ToDto()));
    }
    
    /// <summary>
    /// Get memory statistics for a repository
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(MemoryStatisticsDto), 200)]
    public async Task<ActionResult<MemoryStatisticsDto>> GetStatistics(
        string owner, string name)
    {
        var stats = await _memoryService.GetStatisticsAsync(owner, name);
        return Ok(stats.ToDto());
    }
    
    /// <summary>
    /// Prune expired memories
    /// </summary>
    [HttpPost("prune")]
    [ProducesResponseType(typeof(PruneResultDto), 200)]
    public async Task<ActionResult<PruneResultDto>> PruneExpired(
        string owner, string name)
    {
        var count = await _memoryService.PruneExpiredMemoriesAsync(owner, name);
        return Ok(new PruneResultDto { DeletedCount = count });
    }
}
```

- [ ] Implement full CRUD endpoints for memories
- [ ] Add validation and refresh endpoints
- [ ] Create statistics endpoint
- [ ] Implement prune endpoint for cleanup

#### 5.5.7 Background Memory Maintenance

```csharp
// MakeApp.Infrastructure/BackgroundJobs/MemoryMaintenanceJob.cs
public class MemoryMaintenanceJob : IHostedService, IDisposable
{
    private Timer? _timer;
    private readonly IServiceProvider _services;
    private readonly ILogger<MemoryMaintenanceJob> _logger;
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Memory maintenance job starting");
        
        // Run daily at 2 AM
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(24));
        
        return Task.CompletedTask;
    }
    
    private async void DoWork(object? state)
    {
        using var scope = _services.CreateScope();
        var memoryService = scope.ServiceProvider.GetRequiredService<IMemoryService>();
        
        try
        {
            // Prune expired memories
            var pruned = await memoryService.PruneExpiredMemoriesAsync();
            _logger.LogInformation("Pruned {Count} expired memories", pruned);
            
            // Optionally: Validate random sample of memories
            // This helps identify systematic issues
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in memory maintenance job");
        }
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }
    
    public void Dispose() => _timer?.Dispose();
}
```

- [ ] Implement background job for memory maintenance
- [ ] Add expired memory cleanup
- [ ] Create memory health monitoring
- [ ] Add alerting for memory system issues

**Deliverables**:
- Complete Memory entity and repository
- Memory validation with citation checking
- Memory store tool for Copilot sessions
- Memory-aware workflow integration
- Memory management API endpoints
- Background maintenance jobs
- Memory statistics and reporting

---

### Phase 6: Configuration & Sandbox Management (Weeks 14-15)

**Objective**: Implement Copilot configuration validation and sandbox environment management.

#### 6.1 Copilot Configuration Service

**PowerShell Equivalent**: `Test-CopilotInstructions`, `New-CopilotInstructions`, `Test-McpConfig`, `New-McpConfig`

```csharp
// MakeApp.Application/Services/CopilotConfigService.cs
public interface ICopilotConfigService
{
    Task<CopilotInstructionsStatus> CheckInstructionsAsync(string repoPath);
    Task<string> CreateInstructionsAsync(string repoPath, string projectType, bool force = false);
    Task<string> GetInstructionsTemplateAsync(string projectType);
    Task<McpConfigStatus> CheckMcpConfigAsync(string repoPath);
    Task<string> CreateMcpConfigAsync(string repoPath, bool force = false);
    Task<ConfigurationSummary> ValidateAllConfigurationsAsync(string repoPath);
}
```

- [ ] Implement Copilot instructions validation
- [ ] Add MCP configuration management
- [ ] Create configuration templates for different project types

#### 6.2 Sandbox Service

**PowerShell Equivalent**: `New-Sandbox`, `Remove-Sandbox`, `Reset-Sandbox`, `Get-SandboxInfo`

```csharp
// MakeApp.Application/Services/SandboxService.cs
public interface ISandboxService
{
    Task<SandboxInfo> CreateSandboxAsync(string projectType, bool force = false);
    Task<bool> DeleteSandboxAsync();
    Task<SandboxInfo> ResetSandboxAsync(string? projectType = null);
    Task<SandboxInfo?> GetSandboxInfoAsync();
    Task<bool> IsSandboxActiveAsync();
}

public class SandboxService : ISandboxService
{
    private readonly MakeAppOptions _options;
    private readonly IGitService _gitService;
    private readonly ICopilotConfigService _configService;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<SandboxService> _logger;

    public async Task<SandboxInfo> CreateSandboxAsync(string projectType, bool force = false)
    {
        var sandboxPath = _options.Folders.Sandbox;
        var repoPath = Path.Combine(sandboxPath, "sandbox-repo");

        if (_fileSystem.Directory.Exists(sandboxPath))
        {
            if (force)
            {
                await DeleteSandboxAsync();
            }
            else
            {
                throw new ConflictException("Sandbox already exists. Use force=true to overwrite.");
            }
        }

        // Create directory structure
        _fileSystem.Directory.CreateDirectory(sandboxPath);
        _fileSystem.Directory.CreateDirectory(Path.Combine(sandboxPath, "workspace"));
        _fileSystem.Directory.CreateDirectory(Path.Combine(sandboxPath, "temp"));
        _fileSystem.Directory.CreateDirectory(Path.Combine(sandboxPath, "logs"));
        _fileSystem.Directory.CreateDirectory(Path.Combine(sandboxPath, "cache"));
        _fileSystem.Directory.CreateDirectory(repoPath);

        // Initialize git repository
        await _gitService.InitializeRepositoryAsync(repoPath);

        // Create sample files based on project type
        await CreateSampleFilesAsync(repoPath, projectType);

        // Create initial commit
        await _gitService.StageChangesAsync(repoPath);
        await _gitService.CommitAsync(repoPath, "Initial sandbox commit");

        // Add Copilot instructions
        await _configService.CreateInstructionsAsync(repoPath, projectType, force: true);
        await _gitService.StageChangesAsync(repoPath);
        await _gitService.CommitAsync(repoPath, "Add Copilot instructions");

        return await GetSandboxInfoAsync() ?? throw new InvalidOperationException("Failed to create sandbox");
    }
}
```

- [ ] Implement sandbox CRUD operations
- [ ] Add sample file generation for different project types
- [ ] Create sandbox API endpoints

**Deliverables**:
- Configuration validation and generation
- Complete sandbox management API

---

### Phase 7: Security, Authentication & Polish (Weeks 15-16)

**Objective**: Add authentication, authorization, and production-ready features.

#### 7.1 Authentication & Authorization

```csharp
// MakeApp.Api/Authentication/JwtConfiguration.cs
public static class AuthenticationExtensions
{
    public static IServiceCollection AddMakeAppAuthentication(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
                };
            })
            .AddGitHub(options =>
            {
                options.ClientId = configuration["GitHub:ClientId"]!;
                options.ClientSecret = configuration["GitHub:ClientSecret"]!;
                options.Scope.Add("repo");
                options.Scope.Add("read:user");
            });

        return services;
    }
}
```

- [ ] Implement JWT authentication
- [ ] Add GitHub OAuth integration
- [ ] Implement role-based authorization
- [ ] Add API key support for CI/CD scenarios

#### 7.2 Rate Limiting & Throttling

```csharp
// MakeApp.Api/Configuration/RateLimitingExtensions.cs
public static IServiceCollection AddMakeAppRateLimiting(this IServiceCollection services)
{
    services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("standard", opt =>
        {
            opt.PermitLimit = 100;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 10;
        });

        options.AddTokenBucketLimiter("copilot", opt =>
        {
            opt.TokenLimit = 20;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 5;
            opt.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
            opt.TokensPerPeriod = 5;
        });
    });

    return services;
}
```

- [ ] Implement rate limiting per endpoint category
- [ ] Add request throttling for Copilot operations
- [ ] Implement request queuing

#### 7.3 Error Handling & Validation

```csharp
// MakeApp.Api/Middleware/ExceptionHandlingMiddleware.cs
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status400BadRequest);
        }
        catch (NotFoundException ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status404NotFound);
        }
        catch (ConflictException ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status409Conflict);
        }
        catch (UnauthorizedAccessException ex)
        {
            await HandleExceptionAsync(context, ex, StatusCodes.Status403Forbidden);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex, StatusCodes.Status500InternalServerError);
        }
    }
}
```

- [ ] Implement global exception handling
- [ ] Add FluentValidation for request validation
- [ ] Create problem details responses

#### 7.4 API Documentation & Client Generation

```csharp
// MakeApp.Api/Configuration/SwaggerConfiguration.cs
public static IServiceCollection AddMakeAppSwagger(this IServiceCollection services)
{
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "MakeApp API",
            Version = "v1",
            Description = "AI-powered feature development workflow automation API",
            Contact = new OpenApiContact { Name = "MakeApp Team" }
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    return services;
}
```

- [ ] Configure comprehensive OpenAPI documentation
- [ ] Generate TypeScript client
- [ ] Generate C# client library
- [ ] Add API versioning documentation

**Deliverables**:
- Secure authentication system
- Rate limiting and throttling
- Comprehensive error handling
- Complete API documentation

---

### Phase 8: Testing, Deployment & Documentation (Weeks 17-18)

**Objective**: Ensure quality, create deployment pipeline, and finalize documentation.

#### 8.1 Testing Strategy

```csharp
// Unit Tests
// MakeApp.Application.Tests/Services/FeatureServiceTests.cs
public class FeatureServiceTests
{
    private readonly Mock<IFeatureRepository> _repositoryMock;
    private readonly FeatureService _sut;

    [Fact]
    public async Task CreateFeatureAsync_ValidInput_ReturnsFeature()
    {
        // Arrange
        var dto = new CreateFeatureDto { Title = "Test Feature" };
        
        // Act
        var result = await _sut.CreateFeatureAsync(dto);
        
        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test Feature");
    }
}

// Integration Tests
// MakeApp.Api.Tests/Controllers/FeaturesControllerTests.cs
public class FeaturesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    [Fact]
    public async Task GetFeatures_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/features");
        
        // Assert
        response.EnsureSuccessStatusCode();
    }
}
```

- [ ] Write unit tests for all services (>80% coverage)
- [ ] Create integration tests for API endpoints
- [ ] Add E2E tests with test containers
- [ ] Implement Copilot SDK mock for testing

#### 8.2 Deployment Configuration

```yaml
# docker-compose.yml
version: '3.8'
services:
  makeapp-api:
    build:
      context: .
      dockerfile: src/MakeApp.Api/Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION}
      - GitHub__ClientId=${GITHUB_CLIENT_ID}
      - GitHub__ClientSecret=${GITHUB_CLIENT_SECRET}
    volumes:
      - repos:/app/repos
      - sandbox:/app/sandbox
    depends_on:
      - redis
  
  redis:
    image: redis:alpine
    ports:
      - "6379:6379"

volumes:
  repos:
  sandbox:
```

```dockerfile
# src/MakeApp.Api/Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Install git and GitHub CLI for Copilot operations
RUN apt-get update && apt-get install -y git curl && \
    curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg && \
    echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | tee /etc/apt/sources.list.d/github-cli.list > /dev/null && \
    apt-get update && apt-get install -y gh

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/MakeApp.Api/MakeApp.Api.csproj", "src/MakeApp.Api/"]
COPY ["src/MakeApp.Application/MakeApp.Application.csproj", "src/MakeApp.Application/"]
COPY ["src/MakeApp.Core/MakeApp.Core.csproj", "src/MakeApp.Core/"]
COPY ["src/MakeApp.Infrastructure/MakeApp.Infrastructure.csproj", "src/MakeApp.Infrastructure/"]
RUN dotnet restore "src/MakeApp.Api/MakeApp.Api.csproj"
COPY . .
WORKDIR "/src/src/MakeApp.Api"
RUN dotnet build "MakeApp.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MakeApp.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MakeApp.Api.dll"]
```

- [ ] Create Docker configuration
- [ ] Set up GitHub Actions CI/CD pipeline
- [ ] Configure Kubernetes manifests (optional)
- [ ] Add health checks and readiness probes

#### 8.3 Documentation

- [ ] Update README with API documentation
- [ ] Create API client usage examples
- [ ] Write migration guide from CLI to API
- [ ] Create Postman/Bruno collection
- [ ] Document configuration options

**Deliverables**:
- Comprehensive test suite
- Docker deployment configuration
- CI/CD pipeline
- Complete documentation

---

## Project Structure Summary

```
MakeApp.Api/
├── MakeApp.Api.sln
├── docker-compose.yml
├── README.md
├── docs/
│   ├── api-reference.md
│   ├── authentication.md
│   ├── configuration.md
│   ├── memory-system.md              # NEW: Memory system documentation
│   └── migration-guide.md
├── src/
│   ├── MakeApp.Api/
│   │   ├── Controllers/
│   │   │   ├── BranchesController.cs
│   │   │   ├── ConfigController.cs
│   │   │   ├── CopilotController.cs
│   │   │   ├── FeaturesController.cs
│   │   │   ├── GitController.cs
│   │   │   ├── HealthController.cs
│   │   │   ├── MemoriesController.cs     # NEW: Memory management API
│   │   │   ├── RepositoriesController.cs
│   │   │   ├── SandboxController.cs
│   │   │   └── WorkflowsController.cs
│   │   ├── Middleware/
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   └── RequestLoggingMiddleware.cs
│   │   ├── Authentication/
│   │   │   └── JwtConfiguration.cs
│   │   ├── Configuration/
│   │   │   ├── RateLimitingExtensions.cs
│   │   │   └── SwaggerConfiguration.cs
│   │   ├── BackgroundJobs/               # NEW: Background job hosting
│   │   │   └── MemoryMaintenanceJob.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   └── Dockerfile
│   │
│   ├── MakeApp.Core/
│   │   ├── Entities/
│   │   │   ├── Feature.cs
│   │   │   ├── Memory.cs                 # NEW: Memory entity
│   │   │   ├── MemoryCitation.cs         # NEW: Citation entity
│   │   │   ├── Workflow.cs
│   │   │   ├── WorkflowStep.cs
│   │   │   ├── RepositoryInfo.cs
│   │   │   ├── BranchInfo.cs
│   │   │   └── SandboxInfo.cs
│   │   ├── Enums/
│   │   │   ├── FeatureStatus.cs
│   │   │   ├── FeaturePriority.cs
│   │   │   ├── MemoryStatus.cs           # NEW: Memory status enum
│   │   │   ├── WorkflowPhase.cs
│   │   │   └── PromptStyle.cs
│   │   ├── Interfaces/
│   │   │   ├── IRepositoryService.cs
│   │   │   ├── IBranchService.cs
│   │   │   ├── IFeatureService.cs
│   │   │   ├── IGitService.cs
│   │   │   ├── ICopilotService.cs
│   │   │   ├── IMemoryService.cs         # NEW: Memory service interface
│   │   │   ├── IMemoryRepository.cs      # NEW: Memory repository interface
│   │   │   ├── IOrchestrationService.cs
│   │   │   └── ISandboxService.cs
│   │   ├── Exceptions/
│   │   │   ├── NotFoundException.cs
│   │   │   ├── ConflictException.cs
│   │   │   └── ValidationException.cs
│   │   └── Configuration/
│   │       ├── MakeAppOptions.cs
│   │       └── MemoryOptions.cs          # NEW: Memory configuration
│   │
│   ├── MakeApp.Application/
│   │   ├── Services/
│   │   │   ├── RepositoryService.cs
│   │   │   ├── BranchService.cs
│   │   │   ├── FeatureService.cs
│   │   │   ├── GitService.cs
│   │   │   ├── CopilotService.cs
│   │   │   ├── MemoryService.cs              # NEW: Memory CRUD
│   │   │   ├── MemoryValidationService.cs    # NEW: Citation validation
│   │   │   ├── MemoryAwarePromptFormatter.cs # NEW: Memory-enhanced prompts
│   │   │   ├── OrchestrationService.cs
│   │   │   ├── MemoryAwareOrchestrationService.cs # NEW: Memory-aware workflows
│   │   │   ├── CopilotConfigService.cs
│   │   │   ├── PromptFormatterService.cs
│   │   │   ├── SandboxService.cs
│   │   │   └── NotificationService.cs
│   │   ├── DTOs/
│   │   │   ├── Features/
│   │   │   ├── Workflows/
│   │   │   ├── Repositories/
│   │   │   ├── Copilot/
│   │   │   └── Memory/                   # NEW: Memory DTOs
│   │   │       ├── MemoryDto.cs
│   │   │       ├── CreateMemoryDto.cs
│   │   │       ├── MemoryValidationResultDto.cs
│   │   │       └── MemoryStatisticsDto.cs
│   │   ├── Mappings/
│   │   │   └── MappingProfile.cs
│   │   └── Validators/
│   │       ├── CreateFeatureValidator.cs
│   │       ├── CreateMemoryValidator.cs  # NEW: Memory validation
│   │       └── StartWorkflowValidator.cs
│   │
│   └── MakeApp.Infrastructure/
│       ├── Copilot/
│       │   ├── CopilotClientManager.cs
│       │   ├── MakeAppTools.cs
│       │   └── MemoryStoreTool.cs        # NEW: store_memory tool
│       ├── Git/
│       │   ├── LibGit2SharpGitService.cs
│       │   └── GitOperations.cs
│       ├── GitHub/
│       │   ├── GitHubService.cs
│       │   └── PullRequestService.cs
│       ├── FileSystem/
│       │   └── FileSystemAdapter.cs
│       └── Persistence/
│           ├── FeatureRepository.cs
│           ├── MemoryRepository.cs       # NEW: Memory persistence
│           └── WorkflowRepository.cs
│
└── tests/
    ├── MakeApp.Api.Tests/
    │   └── Controllers/
    │       └── MemoriesControllerTests.cs  # NEW
    ├── MakeApp.Application.Tests/
    │   └── Services/
    │       ├── MemoryServiceTests.cs       # NEW
    │       └── MemoryValidationServiceTests.cs # NEW
    ├── MakeApp.Infrastructure.Tests/
    │   ├── Git/
    │   ├── Copilot/
    │   │   └── MemoryStoreToolTests.cs     # NEW
    │   └── Persistence/
    │       └── MemoryRepositoryTests.cs    # NEW
    └── MakeApp.E2E.Tests/
```

---

## Timeline Summary

| Phase | Duration | Key Deliverables |
|-------|----------|------------------|
| Phase 1: Foundation | 2 weeks | Project structure, configuration, health endpoints |
| Phase 2: Repository & Branch | 2 weeks | Repository scanning, branch management, git operations |
| Phase 3: Feature Management | 2 weeks | Feature CRUD, import/export, prompt formatting |
| Phase 4: Copilot SDK | 3 weeks | Copilot integration, streaming, custom tools |
| Phase 5: Workflow Orchestration | 3 weeks | Orchestration engine, event streaming, workflow control |
| **Phase 5.5: Agentic Memory** | **2 weeks** | **Memory storage, validation, cross-workflow learning** |
| Phase 6: Config & Sandbox | 2 weeks | Configuration validation, sandbox management |
| Phase 7: Security & Polish | 2 weeks | Authentication, rate limiting, error handling |
| Phase 8: Testing & Deployment | 2 weeks | Tests, Docker, CI/CD, documentation |

**Total Estimated Duration**: 20 weeks (5 months)

---

## Risk Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Copilot SDK breaking changes | High | Pin SDK version, add abstraction layer |
| Long-running workflow timeouts | Medium | Implement checkpointing, retry logic |
| Git operation failures | Medium | Comprehensive error handling, rollback capability |
| Authentication complexity | Medium | Start with API keys, add OAuth incrementally |
| Performance with large repos | Medium | Implement pagination, streaming, caching |
| **Memory system stale data** | **Medium** | **Just-in-time validation, citation verification, 28-day TTL** |
| **Memory privacy concerns** | **Low** | **Repository-scoped isolation, permission checks** |

---

## Success Criteria

1. **Functional Parity**: All PowerShell CLI features available via REST API
2. **Performance**: API response times < 500ms for synchronous operations
3. **Reliability**: 99.5% uptime target, graceful degradation
4. **Scalability**: Support for 100+ concurrent workflow executions
5. **Documentation**: Complete OpenAPI spec with examples
6. **Testing**: > 80% code coverage, E2E test suite
7. **Security**: No critical vulnerabilities, proper authentication
8. **Memory System**: Cross-workflow learning with >80% citation validation accuracy

---

## Agentic Memory Quick Reference

### Key Concepts

| Concept | Description |
|---------|-------------|
| **Memory** | A repository-scoped learning with citations |
| **Citation** | File path + optional line number supporting a fact |
| **Just-in-Time Validation** | Verify citations against current code before use |
| **28-Day TTL** | Memories expire unless refreshed by validation |
| **Cross-Agent** | Memories shared across coding agent, code review, CLI |

### Memory Lifecycle

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│ Discover │───▶│  Store   │───▶│ Validate │───▶│   Use    │
│  Pattern │    │  Memory  │    │ Citations│    │ in Task  │
└──────────┘    └──────────┘    └──────────┘    └────┬─────┘
                                                     │
                     ┌───────────────────────────────┘
                     │
                     ▼
              ┌──────────────┐
              │   Refresh    │ (extends 28-day TTL)
              │   or Expire  │
              └──────────────┘
```

### When Copilot Should Store Memories

- **Coding conventions** discovered during implementation
- **File relationships** (files that must stay synchronized)
- **Configuration patterns** (environment setup, build requirements)
- **Testing patterns** (where tests go, naming conventions)
- **Domain knowledge** (business logic relationships)
- **Deprecated patterns** (what NOT to do)

### Example Memory

```json
{
  "subject": "Database connection handling",
  "fact": "All database connections must use the ConnectionPool from src/db/pool.ts. Direct connections are prohibited.",
  "citations": [
    "src/db/pool.ts:15",
    "src/services/userService.ts:42",
    "docs/database.md:28"
  ],
  "reason": "Direct connections can exhaust database resources. The pool manages connection limits and timeouts."
}
```

### API Quick Reference

```bash
# List memories
GET /api/v1/repos/{owner}/{name}/memories

# Create memory
POST /api/v1/repos/{owner}/{name}/memories
{
  "subject": "...",
  "fact": "...",
  "citations": ["file.ts:10", "other.ts:20"],
  "reason": "..."
}

# Validate memory
POST /api/v1/repos/{owner}/{name}/memories/{id}/validate

# Refresh memory (extend TTL)
POST /api/v1/repos/{owner}/{name}/memories/{id}/refresh

# Get memories citing a file
GET /api/v1/repos/{owner}/{name}/memories/by-file?filePath=src/config.ts

# Prune expired
POST /api/v1/repos/{owner}/{name}/memories/prune
```

---

## Next Steps

1. **Review and approve this plan** with stakeholders
2. **Set up development environment** with .NET 8 SDK
3. **Create GitHub repository** for the API project
4. **Begin Phase 1** implementation
5. **Schedule weekly progress reviews**

---

*Document Version: 1.0*  
*Created: January 17, 2026*  
*Author: GitHub Copilot*
