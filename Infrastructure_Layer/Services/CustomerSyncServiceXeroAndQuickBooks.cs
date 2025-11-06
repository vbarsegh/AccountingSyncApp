using Application_Layer.DTO.Customers;
using Application_Layer.Interfaces;
using Application_Layer.Interfaces.QuickBooks;
using Application_Layer.Interfaces.Xero;
using Application_Layer.Interfaces_Repository;
using Application_Layer.Services;
using Domain_Layer.Models;
using Infrastructure_Layer.Repositories;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure_Layer.Services
{
    public class CustomerSyncServiceXeroAndQuickBooks : IXeroCustomerSyncService
    {
        private readonly IXeroApiManager _xero;
        private readonly IQuickBooksApiManager _qb;
        private readonly ICustomerRepository _customers;
        private readonly IConfiguration _config;

        public CustomerSyncServiceXeroAndQuickBooks(IXeroApiManager xero, ICustomerRepository customers, IConfiguration config, IQuickBooksApiManager qb)
        {
            _xero = xero;
            _customers = customers;
            _config = config;
            _qb = qb;
        }
        //// Xero first, then DB
        public async Task<Customer> SyncCreatedCustomerAsync(CustomerCreateDto customerDto)
        {
            // ✅ 1. Save to local DB first
            var customer = new Customer
            {
                Name = customerDto.Name,
                Email = customerDto.Email,
                Phone = customerDto.Phone,
                Address = customerDto.Address,
                XeroId = customerDto.XeroId ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SyncedToXero = false,
                SyncedToQuickBooks = false
            };

            await _customers.InsertAsync(customer);

            // ✅ 2. Create customer in Xero
            var xeroResponse = await _xero.CreateCustomerAsync(customerDto);

            // ✅ 3. Update DB with XeroId
            var dbCustomer = await _customers.GetByIdAsync(customer.Id);

            var xeroJson = await _xero.GetCustomerByEmailAsync(customer.Email);
            var root = JsonConvert.DeserializeObject<JObject>(xeroJson);
            var contact = root["Contacts"]?.FirstOrDefault();
            var xeroReadDto = contact?.ToObject<CustomerReadDto>();

            dbCustomer.XeroId = xeroReadDto.XeroId;
            dbCustomer.SyncedToXero = true;
            await _customers.UpdateAsync(dbCustomer);

            // ✅ 4. NOW sync to QuickBooks
            var qbCustomer = new Customer
            {
                Id = dbCustomer.Id,
                Name = dbCustomer.Name,
                Email = dbCustomer.Email,
                Phone = dbCustomer.Phone,
                Address = dbCustomer.Address,
                XeroId = dbCustomer.XeroId
            };

            var qbResponse = await _qb.CreateOrUpdateCustomerAsync(qbCustomer);

            // ✅ 5. Save QuickBooksId back into DB
            dbCustomer.QuickBooksId = qbResponse.QuickBooksId;
            dbCustomer.SyncedToQuickBooks = true;
            await _customers.UpdateAsync(dbCustomer);

            return dbCustomer;
        }


        public async Task<string> SyncUpdatedCustomerAsync(CustomerCreateDto dto)
        {
            // 1️⃣ Find customer in local DB
            var localCustomer = await _customers.GetByXeroIdAsync(dto.XeroId);//ste poxem vor get arvi CustomerdId-ov!!!
            if (localCustomer == null)
                throw new Exception($"Customer with XeroId {dto.XeroId} not found in local DB.");

            // 2️⃣ Update local DB first (source of truth)
            localCustomer.Name = dto.Name;
            localCustomer.Email = dto.Email;
            localCustomer.Phone = dto.Phone;
            localCustomer.Address = dto.Address;
            localCustomer.UpdatedAt = DateTime.UtcNow;
            localCustomer.SyncedToXero = false;
            localCustomer.SyncedToQuickBooks = false;
            await _customers.UpdateAsync(localCustomer);

            // 3️⃣ Try update in Xero
            string xeroResponse;
            try
            {
                xeroResponse = await _xero.UpdateCustomerAsync(dto);
                localCustomer.SyncedToXero = true;
                await _customers.UpdateAsync(localCustomer);
            }
            catch (Exception ex)
            {
                localCustomer.SyncedToXero = false;
                await _customers.UpdateAsync(localCustomer);
                throw new Exception($"Failed to sync to Xero: {ex.Message}");
            }

            // ✅ 4️⃣ Sync to QuickBooks
            try
            {
                var qbCustomer = new Customer
                {
                    Id = localCustomer.Id,
                    Name = localCustomer.Name,
                    Email = localCustomer.Email,
                    Phone = localCustomer.Phone,
                    Address = localCustomer.Address,
                    QuickBooksId = localCustomer.QuickBooksId, // may be null = create in QBO
                    XeroId = localCustomer.XeroId
                };

                var qbResponse = await _qb.CreateOrUpdateCustomerAsync(qbCustomer);

                // ✅ Save QB Id if this is first time linking
                if (!string.IsNullOrWhiteSpace(qbResponse.QuickBooksId))
                    localCustomer.QuickBooksId = qbResponse.QuickBooksId;

                localCustomer.SyncedToQuickBooks = true;
                await _customers.UpdateAsync(localCustomer);
            }
            catch (Exception ex)
            {
                localCustomer.SyncedToQuickBooks = false;
                await _customers.UpdateAsync(localCustomer);
                throw new Exception($"Failed to sync to QuickBooks: {ex.Message}");
            }

            // 5️⃣ Return Xero response (optional: include QB message too)
            return xeroResponse;
        }



    }

}
