﻿using HES.Core.Entities;
using HES.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HES.Web.Pages.Settings.LicenseOrders
{
    public class IndexModel : PageModel
    {
        private readonly ILicenseService _licenseService;
        private readonly ILogger<IndexModel> _logger;
        public IList<LicenseOrder> LicenseOrder { get; set; }

        public string OrderId { get; set; }

        [TempData]
        public string SuccessMessage { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }

        public IndexModel(ILicenseService licenseService, ILogger<IndexModel> logger)
        {
            _licenseService = licenseService;
            _logger = logger;
        }

        public async Task OnGetAsync()
        {
            LicenseOrder = await _licenseService.GetLicenseOrdersAsync();
        }
    }
}