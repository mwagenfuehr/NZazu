using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NZazu.Contracts.Checks;

namespace NZazu.Fields
{
    abstract class NZazuField : INZazuField
    {
        private readonly Lazy<Control> _labelControl;
        private readonly Lazy<Control> _valueControl;

        protected NZazuField(string key)
        {
            if (String.IsNullOrWhiteSpace(key)) throw new ArgumentException("key");
            Key = key;

            _labelControl = new Lazy<Control>(GetLabelControl);
            _valueControl = new Lazy<Control>(GetValueControl);
        }

        public abstract string StringValue { get; set; }
        protected abstract internal DependencyProperty ContentProperty { get; } // 'internal' required for testing

        public abstract string Type { get; }
        public string Key { get; private set; }
        public string Prompt { get; protected internal set; }
        public string Hint { get; protected internal set; }
        public string Description { get; protected internal set; }

        public Control LabelControl { get { return _labelControl.Value; } }
        public Control ValueControl { get { return _valueControl.Value; } }

        public void Validate()
        {
            var safeChecks = Checks == null ? new IValueCheck[] { } : Checks.ToArray();
            new AggregateCheck(safeChecks).Validate(StringValue, CultureInfo.CurrentUICulture);
        }

        protected internal IEnumerable<IValueCheck> Checks { get; set; } // 'internal' required for testing

        protected virtual Control GetLabel() { return !String.IsNullOrWhiteSpace(Prompt) ? new Label { Content = Prompt } : null; }
        protected abstract Control GetValue();

        private Control GetLabelControl()
        {
            return GetLabel();
        }

        private Control GetValueControl()
        {
            var control = GetValue();
            return DecorateValidation(control);
        }

        private Control DecorateValidation(Control control)
        {
            if (control == null) return null;
            if (ContentProperty == null) return control; // because no validation if no content!

            if (control.GetBindingExpression(ContentProperty) != null) throw new InvalidOperationException("binding already applied.");
            var binding = new Binding("Value")
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                ValidatesOnDataErrors = true,
                ValidatesOnExceptions = true,
                NotifyOnValidationError = true,
                NotifyOnTargetUpdated = true,
                NotifyOnSourceUpdated = true,
                IsAsync = false,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            };

            control.SetBinding(ContentProperty, binding);

            if (Checks == null || !Checks.Any()) return control; // no checks, no validation required. saves performance

            var safeChecks = Checks == null ? new IValueCheck[] { } : Checks.ToArray();
            var aggregateCheck = new AggregateCheck(safeChecks);
            binding.ValidationRules.Clear();
            binding.ValidationRules.Add(new CheckValidationRule(aggregateCheck) { ValidatesOnTargetUpdated = true });

            return control;
        }
    }

    abstract class NZazuField<T> : NZazuField, INZazuField<T>, INotifyPropertyChanged
    {
        private T _value;

        protected NZazuField(string key) : base(key)
        {
        }

        public T Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged();
                // ReSharper disable once ExplicitCallerInfoArgument
                OnPropertyChanged("StringValue");
            }
        }

        public override string StringValue
        {
            get { return GetStringValue(); }
            set { SetStringValue(value); }
        }

        protected abstract void SetStringValue(string value);
        protected abstract string GetStringValue();

        public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };

        // ReSharper disable once MemberCanBePrivate.Global
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}