using MakeApp.Core.Entities;

namespace MakeApp.Core.Interfaces;

/// <summary>
/// Service interface for Copilot operations
/// </summary>
public interface ICopilotService
{
    /// <summary>Create a new Copilot session</summary>
    Task<string> CreateSessionAsync(CreateSessionRequest dto);
    
    /// <summary>Send a message and get a response</summary>
    Task<CopilotResponse> SendMessageAsync(string sessionId, string prompt, MessageOptions? options = null);
    
    /// <summary>Stream a message response</summary>
    IAsyncEnumerable<CopilotStreamEvent> StreamMessageAsync(string sessionId, string prompt, CancellationToken cancellationToken = default);
    
    /// <summary>Get session information</summary>
    Task<SessionInfo?> GetSessionInfoAsync(string sessionId);
    
    /// <summary>Get session messages</summary>
    Task<IEnumerable<SessionMessage>> GetSessionMessagesAsync(string sessionId);
    
    /// <summary>Abort a session</summary>
    Task AbortSessionAsync(string sessionId);
    
    /// <summary>Close a session</summary>
    Task CloseSessionAsync(string sessionId);
    
    /// <summary>List active sessions</summary>
    Task<IEnumerable<SessionInfo>> ListSessionsAsync();
}

/// <summary>
/// Request to create a Copilot session
/// </summary>
public class CreateSessionRequest
{
    /// <summary>Model to use</summary>
    public string Model { get; set; } = "gpt-5";
    
    /// <summary>Whether to enable streaming</summary>
    public bool Streaming { get; set; } = true;
    
    /// <summary>System message configuration</summary>
    public SystemMessageConfig? SystemMessage { get; set; }
    
    /// <summary>Repository path for context</summary>
    public string? RepositoryPath { get; set; }
}

/// <summary>
/// System message configuration
/// </summary>
public class SystemMessageConfig
{
    /// <summary>System message mode</summary>
    public SystemMessageMode Mode { get; set; } = SystemMessageMode.Append;
    
    /// <summary>System message content</summary>
    public string Content { get; set; } = "";
}

/// <summary>
/// System message mode
/// </summary>
public enum SystemMessageMode
{
    /// <summary>Replace the system message</summary>
    Replace,
    
    /// <summary>Append to the system message</summary>
    Append
}

/// <summary>
/// Options for sending a message
/// </summary>
public class MessageOptions
{
    /// <summary>File attachments</summary>
    public List<string>? Attachments { get; set; }
    
    /// <summary>Maximum tokens to generate</summary>
    public int? MaxTokens { get; set; }
    
    /// <summary>Temperature for generation</summary>
    public double? Temperature { get; set; }
}

/// <summary>
/// Response from Copilot
/// </summary>
public class CopilotResponse
{
    /// <summary>Response content</summary>
    public string Content { get; set; } = "";
    
    /// <summary>Finish reason</summary>
    public string? FinishReason { get; set; }
    
    /// <summary>Tokens used (convenience property)</summary>
    public int TokensUsed { get; set; }
    
    /// <summary>Token usage</summary>
    public TokenUsage? Usage { get; set; }
}

/// <summary>
/// Token usage information
/// </summary>
public class TokenUsage
{
    /// <summary>Prompt tokens</summary>
    public int PromptTokens { get; set; }
    
    /// <summary>Completion tokens</summary>
    public int CompletionTokens { get; set; }
    
    /// <summary>Total tokens</summary>
    public int TotalTokens => PromptTokens + CompletionTokens;
}

/// <summary>
/// Streaming event from Copilot
/// </summary>
public class CopilotStreamEvent
{
    /// <summary>Event type</summary>
    public string Type { get; set; } = "";
    
    /// <summary>Content (for delta/message events)</summary>
    public string? Content { get; set; }
    
    /// <summary>Error message (for error events)</summary>
    public string? Error { get; set; }
    
    /// <summary>Timestamp of the event</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Session information
/// </summary>
public class SessionInfo
{
    /// <summary>Session ID</summary>
    public string SessionId { get; set; } = "";
    
    /// <summary>Session ID (alias for SessionId)</summary>
    public string Id { get => SessionId; set => SessionId = value; }
    
    /// <summary>Model being used</summary>
    public string Model { get; set; } = "";
    
    /// <summary>Session state</summary>
    public string State { get; set; } = "";
    
    /// <summary>Session status (alias for State)</summary>
    public string Status { get => State; set => State = value; }
    
    /// <summary>When the session was created</summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>When the session was last active</summary>
    public DateTime LastActiveAt { get; set; }
    
    /// <summary>Number of messages in the session</summary>
    public int MessageCount { get; set; }
}

/// <summary>
/// Message in a session
/// </summary>
public class SessionMessage
{
    /// <summary>Message role</summary>
    public string Role { get; set; } = "";
    
    /// <summary>Message content</summary>
    public string Content { get; set; } = "";
    
    /// <summary>When the message was sent</summary>
    public DateTime Timestamp { get; set; }
}
