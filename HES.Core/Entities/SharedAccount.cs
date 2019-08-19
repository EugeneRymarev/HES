﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HES.Core.Entities
{
    public class SharedAccount
    {
        [Key]
        public string Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string Urls { get; set; }
        public string Apps { get; set; }
        [Required]
        public string Login { get; set; }
        public string Password { get; set; }
        [Display(Name = "Password updated")]
        public DateTime? PasswordChangedAt { get; set; }
        public string OtpSecret { get; set; }
        [Display(Name = "OTP updated")]
        public DateTime? OtpSecretChangedAt { get; set; }
        public bool Deleted { get; set; }

        [NotMapped]
        public TimeSpan GetPasswordUpdated => (DateTime.UtcNow).Subtract((DateTime)PasswordChangedAt);
        [NotMapped]
        public TimeSpan GetOtpUpdated => (DateTime.UtcNow).Subtract(OtpSecretChangedAt ?? DateTime.UtcNow);
    }
}