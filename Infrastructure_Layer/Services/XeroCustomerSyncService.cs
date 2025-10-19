using Application_Layer.DTO.Customers;
using Application_Layer.Interfaces;
using Application_Layer.Interfaces_Repository;
using Application_Layer.Services;
using Domain_Layer.Models;
using Infrastructure_Layer.Repositories;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
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
        public async Task<Customer> SyncCreatedCustomerAsync(CustomerCreateDto dto)
        {
            // 1. Create in Xero
            var xeroResponse = await _xero.CreateCustomerAsync(dto);

            // 2. Deserialize Xero response
            var xeroCustomer = JsonConvert.DeserializeObject<CustomerReadDto>(xeroResponse);

            // 3. Save to local DB
            var customer = new Customer
            {
                Name = xeroCustomer.Name,
                Email = xeroCustomer.Email,
                XeroId = xeroCustomer.XeroId
            };

            await _customers.InsertAsync(customer);
            return customer;
        }

        public async Task<string> SyncUpdatedCustomerAsync(CustomerCreateDto dto)
        {
            // 1️⃣ Send update to Xero
            var xeroResponse = await _xero.UpdateCustomerAsync(dto);

            // 2️⃣ Parse Xero’s response
            var updatedXeroCustomer = JsonConvert.DeserializeObject<CustomerReadDto>(xeroResponse);

            // 3️⃣ Find the same customer in local DB by XeroId
            var localCustomer = await _customers.GetByXeroIdAsync(updatedXeroCustomer.XeroId);
            if (localCustomer == null)
                throw new Exception("Customer not found in local database.");

            // 4️⃣ Update local fields
            localCustomer.Name = updatedXeroCustomer.Name;
            localCustomer.Email = updatedXeroCustomer.Email;
            localCustomer.Phone = updatedXeroCustomer.Phone;
            localCustomer.Address = updatedXeroCustomer.Address;
            localCustomer.UpdatedAt = DateTime.UtcNow;

            // 5️⃣ Save changes
            await _customers.UpdateAsync(localCustomer);

            // 6️⃣ Return Xero’s JSON response
            return xeroResponse;
        }
        ////////////////////////////////////////////////////////
        ///
        // CREATE customer in DB, then Xero
        public async Task<Customer> CreateCustomerAndSyncAsync(Customer customer)
        {
            // 1️⃣ Save in local DB
            await _customers.InsertAsync(customer);

            // 2️⃣ Prepare DTO for Xero
            var dto = new CustomerCreateDto
            {
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = customer.Address
            };

            // 3️⃣ Create in Xero
            var xeroResponse = await _xero.CreateCustomerAsync(dto);

            // 4️⃣ Update local DB with XeroId returned
            var createdXeroCustomer = JsonConvert.DeserializeObject<CustomerReadDto>(xeroResponse);
            customer.XeroId = createdXeroCustomer.XeroId;
            await _customers.UpdateAsync(customer);

            return customer;
        }
        //DB first, then Xero (new)
        public async Task<Customer> UpdateCustomerAndSyncAsync(Customer customer)
        {
            await _customers.UpdateAsync(customer);

            var dto = new CustomerCreateDto
            {
                Name = customer.Name,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = customer.Address
            };

            await _xero.UpdateCustomerAsync(dto);

            return customer;
        }
    }

}
