using Application_Layer.Interfaces_Repository;
using Domain_Layer.Models;
using Infrastructure_Layer.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure_Layer.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly AccountingDbContext _context;

        public InvoiceRepository(AccountingDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Invoice>> GetAllAsync()
        {
            return await _context.Invoices.ToListAsync();
        }

        public async Task InsertAsync(Invoice invoice)
        {
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Invoice invoice)
        {
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task<Invoice?> GetByXeroIdAsync(int id)
        {
            return await _context.Invoices.FindAsync(id);
        }

        public async Task<IEnumerable<Invoice>> GetByCustomerIdAsync(int customerId)
        {
            return await _context.Invoices
                .Where(i => i.CustomerId == customerId)
                .ToListAsync();
        }
    }
}
