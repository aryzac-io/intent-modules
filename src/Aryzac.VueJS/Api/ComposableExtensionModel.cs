using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Metadata.Models;
using Intent.Modules.Common;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Api.ApiElementExtensionModel", Version = "1.0")]

namespace Aryzac.VueJS.Api
{
    [IntentManaged(Mode.Fully, Signature = Mode.Fully)]
    public class ComposableExtensionModel : ComposableModel
    {
        [IntentManaged(Mode.Fully, Body = Mode.Ignore)]
        public ComposableExtensionModel(IElement element) : base(element)
        {
        }

        public IList<ComposableServiceModel> ComposableServices => _element.ChildElements
            .GetElementsOfType(ComposableServiceModel.SpecializationTypeId)
            .Select(x => new ComposableServiceModel(x))
            .ToList();

    }
}