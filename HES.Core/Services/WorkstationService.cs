﻿using HES.Core.Entities;
using HES.Core.Interfaces;
using HES.Core.Models;
using Hideez.SDK.Communication.HES.DTO;
using Hideez.SDK.Communication.Workstation;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Transactions;

namespace HES.Core.Services
{
    public class WorkstationService : IWorkstationService
    {
        private readonly IAsyncRepository<Workstation> _workstationRepository;
        private readonly IAsyncRepository<WorkstationProximityVault> _workstationProximityVaultRepository;

        public WorkstationService(IAsyncRepository<Workstation> workstationRepository,
                                  IAsyncRepository<WorkstationProximityVault> workstationProximityVaultRepository)
        {
            _workstationRepository = workstationRepository;
            _workstationProximityVaultRepository = workstationProximityVaultRepository;
        }

        #region Workstation

        public IQueryable<Workstation> WorkstationQuery()
        {
            return _workstationRepository.Query();
        }

        public async Task<Workstation> GetWorkstationByIdAsync(string id)
        {
            return await _workstationRepository
                .Query()
                .Include(c => c.Department.Company)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<List<Workstation>> GetWorkstationsAsync()
        {
            return await _workstationRepository
                .Query()
                .Include(w => w.WorkstationProximityVaults)
                .Include(c => c.Department.Company)
                .ToListAsync();
        }

        public async Task<List<Workstation>> GetFilteredWorkstationsAsync(WorkstationFilter workstationFilter)
        {
            var filter = _workstationRepository
                .Query()
                .Include(w => w.WorkstationProximityVaults)
                .Include(c => c.Department.Company)
                .AsQueryable();

            if (workstationFilter.Name != null)
            {
                filter = filter.Where(w => w.Name.Contains(workstationFilter.Name));
            }
            if (workstationFilter.Domain != null)
            {
                filter = filter.Where(w => w.Domain.Contains(workstationFilter.Domain));
            }
            if (workstationFilter.ClientVersion != null)
            {
                filter = filter.Where(w => w.ClientVersion.Contains(workstationFilter.ClientVersion));
            }
            if (workstationFilter.CompanyId != null)
            {
                filter = filter.Where(w => w.Department.Company.Id == workstationFilter.CompanyId);
            }
            if (workstationFilter.DepartmentId != null)
            {
                filter = filter.Where(w => w.DepartmentId == workstationFilter.DepartmentId);
            }
            if (workstationFilter.OS != null)
            {
                filter = filter.Where(w => w.OS.Contains(workstationFilter.OS));
            }
            if (workstationFilter.IP != null)
            {
                filter = filter.Where(w => w.IP.Contains(workstationFilter.IP));
            }
            if (workstationFilter.StartDate != null)
            {
                filter = filter.Where(w => w.LastSeen >= workstationFilter.StartDate.Value.AddSeconds(0).AddMilliseconds(0).ToUniversalTime());
            }
            if (workstationFilter.EndDate != null)
            {
                filter = filter.Where(w => w.LastSeen <= workstationFilter.EndDate.Value.AddSeconds(59).AddMilliseconds(999).ToUniversalTime());
            }
            if (workstationFilter.RFID != null)
            {
                filter = filter.Where(w => w.RFID == workstationFilter.RFID);
            }
            if (workstationFilter.ProximityDevicesCount != null)
            {
                filter = filter.Where(w => w.WorkstationProximityVaults.Count() == workstationFilter.ProximityDevicesCount);
            }
            if (workstationFilter.Approved != null)
            {
                filter = filter.Where(w => w.Approved == workstationFilter.Approved);
            }

            return await filter
                .OrderBy(w => w.Name)
                .Take(workstationFilter.Records)
                .ToListAsync();
        }

        public async Task<bool> ExistAsync(Expression<Func<Workstation, bool>> predicate)
        {
            return await _workstationRepository.ExistAsync(predicate);
        }

        public async Task AddWorkstationAsync(WorkstationInfo workstationInfo)
        {
            if (workstationInfo == null)
            {
                throw new ArgumentNullException(nameof(workstationInfo));
            }

            var workstation = new Workstation()
            {
                Id = workstationInfo.Id,
                Name = workstationInfo.MachineName,
                Domain = workstationInfo.Domain,
                OS = workstationInfo.OsName,
                ClientVersion = workstationInfo.AppVersion,
                IP = workstationInfo.IP,
                LastSeen = DateTime.UtcNow,
                DepartmentId = null
            };

            await _workstationRepository.AddAsync(workstation);
        }

        public async Task UpdateWorkstationInfoAsync(WorkstationInfo workstationInfo)
        {
            if (workstationInfo == null)
            {
                throw new ArgumentNullException(nameof(workstationInfo));
            }

            var workstation = await GetWorkstationByIdAsync(workstationInfo.Id);
            if (workstation == null)
            {
                throw new Exception($"Workstation not found.");
            }

            workstation.ClientVersion = workstationInfo.AppVersion;
            workstation.OS = workstationInfo.OsName;
            workstation.IP = workstationInfo.IP;
            workstation.LastSeen = DateTime.UtcNow;

            string[] properties = { "ClientVersion", "OS", "IP", "LastSeen" };
            await _workstationRepository.UpdateOnlyPropAsync(workstation, properties);
        }

        public async Task EditWorkstationAsync(Workstation workstation)
        {
            if (workstation == null)
            {
                throw new ArgumentNullException(nameof(workstation));
            }

            string[] properties = { "DepartmentId", "RFID" };
            await _workstationRepository.UpdateOnlyPropAsync(workstation, properties);
        }

        public async Task ApproveWorkstationAsync(Workstation workstation)
        {
            if (workstation == null)
                throw new ArgumentNullException(nameof(workstation));

            await _workstationRepository.UpdateOnlyPropAsync(workstation, new string[] { nameof(Workstation.Approved), nameof(Workstation.RFID) });
        }

        public async Task UnapproveWorkstationAsync(string workstationId)
        {
            if (workstationId == null)
            {
                throw new ArgumentNullException(nameof(workstationId));
            }

            var workstation = await GetWorkstationByIdAsync(workstationId);
            if (workstation == null)
            {
                throw new Exception("Workstation not found.");
            }

            workstation.Approved = false;
            workstation.RFID = false;

            await _workstationRepository.UpdateOnlyPropAsync(workstation, new string[] { nameof(Workstation.Approved), nameof(Workstation.RFID) });
        }

        public async Task<bool> GetRfidStateAsync(string workstationId)
        {
            var workstation = await _workstationRepository
                        .Query()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(w => w.Id == workstationId);

            return workstation.RFID;
        }

        public async Task<bool> CheckIsApprovedAsync(string workstationId)
        {
            var workstaton = await _workstationRepository.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == workstationId);

            if (workstaton == null)
                return false;

            if (workstaton.Approved == false)
                return false;

            return true;
        }

        #endregion

        #region Proximity

        public IQueryable<WorkstationProximityVault> ProximityVaultQuery()
        {
            return _workstationProximityVaultRepository.Query();
        }

        public async Task<List<WorkstationProximityVault>> GetProximityVaultsByWorkstationIdAsync(string workstationId)
        {
            return await _workstationProximityVaultRepository
                .Query()
                .Include(d => d.HardwareVault.Employee.Department.Company)
                .Where(d => d.WorkstationId == workstationId)
                .ToListAsync();
        }

        public async Task<WorkstationProximityVault> GetProximityVaultByIdAsync(string id)
        {
            return await _workstationProximityVaultRepository
                .Query()
                .Include(d => d.HardwareVault.Employee.Department.Company)
                .Include(d => d.Workstation.Department)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IList<WorkstationProximityVault>> AddProximityVaultsAsync(string workstationId, string[] vaultsIds)
        {
            if (workstationId == null)
            {
                throw new ArgumentNullException(nameof(workstationId));
            }
            if (vaultsIds == null)
            {
                throw new ArgumentNullException(nameof(vaultsIds));
            }

            List<WorkstationProximityVault> proximityVaults = new List<WorkstationProximityVault>();

            foreach (var vault in vaultsIds)
            {
                var exists = await _workstationProximityVaultRepository
                .Query()
                .Where(d => d.HardwareVaultId == vault)
                .Where(d => d.WorkstationId == workstationId)
                .FirstOrDefaultAsync();

                if (exists == null)
                {
                    proximityVaults.Add(new WorkstationProximityVault
                    {
                        WorkstationId = workstationId,
                        HardwareVaultId = vault,
                        LockProximity = 30,
                        UnlockProximity = 70,
                        LockTimeout = 5
                    });
                }
            }

            var addedVaults = await _workstationProximityVaultRepository.AddRangeAsync(proximityVaults);
             return addedVaults;
        }

        public async Task AddMultipleProximityVaultsAsync(string[] workstationsIds, string[] vaultsIds)
        {
            if (workstationsIds == null)
            {
                throw new ArgumentNullException(nameof(workstationsIds));
            }
            if (vaultsIds == null)
            {
                throw new ArgumentNullException(nameof(vaultsIds));
            }

            foreach (var workstation in workstationsIds)
            {
                await AddProximityVaultsAsync(workstation, vaultsIds);
            }
        }

        public async Task EditProximityVaultAsync(WorkstationProximityVault proximityVault)
        {
            if (proximityVault == null)
            {
                throw new ArgumentNullException(nameof(proximityVault));
            }

            string[] properties = { "LockProximity", "UnlockProximity", "LockTimeout" };
            await _workstationProximityVaultRepository.UpdateOnlyPropAsync(proximityVault, properties); 
        }

        public async Task DeleteProximityVaultAsync(string proximityVaultId)
        {
            if (proximityVaultId == null)
            {
                throw new ArgumentNullException(nameof(proximityVaultId));
            }

            var proximityVault = await _workstationProximityVaultRepository.GetByIdAsync(proximityVaultId);
            if (proximityVault == null)
            {
                throw new Exception("Binding not found.");
            }

            await _workstationProximityVaultRepository.DeleteAsync(proximityVault);
        }

        public async Task DeleteRangeProximityVaultsAsync(List<WorkstationProximityVault> proximityVaults)
        {
            if (proximityVaults == null)
            {
                throw new ArgumentNullException(nameof(proximityVaults));
            }

            await _workstationProximityVaultRepository.DeleteRangeAsync(proximityVaults);
        }

        public async Task DeleteProximityByVaultIdAsync(string vaultsId)
        {
            var allProximity = await _workstationProximityVaultRepository
             .Query()
             .Where(w => w.HardwareVaultId == vaultsId)
             .ToListAsync();

            await _workstationProximityVaultRepository.DeleteRangeAsync(allProximity);
        }

        public async Task<IReadOnlyList<DeviceProximitySettingsDto>> GetProximitySettingsAsync(string workstationId)
        {
            var workstation = await GetWorkstationByIdAsync(workstationId);

            if (workstation == null)
            {
                throw new Exception("Workstation not found");
            }

            var deviceProximitySettings = new List<DeviceProximitySettingsDto>();

            var proximityDevices = await _workstationProximityVaultRepository
                .Query()
                .Include(d => d.HardwareVault)
                .Where(d => d.WorkstationId == workstationId)
                .AsNoTracking()
                .ToListAsync();

            if (workstation.Approved)
            {
                foreach (var proximity in proximityDevices)
                {
                    deviceProximitySettings.Add(new DeviceProximitySettingsDto()
                    {
                        SerialNo = proximity.HardwareVaultId,
                        Mac = proximity.HardwareVault.MAC,
                        LockProximity = proximity.LockProximity,
                        UnlockProximity = proximity.UnlockProximity,
                        LockTimeout = proximity.LockTimeout,
                    });
                }
            }

            return deviceProximitySettings;
        }

        #endregion
    }
}