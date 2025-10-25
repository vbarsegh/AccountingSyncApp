using Application_Layer.DTO.Customers;
using Application_Layer.Interfaces;
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
    public class XeroCustomerSyncService : IXeroCustomerSyncService
    {
        private readonly IXeroApiManager _xero;
        private readonly ICustomerRepository _customers;
        private readonly IConfiguration _config;

        public XeroCustomerSyncService(IXeroApiManager xero, ICustomerRepository customers, IConfiguration config)
        {
            _xero = xero;
            _customers = customers;
            _config= config;
        }
        //// Xero first, then DB
        public async Task<Customer> SyncCreatedCustomerAsync(CustomerCreateDto customerDto)
        {

            //meke stugel karoxa lav mitq chi customerRepository-n u xeroApiManager-@ drsic stanaly,check anelllll!!!!
            // 3. Save to local DB
            var customer = new Customer
            {
                Name = customerDto.Name,
                Email = customerDto.Email,
                Phone = customerDto.Phone,
                Address = customerDto.Address,
                XeroId = customerDto.XeroId ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                SyncedToXero = false //false
            };
            //customerDto.SyncedToXero = true;//erevi
            await _customers.InsertAsync(customer);
            //var customerReadDto = ...
            // 2️⃣ Create in Xero
            var xeroResponse = await _xero.CreateCustomerAsync(customerDto);
            // 3️⃣ Update local DB with the Xero ID and set SyncedToXero = true, vor en dublicatio xndiry chunenanq
            var dbCustomer = await _customers.GetByIdAsync(customer.Id);

            var stringJson = await _xero.GetCustomerByEmailAsync(customer.Email);
            var root = JsonConvert.DeserializeObject<JObject>(stringJson);

            var contact = root["Contacts"]?.FirstOrDefault();
            var customerReadDto = contact?.ToObject<CustomerReadDto>();
            dbCustomer.XeroId = customerReadDto.XeroId;
            dbCustomer.SyncedToXero = true;
            await _customers.UpdateAsync(dbCustomer);

            return dbCustomer;
        }

        public async Task<string> SyncUpdatedCustomerAsync(CustomerCreateDto dto)
        {
            // 1️⃣ Update local DB first (our source of truth)
            var localCustomer = await _customers.GetByXeroIdAsync(dto.XeroId);
            if (localCustomer == null)
                throw new Exception($"Customer with XeroId {dto.XeroId} not found in local DB.");

            localCustomer.Name = dto.Name;
            localCustomer.Email = dto.Email;
            localCustomer.Phone = dto.Phone;
            localCustomer.Address = dto.Address;
            localCustomer.UpdatedAt = DateTime.UtcNow;
            localCustomer.SyncedToXero = false; // temporary mark until confirmed synced

            await _customers.UpdateAsync(localCustomer);

            // 2️⃣ Try update in Xero
            string xeroResponse;
            try
            {
                xeroResponse = await _xero.UpdateCustomerAsync(dto);
                localCustomer.SyncedToXero = true; // ✅ success
                await _customers.UpdateAsync(localCustomer);
            }
            catch (Exception ex)
            {
                // ❌ If Xero fails — revert the Synced flag
                localCustomer.SyncedToXero = false;
                await _customers.UpdateAsync(localCustomer);
                throw new Exception($"Failed to sync to Xero: {ex.Message}");
            }

            // 3️⃣ Return confirmation
            return xeroResponse;
        }


    }

}
