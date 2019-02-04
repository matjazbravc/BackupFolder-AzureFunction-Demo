using System.Net.Http.Formatting;
using Autofac;
using Autofac.Core;
using BackupFolderAzureDurableFunctionDemo.Services.Formatters;
using BackupFolderAzureDurableFunctionDemo.Services.Helpers;
using BackupFolderAzureDurableFunctionDemo.Services.Ioc.Extensions;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Config;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.BlobStorage.Helpers;
using BackupFolderAzureDurableFunctionDemo.Services.Repositories.TableStorage.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace BackupFolderAzureDurableFunctionDemo.Services.Modules
{
    public class CommonCoreModule : Module
    {
        /// <inheritdoc />
        /// <summary>
        ///     Add registrations to the container builder.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        protected override void Load(ContainerBuilder builder)
        {
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = { new StringEnumConverter() },
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
            builder.RegisterAsSingleInstance<JsonSerializerSettings, JsonSerializerSettings>(_ => serializerSettings);

            // Register Formatters
            var jsonMediaTypeFormatter = new JsonMediaTypeFormatter
            {
                UseDataContractJsonSerializer = true
            };
            builder.RegisterAsSingleInstance<JsonMediaTypeFormatter, JsonMediaTypeFormatter>(_ => jsonMediaTypeFormatter);

            var yamlMediaTypeFormatter = new YamlMediaTypeFormatter();
            builder.RegisterAsSingleInstance<YamlMediaTypeFormatter, YamlMediaTypeFormatter>(_ => yamlMediaTypeFormatter);

            builder.RegisterType<BlobLeaseHelperConfig>().As<IRepositoryConfig>().AsSelf();
            builder.RegisterType<BlobLeaseHelper>().WithParameter(
                new ResolvedParameter(
                    (pi, ctx) => pi.ParameterType == typeof(IRepositoryConfig),
                    (pi, ctx) => ctx.Resolve<BlobLeaseHelperConfig>()));
            builder.RegisterType<BlobRequestOptionsHelper>();
            builder.RegisterType<CloudBlockBlobMd5Helper>();
            builder.RegisterType<ConfigHelper>();
            builder.RegisterType<HttpRequestMessageHelper>();
            builder.RegisterType<TableRequestOptionsHelper>();
            builder.RegisterType<ValidateStorage>();
        }
    }
}