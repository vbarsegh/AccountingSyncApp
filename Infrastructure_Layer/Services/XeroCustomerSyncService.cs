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
            // 3️⃣ Update local DB with the Xero ID and set SyncedToXero = true
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
            // 1️⃣ Send update to Xero
            var xeroResponse = await _xero.UpdateCustomerAsync(dto);

            // 2️⃣ Parse the response properly
            var root = JsonConvert.DeserializeObject<JObject>(xeroResponse);
            var contact = root["Contacts"]?.FirstOrDefault();
            if (contact == null)
                throw new Exception("No contact data returned from Xero.");

            var updatedXeroCustomer = contact.ToObject<CustomerReadDto>();

            // ✅ Ensure XeroId is not lost
            if (string.IsNullOrWhiteSpace(updatedXeroCustomer.XeroId))
                updatedXeroCustomer.XeroId = dto.XeroId ?? string.Empty;

            // 3️⃣ Find existing local customer by XeroId
            var localCustomer = await _customers.GetByXeroIdAsync(updatedXeroCustomer.XeroId);
            if (localCustomer == null)
                throw new Exception($"Customer with XeroId {updatedXeroCustomer.XeroId} not found in local DB.");

            // 4️⃣ Update local fields
            localCustomer.Name = updatedXeroCustomer.Name;
            localCustomer.Email = updatedXeroCustomer.Email;
            localCustomer.Phone = updatedXeroCustomer.Phone;
            localCustomer.Address = updatedXeroCustomer.Address;
            localCustomer.UpdatedAt = DateTime.UtcNow;
            localCustomer.SyncedToXero = true;

            // 5️⃣ Save changes
            await _customers.UpdateAsync(localCustomer);

            // 6️⃣ Return Xero’s JSON response
            return xeroResponse;
        }

    }

}
