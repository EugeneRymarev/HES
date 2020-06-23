﻿using HES.Core.Entities;
using HES.Core.Enums;
using HES.Core.Exceptions;
using HES.Core.Hubs;
using HES.Core.Interfaces;
using HES.Web.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HES.Web.Pages.SharedAccounts
{
    public partial class EditSharedAccount : ComponentBase
    {
        [Inject] public IRemoteWorkstationConnectionsService RemoteWorkstationConnectionsService { get; set; }
        [Inject] public ISharedAccountService SharedAccountService { get; set; }
        [Inject] public IModalDialogService ModalDialogService { get; set; }
        [Inject] public IToastService ToastService { get; set; }
        [Inject] public ILogger<EditSharedAccountOtp> Logger { get; set; }
        [Inject] public IHubContext<SharedAccountsHub> HubContext { get; set; }
        [Parameter] public string ConnectionId { get; set; }
        [Parameter] public SharedAccount Account { get; set; }

        public ValidationErrorMessage ValidationErrorMessage { get; set; }

        protected override void OnInitialized()
        {
            Account.ConfirmPassword = Account.Password;
            ModalDialogService.OnCancel += CancelAsync;
        }

        private async Task EditAccountAsync()
        {
            try
            {
                var vaults = await SharedAccountService.EditSharedAccountAsync(Account);
                RemoteWorkstationConnectionsService.StartUpdateRemoteDevice(vaults);
                ToastService.ShowToast("Shared account updated.", ToastLevel.Success);
                await HubContext.Clients.All.SendAsync("PageUpdated", ConnectionId);
                await ModalDialogService.CloseAsync();
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
                ToastService.ShowToast(ex.Message, ToastLevel.Error);
                await CancelAsync();
            }
        }

        private async Task CancelAsync()
        {
            await SharedAccountService.UnchangedAsync(Account);
            ModalDialogService.OnCancel -= CancelAsync;
        }
    }
}