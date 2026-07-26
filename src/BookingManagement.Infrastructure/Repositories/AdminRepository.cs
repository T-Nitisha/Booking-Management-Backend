using BookingManagement.Application.Abstractions;
using BookingManagement.Domain.Entities;
using BookingManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BookingManagement.Infrastructure.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly AppDbContext _dbContext;

    public AdminRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<List<Department>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
        => _dbContext.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync(cancellationToken);

    public Task<Department?> GetDepartmentAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task AddDepartmentAsync(Department department, CancellationToken cancellationToken = default)
        => await _dbContext.Departments.AddAsync(department, cancellationToken);

    public Task<List<Doctor>> GetDoctorsAsync(CancellationToken cancellationToken = default)
        => _dbContext.Doctors.AsNoTracking().Include(d => d.Department).OrderBy(d => d.FullName).ToListAsync(cancellationToken);

    public Task<Doctor?> GetDoctorAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Doctors.Include(d => d.Department).FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task AddDoctorAsync(Doctor doctor, CancellationToken cancellationToken = default)
        => await _dbContext.Doctors.AddAsync(doctor, cancellationToken);

    public Task<bool> DepartmentExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Departments.AnyAsync(d => d.Id == id, cancellationToken);

    public async Task AddSlotsAsync(IEnumerable<Slot> slots, CancellationToken cancellationToken = default)
        => await _dbContext.Slots.AddRangeAsync(slots, cancellationToken);

    public Task<Slot?> GetSlotAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Slots.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void RemoveSlot(Slot slot) => _dbContext.Slots.Remove(slot);

    public void RemoveDepartment(Department department) => _dbContext.Departments.Remove(department);

    public void RemoveDoctor(Doctor doctor) => _dbContext.Doctors.Remove(doctor);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
