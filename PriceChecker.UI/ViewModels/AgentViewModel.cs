using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.UI.Forms.Attributes;
using Genius.PriceChecker.UI.Forms.ViewModels;
using Genius.PriceChecker.UI.Validation;

namespace Genius.PriceChecker.UI.ViewModels
{
    public class AgentViewModel : ViewModelBase<AgentViewModel>, IHasDirtyFlag, ISelectable
    {
        private readonly AgentsViewModel _owner;
        private readonly Agent _agent;

        public AgentViewModel(AgentsViewModel owner, Agent agent)
        {
            _owner = owner;
            _agent = agent;

            ResetForm(true);

            PropertiesAreInitialized = true;
        }

        public Agent CreateEntity()
        {
            return new Agent {
                Id = Id,
                Url = Url,
                PricePattern = PricePattern,
                DecimalDelimiter = DecimalDelimiter
            };
        }

        public void ResetForm(bool enforeInitialization = false)
        {
            if (_agent == null && !enforeInitialization)
            {
                return;
            }
            Id = _agent?.Id;
            Url = _agent?.Url;
            PricePattern = _agent?.PricePattern;
            DecimalDelimiter = _agent?.DecimalDelimiter ?? '.';
        }

        [Browsable(false)]
        public IEnumerable<string> UsedIds => _owner.Agents.Select(x => x.Id);

        public bool IsDirty
        {
            get => GetOrDefault(false);
            set => RaiseAndSetIfChanged(value);
        }

        [ValidationRule(typeof(ValueCannotBeEmptyValidationRule))]
        [ValidationRule(typeof(MustBeUniqueValidationRule), nameof(UsedIds))]
        public string Id
        {
            get => GetOrDefault<string>();
            set => RaiseAndSetIfChanged(value);
        }

        [ValidationRule(typeof(ValueCannotBeEmptyValidationRule))]
        public string Url
        {
            get => GetOrDefault<string>();
            set => RaiseAndSetIfChanged(value);
        }

        [ValidationRule(typeof(ValueCannotBeEmptyValidationRule))]
        public string PricePattern
        {
            get => GetOrDefault<string>();
            set => RaiseAndSetIfChanged(value);
        }

        public char DecimalDelimiter
        {
            get => GetOrDefault('.');
            set => RaiseAndSetIfChanged(value);
        }

        public bool IsSelected
        {
            get => GetOrDefault<bool>();
            set => RaiseAndSetIfChanged(value);
        }
    }
}
