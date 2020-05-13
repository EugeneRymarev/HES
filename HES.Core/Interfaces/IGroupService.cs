﻿using HES.Core.Entities;
using HES.Core.Models.Web.Group;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace HES.Core.Interfaces
{
    public interface IGroupService
    {
        IQueryable<Group> Query();
        Task<List<Group>> GetGroupsAsync(int skip, int take, string sortColumn, ListSortDirection sortDirection, string searchText, GroupFilter groupFilter);
        Task<int> GetGroupsCountAsync(string searchText, GroupFilter groupFilter);
        Task<Group> GetGroupByIdAsync(string groupId);
        Task<Group> GetGroupByNameAsync(Group group);
        Task<Group> CreateGroupAsync(Group group);
        Task CreateGroupRangeAsync(List<Group> groups);
        Task EditGroupAsync(Group group);
        Task UnchangedGroupAsync(Group group);
        Task<Group> DeleteGroupAsync(string groupId);
        Task<List<GroupMembership>> GetGruopMembersAsync(string groupId);
        Task<GroupMembership> GetGroupMembershipAsync(string employeeId, string groupId);
        Task<List<Employee>> GetEmployeesSkipExistingInGroupAsync(string groupId);
        Task AddEmployeesToGroupAsync(IList<string> employeeIds, string groupId);
        Task AddEmployeeToGroupsAsync(string employeeId, IList<string> groupIds);
        Task<GroupMembership> RemoveEmployeeFromGroupAsync(string groupMembershipId);
    }
}
