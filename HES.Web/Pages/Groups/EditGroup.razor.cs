﻿using HES.Core.Entities;
using HES.Core.Enums;
using HES.Core.Exceptions;
using HES.Core.Hubs;
using HES.Core.Interfaces;
using HES.Web.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace HES.Web.Pages.Groups
{
    public partial class EditGroup : ComponentBase, IDisposable
    {
        [Inject] public IGroupService GroupService { get; set; }
        [Inject] public ILogger<EditGroup> Logger { get; set; }
        [Inject] public IModalDialogService ModalDialogService { get; set; }
        [Inject] public IToastService ToastService { get; set; }
        [Inject] public IMemoryCache MemoryCache { get; set; }
        [Inject] public IHubContext<RefreshHub> HubContext { get; set; }
        [Parameter] public string ConnectionId { get; set; }
        [Parameter] public string GroupId { get; set; }

        public Group Group { get; set; }
        public ValidationErrorMessage ValidationErrorMessage { get; set; }
        public bool EntityBeingEdited { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                ModalDialogService.OnCancel += ModalDialogService_OnCancel;

                Group = await GroupService.GetGroupByIdAsync(GroupId);

                if (Group == null)
                    throw new Exception("Group not found");

                EntityBeingEdited = MemoryCache.TryGetValue(Group.Id, out object _);
                if (!EntityBeingEdited)
                    MemoryCache.Set(Group.Id, Group);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                ToastService.ShowToast(ex.Message, ToastLevel.Error);
                await ModalDialogService.CancelAsync();
            }
        }

        private async Task EditAsync()
        {
            try
            {
                await GroupService.EditGroupAsync(Group);
                ToastService.ShowToast("Group updated.", ToastLevel.Success);
                await HubContext.Clients.AllExcept(ConnectionId).SendAsync(RefreshPage.Groups, Group.Id);
                await ModalDialogService.CloseAsync();
            }
            catch (AlreadyExistException ex)
            {
                ValidationErrorMessage.DisplayError(nameof(Core.Entities.Group.Name), ex.Message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                ToastService.ShowToast(ex.Message, ToastLevel.Error);
                await ModalDialogService.CancelAsync();
            }
        }

        private async Task ModalDialogService_OnCancel()
        {
            await GroupService.UnchangedGroupAsync(Group);
        }

        public void Dispose()
        {
            ModalDialogService.OnCancel -= ModalDialogService_OnCancel;

            if (!EntityBeingEdited)
                MemoryCache.Remove(Group.Id);
        }
    }
}