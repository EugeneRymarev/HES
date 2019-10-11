﻿using HES.Core.Entities;
using HES.Core.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HES.Web.Pages.Dashboard
{
    public class DeviceTasksModel : PageModel
    {
        private readonly IDeviceTaskService _deviceTaskService;
        private readonly ILogger<DeviceTasksModel> _logger;

        public IList<DeviceTask> DeviceTasks { get; set; }


        public DeviceTasksModel(IDeviceTaskService deviceTaskService, ILogger<DeviceTasksModel> logger)
        {
            _deviceTaskService = deviceTaskService;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            DeviceTasks = await _deviceTaskService.Query().Include(d => d.DeviceAccount.Employee.Department.Company).ToListAsync();
        }
    }
}