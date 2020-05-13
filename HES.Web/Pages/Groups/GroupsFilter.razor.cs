﻿using HES.Core.Models.Web.Group;
using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace HES.Web.Pages.Groups
{
    public partial class GroupsFilter : ComponentBase
    {
        [Parameter] public Func<GroupFilter, Task> FilterChanged { get; set; }
        GroupFilter Filter { get; set; } = new GroupFilter();
        public bool Initialized { get; set; }

        protected override void OnInitialized()
        {
            Initialized = true;
        }

        private async Task FilterAsync()
        {
            await FilterChanged.Invoke(Filter);
        }

        private async Task ClearAsync()
        {
            Filter = new GroupFilter();
            await FilterChanged.Invoke(Filter);
        }
    }
}