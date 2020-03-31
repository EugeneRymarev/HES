﻿using HES.Core.Entities;
using HES.Core.Enums;
using HES.Core.Interfaces;
using HES.Core.Models.Web.AppSettings;
using HES.Core.Models.Web.SoftwareVault;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace HES.Core.Services
{
    public class SoftwareVaultService : ISoftwareVaultService
    {
        private readonly IAsyncRepository<SoftwareVault> _softwareVaultRepository;
        private readonly IAsyncRepository<SoftwareVaultInvitation> _softwareVaultInvitationRepository;
        private readonly IEmailSenderService _emailSenderService;

        public SoftwareVaultService(IAsyncRepository<SoftwareVault> softwareVaultRepository,
                                    IAsyncRepository<SoftwareVaultInvitation> softwareVaultInvitationRepository,
                                    IEmailSenderService emailSenderService)
        {
            _softwareVaultRepository = softwareVaultRepository;
            _softwareVaultInvitationRepository = softwareVaultInvitationRepository;
            _emailSenderService = emailSenderService;
        }

        public IQueryable<SoftwareVault> SoftwareVaultQuery()
        {
            return _softwareVaultRepository.Query();
        }

        public async Task<List<SoftwareVault>> GetSoftwareVaultsAsync(int skip, int take, string sortColumn, ListSortDirection sortDirection, string searchText, SoftwareVaultFilter filter)
        {
            var query = _softwareVaultRepository
               .Query()
               .Include(x => x.Employee.Department.Company)
               .AsQueryable();

            // Filter
            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.OS))
                {
                    query = query.Where(x => x.OS.Contains(filter.OS, StringComparison.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrWhiteSpace(filter.Model))
                {
                    query = query.Where(x => x.Model.Contains(filter.Model, StringComparison.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrWhiteSpace(filter.ClientAppVersion))
                {
                    query = query.Where(x => x.ClientAppVersion.Contains(filter.ClientAppVersion, StringComparison.OrdinalIgnoreCase));
                }
                if (filter.Status != null)
                {
                    query = query.Where(x => x.Status == filter.Status);
                }
                if (filter.LicenseStatus != null)
                {
                    query = query.Where(x => x.LicenseStatus == filter.LicenseStatus);
                }
                if (filter.EmployeeId != null)
                {
                    query = query.Where(x => x.EmployeeId == filter.EmployeeId);
                }
                if (filter.CompanyId != null)
                {
                    query = query.Where(x => x.Employee.Department.CompanyId == filter.CompanyId);
                }
                if (filter.DepartmentId != null)
                {
                    query = query.Where(x => x.Employee.DepartmentId == filter.DepartmentId);
                }
            }

            // Search
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.Trim();

                query = query.Where(x => x.OS.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                    x.Model.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                    x.ClientAppVersion.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                    (x.Employee.FirstName + " " + x.Employee.LastName).Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                    x.Employee.FirstName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                    x.Employee.LastName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                    x.Employee.Department.Company.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                    x.Employee.Department.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            // Sort Direction
            switch (sortColumn)
            {
                case nameof(SoftwareVault.OS):
                    query = sortDirection == ListSortDirection.Ascending ? query.OrderBy(x => x.OS) : query.OrderByDescending(x => x.OS);
                    break;
                case nameof(SoftwareVault.Model):
                    query = sortDirection == ListSortDirection.Ascending ? query.OrderBy(x => x.Model) : query.OrderByDescending(x => x.Model);
                    break;
                case nameof(SoftwareVault.ClientAppVersion):
                    query = sortDirection == ListSortDirection.Ascending ? query.OrderBy(x => x.ClientAppVersion) : query.OrderByDescending(x => x.ClientAppVersion);
                    break;
                case nameof(SoftwareVault.Status):
                    query = sortDirection == ListSortDirection.Ascending ? query.OrderBy(x => x.Status) : query.OrderByDescending(x => x.Status);
                    break;
                case nameof(SoftwareVault.LicenseStatus):
                    query = sortDirection == ListSortDirection.Ascending ? query.OrderBy(x => x.LicenseStatus) : query.OrderByDescending(x => x.LicenseStatus);
                    break;
                case nameof(SoftwareVault.Employee):
                    query = sortDirection == ListSortDirection.Ascending ? query.OrderBy(x => x.Employee.FirstName).ThenBy(x => x.Employee.LastName) : query.OrderByDescending(x => x.Employee.FirstName).ThenByDescending(x => x.Employee.LastName);
                    break;
                case nameof(SoftwareVault.Employee.Department.Company):
                    query = sortDirection == ListSortDirection.Ascending ? query.OrderBy(x => x.Employee.Department.Company.Name) : query.OrderByDescending(x => x.Employee.Department.Company.Name);
                    break;
                case nameof(SoftwareVault.Employee.Department):
                    query = sortDirection == ListSortDirection.Ascending ? query.OrderBy(x => x.Employee.Department.Name) : query.OrderByDescending(x => x.Employee.Department.Name);
                    break;
            }

            return await query.Skip(skip).Take(take).AsNoTracking().ToListAsync();
        }

        public async Task<int> GetVaultsCountAsync(string searchText, SoftwareVaultFilter filter)
        {
            var query = _softwareVaultRepository.Query();

            // Filter
            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.OS))
                {
                    query = query.Where(x => x.OS.Contains(filter.OS, StringComparison.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrWhiteSpace(filter.Model))
                {
                    query = query.Where(x => x.Model.Contains(filter.Model, StringComparison.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrWhiteSpace(filter.ClientAppVersion))
                {
                    query = query.Where(x => x.ClientAppVersion.Contains(filter.ClientAppVersion, StringComparison.OrdinalIgnoreCase));
                }
                if (filter.Status != null)
                {
                    query = query.Where(x => x.Status == filter.Status);
                }
                if (filter.LicenseStatus != null)
                {
                    query = query.Where(x => x.LicenseStatus == filter.LicenseStatus);
                }
                if (filter.EmployeeId != null)
                {
                    query = query.Where(x => x.EmployeeId == filter.EmployeeId);
                }
                if (filter.CompanyId != null)
                {
                    query = query.Where(x => x.Employee.Department.CompanyId == filter.CompanyId);
                }
                if (filter.DepartmentId != null)
                {
                    query = query.Where(x => x.Employee.DepartmentId == filter.DepartmentId);
                }
            }

            // Search
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                searchText = searchText.Trim();

                query = query.Where(x => x.OS.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                    x.Model.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                    x.ClientAppVersion.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                    (x.Employee.FirstName + " " + x.Employee.LastName).Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                    x.Employee.FirstName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                    x.Employee.LastName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                    x.Employee.Department.Company.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                    x.Employee.Department.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            return await query.CountAsync();
        }

        public async Task<List<SoftwareVaultInvitation>> GetSoftwareVaultInvitationsAsync()
        {
            return await _softwareVaultInvitationRepository
               .Query()
               .Include(x => x.Employee)
               .AsTracking()
               .ToListAsync();
        }

        public async Task CreateAndSendInvitationAsync(Employee employee, Server server, DateTime validTo)
        {
            if (employee == null)
                throw new ArgumentNullException(nameof(employee));

            if (employee.Id == null)
                throw new ArgumentNullException(nameof(employee.Id));

            if (employee.Email == null)
                throw new ArgumentNullException(nameof(employee.Email));

            var activationCode = GenerateActivationCode();

            var invitation = new SoftwareVaultInvitation()
            {
                EmployeeId = employee.Id,
                Status = InviteVaultStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ValidTo = validTo.Date.AddHours(23).AddMinutes(59).AddSeconds(59),
                AcceptedAt = null,
                SoftwareVaultId = null,
                ActivationCode = activationCode
            };

            var created = await _softwareVaultInvitationRepository.AddAsync(invitation);

            var activation = new SoftwareVaultActivation()
            {
                ServerAddress = server.Url,
                ActivationId = created.Id,
                ActivationCode = activationCode
            };

            await _emailSenderService.SendSoftwareVaultInvitationAsync(employee, activation, validTo);
        }

        private int GenerateActivationCode()
        {
            return new Random().Next(100000, 999999);
        }

 
    }
}