using BookingManagement.Application.Abstractions;
using BookingManagement.Application.Common;
using BookingManagement.Application.Dtos;
using BookingManagement.Domain.Entities;
using BookingManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace BookingManagement.Application.Services;

/// <summary>
/// Orchestrates slot booking/cancellation using the layered concurrency strategy:
///   1. Redis distributed lock (fast-fail gate; avoids hammering Postgres when many
///      users hit "Book" on the same slot at once).
///   2. Postgres optimistic concurrency (source of truth; the Slot/Appointment
///      Version column guards against any race the lock didn't catch, e.g. lock
///      expiry or multi-instance clock skew).
///   3. Polly retry-with-backoff around the concurrency check, so a genuine but
///      rare version conflict doesn't surface to the user as a hard failure.
///   4. On success, publishes a domain event so the SignalR hub / notification
///      worker can react.
/// </summary>
public class AppointmentBookingService
{
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LockWait = TimeSpan.FromSeconds(3);

    private readonly IAppointmentBookingRepository _repository;
    private readonly IDistributedLockService _lockService;
    private readonly IBookingEventPublisher _eventPublisher;
    private readonly ILogger<AppointmentBookingService> _logger;
    private readonly AsyncRetryPolicy _concurrencyRetryPolicy;

    public AppointmentBookingService(
        IAppointmentBookingRepository repository,
        IDistributedLockService lockService,
        IBookingEventPublisher eventPublisher,
        ILogger<AppointmentBookingService> logger)
    {
        _repository = repository;
        _lockService = lockService;
        _eventPublisher = eventPublisher;
        _logger = logger;

        _concurrencyRetryPolicy = Policy
            .Handle<DbUpdateConcurrencyException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(50 * Math.Pow(2, attempt)),
                onRetry: (exception, delay, attempt, _) =>
                    _logger.LogWarning(
                        "Concurrency conflict on attempt {Attempt}, retrying in {Delay}ms", attempt, delay.TotalMilliseconds));
    }

    public async Task<List<SlotDto>> SearchAvailableSlotsAsync(Guid doctorId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
    {
        var slots = await _repository.GetAvailableSlotsAsync(doctorId, fromUtc, toUtc, cancellationToken);

        return slots
            .Select(s => new SlotDto(s.Id, s.DoctorId, s.Doctor?.FullName ?? string.Empty, s.StartTimeUtc, s.EndTimeUtc, s.Status.ToString()))
            .ToList();
    }

    public async Task<Result<AppointmentDto>> BookAppointmentAsync(
        string authUserId,
        string email,
        string fullName,
        BookAppointmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var lockKey = $"slot-lock:{request.SlotId}";
        await using var slotLock = await _lockService.AcquireAsync(lockKey, LockExpiry, LockWait, cancellationToken);

        if (slotLock is null)
        {
            return Result<AppointmentDto>.Failure(
                "This slot is currently being booked by someone else. Please try again in a moment.",
                BookingErrorCode.LockTimeout);
        }

        try
        {
            Appointment? appointment = null;

            await _concurrencyRetryPolicy.ExecuteAsync(async () =>
            {
                var slot = await _repository.GetSlotWithAppointmentAsync(request.SlotId, cancellationToken);
                if (slot is null)
                {
                    throw new BookingDomainException(BookingErrorCode.NotFound, "Slot not found.");
                }

                if (slot.Status != SlotStatus.Available)
                {
                    throw new BookingDomainException(BookingErrorCode.SlotAlreadyBooked, "This slot is no longer available.");
                }

                var patient = await _repository.GetOrCreatePatientAsync(authUserId, email, fullName, cancellationToken);

                slot.Status = SlotStatus.Booked;
                slot.UpdatedAtUtc = DateTime.UtcNow;

                appointment = new Appointment
                {
                    SlotId = slot.Id,
                    PatientId = patient.Id,
                    Status = AppointmentStatus.Booked,
                    Reason = request.Reason,
                    Slot = slot,
                    Patient = patient
                };

                await _repository.AddAppointmentAsync(appointment, cancellationToken);

                // SaveChangesAsync throws DbUpdateConcurrencyException if another
                // request already flipped this Slot's Version — Polly retries the
                // whole block above, re-reading the slot's latest state.
                await _repository.SaveChangesAsync(cancellationToken);
            });

            await _eventPublisher.PublishAppointmentBookedAsync(appointment!, cancellationToken);

            return Result<AppointmentDto>.Success(ToDto(appointment!));
        }
        catch (BookingDomainException ex)
        {
            return Result<AppointmentDto>.Failure(ex.Message, ex.ErrorCode);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<AppointmentDto>.Failure(
                "This slot was just booked by someone else. Please pick another slot.",
                BookingErrorCode.ConcurrencyConflict);
        }
    }

    public async Task<Result<AppointmentDto>> CancelAppointmentAsync(
        Guid appointmentId,
        string cancellationReason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Appointment? appointment = null;

            await _concurrencyRetryPolicy.ExecuteAsync(async () =>
            {
                appointment = await _repository.GetAppointmentAsync(appointmentId, cancellationToken);
                if (appointment is null)
                {
                    throw new BookingDomainException(BookingErrorCode.NotFound, "Appointment not found.");
                }

                appointment.Status = AppointmentStatus.Cancelled;
                appointment.CancellationReason = cancellationReason;
                appointment.UpdatedAtUtc = DateTime.UtcNow;

                if (appointment.Slot is not null)
                {
                    appointment.Slot.Status = SlotStatus.Available;
                    appointment.Slot.UpdatedAtUtc = DateTime.UtcNow;
                }

                await _repository.SaveChangesAsync(cancellationToken);
            });

            await _eventPublisher.PublishAppointmentCancelledAsync(appointment!, cancellationToken);

            return Result<AppointmentDto>.Success(ToDto(appointment!));
        }
        catch (BookingDomainException ex)
        {
            return Result<AppointmentDto>.Failure(ex.Message, ex.ErrorCode);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<AppointmentDto>.Failure(
                "This appointment was just modified elsewhere. Please refresh and try again.",
                BookingErrorCode.ConcurrencyConflict);
        }
    }

    private static AppointmentDto ToDto(Appointment appointment) => new(
        appointment.Id,
        appointment.SlotId,
        appointment.PatientId,
        appointment.Slot?.StartTimeUtc ?? default,
        appointment.Slot?.EndTimeUtc ?? default,
        appointment.Status.ToString(),
        appointment.Reason);
}

/// <summary>Expected, business-level booking failure (not found / already booked) — not a bug.</summary>
internal class BookingDomainException : Exception
{
    public BookingErrorCode ErrorCode { get; }

    public BookingDomainException(BookingErrorCode errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
