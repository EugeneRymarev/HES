﻿using HES.Core.Entities;
using HES.Core.Enums;
using HES.Core.Interfaces;
using HES.Web.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HES.Web.Pages.HardwareVaults
{
    public partial class ChangeStatus : HESComponentBase, IDisposable
    {
        public IHardwareVaultService HardwareVaultService { get; set; }
        public IRemoteDeviceConnectionsService RemoteDeviceConnectionsService { get; set; }
        [Inject] public IModalDialogService ModalDialogService { get; set; }
        [Inject] public IMemoryCache MemoryCache { get; set; }
        [Inject] public IToastService ToastService { get; set; }
        [Inject] public ILogger<ChangeStatus> Logger { get; set; }
        [Parameter] public string HardwareVaultId { get; set; }
        [Parameter] public VaultStatus VaultStatus { get; set; }
        [Parameter] public string ExceptPageId { get; set; }

        public HardwareVault HardwareVault { get; set; }
        public string StatusDescription { get; set; }
        public VaultStatusReason StatusReason { get; set; } = VaultStatusReason.Lost;
        public string CompromisedConfirmText { get; set; } = string.Empty;
        public bool CompromisedIsDisabled { get; set; } = true;
        public bool EntityBeingEdited { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                HardwareVaultService = ScopedServices.GetRequiredService<IHardwareVaultService>();
                RemoteDeviceConnectionsService = ScopedServices.GetRequiredService<IRemoteDeviceConnectionsService>();

                HardwareVault = await HardwareVaultService.GetVaultByIdAsync(HardwareVaultId);
                if (HardwareVault == null)
                    throw new Exception("HardwareVault not found.");

                EntityBeingEdited = MemoryCache.TryGetValue(HardwareVault.Id, out object _);
                if (!EntityBeingEdited)
                    MemoryCache.Set(HardwareVault.Id, HardwareVault);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                await ToastService.ShowToastAsync(ex.Message, ToastType.Error);
                await ModalDialogService.CancelAsync();
            }
        }

        private async Task ChangeStatusAsync()
        {
            try
            {
                switch (VaultStatus)
                {
                    case VaultStatus.Active:
                        await HardwareVaultService.ActivateVaultAsync(HardwareVault.Id);
                        await ToastService.ShowToastAsync("Vault pending activate.", ToastType.Success);
                        break;
                    case VaultStatus.Suspended:
                        await HardwareVaultService.SuspendVaultAsync(HardwareVault.Id, StatusDescription);
                        await ToastService.ShowToastAsync("Vault pending suspend.", ToastType.Success);
                        break;
                    case VaultStatus.Compromised:
                        if (CompromisedIsDisabled)
                            return;
                        await HardwareVaultService.VaultCompromisedAsync(HardwareVault.Id, StatusReason, StatusDescription);
                        await ToastService.ShowToastAsync("Vault compromised.", ToastType.Success);
                        break;
                }
       
                await SynchronizationService.UpdateHardwareVaults(ExceptPageId);
                RemoteDeviceConnectionsService.StartUpdateHardwareVaultStatus(HardwareVault.Id);
                await ModalDialogService.CloseAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                await ToastService.ShowToastAsync(ex.Message, ToastType.Error);
                await ModalDialogService.CancelAsync();
            }
        }

        private void CompromisedConfirm()
        {
            if (CompromisedConfirmText == HardwareVault.Id)
            {
                CompromisedIsDisabled = false;
            }
            else
            {
                CompromisedIsDisabled = true;
            }
        }

        public void Dispose()
        {
            if (!EntityBeingEdited)
                MemoryCache.Remove(HardwareVault.Id);
        }
    }
}