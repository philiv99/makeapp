namespace MakeApp.Core.Exceptions;

/// <summary>
/// Exception thrown when a phase is not complete but advancement is requested
/// </summary>
public class PhaseNotCompleteException : Exception
{
    /// <summary>Phase number that is not complete</summary>
    public int PhaseNumber { get; }
    
    /// <summary>Reason why the phase is not complete</summary>
    public string Reason { get; }
    
    /// <summary>Creates a new PhaseNotCompleteException</summary>
    public PhaseNotCompleteException(int phaseNumber, string reason) 
        : base($"Phase {phaseNumber} is not complete: {reason}")
    {
        PhaseNumber = phaseNumber;
        Reason = reason;
    }
}
