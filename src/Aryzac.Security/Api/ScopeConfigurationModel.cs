using System;
using System.Collections.Generic;
using System.Linq;
using Intent.Metadata.Models;
using Intent.Modules.Common;
using Intent.RoslynWeaver.Attributes;

[assembly: DefaultIntentManaged(Mode.Fully)]
[assembly: IntentTemplate("Intent.ModuleBuilder.Templates.Api.ApiElementModel", Version = "1.0")]

namespace Aryzac.Security.Api
{
    [IntentManaged(Mode.Fully, Signature = Mode.Fully)]
    public class ScopeConfigurationModel : IMetadataModel, IHasStereotypes, IHasName, IElementWrapper
    {
        public const string SpecializationType = "Scope Configuration";
        public const string SpecializationTypeId = "9e98940e-f2f5-4431-b8b5-e4c547eefe78";
        protected readonly IElement _element;

        [IntentManaged(Mode.Fully)]
        public ScopeConfigurationModel(IElement element, string requiredType = SpecializationTypeId)
        {
            if (!requiredType.Equals(element.SpecializationType, StringComparison.InvariantCultureIgnoreCase) && !requiredType.Equals(element.SpecializationTypeId, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new Exception($"Cannot create a '{GetType().Name}' from element with specialization type '{element.SpecializationType}'. Must be of type '{SpecializationType}'");
            }
            _element = element;
        }

        public string Id => _element.Id;

        public string Name => _element.Name;

        public string Comment => _element.Comment;

        public IEnumerable<IStereotype> Stereotypes => _element.Stereotypes;

        public IElement InternalElement => _element;

        public IList<ScopeModel> Scopes => _element.ChildElements
            .GetElementsOfType(ScopeModel.SpecializationTypeId)
            .Select(x => new ScopeModel(x))
            .ToList();

        public IList<ScopeVerbModel> Verbs => _element.ChildElements
            .GetElementsOfType(ScopeVerbModel.SpecializationTypeId)
            .Select(x => new ScopeVerbModel(x))
            .ToList();

        public override string ToString()
        {
            return _element.ToString();
        }

        public bool Equals(ScopeConfigurationModel other)
        {
            return Equals(_element, other?._element);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ScopeConfigurationModel)obj);
        }

        public override int GetHashCode()
        {
            return (_element != null ? _element.GetHashCode() : 0);
        }
    }

    [IntentManaged(Mode.Fully)]
    public static class ScopeConfigurationModelExtensions
    {

        public static bool IsScopeConfigurationModel(this ICanBeReferencedType type)
        {
            return type != null && type is IElement element && element.SpecializationTypeId == ScopeConfigurationModel.SpecializationTypeId;
        }

        public static ScopeConfigurationModel AsScopeConfigurationModel(this ICanBeReferencedType type)
        {
            return type.IsScopeConfigurationModel() ? new ScopeConfigurationModel((IElement)type) : null;
        }
    }
}