﻿using HES.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace HES.Core.Services
{
    public class SynchronizationService : ISynchronizationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public event Func<string, string, Task> UpdateAlarmPage;
        public event Func<string, string, Task> UpdateEmployeePage;
        public event Func<string, string, string, Task> UpdateEmployeeDetailsPage;
        public event Func<string, string, Task> UpdateGroupsPage;
        public event Func<string, string, string, Task> UpdateGroupDetailsPage;
        public event Func<string, string, Task> UpdateHardwareVaultsPage;
        public event Func<string, Task> UpdateHardwareVaultState;
        public event Func<string, string, Task> UpdateTemplatesPage;
        public event Func<string, string, Task> UpdateSharedAccountsPage;
        public event Func<string, string, Task> UpdateWorkstationsPage;
        public event Func<string, string, string, Task> UpdateWorkstationDetailsPage;
        public event Func<string, string, Task> UpdateDataProtectionPage;
        public event Func<string, string, Task> UpdateAdministratorsPage;
        public event Func<Task> UpdateAdministratorStatePage;
        public event Func<string, string, Task> UpdateHardwareVaultProfilesPage;
        public event Func<string, string, Task> UpdateLicensesPage;
        public event Func<string, string, Task> UpdateParametersPage;
        public event Func<string, string, Task> UpdateOrgSructureCompaniesPage;
        public event Func<string, string, Task> UpdateOrgSructurePositionsPage;

        public SynchronizationService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private async Task InvokeEventAsync(Func<string, string, Task> func, string exceptPageId)
        {
            if (func != null)
            {
                var userName = _httpContextAccessor.HttpContext.User.Identity.Name;
                await func.Invoke(exceptPageId, userName);
            }
        }

        private async Task InvokeEventAsync(Func<string, string, string, Task> func, string exceptPageId, string entityId)
        {
            if (func != null)
            {
                var userName = _httpContextAccessor.HttpContext.User.Identity.Name;
                await func.Invoke(exceptPageId, entityId, userName);
            }
        }

        public async Task UpdateAlarm(string exceptPageId)
        {
            await InvokeEventAsync(UpdateAlarmPage, exceptPageId);
        }

        public async Task UpdateEmployees(string exceptPageId)
        {
            await InvokeEventAsync(UpdateEmployeePage, exceptPageId);
        }

        public async Task UpdateEmployeeDetails(string exceptPageId, string employeeId)
        {
            await InvokeEventAsync(UpdateEmployeeDetailsPage, exceptPageId, employeeId);
        }

        public async Task UpdateGroups(string exceptPageId)
        {
            await InvokeEventAsync(UpdateGroupsPage, exceptPageId);
        }

        public async Task UpdateGroupDetails(string exceptPageId, string groupId)
        {
            await InvokeEventAsync(UpdateGroupDetailsPage, exceptPageId, groupId);
        }

        public async Task HardwareVaultStateChanged(string hardwareVaultId)
        {
            if (UpdateHardwareVaultState != null)
            {
                await UpdateHardwareVaultState.Invoke(hardwareVaultId);
            }
        }

        public async Task UpdateHardwareVaults(string exceptPageId)
        {
            await InvokeEventAsync(UpdateHardwareVaultsPage, exceptPageId);
        }

        public async Task UpdateTemplates(string exceptPageId)
        {
            await InvokeEventAsync(UpdateTemplatesPage, exceptPageId);
        }

        public async Task UpdateSharedAccounts(string exceptPageId)
        {
            await InvokeEventAsync(UpdateSharedAccountsPage, exceptPageId);
        }

        public async Task UpdateWorkstations(string exceptPageId)
        {
            await InvokeEventAsync(UpdateWorkstationsPage, exceptPageId);
        }

        public async Task UpdateWorkstationDetails(string exceptPageId, string workstationId)
        {
            await InvokeEventAsync(UpdateWorkstationDetailsPage, exceptPageId, workstationId);
        }

        public async Task UpdateDataProtection(string exceptPageId)
        {
            await InvokeEventAsync(UpdateDataProtectionPage, exceptPageId);
        }

        public async Task UpdateAdministrators(string exceptPageId)
        {
            await InvokeEventAsync(UpdateAdministratorsPage, exceptPageId);
        }

        public async Task UpdateAdministratorState()
        {
            if (UpdateAdministratorStatePage != null)
            {
                await UpdateAdministratorStatePage.Invoke();
            }
        }

        public async Task UpdateHardwareVaultProfiles(string exceptPageId)
        {
            await InvokeEventAsync(UpdateHardwareVaultProfilesPage, exceptPageId);
        }

        public async Task UpdateLicenses(string exceptPageId)
        {
            await InvokeEventAsync(UpdateLicensesPage, exceptPageId);
        }

        public async Task UpdateParameters(string exceptPageId)
        {
            await InvokeEventAsync(UpdateParametersPage, exceptPageId);
        }

        public async Task UpdateOrgSructureCompanies(string exceptPageId)
        {
            await InvokeEventAsync(UpdateOrgSructureCompaniesPage, exceptPageId);
        }

        public async Task UpdateOrgSructurePositions(string exceptPageId)
        {
            await InvokeEventAsync(UpdateOrgSructurePositionsPage, exceptPageId);
        }
    }
}