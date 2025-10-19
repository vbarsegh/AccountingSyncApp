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

        public async Task<IEnumerable<Customer>> GetAllAsync()
        {
            return await _context.Customers.ToListAsync();
        }

        public async Task InsertAsync(Customer customer)
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Customer customer)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
        }
        public async Task<Customer> GetByXeroIdAsync(string xeroId)
        {
            return await _context.Customers.FirstOrDefaultAsync(c => c.XeroId == xeroId);
        }
    }
}
