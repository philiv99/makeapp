using MakeApp.Core.Entities;
using MakeApp.Core.Interfaces;

namespace MakeApp.Infrastructure.Services;

/// <summary>
/// Implementation of ICopilotService
/// Note: This is a stub implementation. Full implementation would require GitHub Copilot SDK.
/// </summary>
public class CopilotService : ICopilotService
{
    private readonly Dictionary<string, SessionState> _sessions = new();

    /// <inheritdoc/>
    public Task<string> CreateSessionAsync(CreateSessionRequest dto)
    {
        var sessionId = Guid.NewGuid().ToString("N")[..12];
        _sessions[sessionId] = new SessionState
        {
            Id = sessionId,
            Model = dto.Model,
            RepositoryPath = dto.RepositoryPath,
            CreatedAt = DateTime.UtcNow
        };
        return Task.FromResult(sessionId);
    }

    /// <inheritdoc/>
    public Task<CopilotResponse> SendMessageAsync(string sessionId, string prompt, MessageOptions? options = null)
    {
        if (!_sessions.ContainsKey(sessionId))
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        // In a real implementation, this would call the Copilot API
        // For now, return a placeholder response
        return Task.FromResult(new CopilotResponse
        {
            Content = $"[Stub response for: {prompt.Substring(0, Math.Min(100, prompt.Length))}...]",
            FinishReason = "stop",
            TokensUsed = 100
        });
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<CopilotStreamEvent> StreamMessageAsync(
        string sessionId, 
        string prompt, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_sessions.ContainsKey(sessionId))
        {
            throw new InvalidOperationException($"Session {sessionId} not found");
        }

        // Simulate streaming response
        var words = prompt.Split(' ').Take(10);
        foreach (var word in words)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            yield return new CopilotStreamEvent
            {
                Type = "content",
                Content = word + " ",
                Timestamp = DateTime.UtcNow
            };

            await Task.Delay(50, cancellationToken);
        }

        yield return new CopilotStreamEvent
        {
            Type = "done",
            Content = "",
            Timestamp = DateTime.UtcNow
        };
    }

    /// <inheritdoc/>
    public Task<SessionInfo?> GetSessionInfoAsync(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var state))
        {
            return Task.FromResult<SessionInfo?>(null);
        }

        return Task.FromResult<SessionInfo?>(new SessionInfo
        {
            Id = state.Id,
            Model = state.Model,
            CreatedAt = state.CreatedAt,
            MessageCount = state.Messages.Count,
            Status = "active"
        });
    }

    /// <inheritdoc/>
    public Task<IEnumerable<SessionMessage>> GetSessionMessagesAsync(string sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var state))
        {
            return Task.FromResult<IEnumerable<SessionMessage>>(Array.Empty<SessionMessage>());
        }

        return Task.FromResult(state.Messages.AsEnumerable());
    }

    /// <inheritdoc/>
    public Task AbortSessionAsync(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var state))
        {
            state.IsAborted = true;
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task CloseSessionAsync(string sessionId)
    {
        _sessions.Remove(sessionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<SessionInfo>> ListSessionsAsync()
    {
        var sessions = _sessions.Values.Select(s => new SessionInfo
        {
            Id = s.Id,
            Model = s.Model,
            CreatedAt = s.CreatedAt,
            MessageCount = s.Messages.Count,
            Status = s.IsAborted ? "aborted" : "active"
        });

        return Task.FromResult(sessions);
    }

    private class SessionState
    {
        public string Id { get; set; } = "";
        public string Model { get; set; } = "";
        public string? RepositoryPath { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SessionMessage> Messages { get; set; } = new();
        public bool IsAborted { get; set; }
    }
}
