﻿using HES.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace HES.Core.Interfaces
{
    public interface ISharedAccountService
    {
        IQueryable<SharedAccount> Query();
        Task<SharedAccount> GetByIdAsync(dynamic id);
        Task<SharedAccount> CreateSharedAccountAsync(SharedAccount sharedAccount, InputModel input);
        Task UpdateOnlyPropAsync(SharedAccount sharedAccount, string[] properties);
        Task<List<string>> EditSharedAccountAsync(SharedAccount sharedAccount);
        Task<List<string>> EditSharedAccountPwdAsync(SharedAccount sharedAccount, InputModel input);
        Task<List<string>> EditSharedAccountOtpAsync(SharedAccount sharedAccount, InputModel input);
        Task<List<string>> DeleteSharedAccountAsync(string id);
        Task<bool> ExistAync(Expression<Func<SharedAccount, bool>> predicate);
    }
}