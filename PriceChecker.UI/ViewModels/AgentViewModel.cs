using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Genius.PriceChecker.Core.Models;
using Genius.PriceChecker.UI.Forms.Attributes;
using Genius.PriceChecker.UI.Forms.ViewModels;
using Genius.PriceChecker.UI.Validation;

namespace Genius.PriceChecker.UI.ViewModels
{
    public interface IAgentViewModel : IViewModel, IHasDirtyFlag, ISelectable
    {
        Agent CreateEntity();
        void ResetForm();

        string Id { get; }
    }

    internal sealed class AgentViewModel : ViewModelBase, IAgentViewModel
    {
        private readonly IAgentsViewModel _owner;
        private readonly Agent _agent;

        public AgentViewModel(IAgentsViewModel owner, Agent agent)
        {
            _owner = owner;
            _agent = agent;

            ResetForm(true);
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

        public void ResetForm()
        {
            ResetForm(false);
        }

        private void ResetForm(bool firstTimeInit)
        {
            if (_agent == null && !firstTimeInit)
            {
                return;
            }

            Action init = () => {
                Id = _agent?.Id;
                Url = _agent?.Url;
                PricePattern = _agent?.PricePattern;
                DecimalDelimiter = _agent?.DecimalDelimiter ?? '.';
            };

            if (firstTimeInit)
                this.InitializeProperties(init);
            else
                init();
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
