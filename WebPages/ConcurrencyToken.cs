namespace OSDC.Drilling.Trajectory.WebPages;

internal static class ConcurrencyToken
{
    public static DateTimeOffset Require(DateTimeOffset? value) => value ??
        throw new InvalidOperationException("The resource has no LastModificationDate concurrency token. Refresh it before mutating.");
}
