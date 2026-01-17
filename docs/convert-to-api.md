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

## Testing Standards & Requirements

This section defines the mandatory testing standards that **MUST** be applied to every phase of implementation. These same rules are incorporated into the MakeApp workflow prompts to ensure consistent testing practices across all generated code.

### Core Testing Principles

| Principle | Description |
|-----------|-------------|
| **Test-First Mindset** | Tests should be created alongside or immediately after implementation, never deferred |
| **Phase Gate Requirement** | No phase can be marked complete without passing tests |
| **Coverage Minimum** | Each phase must achieve ≥80% code coverage for new code |
| **Integration Required** | API endpoints must have integration tests, not just unit tests |
| **Automated Execution** | All tests must be runnable via `dotnet test` or equivalent command |

### Testing Pyramid for Each Phase

```
                    ┌─────────────────┐
                    │    E2E Tests    │  ← Full workflow tests (Phase 8 focus)
                    │   (Minimal)     │
                    └────────┬────────┘
                             │
                    ┌────────┴────────┐
                    │  Integration    │  ← API endpoint tests, service interactions
                    │     Tests       │  ← REQUIRED for every phase with endpoints
                    └────────┬────────┘
                             │
           ┌─────────────────┴─────────────────┐
           │           Unit Tests              │  ← Service logic, validators, mappers
           │  (Foundation - REQUIRED EVERY     │  ← REQUIRED for every phase
           │          PHASE)                   │
           └───────────────────────────────────┘
```

### Phase Testing Requirements

Each phase MUST include the following test deliverables:

#### Unit Tests (Required Every Phase)
- **Service Tests**: Every service method must have unit tests
- **Validator Tests**: All DTOs with validation must have validator tests
- **Mapper Tests**: AutoMapper profiles must have mapping tests
- **Mock Dependencies**: Use Moq or NSubstitute for dependency mocking
- **Edge Cases**: Test null inputs, empty collections, boundary values
- **Error Paths**: Test exception throwing and error handling

```csharp
// Example: Required unit test pattern
public class FeatureServiceTests
{
    private readonly Mock<IFeatureRepository> _repositoryMock;
    private readonly Mock<ILogger<FeatureService>> _loggerMock;
    private readonly FeatureService _sut;  // System Under Test

    public FeatureServiceTests()
    {
        _repositoryMock = new Mock<IFeatureRepository>();
        _loggerMock = new Mock<ILogger<FeatureService>>();
        _sut = new FeatureService(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidInput_ReturnsCreatedFeature()
    {
        // Arrange
        var dto = new CreateFeatureDto { Title = "Test" };
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Feature>()))
            .ReturnsAsync((Feature f) => f);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Test");
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Feature>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.CreateAsync(null!));
    }
}
```

#### Integration Tests (Required for Phases with API Endpoints)
- **HTTP Client Tests**: Test actual HTTP request/response cycles
- **Authentication Tests**: Verify auth requirements are enforced
- **Status Code Tests**: Verify correct HTTP status codes (200, 201, 400, 401, 404, 500)
- **Response Body Tests**: Verify response DTOs match expected structure
- **Database Integration**: Test actual repository operations with test database

```csharp
// Example: Required integration test pattern
public class FeaturesControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public FeaturesControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetFeatures_ReturnsOkWithFeaturesList()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/features");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var features = await response.Content.ReadFromJsonAsync<List<FeatureDto>>();
        features.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateFeature_ValidInput_ReturnsCreatedWithLocation()
    {
        // Arrange
        var dto = new CreateFeatureDto { Title = "Test Feature", Description = "Description" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/features", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateFeature_InvalidInput_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateFeatureDto { Title = "" };  // Invalid - empty title

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/features", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFeature_NonExistent_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/features/nonexistent-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
```

### Test Naming Conventions

Follow the pattern: `MethodName_StateUnderTest_ExpectedBehavior`

| Test Type | Naming Pattern | Example |
|-----------|----------------|---------|
| Success Path | `Method_ValidInput_ReturnsExpected` | `CreateFeature_ValidDto_ReturnsCreatedFeature` |
| Error Path | `Method_InvalidCondition_ThrowsException` | `CreateFeature_NullDto_ThrowsArgumentNull` |
| Edge Case | `Method_EdgeCondition_HandlesCorrectly` | `GetFeatures_EmptyList_ReturnsEmptyArray` |
| Integration | `Endpoint_Scenario_ReturnsStatusCode` | `PostFeature_ValidInput_Returns201Created` |

### Phase Completion Checklist

Before ANY phase can be marked complete, verify:

- [ ] All new services have corresponding unit test files
- [ ] Unit test coverage ≥ 80% for new code
- [ ] All new API endpoints have integration tests
- [ ] Integration tests verify success AND error status codes
- [ ] Tests pass locally with `dotnet test`
- [ ] No skipped or ignored tests without documented reason
- [ ] Test assertions use FluentAssertions for readability
- [ ] Mocks verify expected interactions occurred

### Testing Tools & Libraries

| Tool | Purpose | Required In |
|------|---------|-------------|
| **xUnit** | Test framework | All test projects |
| **FluentAssertions** | Readable assertions | All test projects |
| **Moq** | Mocking framework | Unit tests |
| **WebApplicationFactory** | API integration testing | API tests |
| **TestContainers** | Database integration | E2E tests |
| **Coverlet** | Code coverage | CI/CD pipeline |

### Agent Configuration for Testing

The `tester.json` agent configuration must enforce these standards:

```json
{
  "role": "tester",
  "description": "Generates and executes tests for implemented code",
  "responsibilities": [
    "Generate unit tests for all new service methods",
    "Generate integration tests for all new API endpoints",
    "Ensure test coverage meets 80% minimum threshold",
    "Verify both success and error paths are tested",
    "Run test suite and report results"
  ],
  "testingRules": {
    "unitTestsRequired": true,
    "integrationTestsRequired": true,
    "minimumCoverage": 80,
    "namingConvention": "MethodName_StateUnderTest_ExpectedBehavior",
    "requiredAssertions": ["success_path", "error_path", "edge_cases"],
    "frameworks": {
      "testFramework": "xunit",
      "assertions": "FluentAssertions",
      "mocking": "Moq"
    }
  },
  "validationChecks": [
    "All services have test files",
    "All endpoints have integration tests",
    "Coverage report shows ≥80%",
    "No tests are skipped without reason",
    "Tests pass with dotnet test command"
  ]
}
```

### MakeApp Workflow Prompt Integration

The testing rules above **MUST** be incorporated into all prompts generated by MakeApp for feature implementation. The `Format-CopilotPrompt` function and agent configurations must include these testing requirements so that:

1. Every code generation task includes corresponding test generation
2. The tester agent validates tests meet the standards above
3. Phase completion requires test validation to pass
4. PR descriptions include test coverage summary

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

### Sandbox & Repository Architecture

All repositories created or checked out by MakeApp reside in a single **sandbox folder**. Each repository contains its own `.makeapp/` folder for workflow data:

```
sandbox/                          # Configured sandbox root folder
├── app-one/                      # First repo (created or cloned)
│   ├── .makeapp/                 # MakeApp workflow data (per-repo)
│   │   ├── plan.json             # ✅ Committed - implementation plan
│   │   ├── status.json           # ✅ Committed - workflow state
│   │   ├── agents/               # ✅ Committed - agent configs
│   │   ├── cache/                # ❌ Gitignored - LLM response cache
│   │   ├── logs/                 # ❌ Gitignored - execution logs
│   │   └── temp/                 # ❌ Gitignored - temporary files
│   ├── .github/copilot-instructions.md
│   ├── .gitignore                # Excludes .makeapp/cache, logs, temp
│   └── src/...
│
├── app-two/                      # Another repo
│   └── .makeapp/...              # Its own isolated workflow data
│
└── existing-checkout/            # Cloned existing repo
    └── .makeapp/...              # MakeApp data added when features added
```

**Key Principles:**
1. **All repos in one sandbox folder** - Easy to manage, consistent location
2. **Per-repo working directories** - Each repo's cache/logs/temp isolated inside `.makeapp/`
3. **Proper .gitignore** - Temporary data excluded, plan/status/agents committed
4. **No global working folders** - No sandbox-level cache/logs/temp folders

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
│  │  │ Apps       │ │ Features     │ │ Repos    │ │ Workflows        │  │   │
│  │  │ Controller │ │ Controller   │ │ Ctrl     │ │ Controller       │  │   │
│  │  └────────────┘ └──────────────┘ └──────────┘ └──────────────────┘  │   │
│  │  ┌────────────┐ ┌──────────────┐ ┌──────────┐ ┌──────────────────┐  │   │
│  │  │ Sandbox    │ │ Branches     │ │ Sessions │ │ Health           │  │   │
│  │  │ Controller │ │ Controller   │ │ Ctrl     │ │ Controller       │  │   │
│  │  └────────────┘ └──────────────┘ └──────────┘ └──────────────────┘  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                         SERVICE LAYER                                │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                 │   │
│  │  │ App          │ │ RepoCreation │ │ Feature      │                 │   │
│  │  │ Service      │ │ Service      │ │ Service      │                 │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘                 │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                 │   │
│  │  │ PlanGen      │ │ PhasedExec   │ │ Git          │                 │   │
│  │  │ Service      │ │ Service      │ │ Service      │                 │   │
│  │  └──────────────┘ └──────────────┘ └──────────────┘                 │   │
│  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                 │   │
│  │  │ Sandbox      │ │ Orchestration│ │ Notification │                 │   │
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

### Documentation & Discovery

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/swagger` | Swagger UI - Interactive API documentation |
| GET | `/swagger/index.html` | Swagger UI (explicit path) |
| GET | `/api-docs/v1/swagger.json` | OpenAPI 3.0 specification (JSON format) |
| GET | `/health` | Health check endpoint |
| GET | `/health/ready` | Readiness probe for container orchestration |
| GET | `/health/live` | Liveness probe for container orchestration |

### Client Configuration

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/config/user` | Get current GitHub user/owner configuration |
| PUT | `/api/v1/config/user` | Configure GitHub user/owner identity |
| GET | `/api/v1/config/user/validate` | Validate GitHub credentials and permissions |

### Core App Lifecycle Endpoints

These are the two primary workflow endpoints that drive the entire system:

| Method | Endpoint | Description |
|--------|----------|-------------|
| **POST** | **`/api/v1/apps`** | **Create a new app from requirements** |
| **POST** | **`/api/v1/apps/{owner}/{name}/features`** | **Add a feature to an existing app** |
| GET | `/api/v1/apps` | List all apps managed by MakeApp |
| GET | `/api/v1/apps/{owner}/{name}` | Get app details and current status |
| GET | `/api/v1/apps/{owner}/{name}/plan` | Get the implementation plan |
| GET | `/api/v1/apps/{owner}/{name}/phases` | Get phase completion status |
| GET | `/api/v1/apps/{owner}/{name}/events` | Stream workflow events (SSE) |
| POST | `/api/v1/apps/{owner}/{name}/abort` | Abort current workflow |

### Repository Management

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/repos` | List available repositories |
| GET | `/api/v1/repos/{owner}/{name}` | Get repository details |
| POST | `/api/v1/repos` | Create a new repository on GitHub |
| POST | `/api/v1/repos/scan` | Scan a folder for repositories |
| GET | `/api/v1/repos/{owner}/{name}/config` | Get repository configuration status |
| PUT | `/api/v1/repos/{owner}/{name}/config/copilot-instructions` | Update Copilot instructions |
| PUT | `/api/v1/repos/{owner}/{name}/config/mcp` | Update MCP configuration |
| PUT | `/api/v1/repos/{owner}/{name}/config/agents` | Update agent orchestration configuration |

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
| GET | `/api/v1/sandbox` | Get sandbox info (path, repo count, total size) |
| GET | `/api/v1/sandbox/repos` | List all repos in sandbox with status summary |
| GET | `/api/v1/sandbox/repos/{name}` | Get detailed repo info including git status |
| GET | `/api/v1/sandbox/repos/{name}/status` | Get detailed pending changes status |
| DELETE | `/api/v1/sandbox/repos/{name}` | Remove repo from sandbox (requires clean status or force) |
| DELETE | `/api/v1/sandbox/repos/{name}?force=true` | Force remove repo regardless of pending changes |
| POST | `/api/v1/sandbox/repos/{name}/cleanup` | Clean working files (cache/logs/temp) for a repo |
| POST | `/api/v1/sandbox/cleanup` | Clean working files for all repos |

#### Repository Status Response

The status endpoint returns detailed information about pending changes:

```json
{
  "repoName": "my-app",
  "path": "/sandbox/my-app",
  "currentBranch": "feature/add-auth",
  "status": "has-pending-changes",  // "clean" | "has-pending-changes"
  "canRemove": false,
  "pendingChanges": {
    "hasUnstagedChanges": true,
    "hasStagedChanges": false,
    "hasUnpushedCommits": true,
    "unstagedFiles": [
      { "path": "src/auth.ts", "status": "modified" },
      { "path": "src/config.ts", "status": "added" }
    ],
    "stagedFiles": [],
    "unpushedCommits": [
      { "hash": "a1b2c3d", "message": "Add authentication module", "date": "2026-01-17T10:30:00Z" }
    ]
  },
  "summary": "2 unstaged files, 1 unpushed commit"
}
```

#### Remove Repository Logic

```
DELETE /api/v1/sandbox/repos/{name}?force={true|false}

┌─────────────────────────────────────────────────────────────┐
│                    REMOVE REPO FLOW                          │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  1. Check repo exists in sandbox                             │
│     └─ 404 if not found                                      │
│                                                              │
│  2. If force=false (default):                                │
│     ├─ Get repo status                                       │
│     ├─ Check for unstaged changes     ──▶ 409 Conflict      │
│     ├─ Check for staged changes       ──▶ 409 Conflict      │
│     └─ Check for unpushed commits     ──▶ 409 Conflict      │
│                                                              │
│  3. If force=true OR status is clean:                        │
│     ├─ Delete repo folder recursively                        │
│     ├─ Remove from any active workflows                      │
│     └─ Return 204 No Content                                 │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Configuration

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/config` | Get current configuration |
| PUT | `/api/v1/config` | Update configuration |
| GET | `/api/v1/config/defaults` | Get default configuration |

---

## Core Workflows: Create App & Add Feature

This section defines the two primary workflows that drive the MakeApp API. Both workflows follow a similar pattern but differ in their initialization phase.

### Workflow Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        CREATE APP / ADD FEATURE WORKFLOW                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────┐    ┌────────────────┐    ┌────────────────┐            │
│  │   1. INIT      │───▶│   2. PLAN      │───▶│   3. SETUP     │            │
│  │  (Repo Setup)  │    │ (LLM Planning) │    │ (Agent Config) │            │
│  └────────────────┘    └────────────────┘    └────────────────┘            │
│                                                     │                        │
│                                                     ▼                        │
│  ┌────────────────────────────────────────────────────────────────────┐    │
│  │                     4. PHASED IMPLEMENTATION LOOP                   │    │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ │    │
│  │  │  PHASE   │▶│  CODE    │▶│  TEST    │▶│  REVIEW  │▶│ COMMIT   │ │    │
│  │  │   N      │ │ (Coder)  │ │ (Tester) │ │(Reviewer)│ │ & PUSH   │ │    │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘ │    │
│  │       │                                                     │       │    │
│  │       │◀────────────────────────────────────────────────────┘       │    │
│  │       │         (Loop until all phases complete)                    │    │
│  └───────┼─────────────────────────────────────────────────────────────┘    │
│          │                                                                   │
│          ▼                                                                   │
│  ┌────────────────┐                                                         │
│  │ 5. FINALIZE    │ (PR Creation, Status Update)                           │
│  └────────────────┘                                                         │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

### POST `/api/v1/apps` - Create a New App

Creates a brand new application from minimal input parameters.

#### Request Body

```json
{
  "name": "my-awesome-app",
  "requirements": "A REST API for managing a todo list with user authentication, PostgreSQL storage, and real-time notifications via WebSockets",
  "projectType": "node",           // Optional: node, python, dotnet, go, etc.
  "createPullRequest": true,       // Optional: default true
  "notifyOnComplete": true         // Optional: webhook/notification
}
```

#### Workflow Steps

##### Step 1: Repository Initialization

| Action | Description | Files Created/Modified |
|--------|-------------|------------------------|
| Create GitHub repo | Use Octokit to create new repository | - |
| Clone locally | Clone to workspace folder | `.git/` |
| Initialize main branch | Set up default branch | - |
| Generate `.gitignore` | Based on projectType, use LLM for requirements-specific exclusions | `.gitignore` |
| Generate `README.md` | LLM creates comprehensive README from requirements | `README.md` |
| Initial commit | "Initial commit: Project scaffold" | - |
| Push main | Push to origin | - |
| Create creation branch | `git checkout -b creation` | - |

##### Step 2: LLM Plan Generation

The LLM analyzes requirements and generates a comprehensive phased implementation plan:

```json
{
  "planId": "plan_abc123",
  "totalPhases": 5,
  "estimatedDuration": "4-6 hours",
  "phases": [
    {
      "phase": 1,
      "name": "Project Foundation",
      "description": "Set up project structure, dependencies, and configuration",
      "status": "not-started",
      "tasks": [
        {
          "id": "1.1",
          "description": "Initialize package.json with dependencies",
          "files": ["package.json"],
          "agentRole": "coder"
        },
        {
          "id": "1.2", 
          "description": "Create TypeScript configuration",
          "files": ["tsconfig.json"],
          "agentRole": "coder"
        }
      ],
      "acceptanceCriteria": [
        "npm install completes without errors",
        "TypeScript compilation succeeds"
      ],
      "dependencies": []
    },
    {
      "phase": 2,
      "name": "Database Layer",
      "description": "PostgreSQL schema, migrations, and connection pooling",
      "status": "not-started",
      "tasks": [...],
      "acceptanceCriteria": [...],
      "dependencies": [1]
    }
  ]
}
```

**Files Created:**
| File | Purpose |
|------|---------|
| `.makeapp/plan.json` | Machine-readable implementation plan |
| `.makeapp/plan.md` | Human-readable plan with status checkboxes |
| `.makeapp/status.json` | Current workflow state and progress |

##### Step 3: Agent Orchestration Setup

Create agent configuration files that define how different AI agents collaborate:

**Files Created:**

| File | Purpose |
|------|---------|
| `.makeapp/agents/orchestrator.json` | Main coordination agent configuration |
| `.makeapp/agents/coder.json` | Code generation agent configuration |
| `.makeapp/agents/tester.json` | Test generation and validation agent |
| `.makeapp/agents/reviewer.json` | Code review and approval agent |
| `.github/copilot-instructions.md` | Targeted instructions based on requirements |

**orchestrator.json:**
```json
{
  "role": "orchestrator",
  "description": "Coordinates all agents and manages phase progression",
  "responsibilities": [
    "Monitor phase completion status",
    "Trigger appropriate agents for each task",
    "Ensure acceptance criteria are met before phase advancement",
    "Update plan status after each task completion",
    "Handle failures and retry logic"
  ],
  "phaseCriteria": {
    "requireAllTasksComplete": true,
    "requireTestsPassing": true,
    "requireReviewApproval": true
  }
}
```

**coder.json:**
```json
{
  "role": "coder",
  "description": "Generates and modifies code files",
  "constraints": [
    "Follow patterns established in copilot-instructions.md",
    "Maintain consistency with existing codebase",
    "Include inline comments for complex logic",
    "Generate corresponding test stubs"
  ],
  "outputRequirements": {
    "mustInclude": ["implementation", "imports"],
    "mustValidate": ["syntax", "types"]
  }
}
```

**tester.json:**
```json
{
  "role": "tester",
  "description": "Generates and executes tests for implemented code",
  "responsibilities": [
    "Generate unit tests for all new service methods",
    "Generate integration tests for all new API endpoints",
    "Ensure test coverage meets 80% minimum threshold",
    "Verify both success and error paths are tested",
    "Run test suite and report results with coverage metrics",
    "Validate all acceptance criteria via tests"
  ],
  "testingRules": {
    "unitTestsRequired": true,
    "integrationTestsRequired": true,
    "minimumCoverage": 80,
    "namingConvention": "MethodName_StateUnderTest_ExpectedBehavior",
    "requiredTestPaths": ["success_path", "error_path", "edge_cases"],
    "frameworks": {
      "testFramework": "xunit",
      "assertions": "FluentAssertions",
      "mocking": "Moq"
    }
  },
  "validationChecks": [
    "All services have corresponding test files",
    "All API endpoints have integration tests",
    "Coverage report shows ≥80% for new code",
    "No tests are skipped without documented reason",
    "All tests pass with dotnet test command"
  ],
  "testStructureTemplate": {
    "arrange": "Set up test data and mocks",
    "act": "Call the method under test",
    "assert": "Verify expected outcomes"
  }
}
```

**reviewer.json:**
```json
{
  "role": "reviewer",
  "description": "Reviews code quality and approves for commit",
  "checkpoints": [
    "Code follows project conventions",
    "No security vulnerabilities introduced",
    "Performance considerations addressed",
    "Documentation updated",
    "Tests are meaningful and pass"
  ],
  "approvalRequired": true
}
```

**Initial copilot-instructions.md (LLM-generated based on requirements):**
```markdown
# GitHub Copilot Instructions

## Project Overview
A REST API for managing a todo list with user authentication, 
PostgreSQL storage, and real-time notifications via WebSockets.

## Architecture Decisions
- Express.js for HTTP server
- TypeScript for type safety
- PostgreSQL with pg-pool for database
- Socket.io for WebSocket support
- JWT for authentication

## Code Style Guidelines
### TypeScript Conventions
- Use strict mode
- Prefer interfaces over types for object shapes
- Use async/await, never callbacks
- [Additional project-specific guidelines...]

## File Organization
- src/routes/ - API route handlers
- src/services/ - Business logic
- src/models/ - Database models
- src/middleware/ - Express middleware
- src/websocket/ - Socket.io handlers
- tests/unit/ - Unit tests
- tests/integration/ - Integration tests

## Testing Standards (MANDATORY)
### Test Requirements
- All new services MUST have corresponding unit tests
- All API endpoints MUST have integration tests
- Minimum 80% code coverage for new code
- No phase can be marked complete without passing tests

### Test Naming Convention
- Pattern: `MethodName_StateUnderTest_ExpectedBehavior`
- Example: `CreateUser_ValidInput_ReturnsCreatedUser`

### Test Structure (Arrange-Act-Assert)
```typescript
describe('ServiceName', () => {
  it('should do X when Y', async () => {
    // Arrange - set up test data and mocks
    // Act - call the method under test
    // Assert - verify expected outcomes
  });
});
```

### Required Test Coverage
- Success path (valid inputs, expected flow)
- Error path (invalid inputs, exceptions)
- Edge cases (null, empty, boundary values)

## Current Phase: [PHASE_NAME]
[Dynamically updated as implementation progresses]
```

##### Step 4: Phased Implementation Loop

For each phase, the following sequence executes:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          PHASE N EXECUTION                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  4.1 ORCHESTRATOR: Load phase tasks from plan.json                          │
│          │                                                                   │
│          ▼                                                                   │
│  4.2 FOR EACH TASK:                                                          │
│      ┌────────────────────────────────────────────────────────────────┐     │
│      │                                                                 │     │
│      │  a) CODER AGENT                                                │     │
│      │     - Read task requirements                                   │     │
│      │     - Generate/modify files                                    │     │
│      │     - Update imports and dependencies                          │     │
│      │                    │                                           │     │
│      │                    ▼                                           │     │
│      │  b) TESTER AGENT                                               │     │
│      │     - Generate tests for new code                              │     │
│      │     - Run test suite                                           │     │
│      │     - Report: PASS or FAIL with details                        │     │
│      │                    │                                           │     │
│      │          ┌────────┴────────┐                                  │     │
│      │          │                  │                                  │     │
│      │       PASS               FAIL                                  │     │
│      │          │                  │                                  │     │
│      │          ▼                  ▼                                  │     │
│      │  c) REVIEWER AGENT    Retry with                               │     │
│      │     - Check quality    CODER (max 3x)                          │     │
│      │     - Verify criteria                                          │     │
│      │     - APPROVE/REJECT                                           │     │
│      │          │                                                     │     │
│      │          ▼                                                     │     │
│      │  d) COMMIT & UPDATE                                            │     │
│      │     - git add .                                                │     │
│      │     - git commit -m "Phase N.task: description"                │     │
│      │     - Update plan.json status                                  │     │
│      │     - Update copilot-instructions.md if needed                 │     │
│      │                                                                 │     │
│      └────────────────────────────────────────────────────────────────┘     │
│          │                                                                   │
│          ▼                                                                   │
│  4.3 PHASE COMPLETION CHECK                                                  │
│      - All tasks complete?                                                   │
│      - All acceptance criteria met?                                          │
│      - All tests passing?                                                    │
│          │                                                                   │
│      ┌───┴───┐                                                              │
│      │       │                                                              │
│     YES     NO ──▶ Continue tasks or handle failure                         │
│      │                                                                       │
│      ▼                                                                       │
│  4.4 PHASE FINALIZATION                                                      │
│      - Update plan.json: phase.status = "completed"                          │
│      - Update copilot-instructions.md with learnings                         │
│      - git commit -m "Complete Phase N: [name]"                              │
│      - git push origin creation                                              │
│          │                                                                   │
│          ▼                                                                   │
│  4.5 ADVANCE TO NEXT PHASE (or finalize if last)                            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Files Modified During Implementation:**
| File | When Modified |
|------|---------------|
| `.makeapp/plan.json` | After each task completion |
| `.makeapp/plan.md` | After each task completion (checkboxes) |
| `.makeapp/status.json` | Continuously during execution |
| `.github/copilot-instructions.md` | When patterns/learnings discovered |
| `[source files]` | During coding tasks |
| `[test files]` | During testing tasks |

##### Step 5: Finalization

| Action | Description |
|--------|-------------|
| Final push | Ensure all changes pushed to creation branch |
| Create PR | PR from `creation` → `main` with full summary |
| Update status | Mark workflow as complete in status.json |
| Store memories | Save learnings to memory system |
| Notify | Send completion notification |

#### Response

```json
{
  "appId": "app_xyz789",
  "name": "my-awesome-app",
  "owner": "configured-user",
  "repositoryUrl": "https://github.com/configured-user/my-awesome-app",
  "workflowId": "wf_abc123",
  "status": "in-progress",
  "currentPhase": 1,
  "totalPhases": 5,
  "eventsUrl": "/api/v1/apps/configured-user/my-awesome-app/events"
}
```

---

### POST `/api/v1/apps/{owner}/{name}/features` - Add Feature to Existing App

Adds a new feature to an existing repository that was previously created or is already checked out.

#### Request Body

```json
{
  "name": "email-notifications",
  "requirements": "Add email notification support for todo item reminders using SendGrid. Users should be able to set reminder times and receive emails 15 minutes before due dates.",
  "baseBranch": "main",            // Optional: branch to create feature from
  "createPullRequest": true
}
```

#### Key Differences from Create App

| Aspect | Create App | Add Feature |
|--------|-----------|-------------|
| Repository | Created new | Already exists |
| Initial files | Generated from scratch | Analyzed for context |
| Branch name | `creation` | `feature/{formatted-name}` |
| copilot-instructions.md | Created new | Updated/appended |
| Plan scope | Full application | Feature-specific |

#### Workflow Steps

##### Step 1: Repository Analysis & Branch Setup

| Action | Description |
|--------|-------------|
| Verify repo exists | Confirm repo is accessible |
| Analyze codebase | LLM reads existing structure, patterns, dependencies |
| Load existing memories | Retrieve repository memories for context |
| Read copilot-instructions.md | Understand existing conventions |
| Create feature branch | `git checkout -b feature/email-notifications` |

##### Step 2: Feature Plan Generation

LLM generates a feature-specific plan that integrates with existing code:

```json
{
  "featureId": "feat_email_notify",
  "basedOn": {
    "existingServices": ["src/services/todoService.ts"],
    "existingModels": ["src/models/todo.ts", "src/models/user.ts"],
    "existingPatterns": ["async/await", "repository pattern"]
  },
  "phases": [
    {
      "phase": 1,
      "name": "Email Service Integration",
      "tasks": [
        {
          "id": "1.1",
          "description": "Add SendGrid dependency and configuration",
          "files": ["package.json", "src/config/email.ts"],
          "integrationPoints": ["src/config/index.ts"]
        }
      ]
    }
  ]
}
```

##### Steps 3-5: Same as Create App

Agent setup, phased implementation, and finalization follow the same pattern.

**Branch naming convention:**
```
feature/{name-slug}-{YYYYMMDD}-{HHMMSS}
```
Example: `feature/email-notifications-20260117-143052`

---

### Complete File/Folder Structure Created

All repositories are created within the configured **sandbox folder**. Each repo contains its own MakeApp working directories to keep temporary data isolated and properly excluded from version control.

**Sandbox Folder Structure:**
```
sandbox/                              # Configured sandbox root (all repos go here)
├── my-awesome-app/                   # Created app repository
│   ├── .git/
│   ├── .github/
│   │   └── copilot-instructions.md   # LLM-generated, dynamically updated
│   ├── .makeapp/                     # MakeApp workflow data (in repo)
│   │   ├── plan.json                 # Machine-readable implementation plan
│   │   ├── plan.md                   # Human-readable plan with checkboxes
│   │   ├── status.json               # Current workflow state
│   │   ├── memories.json             # Local memory cache (syncs with API)
│   │   ├── cache/                    # LLM response cache, API data (repo-specific)
│   │   ├── logs/                     # Workflow execution logs (repo-specific)
│   │   ├── temp/                     # Temporary files during operations (repo-specific)
│   │   └── agents/
│   │       ├── orchestrator.json     # Orchestrator agent config
│   │       ├── coder.json            # Coder agent config
│   │       ├── tester.json           # Tester agent config
│   │       └── reviewer.json         # Reviewer agent config
│   ├── .gitignore                    # Excludes .makeapp working folders
│   ├── README.md                     # LLM-generated from requirements
│   ├── package.json                  # (or equivalent for project type)
│   ├── tsconfig.json                 # (TypeScript projects)
│   ├── src/
│   │   ├── index.ts                  # Entry point
│   │   ├── config/
│   │   ├── routes/
│   │   ├── services/
│   │   ├── models/
│   │   ├── middleware/
│   │   └── utils/
│   └── tests/
│       ├── unit/
│       └── integration/
│
├── another-app/                      # Another created/checked-out repo
│   ├── .makeapp/                     # Its own isolated MakeApp data
│   │   ├── cache/
│   │   ├── logs/
│   │   └── ...
│   └── ...
```

**Required .gitignore Entries:**

Every repository created by MakeApp must include these exclusions in `.gitignore`:

```gitignore
# MakeApp working directories (temporary/cache data)
.makeapp/cache/
.makeapp/logs/
.makeapp/temp/

# MakeApp files to INCLUDE in version control:
# - .makeapp/plan.json (implementation plan)
# - .makeapp/plan.md (human-readable plan)
# - .makeapp/status.json (workflow state - for recovery)
# - .makeapp/agents/*.json (agent configurations)
# - .makeapp/memories.json (optional - depends on team preference)
```

**Folder Purposes:**

| Folder | Purpose | Gitignored? |
|--------|---------|-------------|
| `.makeapp/cache/` | LLM response caching, API response caching to reduce costs/latency | ✅ Yes |
| `.makeapp/logs/` | Detailed workflow execution logs, agent conversation history | ✅ Yes |
| `.makeapp/temp/` | Temporary files during code generation, diff staging | ✅ Yes |
| `.makeapp/agents/` | Agent configuration files (orchestrator, coder, tester, reviewer) | ❌ No - commit these |
| `.makeapp/plan.json` | Machine-readable implementation plan with phase/task status | ❌ No - commit this |
| `.makeapp/plan.md` | Human-readable plan with progress checkboxes | ❌ No - commit this |
| `.makeapp/status.json` | Current workflow state for pause/resume capability | ❌ No - commit this |
| `.makeapp/memories.json` | Local memory cache (team decides if shared) | ⚠️ Optional |

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

- [ ] Create new .NET 8 Web API solution named **`makeapp_api`**:
  ```
  makeapp_api/
  ├── makeapp_api.sln
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

> **Note**: The solution folder is named `makeapp_api` while internal C# projects follow .NET PascalCase naming conventions (e.g., `MakeApp.Api`, `MakeApp.Core`).

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

// NEW: User/Owner configuration for client applications
public class UserConfiguration
{
    public string GitHubUsername { get; set; } = "";
    public string GitHubOwner { get; set; } = "";  // Can be user or organization
    public string DefaultBranch { get; set; } = "main";
    public bool AutoCreateRemote { get; set; } = true;
    public string SandboxPath { get; set; } = "";  // All repos created/checked out here
}

public class FolderOptions
{
    public string Sandbox { get; set; } = "";     // Root folder for all repos
    public string Repos { get; set; } = "";       // Alias for Sandbox (backward compat)
}

// Per-repo working directories (stored INSIDE each repo's .makeapp folder)
// These are NOT configured globally - they're always at {repoPath}/.makeapp/{folder}
// - cache/  - LLM response caching, API data
// - logs/   - Workflow execution logs  
// - temp/   - Temporary files during operations

// etc...
```

- [ ] Define configuration DTOs mirroring defaults.json structure
- [ ] Implement configuration service with layered loading:
  - defaults.json → user config → environment variables → API parameters
- [ ] Add configuration validation
- [ ] Create configuration endpoints
- [ ] **NEW**: Add user/owner configuration endpoint for client apps

#### 1.3 Health & Diagnostics

- [ ] Implement health check endpoints
- [ ] Add OpenAPI/Swagger documentation
- [ ] Set up API versioning

**Deliverables**:
- Working API with `/health`, `/api/v1/config` endpoints
- **NEW**: `/api/v1/config/user` endpoint for GitHub user configuration
- OpenAPI documentation at `/swagger`
- Comprehensive configuration system

**Testing Deliverables (Phase 1):**
- [ ] Unit tests for configuration service (loading, merging, validation)
- [ ] Unit tests for options validation
- [ ] Integration tests for `/health` endpoint
- [ ] Integration tests for `/api/v1/config` endpoints (GET, PUT)
- [ ] Integration tests for `/api/v1/config/user` endpoint
- [ ] Test coverage report showing ≥80% for new code

---

### Phase 1.5: App Creation Infrastructure (NEW - Weeks 2-3)

**Objective**: Implement the core infrastructure for creating new applications and adding features.

#### 1.5.1 Repository Creation Service

```csharp
// MakeApp.Application/Services/RepositoryCreationService.cs
public interface IRepositoryCreationService
{
    Task<CreateAppResult> CreateAppAsync(CreateAppRequest request);
    Task<RepositoryInfo> CreateGitHubRepositoryAsync(CreateRepoOptions options);
    Task<string> InitializeLocalRepositoryAsync(string repoPath, InitOptions options);
    Task GenerateInitialFilesAsync(string repoPath, AppRequirements requirements);
    Task InitializeMakeAppFoldersAsync(string repoPath);
}

public class RepositoryCreationService : IRepositoryCreationService
{
    private readonly IGitHubService _gitHubService;
    private readonly IGitService _gitService;
    private readonly ILlmService _llmService;
    private readonly IFileGeneratorService _fileGenerator;
    private readonly UserConfiguration _userConfig;
    
    public async Task<CreateAppResult> CreateAppAsync(CreateAppRequest request)
    {
        // 1. Create GitHub repository
        var repoInfo = await _gitHubService.CreateRepositoryAsync(new CreateRepoOptions
        {
            Name = request.Name,
            Owner = request.Owner ?? _userConfig.GitHubOwner,
            Description = ExtractDescription(request.Requirements),
            Private = request.IsPrivate,
            AutoInit = false  // We'll initialize locally
        });
        
        // 2. Clone to SANDBOX folder (all repos go here)
        var localPath = Path.Combine(_userConfig.SandboxPath, request.Name);
        await _gitService.CloneAsync(repoInfo.CloneUrl, localPath);
        
        // 3. Initialize .makeapp working directories INSIDE the repo
        await InitializeMakeAppFoldersAsync(localPath);
        
        // 4. Generate initial files based on requirements (including .gitignore)
        await GenerateInitialFilesAsync(localPath, new AppRequirements
        {
            Name = request.Name,
            Requirements = request.Requirements,
            ProjectType = request.ProjectType ?? await _llmService.DetectProjectTypeAsync(request.Requirements)
        });
        
        // 5. Initial commit and push
        await _gitService.StageAllAsync(localPath);
        await _gitService.CommitAsync(localPath, "Initial commit: Project scaffold");
        await _gitService.PushAsync(localPath, "main", setUpstream: true);
        
        // 6. Create creation branch
        await _gitService.CreateBranchAsync(localPath, "creation");
        await _gitService.CheckoutAsync(localPath, "creation");
        
        return new CreateAppResult
        {
            RepositoryInfo = repoInfo,
            LocalPath = localPath,
            CurrentBranch = "creation"
        };
    }
    
    public async Task InitializeMakeAppFoldersAsync(string repoPath)
    {
        // Create .makeapp directory structure INSIDE the repo
        var makeappDir = Path.Combine(repoPath, ".makeapp");
        var agentsDir = Path.Combine(makeappDir, "agents");
        var cacheDir = Path.Combine(makeappDir, "cache");
        var logsDir = Path.Combine(makeappDir, "logs");
        var tempDir = Path.Combine(makeappDir, "temp");
        
        _fileSystem.Directory.CreateDirectory(makeappDir);
        _fileSystem.Directory.CreateDirectory(agentsDir);
        _fileSystem.Directory.CreateDirectory(cacheDir);
        _fileSystem.Directory.CreateDirectory(logsDir);
        _fileSystem.Directory.CreateDirectory(tempDir);
        
        // Create .gitkeep files so empty dirs are tracked (except gitignored ones)
        await _fileSystem.File.WriteAllTextAsync(
            Path.Combine(agentsDir, ".gitkeep"), "");
    }
    
    public async Task GenerateInitialFilesAsync(string repoPath, AppRequirements requirements)
    {
        // Generate .gitignore with MakeApp exclusions + project-type specific
        var gitignoreContent = await _llmService.GenerateGitignoreAsync(
            requirements.ProjectType, 
            requirements.Requirements);
        
        // ALWAYS append MakeApp working directory exclusions
        gitignoreContent += """
            
            # MakeApp working directories (temporary/cache data - do not commit)
            .makeapp/cache/
            .makeapp/logs/
            .makeapp/temp/
            """;
        
        await File.WriteAllTextAsync(
            Path.Combine(repoPath, ".gitignore"), 
            gitignoreContent);
        
        // Generate README.md
        var readmeContent = await _llmService.GenerateReadmeAsync(requirements);
        await File.WriteAllTextAsync(
            Path.Combine(repoPath, "README.md"), 
            readmeContent);
    }
}
```

#### 1.5.2 LLM Plan Generator Service

```csharp
// MakeApp.Application/Services/PlanGeneratorService.cs
public interface IPlanGeneratorService
{
    Task<ImplementationPlan> GeneratePlanAsync(string repoPath, PlanRequest request);
    Task<ImplementationPlan> UpdatePlanStatusAsync(string repoPath, string taskId, TaskStatus status);
    Task<ImplementationPlan> GetCurrentPlanAsync(string repoPath);
    Task SavePlanAsync(string repoPath, ImplementationPlan plan);
}

public class PlanGeneratorService : IPlanGeneratorService
{
    private readonly ICopilotService _copilotService;
    private readonly IFileSystem _fileSystem;
    
    public async Task<ImplementationPlan> GeneratePlanAsync(string repoPath, PlanRequest request)
    {
        // Build comprehensive prompt for plan generation
        var prompt = BuildPlanGenerationPrompt(request);
        
        // Use Copilot to generate structured plan
        var response = await _copilotService.SendStructuredMessageAsync<ImplementationPlan>(
            sessionId: await GetOrCreateSessionAsync(repoPath),
            prompt: prompt,
            schema: ImplementationPlanSchema.JsonSchema);
        
        var plan = response.Data;
        plan.Id = $"plan_{Guid.NewGuid():N}"[..12];
        plan.CreatedAt = DateTime.UtcNow;
        plan.Status = PlanStatus.Active;
        
        // Save plan files
        await SavePlanFilesAsync(repoPath, plan);
        
        return plan;
    }
    
    private async Task SavePlanFilesAsync(string repoPath, ImplementationPlan plan)
    {
        // All MakeApp data goes INSIDE the repo at .makeapp/
        var makeappDir = Path.Combine(repoPath, ".makeapp");
        _fileSystem.Directory.CreateDirectory(makeappDir);
        
        // Save JSON version (committed to repo)
        var jsonPath = Path.Combine(makeappDir, "plan.json");
        await _fileSystem.File.WriteAllTextAsync(
            jsonPath, 
            JsonSerializer.Serialize(plan, JsonOptions.Indented));
        
        // Save Markdown version with checkboxes (committed to repo)
        var mdPath = Path.Combine(makeappDir, "plan.md");
        await _fileSystem.File.WriteAllTextAsync(mdPath, FormatPlanAsMarkdown(plan));
        
        // Initialize status file (committed to repo for pause/resume)
        var statusPath = Path.Combine(makeappDir, "status.json");
        await _fileSystem.File.WriteAllTextAsync(
            statusPath,
            JsonSerializer.Serialize(new WorkflowStatus
            {
                CurrentPhase = 1,
                CurrentTask = plan.Phases[0].Tasks[0].Id,
                StartedAt = DateTime.UtcNow,
                Status = "in-progress"
            }));
        
        // Log to repo-specific logs folder (gitignored)
        var logsDir = Path.Combine(makeappDir, "logs");
        _fileSystem.Directory.CreateDirectory(logsDir);
        await _fileSystem.File.AppendAllTextAsync(
            Path.Combine(logsDir, "workflow.log"),
            $"[{DateTime.UtcNow:O}] Plan generated with {plan.Phases.Count} phases\n");
    }
    
    private string BuildPlanGenerationPrompt(PlanRequest request)
    {
        return $"""
            Analyze the following requirements and generate a comprehensive, 
            phased implementation plan. Each phase should be completable 
            independently and build upon previous phases.
            
            ## Requirements
            {request.Requirements}
            
            ## Project Type
            {request.ProjectType}
            
            ## Existing Context (if any)
            {request.ExistingContext}
            
            ## Instructions
            1. Break down into 3-7 logical phases
            2. Each phase should have clear acceptance criteria
            3. Each task should specify which files to create/modify
            4. Consider dependencies between phases
            5. Include testing requirements for each phase
            6. Estimate complexity (simple/moderate/complex) for each task
            
            Return a structured JSON plan following the ImplementationPlan schema.
            """;
    }
}
```

#### 1.5.3 Agent Configuration Service

```csharp
// MakeApp.Application/Services/AgentConfigurationService.cs
public interface IAgentConfigurationService
{
    Task<AgentConfiguration> CreateAgentConfigAsync(string repoPath, AgentConfigRequest request);
    Task<AgentConfiguration> GetAgentConfigAsync(string repoPath);
    Task UpdateAgentConfigAsync(string repoPath, string agentRole, AgentSettings settings);
    Task<string> GenerateCopilotInstructionsAsync(string repoPath, InstructionsRequest request);
    Task UpdateCopilotInstructionsAsync(string repoPath, string sectionToUpdate, string newContent);
}

public class AgentConfigurationService : IAgentConfigurationService
{
    public async Task<AgentConfiguration> CreateAgentConfigAsync(
        string repoPath, AgentConfigRequest request)
    {
        var agentsDir = Path.Combine(repoPath, ".makeapp", "agents");
        _fileSystem.Directory.CreateDirectory(agentsDir);
        
        // Generate orchestrator config
        var orchestrator = new OrchestratorAgent
        {
            Role = "orchestrator",
            Description = "Coordinates all agents and manages phase progression",
            Responsibilities = new[]
            {
                "Monitor phase completion status",
                "Trigger appropriate agents for each task",
                "Ensure acceptance criteria are met before phase advancement",
                "Update plan status after each task completion",
                "Handle failures and retry logic"
            },
            PhaseCriteria = new PhaseCriteria
            {
                RequireAllTasksComplete = true,
                RequireTestsPassing = true,
                RequireReviewApproval = true
            }
        };
        await SaveAgentConfigAsync(agentsDir, "orchestrator.json", orchestrator);
        
        // Generate coder config based on project type
        var coder = await GenerateCoderConfigAsync(request);
        await SaveAgentConfigAsync(agentsDir, "coder.json", coder);
        
        // Generate tester config
        var tester = await GenerateTesterConfigAsync(request);
        await SaveAgentConfigAsync(agentsDir, "tester.json", tester);
        
        // Generate reviewer config
        var reviewer = new ReviewerAgent
        {
            Role = "reviewer",
            Description = "Reviews code quality and approves for commit",
            Checkpoints = new[]
            {
                "Code follows project conventions",
                "No security vulnerabilities introduced",
                "Performance considerations addressed",
                "Documentation updated",
                "Tests are meaningful and pass"
            },
            ApprovalRequired = true
        };
        await SaveAgentConfigAsync(agentsDir, "reviewer.json", reviewer);
        
        return new AgentConfiguration
        {
            Orchestrator = orchestrator,
            Coder = coder,
            Tester = tester,
            Reviewer = reviewer
        };
    }
    
    public async Task<string> GenerateCopilotInstructionsAsync(
        string repoPath, InstructionsRequest request)
    {
        var prompt = $"""
            Generate a comprehensive copilot-instructions.md file for the following project:
            
            ## Project Name
            {request.Name}
            
            ## Requirements
            {request.Requirements}
            
            ## Project Type
            {request.ProjectType}
            
            ## Agent Orchestration
            This project uses MakeApp's multi-agent orchestration with:
            - Orchestrator: Coordinates phases and agents
            - Coder: Generates implementation code
            - Tester: Creates and runs tests
            - Reviewer: Validates quality before commits
            
            Include sections for:
            1. Project Overview
            2. Architecture Decisions
            3. Code Style Guidelines (specific to project type)
            4. File Organization
            5. Testing Patterns
            6. Agent Coordination Notes
            7. Current Phase: [PLACEHOLDER - updated dynamically]
            
            Make instructions specific and actionable, not generic.
            """;
        
        var response = await _copilotService.SendMessageAsync(
            await GetSessionAsync(repoPath), prompt);
        
        var instructionsPath = Path.Combine(repoPath, ".github", "copilot-instructions.md");
        _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(instructionsPath)!);
        await _fileSystem.File.WriteAllTextAsync(instructionsPath, response.Content);
        
        return instructionsPath;
    }
}
```

#### 1.5.4 Phased Execution Service

```csharp
// MakeApp.Application/Services/PhasedExecutionService.cs
public interface IPhasedExecutionService
{
    Task<PhaseResult> ExecutePhaseAsync(string repoPath, int phaseNumber);
    Task<TaskResult> ExecuteTaskAsync(string repoPath, PhaseTask task);
    Task<bool> ValidatePhaseCompletionAsync(string repoPath, int phaseNumber);
    Task AdvanceToNextPhaseAsync(string repoPath);
}

public class PhasedExecutionService : IPhasedExecutionService
{
    private readonly ICoderAgentService _coder;
    private readonly ITesterAgentService _tester;
    private readonly IReviewerAgentService _reviewer;
    private readonly IGitService _gitService;
    private readonly IPlanGeneratorService _planService;
    private readonly IAgentConfigurationService _agentConfig;
    
    public async Task<PhaseResult> ExecutePhaseAsync(string repoPath, int phaseNumber)
    {
        var plan = await _planService.GetCurrentPlanAsync(repoPath);
        var phase = plan.Phases.First(p => p.Phase == phaseNumber);
        var result = new PhaseResult { Phase = phaseNumber, Name = phase.Name };
        
        foreach (var task in phase.Tasks.Where(t => t.Status != TaskStatus.Completed))
        {
            var taskResult = await ExecuteTaskAsync(repoPath, task);
            result.TaskResults.Add(taskResult);
            
            if (!taskResult.Success)
            {
                result.Success = false;
                result.FailedAt = task.Id;
                return result;
            }
        }
        
        // Validate phase completion
        if (await ValidatePhaseCompletionAsync(repoPath, phaseNumber))
        {
            // Phase commit
            await _gitService.StageAllAsync(repoPath);
            await _gitService.CommitAsync(repoPath, $"Complete Phase {phaseNumber}: {phase.Name}");
            await _gitService.PushAsync(repoPath);
            
            // Update copilot-instructions.md with phase learnings
            await UpdateInstructionsForPhaseCompletionAsync(repoPath, phase);
            
            result.Success = true;
        }
        
        return result;
    }
    
    public async Task<TaskResult> ExecuteTaskAsync(string repoPath, PhaseTask task)
    {
        var result = new TaskResult { TaskId = task.Id };
        var maxRetries = 3;
        var attempt = 0;
        
        while (attempt < maxRetries)
        {
            attempt++;
            
            // 1. CODER: Generate/modify code
            var codeResult = await _coder.ExecuteTaskAsync(repoPath, task);
            if (!codeResult.Success)
            {
                result.CoderAttempts.Add(codeResult);
                continue;  // Retry coding
            }
            
            // 2. TESTER: Generate and run tests
            var testResult = await _tester.ValidateTaskAsync(repoPath, task, codeResult);
            if (!testResult.Success)
            {
                result.TesterResults.Add(testResult);
                // Feed test failures back to coder for fix
                task.Context = $"Previous attempt failed tests:\n{testResult.FailureDetails}";
                continue;  // Retry with feedback
            }
            
            // 3. REVIEWER: Approve code
            var reviewResult = await _reviewer.ReviewTaskAsync(repoPath, task, codeResult);
            if (!reviewResult.Approved)
            {
                result.ReviewResults.Add(reviewResult);
                task.Context = $"Review feedback:\n{reviewResult.Feedback}";
                continue;  // Retry with feedback
            }
            
            // 4. SUCCESS: Commit task
            await _gitService.StageAllAsync(repoPath);
            await _gitService.CommitAsync(repoPath, $"Task {task.Id}: {task.Description}");
            
            // 5. Update plan status
            await _planService.UpdatePlanStatusAsync(repoPath, task.Id, TaskStatus.Completed);
            
            result.Success = true;
            return result;
        }
        
        result.Success = false;
        result.Error = $"Failed after {maxRetries} attempts";
        return result;
    }
    
    private async Task UpdateInstructionsForPhaseCompletionAsync(
        string repoPath, ImplementationPhase phase)
    {
        // Ask LLM if any learnings should be added to instructions
        var prompt = $"""
            Phase "{phase.Name}" is now complete. Review what was implemented and determine
            if any conventions, patterns, or important decisions should be added to 
            copilot-instructions.md for consistency in future phases.
            
            Completed tasks:
            {string.Join("\n", phase.Tasks.Select(t => $"- {t.Description}"))}
            
            If updates are needed, provide the specific section and content to add.
            If no updates needed, respond with "NO_UPDATE_NEEDED".
            """;
        
        var response = await _copilotService.SendMessageAsync(
            await GetSessionAsync(repoPath), prompt);
        
        if (!response.Content.Contains("NO_UPDATE_NEEDED"))
        {
            await _agentConfig.UpdateCopilotInstructionsAsync(
                repoPath, 
                "Phase Learnings", 
                response.Content);
        }
    }
}
```

**Deliverables for Phase 1.5:**
- Repository creation with GitHub integration
- LLM-driven plan generation
- Agent configuration file generation  
- Phased execution engine with retry logic
- Dynamic copilot-instructions.md updates

**Testing Deliverables (Phase 1.5):**
- [ ] Unit tests for `RepositoryCreationService` (mock GitHub API, file system)
- [ ] Unit tests for `PlanGeneratorService` (mock Copilot, verify plan structure)
- [ ] Unit tests for `AgentConfigurationService` (verify JSON generation)
- [ ] Unit tests for `PhasedExecutionService` (mock agents, verify retry logic)
- [ ] Integration tests for `POST /api/v1/apps` endpoint
- [ ] Integration tests for `POST /api/v1/apps/{owner}/{name}/features` endpoint
- [ ] Integration tests verifying `.makeapp/` folder structure creation
- [ ] Integration tests for plan.json generation and status updates
- [ ] Test coverage report showing ≥80% for new code

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
    // Status operations
    Task<GitStatus> GetStatusAsync(string repoPath);
    Task<IReadOnlyList<FileChange>> GetUnstagedChangesAsync(string repoPath);
    Task<IReadOnlyList<FileChange>> GetStagedChangesAsync(string repoPath);
    Task<IReadOnlyList<CommitInfo>> GetUnpushedCommitsAsync(string repoPath, string? remoteName = "origin");
    Task<bool> IsCleanAsync(string repoPath);
    
    // Working tree operations
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

**Testing Deliverables (Phase 2):**
- [ ] Unit tests for `RepositoryService` (mock file system, verify repo detection)
- [ ] Unit tests for `BranchService` (mock LibGit2Sharp, verify branch operations)
- [ ] Unit tests for `GitService` (mock LibGit2Sharp, verify git operations)
- [ ] Integration tests for `GET /api/v1/repos` endpoint
- [ ] Integration tests for `GET /api/v1/repos/{owner}/{name}/branches` endpoint
- [ ] Integration tests for `POST /api/v1/repos/{owner}/{name}/branches` (create branch)
- [ ] Integration tests for `POST /api/v1/repos/{owner}/{name}/commit` endpoint
- [ ] Integration tests for `POST /api/v1/repos/{owner}/{name}/push` endpoint
- [ ] Integration tests for `POST /api/v1/repos/{owner}/{name}/pull-requests` endpoint
- [ ] Test with actual git repository (using TestContainers or temp repo)
- [ ] Test coverage report showing ≥80% for new code

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

**Testing Deliverables (Phase 3):**
- [ ] Unit tests for `FeatureService` (CRUD operations with mocked repository)
- [ ] Unit tests for `PromptFormatterService` (all prompt styles)
- [ ] Unit tests for Markdown parser (various markdown structures)
- [ ] Unit tests for JSON import validation
- [ ] Integration tests for `GET /api/v1/features` endpoint
- [ ] Integration tests for `POST /api/v1/features` endpoint (with validation errors)
- [ ] Integration tests for `PUT /api/v1/features/{id}` endpoint
- [ ] Integration tests for `DELETE /api/v1/features/{id}` endpoint
- [ ] Integration tests for `POST /api/v1/features/import` (JSON and Markdown)
- [ ] Integration tests for `GET /api/v1/features/{id}/prompt` endpoint
- [ ] Test coverage report showing ≥80% for new code

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

**Testing Deliverables (Phase 4):**
- [ ] Unit tests for `CopilotClientManager` (mock SDK client lifecycle)
- [ ] Unit tests for `CopilotService` (session creation, message handling)
- [ ] Unit tests for custom tools (file operations, git operations)
- [ ] Integration tests for `POST /api/v1/copilot/sessions` endpoint
- [ ] Integration tests for `GET /api/v1/copilot/sessions/{id}` endpoint
- [ ] Integration tests for `POST /api/v1/copilot/sessions/{id}/messages` endpoint
- [ ] Integration tests for `GET /api/v1/copilot/sessions/{id}/stream` (SSE streaming)
- [ ] Integration tests for `DELETE /api/v1/copilot/sessions/{id}` endpoint
- [ ] Mock Copilot SDK for deterministic testing
- [ ] Test coverage report showing ≥80% for new code

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

**Testing Deliverables (Phase 5):**
- [ ] Unit tests for `OrchestrationService` (workflow state machine, phase transitions)
- [ ] Unit tests for `WorkflowRepository` (persistence operations)
- [ ] Unit tests for orchestrator agent logic (task coordination)
- [ ] Unit tests for coder, tester, reviewer agent interactions
- [ ] Integration tests for `POST /api/v1/workflows` endpoint
- [ ] Integration tests for `GET /api/v1/workflows/{id}` endpoint
- [ ] Integration tests for `GET /api/v1/workflows/{id}/events` (SSE streaming)
- [ ] Integration tests for `POST /api/v1/workflows/{id}/abort` endpoint
- [ ] Integration tests for `POST /api/v1/workflows/{id}/retry` endpoint
- [ ] E2E test for complete workflow execution (mock Copilot)
- [ ] Test coverage report showing ≥80% for new code

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

**Testing Deliverables (Phase 5.5):**
- [ ] Unit tests for `MemoryService` (CRUD, expiration, filtering)
- [ ] Unit tests for `MemoryValidationService` (citation parsing, file verification)
- [ ] Unit tests for `MemoryStoreToolFactory` (Copilot tool integration)
- [ ] Unit tests for memory expiration logic (28-day TTL)
- [ ] Unit tests for `MemoryMaintenanceJob` (cleanup, statistics)
- [ ] Integration tests for `GET /api/v1/repos/{owner}/{name}/memories` endpoint
- [ ] Integration tests for `POST /api/v1/repos/{owner}/{name}/memories` endpoint
- [ ] Integration tests for `POST /api/v1/repos/{owner}/{name}/memories/{id}/validate` endpoint
- [ ] Integration tests for `POST /api/v1/repos/{owner}/{name}/memories/{id}/refresh` endpoint
- [ ] Integration tests for `GET /api/v1/repos/{owner}/{name}/memories/by-file` endpoint
- [ ] Test memory validation with real/mock file system
- [ ] Test coverage report showing ≥80% for new code

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

The sandbox is now simply the **root folder where all repos are created/checked out**. Per-repo working directories (cache, logs, temp) live inside each repo's `.makeapp/` folder.

```csharp
// MakeApp.Application/Services/SandboxService.cs
public interface ISandboxService
{
    // Sandbox-level operations
    Task<SandboxInfo> GetSandboxInfoAsync();
    Task<IEnumerable<RepositorySummary>> ListSandboxReposAsync();
    Task<bool> ValidateSandboxPathAsync();
    
    // Repo-level operations
    Task<RepositoryStatus> GetRepoStatusAsync(string repoName);
    Task<RemoveResult> RemoveRepoAsync(string repoName, bool force = false);
    Task CleanupRepoWorkingFilesAsync(string repoName);
    Task CleanupAllWorkingFilesAsync();
}

// MakeApp.Core/Entities/RepositoryStatus.cs
public class RepositoryStatus
{
    public string RepoName { get; set; } = "";
    public string Path { get; set; } = "";
    public string CurrentBranch { get; set; } = "";
    public RepoStatusType Status { get; set; }
    public bool CanRemove => Status == RepoStatusType.Clean;
    public PendingChanges PendingChanges { get; set; } = new();
    public string Summary { get; set; } = "";
}

public enum RepoStatusType
{
    Clean,
    HasPendingChanges,
    Unknown
}

public class PendingChanges
{
    public bool HasUnstagedChanges { get; set; }
    public bool HasStagedChanges { get; set; }
    public bool HasUnpushedCommits { get; set; }
    public List<FileChange> UnstagedFiles { get; set; } = new();
    public List<FileChange> StagedFiles { get; set; } = new();
    public List<CommitInfo> UnpushedCommits { get; set; } = new();
    
    public bool HasAnyPendingChanges => 
        HasUnstagedChanges || HasStagedChanges || HasUnpushedCommits;
}

public class FileChange
{
    public string Path { get; set; } = "";
    public string Status { get; set; } = "";  // modified, added, deleted, renamed
}

public class CommitInfo
{
    public string Hash { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Date { get; set; }
}

public class RemoveResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public RepositoryStatus? BlockingStatus { get; set; }  // Populated if removal blocked
}

// MakeApp.Core/Entities/RepositorySummary.cs
public class RepositorySummary
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string CurrentBranch { get; set; } = "";
    public RepoStatusType Status { get; set; }
    public bool HasMakeAppConfig { get; set; }
    public bool CanRemove => Status == RepoStatusType.Clean;
    public string StatusSummary { get; set; } = "";
    public int UnstagedCount { get; set; }
    public int StagedCount { get; set; }
    public int UnpushedCount { get; set; }
    public DateTime LastModified { get; set; }
}

// MakeApp.Core/Entities/SandboxInfo.cs
public class SandboxInfo
{
    public string Path { get; set; } = "";
    public bool Exists { get; set; }
    public int RepoCount { get; set; }
    public List<RepositorySummary> Repositories { get; set; } = new();
    public long TotalSize { get; set; }
}

public class SandboxService : ISandboxService
{
    private readonly UserConfiguration _userConfig;
    private readonly IGitService _gitService;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<SandboxService> _logger;

    public async Task<SandboxInfo> GetSandboxInfoAsync()
    {
        var sandboxPath = _userConfig.SandboxPath;
        
        if (!_fileSystem.Directory.Exists(sandboxPath))
        {
            return new SandboxInfo
            {
                Path = sandboxPath,
                Exists = false,
                RepoCount = 0
            };
        }
        
        var repos = await ListSandboxReposAsync();
        
        return new SandboxInfo
        {
            Path = sandboxPath,
            Exists = true,
            RepoCount = repos.Count(),
            Repositories = repos.ToList(),
            TotalSize = CalculateDirectorySize(sandboxPath)
        };
    }
    
    public async Task<IEnumerable<RepositorySummary>> ListSandboxReposAsync()
    {
        var sandboxPath = _userConfig.SandboxPath;
        var repos = new List<RepositorySummary>();
        
        foreach (var dir in _fileSystem.Directory.GetDirectories(sandboxPath))
        {
            var gitDir = Path.Combine(dir, ".git");
            if (_fileSystem.Directory.Exists(gitDir))
            {
                var status = await GetRepoStatusAsync(Path.GetFileName(dir));
                repos.Add(new RepositorySummary
                {
                    Name = Path.GetFileName(dir),
                    Path = dir,
                    HasMakeAppConfig = _fileSystem.Directory.Exists(
                        Path.Combine(dir, ".makeapp")),
                    LastModified = _fileSystem.Directory.GetLastWriteTime(dir),
                    Status = status.Status,
                    CanRemove = status.CanRemove,
                    StatusSummary = status.Summary
                });
            }
        }
        
        return repos;
    }
    
    public async Task<RepositoryStatus> GetRepoStatusAsync(string repoName)
    {
        var repoPath = Path.Combine(_userConfig.SandboxPath, repoName);
        
        if (!_fileSystem.Directory.Exists(repoPath))
        {
            throw new NotFoundException($"Repository '{repoName}' not found in sandbox");
        }
        
        var status = new RepositoryStatus
        {
            RepoName = repoName,
            Path = repoPath,
            CurrentBranch = await _gitService.GetCurrentBranchAsync(repoPath)
        };
        
        // Check for unstaged changes
        var unstagedChanges = await _gitService.GetUnstagedChangesAsync(repoPath);
        status.PendingChanges.HasUnstagedChanges = unstagedChanges.Any();
        status.PendingChanges.UnstagedFiles = unstagedChanges.Select(f => new FileChange
        {
            Path = f.Path,
            Status = f.Status.ToString().ToLower()
        }).ToList();
        
        // Check for staged changes
        var stagedChanges = await _gitService.GetStagedChangesAsync(repoPath);
        status.PendingChanges.HasStagedChanges = stagedChanges.Any();
        status.PendingChanges.StagedFiles = stagedChanges.Select(f => new FileChange
        {
            Path = f.Path,
            Status = f.Status.ToString().ToLower()
        }).ToList();
        
        // Check for unpushed commits
        var unpushedCommits = await _gitService.GetUnpushedCommitsAsync(repoPath);
        status.PendingChanges.HasUnpushedCommits = unpushedCommits.Any();
        status.PendingChanges.UnpushedCommits = unpushedCommits.Select(c => new CommitInfo
        {
            Hash = c.Sha[..7],
            Message = c.MessageShort,
            Date = c.Author.When.DateTime
        }).ToList();
        
        // Determine overall status
        status.Status = status.PendingChanges.HasAnyPendingChanges 
            ? RepoStatusType.HasPendingChanges 
            : RepoStatusType.Clean;
        
        // Build summary
        var summaryParts = new List<string>();
        if (status.PendingChanges.HasUnstagedChanges)
            summaryParts.Add($"{status.PendingChanges.UnstagedFiles.Count} unstaged file(s)");
        if (status.PendingChanges.HasStagedChanges)
            summaryParts.Add($"{status.PendingChanges.StagedFiles.Count} staged file(s)");
        if (status.PendingChanges.HasUnpushedCommits)
            summaryParts.Add($"{status.PendingChanges.UnpushedCommits.Count} unpushed commit(s)");
        
        status.Summary = summaryParts.Any() 
            ? string.Join(", ", summaryParts) 
            : "Clean - no pending changes";
        
        return status;
    }
    
    public async Task<RemoveResult> RemoveRepoAsync(string repoName, bool force = false)
    {
        var repoPath = Path.Combine(_userConfig.SandboxPath, repoName);
        
        if (!_fileSystem.Directory.Exists(repoPath))
        {
            throw new NotFoundException($"Repository '{repoName}' not found in sandbox");
        }
        
        // Check status unless force is true
        if (!force)
        {
            var status = await GetRepoStatusAsync(repoName);
            
            if (!status.CanRemove)
            {
                _logger.LogWarning(
                    "Cannot remove repo {Repo}: {Summary}", 
                    repoName, status.Summary);
                    
                return new RemoveResult
                {
                    Success = false,
                    Error = $"Repository has pending changes: {status.Summary}. " +
                            "Commit and push changes, or use force=true to remove anyway.",
                    BlockingStatus = status
                };
            }
        }
        else
        {
            _logger.LogWarning(
                "Force removing repo {Repo} - pending changes will be lost", 
                repoName);
        }
        
        try
        {
            // Remove the directory recursively
            _fileSystem.Directory.Delete(repoPath, recursive: true);
            
            _logger.LogInformation("Removed repository {Repo} from sandbox", repoName);
            
            return new RemoveResult { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove repository {Repo}", repoName);
            return new RemoveResult
            {
                Success = false,
                Error = $"Failed to remove repository: {ex.Message}"
            };
        }
    }
    
    public async Task CleanupRepoWorkingFilesAsync(string repoName)
    {
        var repoPath = Path.Combine(_userConfig.SandboxPath, repoName);
        
        if (!_fileSystem.Directory.Exists(repoPath))
        {
            throw new NotFoundException($"Repository '{repoName}' not found in sandbox");
        }
        
        // Clean gitignored working directories inside the repo
        var foldersToClean = new[] { "cache", "logs", "temp" };
        
        foreach (var folder in foldersToClean)
        {
            var folderPath = Path.Combine(repoPath, ".makeapp", folder);
            if (_fileSystem.Directory.Exists(folderPath))
            {
                foreach (var file in _fileSystem.Directory.GetFiles(folderPath))
                {
                    _fileSystem.File.Delete(file);
                }
                foreach (var subdir in _fileSystem.Directory.GetDirectories(folderPath))
                {
                    _fileSystem.Directory.Delete(subdir, recursive: true);
                }
                
                _logger.LogInformation("Cleaned {Folder} in {Repo}", folder, repoName);
            }
        }
    }
    
    public async Task CleanupAllWorkingFilesAsync()
    {
        var repos = await ListSandboxReposAsync();
        foreach (var repo in repos)
        {
            await CleanupRepoWorkingFilesAsync(repo.Name);
        }
    }
}
```

#### 6.3 Sandbox Controller

```csharp
// MakeApp.Api/Controllers/SandboxController.cs
[ApiController]
[Route("api/v1/sandbox")]
public class SandboxController : ControllerBase
{
    private readonly ISandboxService _sandboxService;
    
    /// <summary>
    /// Get sandbox information (path, repo count, size)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(SandboxInfoDto), 200)]
    public async Task<ActionResult<SandboxInfoDto>> GetSandboxInfo()
    {
        var info = await _sandboxService.GetSandboxInfoAsync();
        return Ok(info.ToDto());
    }
    
    /// <summary>
    /// List all repositories in sandbox with status summary
    /// </summary>
    [HttpGet("repos")]
    [ProducesResponseType(typeof(IEnumerable<RepositorySummaryDto>), 200)]
    public async Task<ActionResult<IEnumerable<RepositorySummaryDto>>> ListRepos()
    {
        var repos = await _sandboxService.ListSandboxReposAsync();
        return Ok(repos.Select(r => r.ToDto()));
    }
    
    /// <summary>
    /// Get detailed status for a specific repository
    /// </summary>
    [HttpGet("repos/{name}/status")]
    [ProducesResponseType(typeof(RepositoryStatusDto), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<RepositoryStatusDto>> GetRepoStatus(string name)
    {
        var status = await _sandboxService.GetRepoStatusAsync(name);
        return Ok(status.ToDto());
    }
    
    /// <summary>
    /// Remove a repository from sandbox
    /// </summary>
    /// <remarks>
    /// By default, removal is blocked if the repo has:
    /// - Unstaged changes
    /// - Staged changes  
    /// - Unpushed commits
    /// 
    /// Use force=true to remove regardless of pending changes.
    /// </remarks>
    [HttpDelete("repos/{name}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(RemoveResultDto), 409)]  // Conflict - has pending changes
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveRepo(
        string name, 
        [FromQuery] bool force = false)
    {
        var result = await _sandboxService.RemoveRepoAsync(name, force);
        
        if (!result.Success)
        {
            return Conflict(new RemoveResultDto
            {
                Success = false,
                Error = result.Error,
                BlockingStatus = result.BlockingStatus?.ToDto()
            });
        }
        
        return NoContent();
    }
    
    /// <summary>
    /// Clean working files (cache/logs/temp) for a specific repo
    /// </summary>
    [HttpPost("repos/{name}/cleanup")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CleanupRepo(string name)
    {
        await _sandboxService.CleanupRepoWorkingFilesAsync(name);
        return NoContent();
    }
    
    /// <summary>
    /// Clean working files for all repos in sandbox
    /// </summary>
    [HttpPost("cleanup")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> CleanupAll()
    {
        await _sandboxService.CleanupAllWorkingFilesAsync();
        return NoContent();
    }
}
```

- [ ] Implement sandbox info and repo listing
- [ ] Add repository status checking (unstaged, staged, unpushed)
- [ ] Implement safe repo removal with force option
- [ ] Add working files cleanup functionality
- [ ] Create sandbox API endpoints

**Deliverables**:
- Configuration validation and generation
- Complete sandbox management API with status checking and safe removal

**Testing Deliverables (Phase 6):**
- [ ] Unit tests for `CopilotConfigService` (instruction validation, MCP validation)
- [ ] Unit tests for `SandboxService` (repo detection, status checking, cleanup)
- [ ] Unit tests for repo removal logic (clean status check, force removal)
- [ ] Unit tests for pending changes detection (unstaged, staged, unpushed)
- [ ] Integration tests for `GET /api/v1/sandbox` endpoint
- [ ] Integration tests for `GET /api/v1/sandbox/repos` endpoint
- [ ] Integration tests for `GET /api/v1/sandbox/repos/{name}/status` endpoint
- [ ] Integration tests for `DELETE /api/v1/sandbox/repos/{name}` (with and without force)
- [ ] Integration tests for `POST /api/v1/sandbox/repos/{name}/cleanup` endpoint
- [ ] Integration tests for `PUT /api/v1/repos/{owner}/{name}/config/copilot-instructions` endpoint
- [ ] Test sandbox operations with mock file system
- [ ] Test coverage report showing ≥80% for new code

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

        // Include XML comments for API documentation
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    return services;
}

public static IApplicationBuilder UseMakeAppSwagger(this IApplicationBuilder app)
{
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "api-docs/{documentName}/swagger.json";
    });

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/api-docs/v1/swagger.json", "MakeApp API v1");
        c.RoutePrefix = "swagger";  // Swagger UI at /swagger
        c.DocExpansion(DocExpansion.None);
        c.DefaultModelsExpandDepth(-1);
        c.DisplayRequestDuration();
        c.EnableFilter();
        c.EnableDeepLinking();
    });

    return app;
}
```

**Program.cs Swagger Setup:**
```csharp
// In Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddMakeAppSwagger();
// ... other services

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseMakeAppSwagger();
}

// ... rest of middleware
app.Run();
```

**Swagger Endpoints:**

| Endpoint | Description |
|----------|-------------|
| `GET /swagger` | Swagger UI interactive documentation |
| `GET /swagger/index.html` | Swagger UI (explicit path) |
| `GET /api-docs/v1/swagger.json` | OpenAPI 3.0 specification (JSON) |

- [ ] Configure comprehensive OpenAPI documentation
- [ ] Add XML documentation comments to all controllers
- [ ] Generate TypeScript client from OpenAPI spec
- [ ] Generate C# client library from OpenAPI spec
- [ ] Add API versioning documentation
- [ ] Configure Swagger UI with examples and descriptions

**Deliverables**:
- Secure authentication system
- Rate limiting and throttling
- Comprehensive error handling
- Complete API documentation

**Testing Deliverables (Phase 7):**
- [ ] Unit tests for JWT token generation and validation
- [ ] Unit tests for authorization policies
- [ ] Unit tests for rate limiting middleware
- [ ] Unit tests for error handling middleware
- [ ] Unit tests for validation pipelines
- [ ] Integration tests verifying authentication on protected endpoints
- [ ] Integration tests verifying 401/403 responses for unauthorized requests
- [ ] Integration tests for rate limiting (verify 429 responses)
- [ ] Integration tests for validation error responses (400 with problem details)
- [ ] Integration tests for global error handling (500 responses)
- [ ] Security scan/penetration test setup
- [ ] Test coverage report showing ≥80% for new code

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
makeapp_api/
├── makeapp_api.sln
├── docker-compose.yml
├── README.md
├── docs/
│   ├── api-reference.md
│   ├── authentication.md
│   ├── configuration.md
│   ├── memory-system.md
│   ├── create-app-workflow.md            # NEW: App creation documentation
│   ├── add-feature-workflow.md           # NEW: Feature addition documentation
│   ├── agent-orchestration.md            # NEW: Multi-agent system docs
│   └── migration-guide.md
├── templates/                             # NEW: Project templates
│   ├── agents/
│   │   ├── orchestrator.template.json
│   │   ├── coder.template.json
│   │   ├── tester.template.json
│   │   └── reviewer.template.json
│   ├── copilot-instructions/
│   │   ├── node.template.md
│   │   ├── dotnet.template.md
│   │   ├── python.template.md
│   │   └── generic.template.md
│   └── gitignore/
│       ├── node.gitignore
│       ├── dotnet.gitignore
│       └── python.gitignore
├── src/
│   ├── MakeApp.Api/
│   │   ├── Controllers/
│   │   │   ├── AppsController.cs             # NEW: Create/manage apps
│   │   │   ├── FeaturesController.cs         # UPDATED: Add features to apps
│   │   │   ├── BranchesController.cs
│   │   │   ├── ConfigController.cs
│   │   │   ├── UserConfigController.cs       # NEW: GitHub user configuration
│   │   │   ├── CopilotController.cs
│   │   │   ├── GitController.cs
│   │   │   ├── HealthController.cs
│   │   │   ├── MemoriesController.cs
│   │   │   ├── PlansController.cs            # NEW: Implementation plans
│   │   │   ├── PhasesController.cs           # NEW: Phase management
│   │   │   ├── AgentsController.cs           # NEW: Agent configurations
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
│   │   ├── BackgroundJobs/
│   │   │   ├── MemoryMaintenanceJob.cs
│   │   │   └── WorkflowExecutionJob.cs       # NEW: Background workflow runner
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   └── Dockerfile
│   │
│   ├── MakeApp.Core/
│   │   ├── Entities/
│   │   │   ├── App.cs                        # NEW: App entity
│   │   │   ├── Feature.cs
│   │   │   ├── ImplementationPlan.cs         # NEW: Plan entity
│   │   │   ├── ImplementationPhase.cs        # NEW: Phase entity
│   │   │   ├── PhaseTask.cs                  # NEW: Task entity
│   │   │   ├── AgentConfiguration.cs         # NEW: Agent config entity
│   │   │   ├── Memory.cs
│   │   │   ├── MemoryCitation.cs
│   │   │   ├── Workflow.cs
│   │   │   ├── WorkflowStep.cs
│   │   │   ├── RepositoryInfo.cs
│   │   │   ├── RepositoryStatus.cs           # NEW: Git status tracking
│   │   │   ├── RepositorySummary.cs          # NEW: Repo list summary
│   │   │   ├── PendingChanges.cs             # NEW: Unstaged/staged/unpushed
│   │   │   ├── FileChange.cs                 # NEW: Individual file change
│   │   │   ├── CommitInfo.cs                 # NEW: Unpushed commit info
│   │   │   ├── RemoveResult.cs               # NEW: Repo removal result
│   │   │   ├── BranchInfo.cs
│   │   │   └── SandboxInfo.cs
│   │   ├── Enums/
│   │   │   ├── AppStatus.cs                  # NEW
│   │   │   ├── PhaseStatus.cs                # NEW
│   │   │   ├── TaskStatus.cs                 # NEW
│   │   │   ├── AgentRole.cs                  # NEW
│   │   │   ├── ChangeType.cs                 # NEW: Modified/Added/Deleted/Renamed
│   │   │   ├── RepoStatusType.cs             # NEW: Clean/HasPendingChanges/Unknown
│   │   │   ├── FeatureStatus.cs
│   │   │   ├── FeaturePriority.cs
│   │   │   ├── MemoryStatus.cs
│   │   │   ├── WorkflowPhase.cs
│   │   │   └── PromptStyle.cs
│   │   ├── Interfaces/
│   │   │   ├── IAppService.cs                # NEW
│   │   │   ├── IRepositoryCreationService.cs # NEW
│   │   │   ├── IPlanGeneratorService.cs      # NEW
│   │   │   ├── IAgentConfigurationService.cs # NEW
│   │   │   ├── IPhasedExecutionService.cs    # NEW
│   │   │   ├── ICoderAgentService.cs         # NEW
│   │   │   ├── ITesterAgentService.cs        # NEW
│   │   │   ├── IReviewerAgentService.cs      # NEW
│   │   │   ├── IRepositoryService.cs
│   │   │   ├── IBranchService.cs
│   │   │   ├── IFeatureService.cs
│   │   │   ├── IGitService.cs
│   │   │   ├── IGitHubService.cs             # NEW: GitHub API operations
│   │   │   ├── ILlmService.cs                # NEW: LLM integration abstraction
│   │   │   ├── IFileGeneratorService.cs      # NEW: File generation from templates
│   │   │   ├── ICopilotService.cs
│   │   │   ├── IMemoryService.cs
│   │   │   ├── IMemoryRepository.cs
│   │   │   ├── IOrchestrationService.cs
│   │   │   ├── IFileSystem.cs                # NEW: File system abstraction
│   │   │   └── ISandboxService.cs
│   │   ├── Exceptions/
│   │   │   ├── NotFoundException.cs
│   │   │   ├── ConflictException.cs
│   │   │   ├── PendingChangesException.cs    # NEW: Thrown when repo has uncommitted changes
│   │   │   ├── PhaseNotCompleteException.cs  # NEW
│   │   │   └── ValidationException.cs
│   │   └── Configuration/
│   │       ├── MakeAppOptions.cs
│   │       ├── UserConfiguration.cs          # NEW
│   │       ├── AgentOptions.cs               # NEW
│   │       └── MemoryOptions.cs
│   │
│   ├── MakeApp.Application/
│   │   ├── Services/
│   │   │   ├── AppService.cs                     # NEW: App lifecycle management
│   │   │   ├── RepositoryCreationService.cs      # NEW: Create GitHub repos
│   │   │   ├── PlanGeneratorService.cs           # NEW: LLM plan generation
│   │   │   ├── AgentConfigurationService.cs      # NEW: Agent config management
│   │   │   ├── PhasedExecutionService.cs         # NEW: Phase execution engine
│   │   │   ├── CoderAgentService.cs              # NEW: Code generation agent
│   │   │   ├── TesterAgentService.cs             # NEW: Test generation/execution
│   │   │   ├── ReviewerAgentService.cs           # NEW: Code review agent
│   │   │   ├── FileGeneratorService.cs           # NEW: Generate project files
│   │   │   ├── CopilotInstructionsService.cs     # NEW: Dynamic instructions
│   │   │   ├── RepositoryService.cs
│   │   │   ├── BranchService.cs
│   │   │   ├── FeatureService.cs
│   │   │   ├── GitService.cs
│   │   │   ├── CopilotService.cs
│   │   │   ├── MemoryService.cs
│   │   │   ├── MemoryValidationService.cs
│   │   │   ├── MemoryAwarePromptFormatter.cs
│   │   │   ├── OrchestrationService.cs
│   │   │   ├── MemoryAwareOrchestrationService.cs
│   │   │   ├── CopilotConfigService.cs
│   │   │   ├── PromptFormatterService.cs
│   │   │   ├── SandboxService.cs
│   │   │   └── NotificationService.cs
│   │   ├── DTOs/
│   │   │   ├── Apps/                         # NEW
│   │   │   │   ├── CreateAppRequest.cs
│   │   │   │   ├── CreateAppResult.cs
│   │   │   │   ├── AppDto.cs
│   │   │   │   └── AppStatusDto.cs
│   │   │   ├── Plans/                        # NEW
│   │   │   │   ├── ImplementationPlanDto.cs
│   │   │   │   ├── PhaseDto.cs
│   │   │   │   ├── TaskDto.cs
│   │   │   │   └── PlanStatusDto.cs
│   │   │   ├── Agents/                       # NEW
│   │   │   │   ├── AgentConfigDto.cs
│   │   │   │   ├── TaskResultDto.cs
│   │   │   │   └── PhaseResultDto.cs
│   │   │   ├── Features/
│   │   │   │   ├── AddFeatureRequest.cs      # NEW
│   │   │   │   └── FeatureDto.cs
│   │   │   ├── Sandbox/                      # NEW
│   │   │   │   ├── RepositoryStatusDto.cs
│   │   │   │   ├── RepositorySummaryDto.cs
│   │   │   │   ├── SandboxInfoDto.cs
│   │   │   │   ├── PendingChangesDto.cs
│   │   │   │   ├── FileChangeDto.cs
│   │   │   │   ├── CommitInfoDto.cs
│   │   │   │   └── RemoveResultDto.cs
│   │   │   ├── Workflows/
│   │   │   ├── Repositories/
│   │   │   ├── Copilot/
│   │   │   └── Memory/
│   │   │       ├── MemoryDto.cs
│   │   │       ├── CreateMemoryDto.cs
│   │   │       ├── MemoryValidationResultDto.cs
│   │   │       └── MemoryStatisticsDto.cs
│   │   ├── Mappings/
│   │   │   └── MappingProfile.cs
│   │   └── Validators/
│   │       ├── CreateAppRequestValidator.cs  # NEW
│   │       ├── AddFeatureRequestValidator.cs # NEW
│   │       ├── CreateFeatureValidator.cs
│   │       ├── CreateMemoryValidator.cs
│   │       └── StartWorkflowValidator.cs
│   │
│   └── MakeApp.Infrastructure/
│       ├── Copilot/
│       │   ├── CopilotClientManager.cs
│       │   ├── MakeAppTools.cs
│       │   ├── MemoryStoreTool.cs
│       │   ├── CoderTools.cs                 # NEW: Coder agent tools
│       │   ├── TesterTools.cs                # NEW: Tester agent tools
│       │   └── ReviewerTools.cs              # NEW: Reviewer agent tools
│       ├── Git/
│       │   ├── LibGit2SharpGitService.cs
│       │   └── GitOperations.cs
│       ├── GitHub/
│       │   ├── GitHubService.cs
│       │   ├── RepositoryOperations.cs       # NEW: Repo creation
│       │   └── PullRequestService.cs
│       ├── FileSystem/
│       │   ├── FileSystemAdapter.cs
│       │   └── ProjectTemplateService.cs     # NEW: Project scaffolding
│       └── Persistence/
│           ├── AppRepository.cs              # NEW
│           ├── PlanRepository.cs             # NEW
│           ├── FeatureRepository.cs
│           ├── MemoryRepository.cs
│           └── WorkflowRepository.cs
│
└── tests/
    ├── MakeApp.Api.Tests/
    │   └── Controllers/
    │       ├── AppsControllerTests.cs        # NEW
    │       ├── FeaturesControllerTests.cs    # NEW
    │       └── MemoriesControllerTests.cs
    ├── MakeApp.Application.Tests/
    │   └── Services/
    │       ├── AppServiceTests.cs            # NEW
    │       ├── PlanGeneratorServiceTests.cs  # NEW
    │       ├── PhasedExecutionServiceTests.cs # NEW
    │       ├── MemoryServiceTests.cs
    │       └── MemoryValidationServiceTests.cs
    ├── MakeApp.Infrastructure.Tests/
    │   ├── Git/
    │   ├── GitHub/
    │   │   └── RepositoryOperationsTests.cs  # NEW
    │   ├── Copilot/
    │   │   └── MemoryStoreToolTests.cs
    │   └── Persistence/
    │       ├── PlanRepositoryTests.cs        # NEW
    │       └── MemoryRepositoryTests.cs
    └── MakeApp.E2E.Tests/
        ├── CreateAppWorkflowTests.cs         # NEW
        └── AddFeatureWorkflowTests.cs        # NEW
```

---

## Timeline Summary

| Phase | Duration | Key Deliverables |
|-------|----------|------------------|
| Phase 1: Foundation | 2 weeks | Project structure, configuration, health endpoints, user config |
| **Phase 1.5: App Creation Infrastructure** | **2 weeks** | **Repo creation, plan generation, agent config, phased execution** |
| Phase 2: Repository & Branch | 2 weeks | Repository scanning, branch management, git operations |
| Phase 3: Feature Management | 2 weeks | Feature CRUD, import/export, prompt formatting, Add Feature endpoint |
| Phase 4: Copilot SDK | 3 weeks | Copilot integration, streaming, custom tools, agent tools |
| Phase 5: Workflow Orchestration | 3 weeks | Orchestration engine, event streaming, workflow control |
| Phase 5.5: Agentic Memory | 2 weeks | Memory storage, validation, cross-workflow learning |
| Phase 6: Config & Sandbox | 1 week | Configuration validation, sandbox management |
| Phase 7: Security & Polish | 2 weeks | Authentication, rate limiting, error handling |
| Phase 8: Testing & Deployment | 2 weeks | Tests, Docker, CI/CD, documentation |

**Total Estimated Duration**: 21 weeks (5.25 months)

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
3. **Create GitHub repository** named `makeapp_api` for the API project
4. **Begin Phase 1** implementation
5. **Schedule weekly progress reviews**
6. **Remove PowerShell code and references** after the entire plan has been successfully implemented (see Post-Implementation Cleanup below)

---

## Post-Implementation Cleanup

Once all phases (1-8) have been successfully completed and the `makeapp_api` Web API is fully functional:

- [ ] Remove all PowerShell modules from the `/modules` directory:
  - `BranchManager.ps1`
  - `CopilotConfig.ps1`
  - `Executor.ps1`
  - `FeaturePrompt.ps1`
  - `GitAutomation.ps1`
  - `Sandbox.ps1`
- [ ] Remove `makeapp.ps1` entry point script
- [ ] Remove PowerShell test files from `/tests` directory
- [ ] Update `README.md` to reference only the Web API
- [ ] Remove all `**PowerShell Equivalent**` references from this document
- [ ] Archive the original PowerShell implementation (optional - create a `legacy/` branch)
- [ ] Update `.github/copilot-instructions.md` to reflect .NET/C# conventions instead of PowerShell

> **Important**: Do not remove the PowerShell code until the Web API achieves full functional parity and has been validated in production use.

---

*Document Version: 1.1*  
*Created: January 17, 2026*  
*Updated: January 17, 2026*  
*Author: GitHub Copilot*
