using Application_Layer.DTO.Customers;
using Application_Layer.Interfaces;
using Application_Layer.Interfaces_Repository;
using Domain_Layer.Models;
using Infrastructure_Layer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AccountingSyncApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocalDbController : ControllerBase
    {
        //private readonly ICustomerRepository _customerRepository;
        private readonly IXeroCustomerSyncService _xeroCustomerSync;
        //private readonly IXeroApiManager _xeroApiManager;
        private readonly ILogger<LocalDbController> _logger;

        public LocalDbController(ICustomerRepository customerRepository, IXeroApiManager xeroApiManager, IXeroCustomerSyncService xeroCustomerSync, ILogger<LocalDbController> logger)
        {
            //_customerRepository = customerRepository;
            _xeroCustomerSync = xeroCustomerSync;
            //_xeroApiManager = xeroApiManager;
            _logger = logger;
        }

        // ✅ POST: api/customers/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateCustomer([FromBody] CustomerCreateDto customerDto)
        {
            try
            {
                if (customerDto == null)
                    return BadRequest("Customer data is required.");

                _logger.LogInformation("📥 Creating new customer locally: {Name}", customerDto.Name);

                await _xeroCustomerSync.SyncCreatedCustomerAsync(customerDto);
                return Ok(new
                {
                    message = "Customer created successfully in local DB."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error while creating local customer.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        //// ✅ GET: api/customers
        //[HttpGet]
        //public async Task<IActionResult> GetAllCustomers()
        //{
        //    var customers = await  _customerRepository.GetAllAsync();
        //    return Ok(customers);
        //}
    }
}
