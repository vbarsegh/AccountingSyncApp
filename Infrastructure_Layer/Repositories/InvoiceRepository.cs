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
            try
            {
                invoice.CreatedAt = DateTime.UtcNow;
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Detect SQL constraint violation (duplicate key)
                if (ex.InnerException != null && ex.InnerException.Message.Contains("IX_Invoices_InvoiceNumber"))
                {
                    throw new Exception($"Duplicate invoice number: {invoice.InvoiceNumber}. It must be unique.");
                }

                throw; // rethrow for other cases
            }
        }


        public async Task UpdateAsync(Invoice invoice)
        {
            try
            {
                // Check for duplicates before updating
                bool duplicateExists = await _context.Invoices
                    .AnyAsync(i => i.InvoiceNumber == invoice.InvoiceNumber && i.Id != invoice.Id);
                //esi nra hamara arac vor ete update vaxt nenc Invoice Number(INV-003) enq tali vory arden goyutyun uni exception qcenq
                //sencem are vor chstacvi vor erku nuyn hamari tak invoice-ner linen mcacac tarbervox fielderov!!!

                if (duplicateExists)
                    throw new Exception($"Invoice number '{invoice.InvoiceNumber}' already exists for another invoice.");

                // Update the entity
                _context.Invoices.Update(invoice);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Detect database-level constraint violation (unique index)
                if (ex.InnerException != null && ex.InnerException.Message.Contains("IX_Invoices_InvoiceNumber"))
                {
                    throw new Exception($"Duplicate invoice number: {invoice.InvoiceNumber}. It must be unique.");
                }

                throw; // rethrow for other database issues
            }
        }


        public async Task DeleteAsync(int id)
        {
            var invoice = await GetByIdAsync(id);
            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();
        }

        //quockbooks
        public async Task<Invoice> GetByInvoiceQuickBooksIdAsync(string quickBooksId)
        {
            return await _context.Invoices
                .FirstOrDefaultAsync(i => i.QuickBooksId == quickBooksId);
        }

    }
}
