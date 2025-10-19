using Domain_Layer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application_Layer.Interfaces
{
    public interface IXeroAuthService
    {
        string GetLoginUrlAsync();
        Task<XeroTokenResponse> HandleCallbackAsync(string authorizationCodecode, string state);
        Task SaveXeroTokenAsync(XeroTokenResponse token);
        Task<XeroTokenResponse> RefreshAccessTokenAsync(string refreshToken);
    }
}
