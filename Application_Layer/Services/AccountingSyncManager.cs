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
        public async Task SyncFromXeroAsync(string xeroId)////Its job is to pull updated data from Xero and sync it into your database.
        {
            try
            {
                Console.WriteLine("xeroId->" + xeroId);
                _logger.LogInformation("🔄 Starting Xero → DB synchronization (latest customer only)...");
                var contactsJson = await _xeroApiManager.GetCustomerByXeroIdAsync(xeroId);
                var root = JsonConvert.DeserializeObject<JObject>(contactsJson);
                var contactsArray = root["Contacts"]?.ToObject<List<CustomerReadDto>>() ?? new List<CustomerReadDto>();
                var latestDto = contactsArray.FirstOrDefault();
                Console.WriteLine("Name = " + latestDto.Name);
                if (latestDto == null)
                {
                    _logger.LogInformation("No contacts found in Xero response.");
                    return;
                }

                // 2️⃣ Extract full contact JSON for debugging
                var contact = root["Contacts"]?.FirstOrDefault();
                Console.WriteLine("FULL CONTACT JSON -> " + contact?.ToString(Formatting.Indented));

                // 3️⃣ Safely extract phone number
                var phones = contact?["Phones"]?.ToObject<List<JObject>>();
                if (phones != null && phones.Count > 0)
                {
                    // Try to find the DEFAULT phone; fallback to any non-empty phone
                    var defaultPhone = phones.FirstOrDefault(p =>
                        string.Equals(p["PhoneType"]?.ToString(), "DEFAULT", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(p["PhoneNumber"]?.ToString()));

                    if (defaultPhone == null)
                    {
                        defaultPhone = phones.FirstOrDefault(p =>
                            !string.IsNullOrWhiteSpace(p["PhoneNumber"]?.ToString()));
                    }

                    latestDto.Phone = defaultPhone?["PhoneNumber"]?.ToString() ?? string.Empty;
                }
                else
                {
                    latestDto.Phone = string.Empty;
                }

                Console.WriteLine($"📞 Extracted phone: '{latestDto.Phone}'");

                // 4️⃣ Safely extract address
                var addresses = contact?["Addresses"]?.ToObject<List<JObject>>();
                if (addresses != null && addresses.Count > 0)
                {
                    var street = addresses.FirstOrDefault(a =>
                        string.Equals(a["AddressType"]?.ToString(), "STREET", StringComparison.OrdinalIgnoreCase));
                    latestDto.Address = street?["AddressLine1"]?.ToString() ?? string.Empty;
                }
                else
                {
                    latestDto.Address = string.Empty;
                }

                Console.WriteLine($"🏠 Extracted address: '{latestDto.Address}'");

                // 5️⃣ Check if the customer already exists in DB
                var existing = await _customerRepository.GetByXeroIdAsync(latestDto.XeroId);
                if (existing == null)
                {
                    existing = await _customerRepository.GetByDetailsAsync(
                        latestDto.Name, latestDto.Email, latestDto.Phone, latestDto.Address);
                    //esi nra hamara,vortev erb demic mer db-um customer enq avelacnum,et customery aranc XeroId-ia add arvum mer db-um
                    //heto vor webhook-ov galis enq methodi katarmany,await _customerRepository.GetByXeroIdAsync(latestDto.XeroId); esi null a
                    //veradardznelu vortev es logikayic heto enq mer db-um avelacrac customeri XeroId-n tarmacnum datarkic->Xero mej inch XeroId drvaca dranov
                    //u vor null a talis mer db-um add enq anum nuyn tvyalnerov customer inchy hangecnuma exception-i,isk vor zusapenq dranic mihatel check enq anum
                    //sax filedery arac XeroId-ii,u ete gtnuma tenc field parzapes tarmacnum enq et customeri tvyalnery vochte insert anum.
                }
                if (existing == null)
                {
                    _logger.LogInformation($"🟢 Adding new contact: {latestDto.Name}");

                    var customer = new Customer
                    {
                        XeroId = latestDto.XeroId,
                        Name = latestDto.Name,
                        Email = latestDto.Email,
                        Phone = latestDto.Phone,
                        Address = latestDto.Address,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        SyncedToXero = true
                    };

                    await _customerRepository.InsertAsync(customer);
                }
                else
                {
                    _logger.LogInformation($"🟡 Updating existing contact: {latestDto.Name}");

                    existing.Name = latestDto.Name;
                    existing.Email = latestDto.Email;
                    existing.Phone = latestDto.Phone;
                    existing.Address = latestDto.Address;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.SyncedToXero = true;

                    await _customerRepository.UpdateAsync(existing);
                }

                _logger.LogInformation("✅ Latest Xero contact synced successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during Xero → DB synchronization");
                throw;
            }
        }

    }
}
