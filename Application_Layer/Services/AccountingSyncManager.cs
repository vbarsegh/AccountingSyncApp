using Application.DTOs;
using Application_Layer.DTO.Customers;
using Application_Layer.DTO.Invoices;
using Application_Layer.DTO.Quotes;
using Application_Layer.Interfaces;
using Application_Layer.Interfaces.QuickBooks;
using Application_Layer.Interfaces.Xero;
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
    public class AccountingSyncManager : IAccountingSyncManager
    {
        private readonly IXeroApiManager _xeroApiManager;
        private readonly IQuickBooksApiManager _qb;//
        private readonly IXeroCustomerSyncService _xeroCustomerSync;
        private readonly IXeroInvoiceSyncService _xeroInvoiceSync;
        private readonly  IXeroQuoteSyncService _xeroQuoteSync;
        private readonly ICustomerRepository _customerRepository;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IQuoteRepository _quoteRepository;

        private readonly ILogger<AccountingSyncManager> _logger;

        public AccountingSyncManager(
            IXeroApiManager xeroApiManager,
             IQuickBooksApiManager qb,
     IXeroCustomerSyncService xeroCustomerSync,
     IXeroInvoiceSyncService xeroInvoiceSync,
     IXeroQuoteSyncService xeroQuoteSync,
     ICustomerRepository customerRepository,
     IInvoiceRepository invoiceRepository,
     IQuoteRepository quoteRepository,
     ILogger<AccountingSyncManager> logger)
        {
            _xeroApiManager = xeroApiManager;
            _qb = qb;
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
        public async Task SyncCustomersFromXeroAsync(string CustomerXeroId)////Its job is to pull updated data from Xero and sync it into your database.
        {
            try
            {
                Console.WriteLine("xeroId->" + CustomerXeroId);
                _logger.LogInformation("🔄 Starting Xero → DB synchronization (latest customer only)...");
                var contactsJson = await _xeroApiManager.GetCustomerByXeroIdAsync(CustomerXeroId);
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
        //part of Invoices
        public async Task SyncInvoicesFromXeroAsync(string invoiceXeroId)
        {
            try
            {
                _logger.LogInformation("🔄 Starting Xero → DB synchronization for invoice {invoiceXeroId}", invoiceXeroId);
                Console.WriteLine("Invoice Xero Id = "  + invoiceXeroId);
                // 1️⃣ Get full invoice JSON from Xero
                var invoicesJson = await _xeroApiManager.GetInvoiceByXeroIdAsync(invoiceXeroId);
                var root = JsonConvert.DeserializeObject<JObject>(invoicesJson);
                var invoicesArray = root["Invoices"]?.ToObject<List<InvoiceReadDto>>() ?? new List<InvoiceReadDto>();
                var latestDto = invoicesArray.FirstOrDefault();

                if (latestDto == null)
                {
                    _logger.LogWarning("No invoice found in Xero response for ID: {invoiceXeroId}", invoiceXeroId);
                    return;
                }

                // ✅ Extract customer (ContactID) from nested Contact object
                string customerXeroId = latestDto.Contact?.ContactID ?? string.Empty;

                _logger.LogInformation("✅ Received invoice #{InvoiceNumber} for customer {CustomerXeroId}",
                    latestDto.InvoiceNumber, customerXeroId);

                // 2️⃣ Find the local customer by their XeroId
                var customer = await _customerRepository.GetByXeroIdAsync(customerXeroId);
                if (customer == null)
                {
                    Console.WriteLine("⚠️ No matching local customer for XeroId={CustomerXeroId}. Skipping invoice sync." + customerXeroId);
                    return;
                }

                // 3️⃣ Check if invoice already exists in local DB
                var existingInvoice = await _invoiceRepository.GetByInvoiceXeroIdAsync(latestDto.InvoiceXeroId);

                if (existingInvoice == null)
                {
                    Console.WriteLine("🟢 Adding new invoice: {InvoiceNumber}" +  latestDto.InvoiceNumber);

                    var invoice = new Invoice
                    {
                        XeroId = latestDto.InvoiceXeroId, // matches your Domain model property
                        CustomerId = customer.Id,
                        CustomerXeroId = customerXeroId,
                        InvoiceNumber = latestDto.InvoiceNumber,
                        Description = latestDto.Description,
                        TotalAmount = latestDto.TotalAmount,
                        DueDate = latestDto.DueDate ?? DateTime.UtcNow.AddDays(30),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        SyncedToXero = true
                    };

                    await _invoiceRepository.InsertAsync(invoice);
                }
                else
                {
                    Console.WriteLine("🟡 Updating existing invoice: {InvoiceNumber}" + latestDto.InvoiceNumber);

                    existingInvoice.CustomerId = customer.Id;
                    existingInvoice.CustomerXeroId = customerXeroId;
                    existingInvoice.InvoiceNumber = latestDto.InvoiceNumber;
                    existingInvoice.Description = latestDto.Description;
                    existingInvoice.TotalAmount = latestDto.TotalAmount;
                    existingInvoice.DueDate = latestDto.DueDate ?? DateTime.UtcNow.AddDays(30);
                    existingInvoice.UpdatedAt = DateTime.UtcNow;
                    existingInvoice.SyncedToXero = true;

                    await _invoiceRepository.UpdateAsync(existingInvoice);
                }

                _logger.LogInformation("✅ Invoice synced successfully (#{InvoiceNumber}).", latestDto.InvoiceNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during Xero → DB invoice synchronization");
                throw;
            }
        }

        public async Task SyncQuotesFromXeroPeriodicallyAsync()
        {
            try
            {
                _logger.LogInformation("🔁 Polling Xero for new or updated Quotes...");

                // 1️⃣ Get all quotes from Xero
                var quotesJson = await _xeroApiManager.GetQuotesAsync();
                var root = JsonConvert.DeserializeObject<JObject>(quotesJson);
                var quotesArray = root["Quotes"]?.ToObject<List<QuoteReadDto>>() ?? new List<QuoteReadDto>();

                foreach (var q in quotesArray)
                {
                    if (q == null || q.QuoteXeroId == null)
                        continue;

                    var local = await _quoteRepository.GetByQuoteXeroIdAsync(q.QuoteXeroId);

                    if (local == null)
                    {
                        // !!!!!!!!!!!!1quote doesn’t exist locally → INSERT!!!!!!!!!!!!!!!1
                        // New quote found in Xero → insert to local DB
                        var customer = await _customerRepository.GetByXeroIdAsync(q.Contact?.ContactID ?? "");
                        if (customer == null)
                        {
                            Console.WriteLine("⚠️ No matching local customer. Skipping quote sync.");
                            continue;
                        }

                        var newQuote = new Quote
                        {
                            XeroId = q.QuoteXeroId,
                            CustomerId = customer.Id,
                            CustomerXeroId = customer.XeroId,
                            QuoteNumber = q.QuoteNumber,
                            Description = q.Description,
                            TotalAmount = q.TotalAmount,
                            DueDate = q.DueDate,
                            ExpiryDate = q.ExpiryDate,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            SyncedToXero = true
                        };

                        await _quoteRepository.InsertAsync(newQuote);
                        _logger.LogInformation("🆕 Added new quote from Xero: {QuoteNumber}", q.QuoteNumber);
                    }
                    else if (q.UpdatedDateUTC > local.UpdatedAt)
                    {
                        var customer = await _customerRepository.GetByXeroIdAsync(q.Contact?.ContactID ?? "");
                        //!!!!!!!!!!quote exists but Xero version is newer → UPDATE!!!!!!!!!!!!!
                        // Quote updated in Xero → refresh local record
                        local.CustomerId = customer.Id;
                        local.CustomerXeroId = customer.XeroId;

                        local.QuoteNumber = q.QuoteNumber;
                        local.Description = q.Description;
                        local.TotalAmount = q.TotalAmount;
                        local.DueDate = q.DueDate;
                        local.ExpiryDate = q.ExpiryDate;
                        local.UpdatedAt = DateTime.UtcNow;
                        local.SyncedToXero = true;

                        await _quoteRepository.UpdateAsync(local);
                        _logger.LogInformation("♻️ Updated quote from Xero: {QuoteNumber}", q.QuoteNumber);
                    }
                }

                _logger.LogInformation("✅ Quote polling sync completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during periodic quote sync from Xero");
            }
        }

        //public async Task SyncQuotesFromXeroAsync(string quoteXeroId)
        //{
        //    try
        //    {
        //        _logger.LogInformation("🔄 Starting Xero → DB synchronization for quote {quoteXeroId}", quoteXeroId);

        //        // 1️⃣ Fetch quote data from Xero
        //        var quotesJson = await _xeroApiManager.GetQuoteByXeroIdAsync(quoteXeroId);
        //        var root = JsonConvert.DeserializeObject<JObject>(quotesJson);
        //        var quotesArray = root["Quotes"]?.ToObject<List<QuoteReadDto>>() ?? new List<QuoteReadDto>();
        //        var latestDto = quotesArray.FirstOrDefault();

        //        if (latestDto == null)
        //        {
        //            _logger.LogWarning("No quote found in Xero response for ID: {quoteXeroId}", quoteXeroId);
        //            return;
        //        }

        //        // ✅ Extract related customer info
        //        string customerXeroId = latestDto.Contact?.ContactID ?? string.Empty;

        //        _logger.LogInformation("✅ Received quote #{QuoteNumber} for customer {CustomerXeroId}",
        //            latestDto.QuoteNumber, customerXeroId);

        //        // 2️⃣ Find the local customer by their XeroId
        //        var customer = await _customerRepository.GetByXeroIdAsync(customerXeroId);
        //        if (customer == null)
        //        {
        //            Console.WriteLine("⚠️ No matching local customer for XeroId={CustomerXeroId}. Skipping quote sync." + customerXeroId);
        //            return;
        //        }

        //        // 3️⃣ Check if quote already exists locally
        //        var existingQuote = await _quoteRepository.GetByQuoteXeroIdAsync(latestDto.QuoteXeroId);

        //        if (existingQuote == null)
        //        {
        //            Console.WriteLine("🟢 Adding new quote: {QuoteNumber}" + latestDto.QuoteNumber);

        //            var quote = new Quote
        //            {
        //                XeroId = latestDto.QuoteXeroId,
        //                CustomerId = customer.Id,
        //                CustomerXeroId = customerXeroId,
        //                QuoteNumber = latestDto.QuoteNumber,
        //                Description = latestDto.Description,
        //                TotalAmount = latestDto.TotalAmount,
        //                DueDate = latestDto.DueDate,
        //                ExpiryDate = latestDto.ExpiryDate,
        //                CreatedAt = DateTime.UtcNow,
        //                UpdatedAt = DateTime.UtcNow,
        //                SyncedToXero = true
        //            };

        //            await _quoteRepository.InsertAsync(quote);
        //        }
        //        else
        //        {
        //            Console.WriteLine("🟡 Updating existing quote: {QuoteNumber}" + latestDto.QuoteNumber);

        //            existingQuote.CustomerId = customer.Id;
        //            existingQuote.CustomerXeroId = customerXeroId;
        //            existingQuote.QuoteNumber = latestDto.QuoteNumber;
        //            existingQuote.Description = latestDto.Description;
        //            existingQuote.TotalAmount = latestDto.TotalAmount;
        //            existingQuote.DueDate = latestDto.DueDate;
        //            existingQuote.ExpiryDate = latestDto.ExpiryDate;
        //            existingQuote.UpdatedAt = DateTime.UtcNow;
        //            existingQuote.SyncedToXero = true;

        //            await _quoteRepository.UpdateAsync(existingQuote);
        //        }

        //        _logger.LogInformation("✅ Quote synced successfully (#{QuoteNumber}).", latestDto.QuoteNumber);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "❌ Error during Xero → DB quote synchronization");
        //        throw;
        //    }
        //}

        public async Task<bool> CheckInvoice_QuotesDtoCustomerIdAndCustomerXeroIDAppropriatingInLocalDbValues(int CustomerId, string CustomerXeroId)
        {
            Console.WriteLine("hasanq check methodin");
            var customer = await _customerRepository.GetByIdAsync(CustomerId);

            if (customer == null)
                throw new Exception($"Customer with ID {CustomerId} not found in local DB.");

            if (customer.XeroId != CustomerXeroId)
                throw new Exception($"Mismatch: local customer (ID={CustomerId}) has XeroId={customer.XeroId}, " +
                                    $"but request provided {CustomerXeroId}.");

            return true;
        }
        //ste pti QuickBooksi pahy avelacvi


        public async Task HandleQuickBooksCustomerChangedAsync(string quickBooksCustomerId)
        {
            try
            {
                _logger.LogInformation("🔄 Starting QuickBooks → DB sync for Customer ID={Id}", quickBooksCustomerId);

                // 1️⃣ Get full customer JSON from QuickBooks API
                var customerJson = await _qb.GetCustomerByIdAsync(quickBooksCustomerId);
                if (string.IsNullOrWhiteSpace(customerJson))
                {
                    _logger.LogWarning("⚠️ Empty response from QuickBooks for Customer ID={Id}", quickBooksCustomerId);
                    return;
                }

                var root = JsonConvert.DeserializeObject<JObject>(customerJson);
                var customerObj = root["Customer"];
                if (customerObj == null)
                {
                    _logger.LogWarning("⚠️ No 'Customer' object found in QuickBooks JSON for ID={Id}", quickBooksCustomerId);
                    return;
                }

                // 2️⃣ Map QuickBooks JSON → DTO
                var name = customerObj["DisplayName"]?.ToString() ?? string.Empty;
                var email = customerObj["PrimaryEmailAddr"]?["Address"]?.ToString() ?? string.Empty;
                var phone = customerObj["PrimaryPhone"]?["FreeFormNumber"]?.ToString() ?? string.Empty;
                var address = customerObj["BillAddr"]?["Line1"]?.ToString() ?? string.Empty;

                _logger.LogInformation("📦 Received QuickBooks customer: {Name}, {Email}, {Phone}, {Address}", name, email, phone, address);

                // 3️⃣ Try to find this customer in local DB by QuickBooksId
                var existing = await _customerRepository.GetByQuickBooksIdAsync(quickBooksCustomerId);

                if (existing == null)
                {
                    // Also check by name + email to avoid duplicates
                    existing = await _customerRepository.GetByDetailsAsync(name, email, phone, address);
                }

                // 4️⃣ Create or update local record
                if (existing == null)
                {
                    _logger.LogInformation("🟢 Adding new customer from QuickBooks: {Name}", name);

                    var newCustomer = new Customer
                    {
                        QuickBooksId = quickBooksCustomerId,
                        Name = name,
                        Email = email,
                        Phone = phone,
                        Address = address,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        SyncedToQuickBooks = true
                    };

                    await _customerRepository.InsertAsync(newCustomer);
                    _logger.LogInformation("✅ Customer inserted in local DB: {Name} (QuickBooksId={Id})", name, quickBooksCustomerId);
                }
                else
                {
                    _logger.LogInformation("🟡 Updating existing local customer: {Name}", name);

                    existing.QuickBooksId = quickBooksCustomerId;
                    existing.Name = name;
                    existing.Email = email;
                    existing.Phone = phone;
                    existing.Address = address;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.SyncedToQuickBooks = true;

                    await _customerRepository.UpdateAsync(existing);
                    _logger.LogInformation("✅ Customer updated in local DB: {Name} (QuickBooksId={Id})", name, quickBooksCustomerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during QuickBooks → DB customer synchronization");
                throw;
            }
        }

    }
}
