using ComputerRepairSystem.company.Data;
using ComputerRepairSystem.company.Entities;
using ComputerRepairSystem.company.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComputerRepairSystem.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly CustomerService _customerService;
    private readonly IDbContextFactory<TenantDbContext> _dbFactory;

    public CustomersController(
        CustomerService customerService,
        IDbContextFactory<TenantDbContext> dbFactory)
    {
        _customerService = customerService;
        _dbFactory = dbFactory;
    }


    // ==========================================
    // GET ALL CUSTOMERS
    // GET: api/customers
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var customers =
            await _customerService.GetAllAsync();

        return Ok(customers);
    }


    // ==========================================
    // GET CUSTOMER BY ID
    // GET: api/customers/{id}
    // ==========================================

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id)
    {
        var customers =
            await _customerService.GetAllAsync();

        var customer =
            customers.FirstOrDefault(
                x => x.CustomerId == id);

        if (customer == null)
        {
            return NotFound();
        }

        return Ok(customer);
    }


    // ==========================================
    // SEARCH CUSTOMERS
    // GET: api/customers/search?name=Juan
    // ==========================================

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(
                "Search name is required.");
        }

        var customers =
            await _customerService.GetAllAsync();

        var results =
            customers
                .Where(x =>
                    x.FirstName.Contains(
                        name,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    x.LastName.Contains(
                        name,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    (
                        x.FirstName + " " + x.LastName
                    ).Contains(
                        name,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

        return Ok(results);
    }


    // ==========================================
    // GET CUSTOMER + DEVICES + SERVICE REQUESTS
    // GET: api/customers/{id}/services
    // ==========================================

    // ==========================================
    // GET CUSTOMER + DEVICES + SERVICE REQUESTS + REPAIRS
    // GET: api/customers/{id}/services
    // ==========================================

    [HttpGet("{id:int}/services")]
    public async Task<IActionResult> GetCustomerServices(
        int id)
    {
        await using var db =
            await _dbFactory.CreateDbContextAsync();

        var customer =
            await db.Customers
                .AsNoTracking()
                .Where(c =>
                    c.CustomerId == id)
                .Select(c => new
                {
                    c.CustomerId,
                    c.FirstName,
                    c.MiddleName,
                    c.LastName,
                    c.Phone,
                    c.Email,
                    c.Address,

                    Devices =
                        c.Devices
                            .Select(d => new
                            {
                                d.DeviceId,
                                d.DeviceType,
                                d.Brand,
                                d.Model,
                                d.SerialNumber,
                                d.DeviceCondition,

                                ServiceRequests =
                                    d.ServiceRequests
                                        .Select(r => new
                                        {
                                            r.ServiceRequestId,
                                            r.RequestDate,
                                            r.Description,
                                            r.Status,
                                            r.Priority,

                                            Repair =
                                                r.Repair == null
                                                    ? null
                                                    : new
                                                    {
                                                        r.Repair.RepairId,
                                                        r.Repair.ServiceRequestId,
                                                        r.Repair.TechnicianId,
                                                        r.Repair.Diagnosis,
                                                        r.Repair.RepairDescription,
                                                        r.Repair.Status,
                                                        r.Repair.StartDate,
                                                        r.Repair.EndDate,

                                                        RepairItems =
                                                            r.Repair.RepairItems
                                                                .Select(ri => new
                                                                {
                                                                    ri.RepairItemId,
                                                                    ri.RepairId,
                                                                    ri.ItemId,
                                                                    ri.Quantity,
                                                                    ri.UnitPrice,
                                                                    ri.Discount,

                                                                    Item =
                                                                        ri.Item == null
                                                                            ? null
                                                                            : new
                                                                            {
                                                                                ri.Item.ItemId,
                                                                                ri.Item.ItemName,
                                                                                ri.Item.Category,
                                                                                ri.Item.Brand,
                                                                                ri.Item.Model,
                                                                                ri.Item.Unit,
                                                                                ri.Item.UnitPrice
                                                                            }
                                                                })
                                                    }
                                        })
                            })
                })
                .FirstOrDefaultAsync();

        if (customer == null)
        {
            return NotFound();
        }

        return Ok(customer);
    }


    // ==========================================
    // CREATE CUSTOMER
    // POST: api/customers
    // ==========================================

    [HttpPost]
    public async Task<IActionResult> Create(
        Customer customer)
    {
        await _customerService.AddAsync(
            customer);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = customer.CustomerId
            },
            customer);
    }


    // ==========================================
    // UPDATE CUSTOMER
    // PUT: api/customers/{id}
    // ==========================================

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        Customer updatedCustomer)
    {
        var customers =
            await _customerService.GetAllAsync();

        var customer =
            customers.FirstOrDefault(
                x => x.CustomerId == id);

        if (customer == null)
        {
            return NotFound();
        }

        customer.FirstName =
            updatedCustomer.FirstName;

        customer.MiddleName =
            updatedCustomer.MiddleName;

        customer.LastName =
            updatedCustomer.LastName;

        customer.Phone =
            updatedCustomer.Phone;

        customer.Email =
            updatedCustomer.Email;

        customer.Address =
            updatedCustomer.Address;

        await _customerService.UpdateAsync(
            customer);

        return NoContent();
    }


    // ==========================================
    // DELETE CUSTOMER
    // DELETE: api/customers/{id}
    // ==========================================

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(
        int id)
    {
        var customers =
            await _customerService.GetAllAsync();

        var customer =
            customers.FirstOrDefault(
                x => x.CustomerId == id);

        if (customer == null)
        {
            return NotFound();
        }

        await _customerService.DeleteAsync(
            id);

        return NoContent();
    }
}