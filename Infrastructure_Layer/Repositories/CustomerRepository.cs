using Application.DTOs;
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
        public async Task<Customer?> GetByDetailsAsync(string name, string email, string phone, string address)
        {
            return await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.Name == name &&
                    c.Email == email &&
                    c.Phone == phone &&
                    c.Address == address);
        }


        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            return await _context.Customers.ToListAsync();
        }

        public async Task InsertAsync(Customer customer)
        {
            try
            {
                customer.CreatedAt = DateTime.UtcNow;
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException != null &&
                    ex.InnerException.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("A customer with the same Name, Email, Phone, and Address already exists.");
                }
                throw;
            }
        }

        public async Task UpdateAsync(Customer customer)
        {
            try
            {
                customer.UpdatedAt = DateTime.UtcNow;
                _context.Customers.Update(customer);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException != null &&
                    ex.InnerException.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception("Updating this customer would create a duplicate entry.");
                }
                throw;
            }
        }
        public async Task<Customer> GetByXeroIdAsync(string xeroId)
        {
            return await _context.Customers.FirstOrDefaultAsync(c => c.XeroId == xeroId);
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

        //////////////////
        //public async Task checkIdAndXeroId(InvoiceCreateDto invoice)
        //{
        //    var customer = await _context.Customers.FindAsync(invoice.CustomerId);
        //    if (customer == null)
        //        throw new Exception("Customer not found");
        //    if (customer.XeroId != invoice.CustomerXeroId)
        //        throw new Exception($"Mismatch: local customer (ID={invoice.CustomerId}) has XeroId={customer.XeroId}, " +
        //                            $"but request provided {invoice.CustomerXeroId}.");
        //}


        //QuickBooks
        public async Task<Customer> GetByQuickBooksIdAsync(string quickBooksId)
        {
            return await _context.Customers.FirstOrDefaultAsync(c => c.QuickBooksId == quickBooksId);
        }

    }
}
