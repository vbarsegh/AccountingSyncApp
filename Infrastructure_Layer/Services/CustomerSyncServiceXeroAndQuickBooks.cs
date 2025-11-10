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
                XeroId = null,
                QuickBooksId = null,
                //CompanyName = customerDto.CompanyName,
                //OpenBalance = customerDto.OpenBalance,
                Name = customerDto.Name,
                Email = customerDto.Email,
                Phone = customerDto.Phone,
                Address = customerDto.Address,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            await _customers.InsertAsync(customer);
            // ✅ 2. Create customer in Xero
            var xeroResponse = await _xero.CreateCustomerAsync(customerDto);

            var root = JsonConvert.DeserializeObject<JObject>(xeroResponse);
            var contact = root["Contacts"]?.FirstOrDefault();
            var newXeroId = contact?["ContactID"]?.ToString();
            // ✅ 3. Update DB with XeroId
            var dbCustomer = await _customers.GetByIdAsync(customer.Id);

            
            //var xeroReadDto = contact?.ToObject<CustomerReadDto>();

            dbCustomer.XeroId = newXeroId;
            await _customers.UpdateAsync(dbCustomer);
            // ✅ 4. NOW sync to QuickBooks
            //var qbCustomer = new Customer
            //{
            //    Id = dbCustomer.Id,
            //    Name = dbCustomer.Name,
            //    Email = dbCustomer.Email,
            //    Phone = dbCustomer.Phone,
            //    Address = dbCustomer.Address,
            //    XeroId = dbCustomer.XeroId
            //};
            var qbResponse = await _qb.CreateOrUpdateCustomerAsync(dbCustomer);
            // ✅ 5. Save QuickBooksId back into DB
            if (!string.IsNullOrWhiteSpace(qbResponse.QuickBooksId))
            {
                dbCustomer.QuickBooksId = qbResponse.QuickBooksId;
                await _customers.UpdateAsync(dbCustomer);
            }
            return dbCustomer;
        }


        public async Task<string> SyncUpdatedCustomerAsync(CustomerUpdateDto dto)
        {
            // 1️⃣ Find customer in local DB
            //var localCustomer = await _customers.GetByXeroIdAsync(dto.XeroId);//ste poxem vor get arvi CustomerdId-ov!!!
            var localCustomer = await _customers.GetByIdAsync(dto.Id);
            if (localCustomer == null)
                throw new Exception($"Customer with Id {dto.Id} not found in local DB.");
            // 2️⃣ Update local DB first (source of truth)
            localCustomer.Name = dto.Name;
            localCustomer.Email = dto.Email;
            localCustomer.Phone = dto.Phone;
            localCustomer.Address = dto.Address;
            localCustomer.UpdatedAt = DateTime.UtcNow;
            //ste hetagayum karanq avelacnenq quickbooks-i hamar
            await _customers.UpdateAsync(localCustomer);
            // 3️⃣ Try update in Xero
            string xeroResponse;
            try
            {
                Console.WriteLine("\n\n\n hasav \n\n\n");
                xeroResponse = await _xero.UpdateCustomerAsync(dto);
                //await _customers.UpdateAsync(localCustomer);
            }
            catch (Exception ex)
            {
                await _customers.UpdateAsync(localCustomer);
                throw new Exception($"Failed to sync to Xero: {ex.Message}");
            }
            // ✅ 4️⃣ Sync to QuickBooks
            try
            {
                //var qbCustomer = new Customer
                //{
                //    Id = localCustomer.Id,
                //    Name = localCustomer.Name,
                //    Email = localCustomer.Email,
                //    Phone = localCustomer.Phone,
                //    Address = localCustomer.Address,
                //    QuickBooksId = localCustomer.QuickBooksId, // may be null = create in QBO
                //    XeroId = localCustomer.XeroId
                //};

                var qbResponse = await _qb.CreateOrUpdateCustomerAsync(localCustomer);
                // ✅ Save QB Id if this is first time linking
                //if (!string.IsNullOrWhiteSpace(qbResponse.QuickBooksId))
                //    localCustomer.QuickBooksId = qbResponse.QuickBooksId;
                //await _customers.UpdateAsync(localCustomer);
            }
            catch (Exception ex)
            {
                //await _customers.UpdateAsync(localCustomer);
                throw new Exception($"Failed to sync update to QuickBooks: {ex.Message}");
            }
            // 5️⃣ Return Xero response (optional: include QB message too)
            return xeroResponse;
        }
    }

}
