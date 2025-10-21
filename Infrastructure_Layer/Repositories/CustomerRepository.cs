using Application_Layer.DTO.Customers;
using Application_Layer.Interfaces_Repository;
using Dapper;
using Domain_Layer.Models;
using Infrastructure_Layer.Data;
using Infrastructure_Layer.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Infrastructure_Layer.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        //✅ This repository can now read and write customers to your SQL Server database.
        private readonly AccountingDbContext _context;

        public CustomerRepository(AccountingDbContext context)
        {
            _context = context;
        }

        public async Task<Customer> GetByIdAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                throw new Exception("Customer not found");

            return customer;
        }

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            return await _context.Customers.ToListAsync();
        }

        public async Task InsertAsync(Customer customer)
        {
            customer.CreatedAt = DateTime.UtcNow;
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Customer customer)
        {
            customer.UpdatedAt = DateTime.UtcNow;
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
        }
        public async Task<Customer> GetByXeroIdAsync(string xeroId)
        {
            return await _context.Customers.FirstOrDefaultAsync(c => c.XeroId == xeroId);
        }

        public async Task<Customer> GetByXeroIdAndSyncedToXeroAsync(CustomerReadDto dto)
        {
            //Console.WriteLine("\n\nKANCHVAAAV     ");
            //if (await _context.Customers.FirstOrDefaultAsync(c => c.XeroId == dto.XeroId) == null)
            //{
            //    Console.WriteLine("de ste parza vor pti mtner");
            //    Console.WriteLine("incha bool-@ -> " + dto.SyncedToXero);
            //    Console.WriteLine(dto.UpdatedAt);
            //    if (await _context.Customers.FirstOrDefaultAsync(c => c.UpdatedAt == dto.UpdatedAt) != null &&  dto.SyncedToXero == true)
            //        return await _context.Customers.FirstOrDefaultAsync(c => c.UpdatedAt == dto.UpdatedAt);
            //}
            //Console.WriteLine("stexia helnum??");
            return await _context.Customers.FirstOrDefaultAsync(c => c.XeroId == dto.XeroId);
        }
        public async Task UpdateSyncedToXeroAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                throw new Exception("Customer not found");

            customer.SyncedToXero = true;
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
                throw new Exception("Customer not found");

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
        }


    }
}
