namespace BookingManagement.Application.Abstractions;

/// <summary>
/// A short-lived distributed lock, e.g. backed by Redis SET NX PX.
/// Used as a fast-fail gate before hitting Postgres, so concurrent booking
/// attempts for the same slot don't all pile onto the database at once.
/// </summary>
public interface IDistributedLockService
{
    /// <summary>
    /// Attempts to acquire a lock for <paramref name="key"/>. Returns a disposable
    /// handle that releases the lock on Dispose, or null if the lock could not be
    /// acquired within <paramref name="wait"/>.
    /// </summary>
    Task<IAsyncDisposable?> AcquireAsync(string key, TimeSpan expiry, TimeSpan wait, CancellationToken cancellationToken = default);
}
