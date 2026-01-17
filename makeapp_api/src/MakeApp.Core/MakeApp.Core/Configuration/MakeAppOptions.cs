namespace MakeApp.Core.Configuration;

/// <summary>
/// Main configuration options for MakeApp
/// </summary>
public class MakeAppOptions
{
    /// <summary>Configuration section name</summary>
    public const string SectionName = "MakeApp";
    
    /// <summary>Folder configuration</summary>
    public FolderOptions Folders { get; set; } = new();
    
    /// <summary>GitHub configuration</summary>
    public GitHubOptions GitHub { get; set; } = new();
    
    /// <summary>LLM/Copilot configuration</summary>
    public LlmOptions Llm { get; set; } = new();
    
    /// <summary>Agent configuration</summary>
    public AgentOptions Agent { get; set; } = new();
    
    /// <summary>Git configuration</summary>
    public GitOptions Git { get; set; } = new();
    
    /// <summary>UI configuration</summary>
    public UiOptions Ui { get; set; } = new();
    
    /// <summary>Timeout configuration</summary>
    public TimeoutOptions Timeouts { get; set; } = new();
    
    /// <summary>Limit configuration</summary>
    public LimitOptions Limits { get; set; } = new();
}

/// <summary>
/// Folder path configuration
/// </summary>
public class FolderOptions
{
    /// <summary>Root folder for all repositories (sandbox)</summary>
    public string Sandbox { get; set; } = "";
    
    /// <summary>Alias for Sandbox (backward compatibility)</summary>
    public string Repos
    {
        get => Sandbox;
        set => Sandbox = value;
    }
}

/// <summary>
/// GitHub configuration options
/// </summary>
public class GitHubOptions
{
    /// <summary>GitHub personal access token</summary>
    public string? Token { get; set; }
    
    /// <summary>OAuth client ID</summary>
    public string? ClientId { get; set; }
    
    /// <summary>OAuth client secret</summary>
    public string? ClientSecret { get; set; }
    
    /// <summary>Default organization or user</summary>
    public string? DefaultOwner { get; set; }
    
    /// <summary>GitHub API base URL (for GitHub Enterprise)</summary>
    public string ApiBaseUrl { get; set; } = "https://api.github.com";
}

/// <summary>
/// LLM/Copilot configuration options
/// </summary>
public class LlmOptions
{
    /// <summary>LLM providers configuration</summary>
    public Dictionary<string, LlmProviderOptions> Providers { get; set; } = new()
    {
        ["copilot"] = new LlmProviderOptions { Model = "gpt-5" }
    };
    
    /// <summary>Default provider to use</summary>
    public string DefaultProvider { get; set; } = "copilot";
}

/// <summary>
/// LLM provider options
/// </summary>
public class LlmProviderOptions
{
    /// <summary>Model to use</summary>
    public string Model { get; set; } = "";
    
    /// <summary>API key (if not using Copilot)</summary>
    public string? ApiKey { get; set; }
    
    /// <summary>API base URL</summary>
    public string? BaseUrl { get; set; }
}

/// <summary>
/// Agent configuration options
/// </summary>
public class AgentOptions
{
    /// <summary>Maximum retries per task</summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>Maximum iterations per workflow</summary>
    public int MaxIterations { get; set; } = 50;
    
    /// <summary>Whether to auto-approve code review</summary>
    public bool AutoApprove { get; set; } = false;
}

/// <summary>
/// Git configuration options
/// </summary>
public class GitOptions
{
    /// <summary>Default branch name</summary>
    public string DefaultBranch { get; set; } = "main";
    
    /// <summary>Commit author name</summary>
    public string? AuthorName { get; set; }
    
    /// <summary>Commit author email</summary>
    public string? AuthorEmail { get; set; }
    
    /// <summary>Whether to sign commits</summary>
    public bool SignCommits { get; set; } = false;
    
    /// <summary>Whether to auto-push after commit</summary>
    public bool AutoPush { get; set; } = true;
}

/// <summary>
/// UI configuration options
/// </summary>
public class UiOptions
{
    /// <summary>Whether to show verbose output</summary>
    public bool VerboseOutput { get; set; } = false;
    
    /// <summary>Whether to show progress indicators</summary>
    public bool ShowProgress { get; set; } = true;
    
    /// <summary>Whether to colorize output</summary>
    public bool ColorOutput { get; set; } = true;
}

/// <summary>
/// Timeout configuration options
/// </summary>
public class TimeoutOptions
{
    /// <summary>Git operation timeout in seconds</summary>
    public int GitOperationSeconds { get; set; } = 120;
    
    /// <summary>Copilot request timeout in seconds</summary>
    public int CopilotRequestSeconds { get; set; } = 300;
    
    /// <summary>Workflow step timeout in seconds</summary>
    public int WorkflowStepSeconds { get; set; } = 600;
}

/// <summary>
/// Limit configuration options
/// </summary>
public class LimitOptions
{
    /// <summary>Maximum file size in bytes for processing</summary>
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10 MB
    
    /// <summary>Maximum number of files to process</summary>
    public int MaxFiles { get; set; } = 1000;
    
    /// <summary>Maximum concurrent workflows</summary>
    public int MaxConcurrentWorkflows { get; set; } = 5;
}
