 using Application_Layer.DTO.Customers;
using Application_Layer.Interfaces;
using Application_Layer.Interfaces_Repository;
using Domain_Layer.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Application_Layer.Services
{
    /// <summary>
    /// Central coordinator that manages 2-way synchronization between the local database,
    /// Xero, and (later) QuickBooks. AccountingSyncManager acts as the “brain” of your entire synchronization system.
    //It doesn’t talk to APIs or the database directly — it coordinates other services that do.
    /// </summary>      
    public class AccountingSyncManager
    {
        private readonly IXeroApiManager _xeroApiManager;
        private readonly IXeroCustomerSyncService _xeroCustomerSync;
        private readonly IXeroInvoiceSyncService _xeroInvoiceSync;
        private readonly  IXeroQuoteSyncService _xeroQuoteSync;
        private readonly ICustomerRepository _customerRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IQuoteRepository _quoteRepository;

        private readonly ILogger<AccountingSyncManager> _logger;

        public AccountingSyncManager(
            IXeroApiManager xeroApiManager,
     IXeroCustomerSyncService xeroCustomerSync,
     IXeroInvoiceSyncService xeroInvoiceSync,
     IXeroQuoteSyncService xeroQuoteSync,
     ICustomerRepository customerRepository,
     IInvoiceRepository invoiceRepository,
     IQuoteRepository quoteRepository,
     ILogger<AccountingSyncManager> logger)
        {
            _xeroApiManager = xeroApiManager;
            _xeroCustomerSync = xeroCustomerSync;
            _xeroInvoiceSync = xeroInvoiceSync;
            _xeroQuoteSync = xeroQuoteSync;
            _customerRepository = customerRepository;
            _invoiceRepository = invoiceRepository;
            _quoteRepository = quoteRepository;
            _logger = logger;
        }


        //Xero → Local DB.
        //When does SyncFromXeroAsync happen?Automatically when Xero sends a webhook → your controller receives it and calls the sync.
        //what happens->Reads all customers from the local DB. If XeroId is empty → it’s new → create in Xero; otherwise → update Xero record.
        //The idea: whenever a webhook from Xero arrives (for example, a new invoice was created in Xero), you can call this method.
        public async Task SyncFromXeroAsync()////Its job is to pull updated data from Xero and sync it into your database.
        {
            try
            {
                Console.WriteLine("hasavvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv\n\n\n");
                _logger.LogInformation("🔄 Starting Xero → DB synchronization...");
                // 1️⃣ Get all contacts from Xero API
                var contactsJson = await _xeroApiManager.GetCustomersAsync();
                // 2️⃣ Parse JSON and extract "Contacts" array
                var root = JsonConvert.DeserializeObject<JObject>(contactsJson);
                var contactsArray = root["Contacts"]?.ToObject<List<CustomerReadDto>>() ?? new List<CustomerReadDto>();

                if (!contactsArray.Any())
                {
                    _logger.LogInformation("No contacts received from Xero.");
                    return;
                }

                // 3️⃣ Manually extract Phone and Address from nested JSON
                foreach (var item in root["Contacts"])
                {
                    var xeroId = item["ContactID"]?.ToString();
                    var dto = contactsArray.FirstOrDefault(c => c.XeroId == xeroId);
                    if (dto == null) continue;

                    dto.Phone = item["Phones"]?.FirstOrDefault()?["PhoneNumber"]?.ToString() ?? string.Empty;
                    dto.Address = item["Addresses"]?.FirstOrDefault()?["AddressLine1"]?.ToString() ?? string.Empty;
                }
                // 4️⃣ Sync each contact into local DB
                foreach (var dto in contactsArray)
                {
                    var existing = await _customerRepository.GetByXeroIdAndSyncedToXeroAsync(dto);
                   
                    if (existing != null && existing.SyncedToXero == true)
                        continue;
                    //{
                        if (existing == null)
                        {
                            ////doesn’t exist locally, insert it.
                            // 🟢 New contact → Insert
                            _logger.LogInformation($"🟢 Adding new contact: {dto.Name}");

                        var customer = new Customer()
                        {
                            XeroId = dto.XeroId,
                            Name = dto.Name,
                            Email = dto.Email,
                            Phone = dto.Phone,
                            Address = dto.Address,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            SyncedToXero = true
                        };
                        await _customerRepository.InsertAsync(customer);
                        ////DeleteAsync
                        //var tempCustomer = await _customerRepository.GetByXeroIdAsync(customer.XeroId);
                        //if (tempCustomer.SyncedToXero == false)
                        //{
                        //    Console.WriteLine("\n\n\nMTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAVVVVVVVVVVVVVVVVVVVVVVVVVV\n\n\n\n\n\n");
                        //    await _customerRepository.DeleteAsync(tempCustomer.Id - 1);
                        //    await _customerRepository.UpdateSyncedToXeroAsync(tempCustomer.Id);
                        //}
                        //remove customer by id-1
                    }
                        else
                        {
                            // 🟡 Existing contact → Update
                            ////already exists, update it.
                            _logger.LogInformation($"🟡 Updating existing customer: {dto.Name}");

                            existing.Name = dto.Name;
                            existing.Email = dto.Email;
                            existing.Phone = dto.Phone;
                            existing.Address = dto.Address;
                            existing.UpdatedAt = DateTime.UtcNow;
                            existing.SyncedToXero = false;//check anel

                            await _customerRepository.UpdateAsync(existing);
                        }
                    //}
                }
                _logger.LogInformation("✅ Xero → DB synchronization completed successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error during Xero → DB synchronization");
                    throw;
                }
        }


        
    }
}
