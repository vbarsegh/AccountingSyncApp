using Domain_Layer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application_Layer.Interfaces_Repository
{
    public interface IXeroTokenRepository
    {
        Task<XeroTokenResponse?> GetTokenAsync();
        Task SaveTokenAsync(XeroTokenResponse token);
    }
}
