﻿using HES.Core.Entities;
using HES.Core.Interfaces;
using HES.Core.Models;
using HES.Core.Models.API;
using HES.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HES.Web.Controllers
{
    [Authorize(Roles = ApplicationRoles.AdminRole)]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class WorkstationsController : ControllerBase
    {
        private readonly IWorkstationService _workstationService;
        private readonly ILogger<WorkstationsController> _logger;

        public WorkstationsController(IWorkstationService workstationService, ILogger<WorkstationsController> logger)
        {
            _workstationService = workstationService;
            _logger = logger;
        }

        #region Workstation

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Workstation>>> GetWorkstations()
        {
            return await _workstationService.GetWorkstationsAsync();
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Workstation>>> GetFilteredWorkstations(WorkstationFilter workstationFilter)
        {
            return await _workstationService.GetFilteredWorkstationsAsync(workstationFilter);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Workstation>> GetWorkstationById(string id)
        {
            var workstation = await _workstationService.GetWorkstationByIdAsync(id);

            if (workstation == null)
            {
                return NotFound();
            }

            return workstation;
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditWorkstation(string id, UpdateWorkstationDto workstationDto)
        {
            if (id != workstationDto.Id)
            {
                return BadRequest();
            }

            try
            {
                var workstation = new Workstation()
                {
                    Id = workstationDto.Id,
                    DepartmentId = workstationDto.DepartmentId,
                    RFID = workstationDto.RfidEnabled
                };
                await _workstationService.EditWorkstationAsync(workstation);
                await _workstationService.UpdateRfidStateAsync(workstation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }

            return NoContent();
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ApproveWorkstation(string id, UpdateWorkstationDto workstationDto)
        {
            if (id != workstationDto.Id)
            {
                return BadRequest();
            }

            try
            {
                var workstation = new Workstation()
                {
                    Id = workstationDto.Id,
                    DepartmentId = workstationDto.DepartmentId,
                    RFID = workstationDto.RfidEnabled,
                    Approved = true
                };
                await _workstationService.ApproveWorkstationAsync(workstation);
                await _workstationService.UpdateRfidStateAsync(workstation.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }

            return NoContent();
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> UnapproveWorkstation(string id)
        {
            try
            {
                await _workstationService.UnapproveWorkstationAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }

            return NoContent();
        }

        #endregion

        #region Proximity Device

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProximityDevice>>> GetProximityDevices(string id)
        {
            return await _workstationService.GetProximityDevicesAsync(id);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> AddProximityDevice(AddProximityDeviceDto proximityDeviceDto)
        {
            try
            {
                await _workstationService.AddProximityDevicesAsync(proximityDeviceDto.WorkstationId, new string[] { proximityDeviceDto.DeviceId });
                await _workstationService.UpdateProximitySettingsAsync(proximityDeviceDto.WorkstationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }

            return NoContent();
        }

        [HttpPost("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteProximityDevice(string id)
        {
            try
            {
                await _workstationService.DeleteProximityDeviceAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return StatusCode(500, new { error = ex.Message });
            }

            return NoContent();
        }

        #endregion
    }
}