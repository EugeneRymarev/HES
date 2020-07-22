﻿using HES.Core.Entities;
using HES.Core.Enums;
using HES.Core.Hubs;
using HES.Core.Interfaces;
using Hideez.SDK.Communication;
using Hideez.SDK.Communication.HES.DTO;
using Hideez.SDK.Communication.PasswordManager;
using Hideez.SDK.Communication.Remote;
using Hideez.SDK.Communication.Utils;
using Hideez.SDK.Communication.Workstation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HES.Core.Services
{
    public class RemoteWorkstationConnectionsService : IRemoteWorkstationConnectionsService
    {
        static readonly ConcurrentDictionary<string, IRemoteAppConnection> _workstationConnections
                    = new ConcurrentDictionary<string, IRemoteAppConnection>();

        static readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _devicesInProgress
            = new ConcurrentDictionary<string, TaskCompletionSource<bool>>();

        private readonly IServiceProvider _services;
        private readonly IRemoteTaskService _remoteTaskService;
        private readonly IRemoteDeviceConnectionsService _remoteDeviceConnectionsService;
        private readonly IEmployeeService _employeeService;
        private readonly IAccountService _accountService;
        private readonly IWorkstationService _workstationService;
        private readonly IHardwareVaultService _hardwareVaultService;
        private readonly IDataProtectionService _dataProtectionService;
        private readonly IWorkstationAuditService _workstationAuditService;
        private readonly ILogger<RemoteWorkstationConnectionsService> _logger;
        private readonly IHubContext<RefreshHub> _hubContext;

        public RemoteWorkstationConnectionsService(IServiceProvider services,
                      IRemoteTaskService remoteTaskService,
                      IRemoteDeviceConnectionsService remoteDeviceConnectionsService,
                      IEmployeeService employeeService,
                      IAccountService accountService,
                      IWorkstationService workstationService,
                      IHardwareVaultService hardwareVaultService,
                      IDataProtectionService dataProtectionService,
                      IWorkstationAuditService workstationAuditService,
                      ILogger<RemoteWorkstationConnectionsService> logger,
                      IHubContext<RefreshHub> hubContext)
        {
            _services = services;
            _remoteTaskService = remoteTaskService;
            _remoteDeviceConnectionsService = remoteDeviceConnectionsService;
            _employeeService = employeeService;
            _accountService = accountService;
            _workstationService = workstationService;
            _hardwareVaultService = hardwareVaultService;
            _dataProtectionService = dataProtectionService;
            _workstationAuditService = workstationAuditService;
            _logger = logger;
            _hubContext = hubContext;
        }

        #region Hardware Vault

        public void StartUpdateRemoteDevice(IList<string> vaultIds)
        {
            foreach (var vaultId in vaultIds)
            {
                StartUpdateRemoteDevice(vaultId);
            }
        }

        public void StartUpdateRemoteDevice(string vaultId)
        {
            if (vaultId == null)
                throw new ArgumentNullException(nameof(vaultId));

#pragma warning disable IDE0067 // Dispose objects before losing scope
            var scope = _services.CreateScope();
#pragma warning restore IDE0067 // Dispose objects before losing scope

            if (!RemoteDeviceConnectionsService.IsDeviceConnectedToHost(vaultId))
            {
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    var remoteWorkstationConnectionsService = scope.ServiceProvider.GetRequiredService<IRemoteWorkstationConnectionsService>();
                    await remoteWorkstationConnectionsService.UpdateRemoteDeviceAsync(vaultId, workstationId: null, primaryAccountOnly: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[{vaultId}] {ex.Message}");
                }
                finally
                {
                    scope.Dispose();
                }
            });
        }

        public async Task UpdateRemoteDeviceAsync(string vaultId, string workstationId, bool primaryAccountOnly)
        {
            if (vaultId == null)
                throw new ArgumentNullException(nameof(vaultId));

            var isNew = false;
            var tcs = _devicesInProgress.GetOrAdd(vaultId, (x) =>
            {
                isNew = true;
                return new TaskCompletionSource<bool>();
            });

            if (!isNew)
            {
                await tcs.Task;
                return;
            }

            try
            {
                await UpdateRemoteDevice(vaultId, workstationId, primaryAccountOnly);
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
                throw;
            }
            finally
            {
                _devicesInProgress.TryRemove(vaultId, out TaskCompletionSource<bool> _);
                await _hubContext.Clients.All.SendAsync(RefreshPage.HardwareVaultStateChanged);
            }
        }

        private async Task<bool> UpdateRemoteDevice(string vaultId, string workstationId, bool primaryAccountOnly)
        {
            var remoteDevice = await _remoteDeviceConnectionsService
               .ConnectDevice(vaultId, workstationId)
               .TimeoutAfter(30_000);

            await remoteDevice.RefreshDeviceInfo();

            if (remoteDevice == null)
                throw new HideezException(HideezErrorCode.HesFailedEstablishRemoteDeviceConnection);

            var vault = await _hardwareVaultService.GetVaultByIdAsync(vaultId);

            if (vault == null)
                throw new HideezException(HideezErrorCode.HesDeviceNotFound);

            await _hardwareVaultService.ChangeHardwareVaultStatusAsync(remoteDevice, vault);

            switch (vault.Status)
            {
                case VaultStatus.Ready:
                    throw new HideezException(HideezErrorCode.HesDeviceNotAssignedToAnyUser);
                case VaultStatus.Reserved:
                    await _remoteTaskService.LinkVaultAsync(remoteDevice, vault);
                    break;
                case VaultStatus.Active:
                    await CheckPassphraseAsync(remoteDevice, vault.Id);
                    await CheckTaskAsync(remoteDevice, vault.Id, primaryAccountOnly);
                    await _hubContext.Clients.All.SendAsync(RefreshPage.EmployeesDetailsVaultState, vault.EmployeeId);
                    break;
                case VaultStatus.Locked:
                    throw new HideezException(HideezErrorCode.HesDeviceLocked);
                case VaultStatus.Suspended:
                    await _remoteTaskService.SuspendVaultAsync(remoteDevice, vault);
                    break;
                case VaultStatus.Deactivated:
                    //await CheckIsNeedBackupAsync(remoteDevice, vault);
                    await _remoteTaskService.WipeVaultAsync(remoteDevice, vault);
                    throw new HideezException(HideezErrorCode.DeviceHasBeenWiped);
                case VaultStatus.Compromised:
                    throw new HideezException(HideezErrorCode.HesDeviceCompromised);
                default:
                    _logger.LogCritical($"Unhandled vault status ({vault.Status})");
                    throw new Exception("Unhandled vault status.");
            }

            return true;
        }

        private async Task CheckIsNeedBackupAsync(RemoteDevice remoteDevice, HardwareVault vault)
        {
            var serverAccounts = await _employeeService.GetAccountsByEmployeeIdAsync(vault.EmployeeId);
            if (serverAccounts.Count > 0)
            {
                var pm = new DevicePasswordManager(remoteDevice, null);
                await pm.Load();

                foreach (var vaultAccount in pm.Accounts)
                {
                    var account = serverAccounts.First(x => vaultAccount.StorageId.Equals(x.StorageId));
                    account.Password = _dataProtectionService.Encrypt(vaultAccount.Password);
                    account.OtpSecret = _dataProtectionService.Encrypt(vaultAccount.OtpSecret);
                }
                await _accountService.UpdateOnlyPropAsync(serverAccounts, new string[] { nameof(Account.Password), nameof(Account.OtpSecret) });
            }
        }

        private async Task CheckPassphraseAsync(RemoteDevice remoteDevice, string vaultId)
        {
            var vault = await _hardwareVaultService.VaultQuery().AsNoTracking().FirstOrDefaultAsync(d => d.Id == vaultId);

            if (vault == null)
                throw new Exception($"Vault {vaultId} not found. Contact your system administrator.");

            if (!remoteDevice.AccessLevel.IsLinkRequired && vault.MasterPassword == null)
                throw new Exception($"Vault {vaultId} is linked to another server. Contact your system administrator.");

            if (remoteDevice.AccessLevel.IsLinkRequired && vault.MasterPassword != null)
                throw new Exception($"Vault {vaultId} was wiped a non-current server. Contact your system administrator.");

            var key = ConvertUtils.HexStringToBytes(_dataProtectionService.Decrypt(vault.MasterPassword));

            await remoteDevice.CheckPassphrase(key);
        }

        private async Task CheckTaskAsync(RemoteDevice remoteDevice, string deviceId, bool primaryAccountOnly)
        {
            if (primaryAccountOnly)
            {
                await _remoteTaskService.ExecuteRemoteTasks(deviceId, remoteDevice, TaskOperation.Primary);
            }
            else
            {
                await _remoteTaskService.ExecuteRemoteTasks(deviceId, remoteDevice, TaskOperation.None);
            }
        }

        #endregion

        #region Workstation

        public async Task RegisterWorkstationInfoAsync(IRemoteAppConnection remoteAppConnection, WorkstationInfo workstationInfo)
        {
            if (workstationInfo == null)
                throw new ArgumentNullException(nameof(workstationInfo));

            _workstationConnections.AddOrUpdate(workstationInfo.Id, remoteAppConnection, (id, oldConnection) =>
            {
                return remoteAppConnection;
            });

            if (await _workstationService.ExistAsync(w => w.Id == workstationInfo.Id))
            {
                // Workstation exists, update information
                await _workstationService.UpdateWorkstationInfoAsync(workstationInfo);
            }
            else
            {
                // Workstation does not exist or name + domain was changed, create new
                await _workstationService.AddWorkstationAsync(workstationInfo);
                _logger.LogInformation($"New workstation {workstationInfo.MachineName} was added");
            }

            await UpdateProximitySettingsAsync(workstationInfo.Id, await _workstationService.GetProximitySettingsAsync(workstationInfo.Id));
            await UpdateRfidStateAsync(workstationInfo.Id, await _workstationService.GetRfidStateAsync(workstationInfo.Id));
        }

        public async Task OnAppHubDisconnectedAsync(string workstationId)
        {
            _workstationConnections.TryRemove(workstationId, out IRemoteAppConnection _);

            await _workstationAuditService.CloseSessionAsync(workstationId);
        }

        public static IRemoteAppConnection FindWorkstationConnection(string workstationId)
        {
            _workstationConnections.TryGetValue(workstationId, out IRemoteAppConnection workstation);
            return workstation;
        }

        public static bool IsWorkstationConnected(string workstationId)
        {
            return _workstationConnections.ContainsKey(workstationId);
        }

        public static int WorkstationsOnlineCount()
        {
            return _workstationConnections.Count;
        }

        public async Task UpdateProximitySettingsAsync(string workstationId, IReadOnlyList<DeviceProximitySettingsDto> proximitySettings)
        {
            var remoteAppConnection = FindWorkstationConnection(workstationId);
            if (remoteAppConnection == null)
                return;

            await remoteAppConnection.UpdateProximitySettings(proximitySettings);
        }

        public async Task UpdateRfidStateAsync(string workstationId, bool isEnabled)
        {
            var remoteAppConnection = FindWorkstationConnection(workstationId);
            if (remoteAppConnection == null)
                return;

            await remoteAppConnection.UpdateRFIDIndicatorState(isEnabled);
        }

        public async Task UpdateWorkstationApprovedAsync(string workstationId, bool isApproved)
        {
            var remoteAppConnection = FindWorkstationConnection(workstationId);
            if (remoteAppConnection == null)
                return;

            if (isApproved)
            {
                await remoteAppConnection.WorkstationApproved();
            }
            else
            {
                await remoteAppConnection.WorkstationUnapproved();
            }
        }

        #endregion
    }
}