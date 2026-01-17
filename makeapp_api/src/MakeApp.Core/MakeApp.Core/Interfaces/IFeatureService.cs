using MakeApp.Core.Entities;
using MakeApp.Core.Enums;

namespace MakeApp.Core.Interfaces;

/// <summary>
/// Service interface for feature management
/// </summary>
public interface IFeatureService
{
    /// <summary>Create a new feature</summary>
    Task<Feature> CreateFeatureAsync(CreateFeatureRequest dto);
    
    /// <summary>Get a feature by ID</summary>
    Task<Feature?> GetFeatureAsync(string id);
    
    /// <summary>Get all features</summary>
    Task<IEnumerable<Feature>> GetAllFeaturesAsync();
    
    /// <summary>Get features for a repository</summary>
    Task<IEnumerable<Feature>> GetFeaturesByRepositoryAsync(string owner, string name);
    
    /// <summary>Update a feature</summary>
    Task<Feature> UpdateFeatureAsync(string id, UpdateFeatureRequest dto);
    
    /// <summary>Delete a feature</summary>
    Task<bool> DeleteFeatureAsync(string id);
    
    /// <summary>Import a feature from a file stream</summary>
    Task<Feature> ImportFromFileAsync(Stream fileStream, string fileName);
    
    /// <summary>Import a feature from markdown content</summary>
    Task<Feature> ImportFromMarkdownAsync(string markdown);
    
    /// <summary>Format a feature as a Copilot prompt</summary>
    Task<string> FormatAsPromptAsync(string featureId, PromptStyle style = PromptStyle.Structured);
}

/// <summary>
/// Request to create a new feature
/// </summary>
public class CreateFeatureRequest
{
    /// <summary>Feature title</summary>
    public string Title { get; set; } = "";
    
    /// <summary>Feature description</summary>
    public string Description { get; set; } = "";
    
    /// <summary>Acceptance criteria</summary>
    public List<string>? AcceptanceCriteria { get; set; }
    
    /// <summary>Technical notes</summary>
    public List<string>? TechnicalNotes { get; set; }
    
    /// <summary>Priority</summary>
    public FeaturePriority Priority { get; set; } = FeaturePriority.Medium;
    
    /// <summary>Repository owner</summary>
    public string? RepositoryOwner { get; set; }
    
    /// <summary>Repository name</summary>
    public string? RepositoryName { get; set; }
    
    /// <summary>Base branch</summary>
    public string? BaseBranch { get; set; }
}

/// <summary>
/// Request to update a feature
/// </summary>
public class UpdateFeatureRequest
{
    /// <summary>Feature title</summary>
    public string? Title { get; set; }
    
    /// <summary>Feature description</summary>
    public string? Description { get; set; }
    
    /// <summary>Acceptance criteria</summary>
    public List<string>? AcceptanceCriteria { get; set; }
    
    /// <summary>Technical notes</summary>
    public List<string>? TechnicalNotes { get; set; }
    
    /// <summary>Priority</summary>
    public FeaturePriority? Priority { get; set; }
    
    /// <summary>Status</summary>
    public FeatureStatus? Status { get; set; }
}
