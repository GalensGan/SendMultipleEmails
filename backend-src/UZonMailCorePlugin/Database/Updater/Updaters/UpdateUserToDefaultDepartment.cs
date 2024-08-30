﻿using Microsoft.EntityFrameworkCore;
using UZonMail.Core.Utils.Database;
using UZonMail.DB.SQL;
using UZonMail.DB.SQL.Organization;

namespace UZonMail.Core.Database.Updater.Updaters
{
    public class UpdateUserToDefaultDepartment : IDataUpdater
    {
        public Version Version => new("1.0.2.0");

        public async Task Update(SqlContext db)
        {
            var defaultOrganization = await db.Departments.FirstOrDefaultAsync(x => x.Type == DepartmentType.Organization && !x.IsSystem);
            // 添加默认组织
            defaultOrganization ??= new Department
            {
                Name = Department.DefaultOrganizationName,
                Description = "默认组织",
                FullPath = Department.DefaultDepartmentName,
                IsSystem = false,
                IsHidden = false,
                Type = DepartmentType.Organization
            };

            // 添加默认部门
            var defaultDepartment = await db.Departments.FirstOrDefaultAsync(x => x.Type == DepartmentType.Department && !x.IsSystem);
            defaultDepartment ??= new Department
            {
                Name = Department.DefaultDepartmentName,
                Description = "默认部门",
                FullPath = $"{Department.DefaultOrganizationName}/{Department.DefaultDepartmentName}",
                ParentId = defaultOrganization.Id,
                IsSystem = false,
                IsHidden = false,
                Type = DepartmentType.Department
            };

            // 将所有用户切换到默认的部门
            await db.Users.UpdateAsync(x => true, x => x.SetProperty(y => y.OrganizationId, defaultOrganization.Id)
                           .SetProperty(y => y.DepartmentId, defaultDepartment.Id));
        }
    }
}