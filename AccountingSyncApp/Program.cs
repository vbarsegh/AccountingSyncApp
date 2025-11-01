
using Application_Layer.Interfaces;
using Application_Layer.Interfaces.QuickBooks;
using Application_Layer.Interfaces.Xero;
using Application_Layer.Interfaces_Repository;
using Application_Layer.Services;
//using Application_Layer.Services.QuickBooks;
using Application_Layer.Services.Xero;
using Domain_Layer.Models;
using Infrastructure_Layer.Data;
using Infrastructure_Layer.Helpers;
using Infrastructure_Layer.Repositories;
using Infrastructure_Layer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Threading.Tasks;



            var builder = WebApplication.CreateBuilder(args);
// ----------------------
// 2. Register repositories
// ----------------------
builder.Services.AddScoped<IXeroTokenRepository, XeroTokenRepository>();
builder.Services.AddScoped<IQuickBooksTokenRepository, QuickBooksTokenRepository>();

builder.Services.AddScoped<CustomerRepository>();
builder.Services.AddScoped<InvoiceRepository>();
builder.Services.AddScoped<QuoteRepository>();
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();



// 3. Register services
// ----------------------
builder.Services.AddScoped<IAccountingSyncManager, AccountingSyncManager>();
builder.Services.AddScoped<IXeroApiManager, XeroApiManager>();
builder.Services.AddScoped<IXeroAuthService, XeroAuthService>();
builder.Services.AddScoped<IQuickBooksAuthService, QuickBooksAuthService>();
builder.Services.AddScoped<IQuickBooksApiManager, QuickBooksApiManager>();
builder.Services.AddScoped<IXeroCustomerSyncService, CustomerSyncServiceXeroAndQuickBooks>();
builder.Services.AddScoped<IXeroInvoiceSyncService, InvoiceSyncServiceXeroAndQuickBooks>();
builder.Services.AddScoped<IXeroQuoteSyncService, QuoteSyncServiceXeroAndQuickBooks>();
////
builder.Services.AddHostedService<XeroQuotePollingService>();
////
builder.Services.AddScoped<AccountingSyncManager>();


builder.Services.AddDbContext<AccountingDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



//// create DatabaseHelper
//var dbHelper = new DatabaseHelper(builder.Configuration);
//            // create repository
//            var customerRepo = new CustomerRepository(dbHelper);
//            // create a test customer
//            var testCustomer = new Customer
//            {
//                Id = Guid.NewGuid(),//Integers (1, 2, 3, …) are fine for one system.But if two systems(like QuickBooks + your app) both create new customers with ID 1, they’ll conflict when synced. GUIDs are globally unique — perfect for multi-system integration.
//                Name = "Test Customer",
//                Email = "test@example.com",
//                Phone = "1234567890",
//                Address = "123 Test St",
//                XeroId = "XERO123",
//                CreatedAt = DateTime.UtcNow,
//    UpdatedAt = DateTime.UtcNow
//};

//await customerRepo.InsertAsync(testCustomer);
//Console.WriteLine("Customer inserted!");

// read all customers
//var customers = await customerRepo.GetAllAsync();
//foreach (var c in customers)
//{
//    Console.WriteLine($"{c.Id} - {c.Name} - {c.Email}");
//}


// ----------------------
// 4. Add controllers
// ----------------------
builder.Services.AddControllers();
// Optional: Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
