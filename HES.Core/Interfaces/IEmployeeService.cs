﻿using HES.Core.Entities;
using HES.Core.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace HES.Core.Interfaces
{
    public interface IEmployeeService
    {
        IQueryable<Employee> Query();
        Task<Employee> GetByIdAsync(dynamic id);
        Task<Employee> CreateEmployeeAsync(Employee employee);
        Task EditEmployeeAsync(Employee employee);
        Task DeleteEmployeeAsync(string id);
        Task<bool> ExistAsync(Expression<Func<Employee, bool>> predicate);
        Task CreateSamlIdpAccountAsync(string email, string password, string hesUrl, string deviceId);
        Task UpdatePasswordSamlIdpAccountAsync(string email, string password);
        Task UpdateOtpSamlIdpAccountAsync(string email, string otp);
        Task<IList<string>> UpdateUrlSamlIdpAccountAsync(string hesUrl);
        Task DeleteSamlIdpAccountAsync(string employeeId);
        Task SetPrimaryAccount(string deviceId, string deviceAccountId);
        Task AddDeviceAsync(string employeeId, string[] selectedDevices);
        Task RemoveDeviceAsync(string employeeId, string deviceId);
        Task CreateWorkstationAccountAsync(WorkstationAccountModel workstationAccount, string employeeId, string deviceId);
        Task CreatePersonalAccountAsync(DeviceAccount deviceAccount, InputModel input, string[] selectedDevices);
        Task EditPersonalAccountAsync(DeviceAccount deviceAccount);
        Task EditPersonalAccountPwdAsync(DeviceAccount deviceAccount, InputModel input);
        Task EditPersonalAccountOtpAsync(DeviceAccount deviceAccount, InputModel input);
        Task AddSharedAccount(string employeeId, string sharedAccountId, string[] selectedDevices);
        Task<string> DeleteAccount(string accountId);
        Task UndoChanges(string accountId);
        Task HandlingMasterPasswordErrorAsync(string deviceId);
    }
}