﻿using HES.Core.Entities;
using HES.Core.Enums;
using HES.Core.Exceptions;
using HES.Core.Interfaces;
using HES.Core.Models.Web.SharedAccounts;
using HES.Web.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HES.Web.Pages.SharedAccounts
{
    public partial class EditSharedAccount : HESComponentBase, IDisposable
    {
        public ISharedAccountService SharedAccountService { get; set; }
        public IRemoteDeviceConnectionsService RemoteDeviceConnectionsService { get; set; }
        [Inject] public IModalDialogService ModalDialogService { get; set; }
        [Inject] public IToastService ToastService { get; set; }
        [Inject] public IMemoryCache MemoryCache { get; set; }
        [Inject] public ILogger<EditSharedAccount> Logger { get; set; }
        [Parameter] public string ExceptPageId { get; set; }
        [Parameter] public string AccountId { get; set; }

        public SharedAccount SharedAccount { get; set; }
        public SharedAccountEditModel SharedAccountEditModel { get; set; }
        public ValidationErrorMessage ValidationErrorMessage { get; set; }
        public ButtonSpinner ButtonSpinner { get; set; }
        public bool EntityBeingEdited { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                SharedAccountService = ScopedServices.GetRequiredService<ISharedAccountService>();
                RemoteDeviceConnectionsService = ScopedServices.GetRequiredService<IRemoteDeviceConnectionsService>();

                ModalDialogService.OnCancel += OnCancelAsync;

                SharedAccount = await SharedAccountService.GetSharedAccountByIdAsync(AccountId);
                if (SharedAccount == null)
                    throw new Exception("Account not found");

                SharedAccountEditModel = new SharedAccountEditModel().Initialize(SharedAccount);

                EntityBeingEdited = MemoryCache.TryGetValue(SharedAccount.Id, out object _);
                if (!EntityBeingEdited)
                    MemoryCache.Set(SharedAccount.Id, SharedAccount);

                SetInitialized();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                await ToastService.ShowToastAsync(ex.Message, ToastType.Error);
                await ModalDialogService.CancelAsync();
            }
        }

        private async Task EditAccountAsync()
        {
            try
            {
                await ButtonSpinner.SpinAsync(async () =>
                {
                    var vaults = await SharedAccountService.EditSharedAccountAsync(SharedAccountEditModel);
                    RemoteDeviceConnectionsService.StartUpdateHardwareVaultAccounts(vaults);             
                    await SynchronizationService.UpdateSharedAccounts(ExceptPageId);
                    await ToastService.ShowToastAsync("Shared account updated.", ToastType.Success);
                    await ModalDialogService.CloseAsync();
                });
            }
            catch (AlreadyExistException ex)
            {
                ValidationErrorMessage.DisplayError(nameof(SharedAccount.Name), ex.Message);
            }
            catch (IncorrectUrlException ex)
            {
                ValidationErrorMessage.DisplayError(nameof(SharedAccount.Urls), ex.Message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message, ex);
                await ToastService.ShowToastAsync(ex.Message, ToastType.Error);
                await ModalDialogService.CancelAsync();
            }
        }

        private async Task OnCancelAsync()
        {
            await SharedAccountService.UnchangedAsync(SharedAccount);
        }

        public void Dispose()
        {
            ModalDialogService.OnCancel -= OnCancelAsync;

            if (!EntityBeingEdited)
                MemoryCache.Remove(SharedAccountEditModel.Id);
        }
    }
}