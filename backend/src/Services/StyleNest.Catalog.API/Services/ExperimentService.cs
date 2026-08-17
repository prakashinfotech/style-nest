namespace StyleNest.Catalog.API.Services;

/// <summary>
/// ENH-CAT-003 — A/B Variant Framework: deterministic, stable-hash bucket assignment.
///
/// The same (experimentName, participantId) pair always resolves to the same variant,
/// so users never change buckets mid-experiment. Variants are evenly distributed because
/// the hash is taken modulo the variant count without further bias.
///
/// Experiment duration persistence lives on the client (30-day localStorage TTL).
/// This endpoint acts as the authoritative server-side source and can be extended
/// with DB-backed experiment config (start/end dates, traffic split %) in a future phase.
/// </summary>
public interface IExperimentService
{
    /// <summary>
    /// Returns the stable variant name for the given participant in the given experiment.
    /// Variants are evenly distributed: each participant always receives the same variant.
    /// </summary>
    string AssignVariant(
        string experimentName,
        string participantId,
        IReadOnlyList<string> variants);
}

public sealed class ExperimentService : IExperimentService
{
    /// <inheritdoc />
    public string AssignVariant(
        string experimentName,
        string participantId,
        IReadOnlyList<string> variants)
    {
        if (variants.Count == 0)
            throw new ArgumentException(
                "At least one variant must be provided.", nameof(variants));

        // Combine experiment name + participant ID so different experiments produce
        // independent bucketing — a user in bucket 0 for experiment A need not be
        // in bucket 0 for experiment B.
        var key = $"{experimentName}:{participantId}";

        // Use ordinal (culture-insensitive) hash for determinism across app restarts;
        // GetHashCode with StringComparison.Ordinal is stable within a single process
        // but not across runtimes — use a custom FNV-1a if cross-runtime determinism
        // is required in a future phase.
        var hash = Fnv1a32(key);
        return variants[(int)(hash % (uint)variants.Count)];
    }

    /// <summary>FNV-1a 32-bit — process-restart-stable, evenly distributed.</summary>
    private static uint Fnv1a32(string s)
    {
        const uint OffsetBasis = 2166136261u;
        const uint Prime       = 16777619u;

        uint hash = OffsetBasis;
        foreach (var c in s)
        {
            hash ^= (byte)(c & 0xFF);
            hash *= Prime;
            hash ^= (byte)(c >> 8);
            hash *= Prime;
        }
        return hash;
    }
}
