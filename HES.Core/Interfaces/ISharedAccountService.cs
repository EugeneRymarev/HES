﻿using HES.Core.Entities;
using HES.Core.Models.Web;
using HES.Core.Models.Web.Accounts;
using HES.Core.Models.Web.SharedAccounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HES.Core.Interfaces
{
    public interface ISharedAccountService : IDisposable
    {
        IQueryable<SharedAccount> Query();
        Task UnchangedAsync(SharedAccount account);
        Task<SharedAccount> GetSharedAccountByIdAsync(string id);
        Task<List<SharedAccount>> GetSharedAccountsAsync(DataLoadingOptions<SharedAccountsFilter> dataLoadingOptions);
        Task<int> GetSharedAccountsCountAsync(DataLoadingOptions<SharedAccountsFilter> dataLoadingOptions);
        Task<List<SharedAccount>> GetAllSharedAccountsAsync();
        Task<SharedAccount> CreateSharedAccountAsync(SharedAccountAddModel sharedAccount);
        Task<List<string>> EditSharedAccountAsync(SharedAccountEditModel sharedAccount);
        Task<List<string>> EditSharedAccountPwdAsync(SharedAccount sharedAccount, AccountPassword accountPassword);
        Task<List<string>> EditSharedAccountOtpAsync(SharedAccount sharedAccount, AccountOtp accountOtp);
        Task<List<string>> DeleteSharedAccountAsync(string id);
    }
}