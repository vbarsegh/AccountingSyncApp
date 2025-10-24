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

        public async Task<Invoice> GetByIdAsync(int id) =>
            await _context.Invoices.FindAsync(id) ?? throw new Exception("Invoice not found");

        public async Task<Invoice> GetByInvoiceXeroIdAsync(string InvoicexeroId)
        {
            return await _context.Invoices
                .FirstOrDefaultAsync(i => i.XeroId == InvoicexeroId);
        }


        public async Task<IEnumerable<Invoice>> GetAllAsync() =>
            await _context.Invoices.ToListAsync();

        public async Task InsertAsync(Invoice invoice)
        {
            invoice.CreatedAt = DateTime.UtcNow;
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Invoice invoice)
        {
            invoice.UpdatedAt = DateTime.UtcNow;
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var invoice = await GetByIdAsync(id);
            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();
        }
    }
}
