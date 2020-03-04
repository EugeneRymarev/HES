﻿using System.ComponentModel.DataAnnotations;

namespace HES.Core.Models.ActiveDirectory
{
    public class ActiveDirectoryLogin
    {
        [Required]
        public string Server { get; set; }
        [Required]
        [Display(Name = "User Name")]
        public string UserName { get; set; }
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}