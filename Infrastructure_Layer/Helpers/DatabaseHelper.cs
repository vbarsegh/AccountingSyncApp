using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Infrastructure_Layer.Helpers
{
    //This class reads the connection string from appsettings.json and gives a SQL connection.
    public class DatabaseHelper
    {
        private readonly IConfiguration _configuration;
        public DatabaseHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IDbConnection GetConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }
    }
}
