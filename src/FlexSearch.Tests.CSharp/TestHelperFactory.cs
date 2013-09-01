using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlexSearch.Tests.CSharp
{
    using FlexSearch.Api.Types;
    using FlexSearch.Core;
    using FlexSearch.Core.Index;
    using FlexSearch.Validators;

    using ServiceStack.Common;
    using ServiceStack.OrmLite;

    public static class TestHelperFactory
    {
        public static readonly Interface.IFactoryCollection FactoryCollection;

        static TestHelperFactory()
        {
            var pluginContainer = Factories.PluginContainer(false).Value;
            FactoryCollection = new Factories.FactoryCollection(pluginContainer);
        }

        public static Interface.IIndexService GetDefaultIndexService()
        {
            var settingsBuilder = SettingsBuilder.SettingsBuilder(FactoryCollection, new IndexValidator(FactoryCollection, new IndexValidationParameters(true)));
            var searchService = new SearchDsl.SearchService(FactoryCollection.SearchQueryFactory.GetAllModules());
            var dbFactory = new OrmLiteConnectionFactory(Constants.ConfFolder.Value + "//conf.sqlite", SqliteDialect.Provider);
            dbFactory.OpenDbConnection().Run(db => db.CreateTable<Api.Types.Index>(true));
            Interface.IIndexService indexservice = new FlexIndexModule.IndexService(settingsBuilder, searchService, dbFactory.Open(), false);
            return indexservice;
        }

        /// <summary>
        /// Helper implemention of contact based index settings
        /// </summary>
        /// <returns></returns>
        public static Api.Types.Index GetBasicIndexSettingsForContact()
        {
            var index = new Api.Types.Index();
            index.IndexName = "contact";
            index.Online = true;
            index.Configuration.DirectoryType = DirectoryType.Ram;
            index.Fields = new FieldDictionary();

            index.Fields.Add("gender", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("title", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("givenname", new IndexFieldProperties { FieldType = FieldType.Text });
            index.Fields.Add("middleinitial", new IndexFieldProperties { FieldType = FieldType.Text });
            index.Fields.Add("surname", new IndexFieldProperties { FieldType = FieldType.Text });
            index.Fields.Add("streetaddress", new IndexFieldProperties { FieldType = FieldType.Text });

            index.Fields.Add("city", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("state", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("zipcode", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("country", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("countryfull", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("emailaddress", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("username", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("password", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("cctype", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("ccnumber", new IndexFieldProperties { FieldType = FieldType.ExactText });

            index.Fields.Add("cvv2", new IndexFieldProperties { FieldType = FieldType.Int });
            index.Fields.Add("nationalid", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("ups", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("company", new IndexFieldProperties { FieldType = FieldType.Text });
            index.Fields.Add("pounds", new IndexFieldProperties { FieldType = FieldType.Double });
            index.Fields.Add("centimeters", new IndexFieldProperties { FieldType = FieldType.Int });
            index.Fields.Add("guid", new IndexFieldProperties { FieldType = FieldType.ExactText });
            index.Fields.Add("latitude", new IndexFieldProperties { FieldType = FieldType.Double });
            index.Fields.Add("longitude", new IndexFieldProperties { FieldType = FieldType.Double });

            index.Fields.Add("importdate", new IndexFieldProperties { FieldType = FieldType.Date });
            index.Fields.Add("timestamp", new IndexFieldProperties { FieldType = FieldType.DateTime });

            // Computed fields
            index.Fields.Add("fullname", new IndexFieldProperties{FieldType = FieldType.Text, ScriptName = "fullname"});
            index.Scripts.Add("fullname", new ScriptProperties { ScriptOption = ScriptOption.SingleLine, ScriptType = ScriptType.ComputedField, ScriptSource = "fields[\"givenname\"] + \" \" + fields[\"surname\"]" });
            return index;
        }
    }
}
