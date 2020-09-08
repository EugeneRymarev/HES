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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HES.Web.Pages.Employees
{
    public partial class EditEmployee : ComponentBase, IDisposable
    {
        [Inject] public IEmployeeService EmployeeService { get; set; }
        [Inject] public IMemoryCache MemoryCache { get; set; }
        [Inject] public IModalDialogService ModalDialogService { get; set; }
        [Inject] public IOrgStructureService OrgStructureService { get; set; }
        [Inject] public IToastService ToastService { get; set; }
        [Inject] public ILogger<EditEmployee> Logger { get; set; }
        [Inject] public IHubContext<RefreshHub> HubContext { get; set; }
        [Parameter] public string EmployeeId { get; set; }
        [Parameter] public string ConnectionId { get; set; }

        public Employee Employee { get; set; }
        public ValidationErrorMessage ValidationErrorMessage { get; set; }
        public List<Company> Companies { get; set; }
        public List<Department> Departments { get; set; }
        public List<Position> Positions { get; set; }
        public bool Initialized { get; set; }
        public bool EntityBeingEdited { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                ModalDialogService.OnCancel += ModalDialogService_OnCancel;

                Employee = await EmployeeService.GetEmployeeByIdAsync(EmployeeId);
                if (Employee == null)
                    throw new Exception("Employee not found.");

                EntityBeingEdited = MemoryCache.TryGetValue(Employee.Id, out object _);
                if (!EntityBeingEdited)
                    MemoryCache.Set(Employee.Id, Employee);

                Companies = await OrgStructureService.GetCompaniesAsync();

                if (Employee.DepartmentId == null)
                {
                    Departments = new List<Department>();
                }
                else
                {
                    Departments = await OrgStructureService.GetDepartmentsByCompanyIdAsync(Employee.Department.CompanyId);
                }

                Positions = await OrgStructureService.GetPositionsAsync();

                Initialized = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                ToastService.ShowToast(ex.Message, ToastLevel.Error);
                await ModalDialogService.CancelAsync();
            }
        }

        public async Task OnCompanyChangeAsync(ChangeEventArgs args)
        {
            var companyId = (string)args.Value;

            if (companyId == string.Empty)
                Employee.DepartmentId = null;

            Departments = await OrgStructureService.GetDepartmentsByCompanyIdAsync(companyId);
            Employee.DepartmentId = Departments.FirstOrDefault()?.Id;
        }

        private async Task EditAsync()
        {
            try
            {
                await EmployeeService.EditEmployeeAsync(Employee);
                ToastService.ShowToast("Employee updated.", ToastLevel.Success);
                await HubContext.Clients.AllExcept(ConnectionId).SendAsync(RefreshPage.Employees, Employee.Id);
                await ModalDialogService.CloseAsync();
            }
            catch (AlreadyExistException ex)
            {
                ValidationErrorMessage.DisplayError(nameof(Employee.FirstName), ex.Message);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                ToastService.ShowToast(ex.Message, ToastLevel.Error);
                await ModalDialogService.CloseAsync();
            }
        }

        private async Task ModalDialogService_OnCancel()
        {
            await EmployeeService.UnchangedEmployeeAsync(Employee);
        }

        public void Dispose()
        {
            ModalDialogService.OnCancel -= ModalDialogService_OnCancel;

            if (!EntityBeingEdited)
                MemoryCache.Remove(Employee.Id);
        }
    }
}