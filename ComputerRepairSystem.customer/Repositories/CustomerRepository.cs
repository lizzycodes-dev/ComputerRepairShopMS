using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Entities;
using ComputerRepairSystem.company.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace ComputerRepairSystem.company.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly ITenantDbContextFactory _contextFactory;
    private readonly ICompanyContext _companyContext;
    public CustomerRepository(
        ITenantDbContextFactory contextFactory,
        ICompanyContext companyContext)
    {
        _contextFactory = contextFactory;
        _companyContext = companyContext;
    }


    // ==========================================
    // GET ALL
    // ==========================================

    public async Task<List<Customer>> GetAllAsync()
    {
        if (!_companyContext.CompanyId.HasValue)
        {
            throw new InvalidOperationException(
                "A company is required to access customer data.");
        }

        await using var context =
            await _contextFactory.CreateAsync(
                _companyContext.CompanyId.Value);

        return await context.Customers
            .AsNoTracking()
            .OrderByDescending(c => c.CustomerId)
            .ToListAsync();
    }


    // ==========================================
    // GET BY ID
    // ==========================================

    public async Task<Customer?> GetByIdAsync(
        int customerId)
    {
        if (!_companyContext.CompanyId.HasValue)
        {
            throw new InvalidOperationException(
                "A company is required to access customer data.");
        }

        await using var context =
            await _contextFactory.CreateAsync(
                _companyContext.CompanyId.Value);

        return await context.Customers
            .FirstOrDefaultAsync(
                x => x.CustomerId == customerId);
    }


    // ==========================================
    // ADD
    // ==========================================

    public async Task<Customer> AddAsync(
        Customer customer)
    {
        if (!_companyContext.CompanyId.HasValue)
        {
            throw new InvalidOperationException(
                "A company is required to access customer data.");
        }

        await using var context =
            await _contextFactory.CreateAsync(
                _companyContext.CompanyId.Value);

        context.Customers.Add(customer);

        await context.SaveChangesAsync();

        return customer;
    }


    // ==========================================
    // UPDATE
    // ==========================================

    public async Task UpdateAsync(
        Customer customer)
    {
        if (!_companyContext.CompanyId.HasValue)
        {
            throw new InvalidOperationException(
                "A company is required to access customer data.");
        }

        await using var context =
            await _contextFactory.CreateAsync(
                _companyContext.CompanyId.Value);

        context.Customers.Update(customer);

        await context.SaveChangesAsync();
    }


    // ==========================================
    // DELETE
    // ==========================================

    public async Task DeleteAsync(
        int customerId)
    {
        if (!_companyContext.CompanyId.HasValue)
        {
            throw new InvalidOperationException(
                "A company is required to access customer data.");
        }

        await using var context =
            await _contextFactory.CreateAsync(
                _companyContext.CompanyId.Value);

        var customer =
            await context.Customers
                .FirstOrDefaultAsync(
                    x => x.CustomerId == customerId);

        if (customer == null)
            return;

        context.Customers.Remove(customer);

        await context.SaveChangesAsync();
    }
}