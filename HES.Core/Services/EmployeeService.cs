﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HES.Core.Entities;
using HES.Core.Entities.Models;
using HES.Core.Interfaces;
using HES.Core.Utilities;
using Hideez.SDK.Communication.Security;
using Hideez.SDK.Communication.Utils;
using Microsoft.EntityFrameworkCore;

namespace HES.Core.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IAsyncRepository<Employee> _employeeRepository;
        private readonly IDeviceService _deviceService;
        private readonly IDeviceTaskService _deviceTaskService;
        private readonly IDeviceAccountService _deviceAccountService;
        private readonly ISharedAccountService _sharedAccountService;
        private readonly IWorkstationProximityDeviceService _workstationProximityDeviceService;
        private readonly IAsyncRepository<WorkstationEvent> _workstationEventRepository;
        private readonly IAsyncRepository<WorkstationSession> _workstationSessionRepository;
        private readonly IDataProtectionService _dataProtectionService;
        private readonly ISamlIdentityProviderService _samlIdentityProviderService;

        public EmployeeService(IAsyncRepository<Employee> employeeRepository,
                               IDeviceService deviceService,
                               IDeviceTaskService deviceTaskService,
                               IDeviceAccountService deviceAccountService,
                               ISharedAccountService sharedAccountService,
                               IWorkstationProximityDeviceService workstationProximityDeviceService,
                               IAsyncRepository<WorkstationEvent> workstationEventRepository,
                               IAsyncRepository<WorkstationSession> workstationSessionRepository,
                               IDataProtectionService dataProtectionService,
                               ISamlIdentityProviderService samlIdentityProviderService)
        {
            _employeeRepository = employeeRepository;
            _deviceService = deviceService;
            _deviceTaskService = deviceTaskService;
            _deviceAccountService = deviceAccountService;
            _sharedAccountService = sharedAccountService;
            _workstationProximityDeviceService = workstationProximityDeviceService;

            _workstationEventRepository = workstationEventRepository;
            _workstationSessionRepository = workstationSessionRepository;
            _dataProtectionService = dataProtectionService;
            _samlIdentityProviderService = samlIdentityProviderService;
        }

        #region Employee

        public IQueryable<Employee> Query()
        {
            return _employeeRepository.Query();
        }

        public async Task<int> GetCountAsync()
        {
            return await _employeeRepository.GetCountAsync();
        }

        public async Task<Employee> GetByIdAsync(dynamic id)
        {
            return await _employeeRepository.GetByIdAsync(id);
        }

        public async Task<Employee> CreateEmployeeAsync(Employee employee)
        {
            if (employee == null)
                throw new ArgumentNullException(nameof(employee));

            var emailExist = await _employeeRepository
                .Query()
                .Where(e => e.Email == employee.Email)
                .AnyAsync();

            if (emailExist)
            {
                throw new Exception($"Email {employee.Email} already used.");
            }

            return await _employeeRepository.AddAsync(employee);
        }

        public async Task EditEmployeeAsync(Employee employee)
        {
            if (employee == null)
                throw new ArgumentNullException(nameof(employee));

            await _employeeRepository.UpdateAsync(employee);
        }

        public async Task DeleteEmployeeAsync(string id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var employee = await _employeeRepository.GetByIdAsync(id);
            if (employee == null)
                throw new Exception("Employee not found");

            // Remove all events
            var allEvents = await _workstationEventRepository.Query().Where(e => e.EmployeeId == id).ToListAsync();
            await _workstationEventRepository.DeleteRangeAsync(allEvents);
            // Remove all events
            var allSessions = await _workstationSessionRepository.Query().Where(s => s.EmployeeId == id).ToListAsync();
            await _workstationSessionRepository.DeleteRangeAsync(allSessions);
            // Remove all accounts
            await _deviceAccountService.RemoveAllByEmployeeIdAsync(id);

            await _employeeRepository.DeleteAsync(employee);
        }

        public async Task<bool> ExistAsync(Expression<Func<Employee, bool>> predicate)
        {
            return await _employeeRepository.ExistAsync(predicate);
        }

        #endregion

        #region SamlIdp

        public async Task CreateSamlIdpAccountAsync(string email, string password, string hesUrl, string deviceId)
        {
            _dataProtectionService.Validate();

            var employee = await _employeeRepository.Query().FirstOrDefaultAsync(e => e.Email == email);
            if (employee == null)
            {
                throw new ArgumentNullException(nameof(employee));
            }

            var device = await _deviceService.GetByIdAsync(deviceId);
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            var samlIdP = await _samlIdentityProviderService.GetByIdAsync(SamlIdentityProvider.PrimaryKey);

            // Create account
            var deviceAccountId = Guid.NewGuid().ToString();
            var deviceAccount = new DeviceAccount
            {
                Id = deviceAccountId,
                Name = SamlIdentityProvider.DeviceAccountName,
                Urls = $"{samlIdP.Url};{hesUrl}",
                Apps = null,
                Login = email,
                Type = AccountType.Personal,
                Status = AccountStatus.Creating,
                CreatedAt = DateTime.UtcNow,
                PasswordUpdatedAt = DateTime.UtcNow,
                OtpUpdatedAt = null,
                EmployeeId = employee.Id,
                DeviceId = device.Id,
                SharedAccountId = null
            };

            // Validate url
            deviceAccount.Urls = ValidationHepler.VerifyUrls(deviceAccount.Urls);

            // Create task
            var deviceTask = new DeviceTask
            {
                DeviceAccountId = deviceAccountId,
                Name = deviceAccount.Name,
                Urls = deviceAccount.Urls,
                Apps = deviceAccount.Apps,
                Login = deviceAccount.Login,
                Password = _dataProtectionService.Protect(password),
                OtpSecret = null,
                CreatedAt = DateTime.UtcNow,
                Operation = TaskOperation.Create,
                DeviceId = device.Id
            };

            // Add account
            await _deviceAccountService.AddAsync(deviceAccount);

            try
            {
                // Add task
                await _deviceTaskService.AddTaskAsync(deviceTask);
            }
            catch (Exception)
            {
                // Remove account
                await _deviceAccountService.DeleteAsync(deviceAccount);
                throw;
            }
        }

        public async Task UpdatePasswordSamlIdpAccountAsync(string email, string password)
        {
            _dataProtectionService.Validate();

            var employee = await _employeeRepository.Query().FirstOrDefaultAsync(e => e.Email == email);
            if (employee == null)
            {
                throw new ArgumentNullException(nameof(employee));
            }
            var deviceAccount = await _deviceAccountService
             .Query()
             .Where(d => d.EmployeeId == employee.Id && d.Name == SamlIdentityProvider.DeviceAccountName)
             .FirstOrDefaultAsync();

            // Update Device Account
            deviceAccount.Status = AccountStatus.Updating;
            deviceAccount.UpdatedAt = DateTime.UtcNow;
            string[] properties = { "Status", "UpdatedAt" };
            await _deviceAccountService.UpdateOnlyPropAsync(deviceAccount, properties);

            // Create Device Task
            try
            {
                await _deviceTaskService.AddTaskAsync(new DeviceTask
                {
                    DeviceAccountId = deviceAccount.Id,
                    Password = _dataProtectionService.Protect(password),
                    CreatedAt = DateTime.UtcNow,
                    Operation = TaskOperation.Update,
                    DeviceId = deviceAccount.DeviceId
                });
            }
            catch (Exception)
            {
                deviceAccount.Status = AccountStatus.Error;
                await _deviceAccountService.UpdateOnlyPropAsync(deviceAccount, properties);
                throw;
            }
        }

        public async Task UpdateOtpSamlIdpAccountAsync(string email, string otp)
        {
            if (email == null)
            {
                throw new ArgumentNullException(nameof(email));
            }
            if (otp == null)
            {
                throw new ArgumentNullException(nameof(otp));
            }

            ValidationHepler.VerifyOtpSecret(otp);

            _dataProtectionService.Validate();

            var employee = await _employeeRepository.Query().FirstOrDefaultAsync(e => e.Email == email);
            if (employee == null)
            {
                throw new ArgumentNullException(nameof(employee));
            }
            var deviceAccount = await _deviceAccountService
             .Query()
             .Where(d => d.EmployeeId == employee.Id && d.Name == SamlIdentityProvider.DeviceAccountName)
             .FirstOrDefaultAsync();

            var task = await _deviceTaskService
                .Query()
                .AsNoTracking()
                .Where(d => d.DeviceAccountId == deviceAccount.Id && _dataProtectionService.Unprotect(d.OtpSecret) == otp)
                .FirstOrDefaultAsync();

            if (task != null)
            {
                return;
            }

            // Update Device Account
            deviceAccount.Status = AccountStatus.Updating;
            deviceAccount.UpdatedAt = DateTime.UtcNow;
            string[] properties = { "Status", "UpdatedAt" };
            await _deviceAccountService.UpdateOnlyPropAsync(deviceAccount, properties);

            // Create Device Task
            try
            {
                await _deviceTaskService.AddTaskAsync(new DeviceTask
                {
                    DeviceAccountId = deviceAccount.Id,
                    OtpSecret = _dataProtectionService.Protect(otp),
                    CreatedAt = DateTime.UtcNow,
                    Operation = TaskOperation.Update,
                    DeviceId = deviceAccount.DeviceId
                });
            }
            catch (Exception)
            {
                deviceAccount.Status = AccountStatus.Error;
                await _deviceAccountService.UpdateOnlyPropAsync(deviceAccount, properties);
                throw;
            }
        }

        public async Task<IList<string>> UpdateUrlSamlIdpAccountAsync(string hesUrl)
        {
            _dataProtectionService.Validate();

            var deviceAccounts = await _deviceAccountService
             .Query()
             .Where(d => d.Name == SamlIdentityProvider.DeviceAccountName && d.Deleted == false)
             .ToListAsync();

            var samlIdP = await _samlIdentityProviderService.GetByIdAsync(SamlIdentityProvider.PrimaryKey);
            var validUrls = ValidationHepler.VerifyUrls($"{samlIdP.Url};{hesUrl}");

            foreach (var account in deviceAccounts)
            {
                // Update Device Account
                account.Status = AccountStatus.Updating;
                account.UpdatedAt = DateTime.UtcNow;
                string[] properties = { "Status", "UpdatedAt" };
                await _deviceAccountService.UpdateOnlyPropAsync(account, properties);

                // Create Device Task
                try
                {
                    await _deviceTaskService.AddTaskAsync(new DeviceTask
                    {
                        DeviceAccountId = account.Id,
                        Urls = validUrls,
                        CreatedAt = DateTime.UtcNow,
                        Operation = TaskOperation.Update,
                        DeviceId = account.DeviceId
                    });
                }
                catch (Exception)
                {
                    account.Status = AccountStatus.Error;
                    await _deviceAccountService.UpdateOnlyPropAsync(account, properties);
                    throw;
                }
            }

            return deviceAccounts.Select(s => s.Id).ToList();
        }

        public async Task DeleteSamlIdpAccountAsync(string employeeId)
        {
            if (employeeId == null)
            {
                throw new ArgumentNullException(nameof(employeeId));
            }

            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee == null)
            {
                throw new Exception("Employee not found.");
            }

            var account = await _deviceAccountService
                .Query()
                .Where(d => d.EmployeeId == employeeId && d.Name == SamlIdentityProvider.DeviceAccountName)
                .FirstOrDefaultAsync();

            if (account != null)
            {
                await DeleteAccount(account.Id);
            }
        }

        #endregion

        #region Device

        public async Task AddDeviceAsync(string employeeId, string[] selectedDevices)
        {
            _dataProtectionService.Validate();

            var employee = await _employeeRepository.GetByIdAsync(employeeId);
            if (employee == null)
                throw new Exception("Employee not found");

            foreach (var deviceId in selectedDevices)
            {
                var device = await _deviceService.GetByIdAsync(deviceId);
                if (device == null)
                    throw new Exception($"Device not found, ID: {deviceId}");

                if (device.EmployeeId != null)
                    throw new Exception($"Device {deviceId} already linked to another employee");

                var masterPassword = GenerateMasterPassword();

                device.EmployeeId = employeeId;
                await _deviceService.UpdateOnlyPropAsync(device, new string[] { "EmployeeId" });

                await _deviceTaskService.AddTaskAsync(new DeviceTask
                {
                    Password = _dataProtectionService.Protect(masterPassword),
                    Operation = TaskOperation.Link,
                    CreatedAt = DateTime.UtcNow,
                    DeviceId = device.Id
                });
            }
        }

        public async Task RemoveDeviceAsync(string employeeId, string deviceId)
        {
            _dataProtectionService.Validate();

            var device = await _deviceService.GetByIdAsync(deviceId);
            if (device == null)
                throw new Exception($"Device not found, ID: {deviceId}");

            if (device.EmployeeId != employeeId)
                throw new Exception($"Device {deviceId} not linked to employee");

            // Remove all tasks
            await _deviceTaskService.RemoveAllTasksAsync(deviceId);

            // Remove all accounts
            await _deviceAccountService.RemoveAllByDeviceIdAsync(deviceId);

            // Remove proximity device
            var allProximityDevices = await _workstationProximityDeviceService
                .Query()
                .Where(w => w.DeviceId == deviceId)
                .ToListAsync();
            await _workstationProximityDeviceService.DeleteRangeProximityDevicesAsync(allProximityDevices);

            // Remove employee from device
            device.EmployeeId = null;
            device.PrimaryAccountId = null;
            await _deviceService.UpdateOnlyPropAsync(device, new string[] { "EmployeeId", "PrimaryAccountId" });

            if (device.MasterPassword != null)
            {
                // Add Task remove device
                await _deviceTaskService.AddTaskAsync(new DeviceTask
                {
                    Password = _dataProtectionService.Protect(device.MasterPassword),
                    CreatedAt = DateTime.UtcNow,
                    Operation = TaskOperation.Wipe,
                    DeviceId = device.Id
                });
            }
        }

        #endregion

        #region Account

        public async Task SetPrimaryAccount(string deviceId, string deviceAccountId)
        {
            if (deviceId == null)
                throw new ArgumentNullException(nameof(deviceId));

            if (deviceAccountId == null)
                throw new ArgumentNullException(nameof(deviceAccountId));

            var device = await _deviceService.GetByIdAsync(deviceId);
            if (device == null)
                throw new Exception($"Device not found, ID: {deviceId}");

            // Update Device Account
            var deviceAccount = await _deviceAccountService.GetByIdAsync(deviceAccountId);
            if (deviceAccount == null)
                throw new Exception($"DeviceAccount not found, ID: {deviceAccountId}");

            deviceAccount.Status = AccountStatus.Updating;
            deviceAccount.UpdatedAt = DateTime.UtcNow;
            string[] properties = { "Status", "UpdatedAt" };
            await _deviceAccountService.UpdateOnlyPropAsync(deviceAccount, properties);

            // Add task
            await _deviceTaskService.AddTaskAsync(new DeviceTask()
            {
                Operation = TaskOperation.Primary,
                CreatedAt = DateTime.UtcNow,
                DeviceId = device.Id,
                DeviceAccountId = deviceAccountId
            });
        }

        private async Task SetAsPrimaryIfEmpty(string deviceId, string deviceAccountId)
        {
            var device = await _deviceService.GetByIdAsync(deviceId);

            if (device.PrimaryAccountId == null)
            {
                device.PrimaryAccountId = deviceAccountId;
                await _deviceService.UpdateOnlyPropAsync(device, new string[] { "PrimaryAccountId" });
            }
        }

        public async Task CreateWorkstationAccountAsync(WorkstationAccount workstationAccount, string employeeId, string deviceId)
        {
            if (workstationAccount == null)
            {
                throw new ArgumentNullException(nameof(workstationAccount));
            }
            if (employeeId == null)
            {
                throw new ArgumentNullException(nameof(employeeId));
            }
            if (deviceId == null)
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            var deviceAccount = new DeviceAccount()
            {
                Name = workstationAccount.Name,
                EmployeeId = employeeId,
                Kind = AccountKind.Workstation
            };

            switch (workstationAccount.AccountType)
            {
                case WorkstationAccountType.Local:
                    deviceAccount.Login = $".\\{workstationAccount.Login}";
                    break;
                case WorkstationAccountType.Domain:
                    deviceAccount.Login = $"{workstationAccount.Domain}\\{workstationAccount.Login}";
                    break;
                case WorkstationAccountType.Microsoft:
                    deviceAccount.Login = $"@\\{workstationAccount.Login}";
                    break;
            }

            var input = new InputModel()
            {
                Password = workstationAccount.Password
            };

            await CreatePersonalAccountAsync(deviceAccount, input, new string[] { deviceId });
        }

        public async Task CreatePersonalAccountAsync(DeviceAccount deviceAccount, InputModel input, string[] selectedDevices)
        {
            if (deviceAccount == null)
                throw new ArgumentNullException(nameof(deviceAccount));

            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (selectedDevices == null)
                throw new ArgumentNullException(nameof(selectedDevices));

            ValidationHepler.VerifyOtpSecret(input.OtpSecret);

            _dataProtectionService.Validate();

            List<DeviceAccount> accounts = new List<DeviceAccount>();
            List<DeviceTask> tasks = new List<DeviceTask>();

            foreach (var deviceId in selectedDevices)
            {
                var exist = await _deviceAccountService
                    .Query()
                    .Where(s => s.Name == deviceAccount.Name)
                    .Where(s => s.Login == deviceAccount.Login)
                    .Where(s => s.Deleted == false)
                    .Where(s => s.DeviceId == deviceId)
                    .AnyAsync();

                if (exist)
                    throw new Exception("An account with the same name and login exists.");

                // Validate url
                deviceAccount.Urls = ValidationHepler.VerifyUrls(deviceAccount.Urls);

                // Create Device Account
                var deviceAccountId = Guid.NewGuid().ToString();
                accounts.Add(new DeviceAccount
                {
                    Id = deviceAccountId,
                    Name = deviceAccount.Name,
                    Urls = deviceAccount.Urls,
                    Apps = deviceAccount.Apps,
                    Login = deviceAccount.Login,
                    Type = AccountType.Personal,
                    Status = AccountStatus.Creating,
                    CreatedAt = DateTime.UtcNow,
                    PasswordUpdatedAt = DateTime.UtcNow,
                    OtpUpdatedAt = input.OtpSecret != null ? new DateTime?(DateTime.UtcNow) : null,
                    Kind = deviceAccount.Kind,
                    EmployeeId = deviceAccount.EmployeeId,
                    DeviceId = deviceId,
                    SharedAccountId = null
                });

                // Create Device Task
                tasks.Add(new DeviceTask
                {
                    DeviceAccountId = deviceAccountId,
                    Name = deviceAccount.Name,
                    Urls = deviceAccount.Urls,
                    Apps = deviceAccount.Apps,
                    Login = deviceAccount.Login,
                    Password = _dataProtectionService.Protect(input.Password),
                    OtpSecret = input.OtpSecret,
                    CreatedAt = DateTime.UtcNow,
                    Operation = TaskOperation.Create,
                    DeviceId = deviceId
                });

                // Set primary account
                await SetAsPrimaryIfEmpty(deviceId, deviceAccountId);
            }

            await _deviceAccountService.AddRangeAsync(accounts);

            try
            {
                await _deviceTaskService.AddRangeAsync(tasks);
            }
            catch (Exception)
            {
                await _deviceAccountService.DeleteRangeAsync(accounts);
                throw;
            }
        }

        public async Task EditPersonalAccountAsync(DeviceAccount deviceAccount)
        {
            _dataProtectionService.Validate();

            if (deviceAccount == null)
                throw new ArgumentNullException(nameof(deviceAccount));

            var exist = await _deviceAccountService
                .Query()
                .Where(s => s.Name == deviceAccount.Name)
                .Where(s => s.Login == deviceAccount.Login)
                .Where(s => s.Deleted == false)
                .Where(s => s.Id != deviceAccount.Id)
                .Where(s => s.DeviceId == deviceAccount.DeviceId)
                .AnyAsync();

            if (exist)
                throw new Exception("An account with the same name and login exists.");

            // Validate url
            deviceAccount.Urls = ValidationHepler.VerifyUrls(deviceAccount.Urls);

            // Update Device Account
            deviceAccount.Status = AccountStatus.Updating;
            deviceAccount.UpdatedAt = DateTime.UtcNow;
            string[] properties = { "Status", "UpdatedAt" };
            await _deviceAccountService.UpdateOnlyPropAsync(deviceAccount, properties);

            // Create Device Task
            try
            {
                await _deviceTaskService.AddTaskAsync(new DeviceTask
                {
                    DeviceAccountId = deviceAccount.Id,
                    Name = deviceAccount.Name,
                    Urls = deviceAccount.Urls ?? string.Empty,
                    Apps = deviceAccount.Apps ?? string.Empty,
                    Login = deviceAccount.Login,
                    Password = null,
                    OtpSecret = null,
                    CreatedAt = DateTime.UtcNow,
                    Operation = TaskOperation.Update,
                    DeviceId = deviceAccount.DeviceId
                });
            }
            catch (Exception)
            {
                deviceAccount.Status = AccountStatus.Error;
                await _deviceAccountService.UpdateOnlyPropAsync(deviceAccount, properties);
                throw;
            }
        }

        public async Task EditPersonalAccountPwdAsync(DeviceAccount deviceAccount, InputModel input)
        {
            _dataProtectionService.Validate();

            if (deviceAccount == null)
                throw new ArgumentNullException(nameof(deviceAccount));

            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // Update Device Account
            deviceAccount.Status = AccountStatus.Updating;
            deviceAccount.UpdatedAt = DateTime.UtcNow;
            string[] properties = { "Status", "UpdatedAt" };
            await _deviceAccountService.UpdateOnlyPropAsync(deviceAccount, properties);

            // Create Device Task
            try
            {
                await _deviceTaskService.AddTaskAsync(new DeviceTask
                {
                    DeviceAccountId = deviceAccount.Id,
                    Password = _dataProtectionService.Protect(input.Password),
                    CreatedAt = DateTime.UtcNow,
                    Operation = TaskOperation.Update,
                    DeviceId = deviceAccount.DeviceId
                });
            }
            catch (Exception)
            {
                deviceAccount.Status = AccountStatus.Error;
                await _deviceAccountService.UpdateOnlyPropAsync(deviceAccount, properties);
                throw;
            }
        }

        public async Task EditPersonalAccountOtpAsync(DeviceAccount deviceAccount, InputModel input)
        {
            ValidationHepler.VerifyOtpSecret(input.OtpSecret);

            _dataProtectionService.Validate();

            if (deviceAccount == null)
                throw new ArgumentNullException(nameof(deviceAccount));

            // Update Device Account
            deviceAccount.Status = AccountStatus.Updating;
            deviceAccount.UpdatedAt = DateTime.UtcNow;
            deviceAccount.OtpUpdatedAt = DateTime.UtcNow;
            string[] properties = { "Status", "UpdatedAt", "OtpUpdatedAt" };
            await _deviceAccountService.UpdateOnlyPropAsync(deviceAccount, properties);

            // Create Device Task
            try
            {
                await _deviceTaskService.AddTaskAsync(new DeviceTask
                {
                    DeviceAccountId = deviceAccount.Id,
                    OtpSecret = _dataProtectionService.Protect(input.OtpSecret ?? string.Empty),
                    CreatedAt = DateTime.UtcNow,
                    Operation = TaskOperation.Update,
                    DeviceId = deviceAccount.DeviceId
                });
            }
            catch (Exception)
            {
                deviceAccount.Status = AccountStatus.Error;
                await _deviceAccountService.UpdateOnlyPropAsync(deviceAccount, properties);
                throw;
            }
        }

        public async Task AddSharedAccount(string employeeId, string sharedAccountId, string[] selectedDevices)
        {
            _dataProtectionService.Validate();

            if (employeeId == null)
                throw new ArgumentNullException(nameof(employeeId));

            if (sharedAccountId == null)
                throw new ArgumentNullException(nameof(sharedAccountId));

            if (selectedDevices == null)
                throw new ArgumentNullException(nameof(selectedDevices));


            List<DeviceAccount> accounts = new List<DeviceAccount>();
            List<DeviceTask> tasks = new List<DeviceTask>();

            foreach (var deviceId in selectedDevices)
            {
                // Get Shared Account
                var sharedAccount = await _sharedAccountService.GetByIdAsync(sharedAccountId);
                if (sharedAccount == null)
                    throw new Exception("SharedAccount not found");

                var exist = await _deviceAccountService
                    .Query()
                    .Where(s => s.Name == sharedAccount.Name)
                    .Where(s => s.Login == sharedAccount.Login)
                    .Where(s => s.Deleted == false)
                    .Where(d => d.DeviceId == deviceId)
                    .AnyAsync();

                if (exist)
                    throw new Exception("An account with the same name and login exists");

                // Create Device Account
                var deviceAccountId = Guid.NewGuid().ToString();
                accounts.Add(new DeviceAccount
                {
                    Id = deviceAccountId,
                    Name = sharedAccount.Name,
                    Urls = sharedAccount.Urls,
                    Apps = sharedAccount.Apps,
                    Login = sharedAccount.Login,
                    Type = AccountType.Shared,
                    Status = AccountStatus.Creating,
                    CreatedAt = DateTime.UtcNow,
                    PasswordUpdatedAt = DateTime.UtcNow,
                    OtpUpdatedAt = sharedAccount.OtpSecret != null ? new DateTime?(DateTime.UtcNow) : null,
                    EmployeeId = employeeId,
                    DeviceId = deviceId,
                    SharedAccountId = sharedAccountId
                });

                // Create Device Task
                tasks.Add(new DeviceTask
                {
                    DeviceAccountId = deviceAccountId,
                    Name = sharedAccount.Name,
                    Urls = sharedAccount.Urls,
                    Apps = sharedAccount.Apps,
                    Login = sharedAccount.Login,
                    Password = sharedAccount.Password,
                    OtpSecret = sharedAccount.OtpSecret,
                    CreatedAt = DateTime.UtcNow,
                    Operation = TaskOperation.Create,
                    DeviceId = deviceId
                });

                // Set primary account
                await SetAsPrimaryIfEmpty(deviceId, deviceAccountId);
            }

            await _deviceAccountService.AddRangeAsync(accounts);
            try
            {
                await _deviceTaskService.AddRangeAsync(tasks);
            }
            catch (Exception)
            {
                await _deviceAccountService.DeleteRangeAsync(accounts);
                throw;
            }
        }

        public async Task<string> DeleteAccount(string accountId)
        {
            _dataProtectionService.Validate();

            if (accountId == null)
                throw new ArgumentNullException(nameof(accountId));

            var deviceAccount = await _deviceAccountService.GetByIdAsync(accountId);
            if (deviceAccount == null)
                throw new Exception("Device account not found");

            // Update Device Account
            deviceAccount.Status = AccountStatus.Removing;
            deviceAccount.UpdatedAt = DateTime.UtcNow;
            string[] properties = { "Status", "UpdatedAt" };
            await _deviceAccountService.UpdateOnlyPropAsync(deviceAccount, properties);

            try
            {
                // Create Device Task
                await _deviceTaskService.AddTaskAsync(new DeviceTask
                {
                    DeviceAccountId = accountId,
                    CreatedAt = DateTime.UtcNow,
                    Operation = TaskOperation.Delete,
                    DeviceId = deviceAccount.DeviceId
                });
            }
            catch (Exception)
            {
                deviceAccount.Status = AccountStatus.Error;
                await _deviceAccountService.UpdateOnlyPropAsync(deviceAccount, properties);
                throw;
            }
            return deviceAccount.DeviceId;
        }

        public async Task UndoChanges(string accountId)
        {
            if (accountId == null)
                throw new ArgumentNullException(nameof(accountId));

            _dataProtectionService.Validate();

            await _deviceTaskService.UndoLastTaskAsync(accountId);
        }

        private string GenerateMasterPassword()
        {
            var buf = AesCryptoHelper.CreateRandomBuf(32);
            var pass = ConvertUtils.ByteArrayToHexString(buf);
            return pass;
        }

        #endregion

        public async Task HandlingMasterPasswordErrorAsync(string deviceId)
        {
            // Remove all tasks
            await _deviceTaskService.RemoveAllTasksAsync(deviceId);

            // Remove all accounts
            await _deviceAccountService.RemoveAllByDeviceIdAsync(deviceId);

            // Remove all proximity
            await _workstationProximityDeviceService.RemoveAllProximityAsync(deviceId);

            // Remove employee
            await _deviceService.RemoveEmployeeAsync(deviceId);
        }
    }
}