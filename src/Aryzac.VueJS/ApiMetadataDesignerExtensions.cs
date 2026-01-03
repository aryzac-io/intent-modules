using Intent.Engine;
using Intent.Metadata.Models;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Api.ApiMetadataDesignerExtensions", Version = "1.0")]

namespace Aryzac.VueJS.Api
{
    public static class ApiMetadataDesignerExtensions
    {
        public const string VueJSDesignerId = "6c82e31e-f334-4277-96c3-16b2504f368e";

        public static IDesigner VueJS(this IMetadataManager metadataManager, IApplication application)
        {
            return metadataManager.VueJS(application.Id);
        }

        public static IDesigner VueJS(this IMetadataManager metadataManager, string applicationId)
        {
            return metadataManager.GetDesigner(applicationId, VueJSDesignerId);
        }
    }
}