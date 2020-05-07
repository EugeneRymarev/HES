﻿using HES.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace HES.Core.Interfaces
{
    public interface IAccountService
    {
        IQueryable<Account> Query();
        Task<Account> GetAccountByIdAsync(string accountId);
        Task<Account> AddAsync(Account deviceAccount);
        Task<IList<Account>> AddRangeAsync(IList<Account> deviceAccounts);
        Task UpdateOnlyPropAsync(Account deviceAccount, string[] properties);
        Task UpdateOnlyPropAsync(IList<Account> deviceAccounts, string[] properties);
        Task UpdateAfterAccountCreationAsync(Account account, uint storageId, uint timestamp);
        Task DeleteAsync(Account deviceAccount);
        Task DeleteRangeAsync(IList<Account> deviceAccounts);
        Task DeleteAccountsByEmployeeIdAsync(string employeeId);
        Task<bool> ExistAsync(Expression<Func<Account, bool>> predicate);
    }
}