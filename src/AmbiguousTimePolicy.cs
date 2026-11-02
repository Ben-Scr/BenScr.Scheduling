namespace BenScr.Scheduling;

/// <summary>Determines which UTC instants represent a repeated local time during a backward offset change.</summary>
public enum AmbiguousTimePolicy
{
    /// <summary>Keep the earlier UTC instant only; it uses the larger UTC offset.</summary>
    Earlier,
    /// <summary>Keep the later UTC instant only; it uses the smaller UTC offset.</summary>
    Later,
    /// <summary>Keep both instants; successive exclusive queries can retrieve each one.</summary>
    Both,
    /// <summary>Omit the ambiguous local time entirely.</summary>
    Skip
}
