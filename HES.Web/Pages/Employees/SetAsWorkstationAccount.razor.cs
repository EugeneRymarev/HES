﻿using HES.Core.Entities;
using HES.Core.Enums;
using HES.Core.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HES.Web.Pages.Employees
{
    public partial class SetAsWorkstationAccount : ComponentBase
    {
        [Inject] public IEmployeeService EmployeeService { get; set; }
        [Inject] public IModalDialogService ModalDialogService { get; set; }
        [Inject] public IRemoteWorkstationConnectionsService RemoteWorkstationConnectionsService { get; set; }
        [Inject] public IToastService ToastService { get; set; }
        [Inject] public ILogger<DeleteAccount> Logger { get; set; }

        [Parameter] public EventCallback Refresh { get; set; }
        [Parameter] public Account Account { get; set; }

        private async Task SetAsWorkstationAccoountAsync()
        {
            try
            {
                await EmployeeService.SetAsWorkstationAccountAsync(Account.Employee.Id, Account.Id);
                var employee = await EmployeeService.GetEmployeeByIdAsync(Account.Employee.Id);
                RemoteWorkstationConnectionsService.StartUpdateRemoteDevice(await EmployeeService.GetEmployeeVaultIdsAsync(employee.Id));

                await ModalDialogService.CloseAsync();
                await Refresh.InvokeAsync(this);
                ToastService.ShowToast("Windows account changed successfully.", ToastLevel.Success);
            }
            catch (Exception ex)
            {
                await ModalDialogService.CloseAsync();
                Logger.LogError(ex.Message, ex);
                ToastService.ShowToast(ex.Message, ToastLevel.Error);
            }
        }

        private async Task CloseAsync()
        {
            await ModalDialogService.CloseAsync();
        }
    }
}
