using System;
using System.Collections.Generic;
using FluentMigrator;
using Nop.Core.Domain.ScheduleTasks;
using Nop.Core.Infrastructure;
using Nop.Data.Mapping;
using Nop.Data;
using Nop.Data.Migrations;
using Nop.Services.Localization;
using Nop.Web.Framework.Extensions;

namespace Nop.Plugin.Crossroad.Integration.Migrations
{
    [NopMigration("2022-09-24 11:12:00", "Nop.Plugin.Crossroad.Integration schema", MigrationProcessType.Installation)]
    public class SchemaMigration : MigrationBase
    {
        private readonly IMigrationManager _migrationManager;

        public SchemaMigration(IMigrationManager migrationManager)
        {
            _migrationManager = migrationManager;
        }

        /// <summary>
        /// Collect the UP migration expressions
        /// </summary>
        public override void Up()
        {
            if (!DataSettingsManager.IsDatabaseInstalled())
                return;

            ILocalizationService localizationService = EngineContext.Current.Resolve<ILocalizationService>();

            (int? languageId, _) = this.GetLanguageData();

            localizationService.AddOrUpdateLocaleResource(new Dictionary<string, string>
            {
                ["Admin.Configuration.Settings.Integration"] = "Integration",

                ["Admin.Configuration.Settings.Integration.MenuItem"] = "Integration settings",

                ["Admin.Configuration.Settings.Integration.OpenKM"] = "OpenKM",
                ["Admin.Configuration.Settings.Integration.OnixEdit"] = "OnixEdit",

                ["Admin.Configuration.Settings.Integration.Url"] = "Url",
                ["Admin.Configuration.Settings.Integration.Url.Hint"] = "Url for API endpoints",

                ["Admin.Configuration.Settings.Integration.Username"] = "Username",

                ["Admin.Configuration.Settings.Integration.Password"] = "Password",

                ["Admin.Catalog.Products.List.ShowProductsWithType"] = "Show products with different prices"
            }, languageId);

            var scheduleTaskTableName = NameCompatibilityManager.GetTableName(typeof(ScheduleTask));

            //add column if not exists
            if (!Schema.Table(scheduleTaskTableName)
                    .Column(nameof(ScheduleTask.LastEnabledUtc))
                    .Exists())
            {
                Alter.Table(scheduleTaskTableName)
                    .AddColumn(nameof(ScheduleTask.LastEnabledUtc))
                    .AsDateTime2()
                    .Nullable();
            }

            //schedule task
            Insert.IntoTable(scheduleTaskTableName).Row(new
            {
                Name = "Onix edit product update",
                Type = "Nop.Plugin.Crossroad.Integration.Services.Onix.OnixEditProductsUpdateTask",
                Enabled = true,
                LastEnabledUtc = DateTime.UtcNow,
                Seconds = 604800,
                StopOnError = false
            });
        }

        public override void Down() { }
    }
}
