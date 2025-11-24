using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Metadata.Models;
using Intent.Modelers.Services.CQRS.Api;
using Intent.Modules.Common;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Api.ApiElementModelExtensions", Version = "1.0")]

namespace Aryzac.GitHubIssues.Api
{
    public static class QueryModelStereotypeExtensions
    {
        public static BusinessRequirement GetBusinessRequirement(this QueryModel model)
        {
            var stereotype = model.GetStereotype(BusinessRequirement.DefinitionId);
            return stereotype != null ? new BusinessRequirement(stereotype) : null;
        }


        public static bool HasBusinessRequirement(this QueryModel model)
        {
            return model.HasStereotype(BusinessRequirement.DefinitionId);
        }

        public static bool TryGetBusinessRequirement(this QueryModel model, out BusinessRequirement stereotype)
        {
            if (!HasBusinessRequirement(model))
            {
                stereotype = null;
                return false;
            }

            stereotype = new BusinessRequirement(model.GetStereotype(BusinessRequirement.DefinitionId));
            return true;
        }

        public static GitHubIssueClosed GetGitHubIssueClosed(this QueryModel model)
        {
            var stereotype = model.GetStereotype(GitHubIssueClosed.DefinitionId);
            return stereotype != null ? new GitHubIssueClosed(stereotype) : null;
        }


        public static bool HasGitHubIssueClosed(this QueryModel model)
        {
            return model.HasStereotype(GitHubIssueClosed.DefinitionId);
        }

        public static bool TryGetGitHubIssueClosed(this QueryModel model, out GitHubIssueClosed stereotype)
        {
            if (!HasGitHubIssueClosed(model))
            {
                stereotype = null;
                return false;
            }

            stereotype = new GitHubIssueClosed(model.GetStereotype(GitHubIssueClosed.DefinitionId));
            return true;
        }

        public class BusinessRequirement
        {
            private IStereotype _stereotype;
            public const string DefinitionId = "b0d77ec8-1af5-44e9-97c4-c101cdc3c173";

            public BusinessRequirement(IStereotype stereotype)
            {
                _stereotype = stereotype;
            }

            public string Name => _stereotype.Name;

            public string Summary()
            {
                return _stereotype.GetProperty<string>("Summary");
            }

        }

        public class GitHubIssueClosed
        {
            private IStereotype _stereotype;
            public const string DefinitionId = "eeba2b8c-1455-43d9-9af1-302fa0d8f430";

            public GitHubIssueClosed(IStereotype stereotype)
            {
                _stereotype = stereotype;
            }

            public string Name => _stereotype.Name;

        }

    }
}