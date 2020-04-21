﻿using HES.Core.Enums;
using HES.Core.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HES.Core.Entities
{
    public class Device
    {
        [Display(Name = "ID")]
        [Key]
        public string Id { get; set; }

        [Required]
        public string MAC { get; set; }

        [Required]
        public string Model { get; set; }

        [Required]
        public string RFID { get; set; }

        public int Battery { get; set; }

        [Required]
        public string Firmware { get; set; }

        public DeviceState Status { get; set; } // Todo set to VaultStatus

        [Display(Name = "Status Reason")]
        public VaultStatusReason StatusReason { get; set; }

        [Display(Name = "Status Description")]
        public string StatusDescription { get; set; }

        [Display(Name = "Last Synced")]
        public DateTime? LastSynced { get; set; }

        public bool NeedSync { get; set; }

        public string EmployeeId { get; set; }

        [Required]
        public string AcceessProfileId { get; set; }

        public string MasterPassword { get; set; }

        public DateTime ImportedAt { get; set; }

        public bool HasNewLicense { get; set; }


        [Display(Name = "License Status")]
        public LicenseStatus LicenseStatus { get; set; }

        [Display(Name = "License End Date")]
        public DateTime? LicenseEndDate { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }

        [Display(Name = "Profile")]
        [ForeignKey("AcceessProfileId")]
        public DeviceAccessProfile DeviceAccessProfile { get; set; }

        [NotMapped]
        public bool IsOnline => RemoteDeviceConnectionsService.IsDeviceConnectedToHost(Id);
    }
}