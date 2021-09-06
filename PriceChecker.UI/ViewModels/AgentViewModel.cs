using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Genius.PriceChecker.Core.Models;
using Genius.Atom.UI.Forms.Attributes;
using Genius.Atom.UI.Forms.ViewModels;
using Genius.PriceChecker.UI.Validation;

namespace Genius.PriceChecker.UI.ViewModels
{
    public interface IAgentViewModel : IViewModel, IHasDirtyFlag, ISelectable
    {
        Agent GetOrCreateEntity();
        void ResetForm();

        Guid? Id { get; }
        string Key { get; }
    }

    internal sealed class AgentViewModel : ViewModelBase, IAgentViewModel
    {
        private readonly IAgentsViewModel _owner;
        private Agent _agent;

        public AgentViewModel(IAgentsViewModel owner, Agent agent)
        {
            _owner = owner;
            _agent = agent;

            ResetForm(true);
        }

        public Agent GetOrCreateEntity()
        {
            return _agent ??= new Agent {
                Id = Guid.NewGuid(),
                Key = Key,
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

            if (firstTimeInit)
                InitializeProperties(init);
            else
                init();

            void init()
            {
                Key = _agent?.Key;
                Url = _agent?.Url;
                PricePattern = _agent?.PricePattern;
                DecimalDelimiter = _agent?.DecimalDelimiter ?? '.';
            }
        }

        [Browsable(false)]
        public IEnumerable<string> UsedKeys => _owner.Agents.Select(x => x.Key);

        [Browsable(false)]
        public Guid? Id => _agent?.Id;

        public bool IsDirty
        {
            get => GetOrDefault(false);
            set => RaiseAndSetIfChanged(value);
        }

        [ValidationRule(typeof(ValueCannotBeEmptyValidationRule))]
        [ValidationRule(typeof(MustBeUniqueValidationRule), nameof(UsedKeys))]
        public string Key
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
