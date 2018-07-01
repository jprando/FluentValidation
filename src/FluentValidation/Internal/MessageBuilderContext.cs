namespace FluentValidation.Internal {
	using System;
	using System.Collections.Generic;
	using Resources;
	using Results;
	using Validators;

	public class MessageBuilderContext : IValidationContext {
		private PropertyValidatorContext _innerContext;

		public MessageBuilderContext(PropertyValidatorContext innerContext, IStringSource errorSource, IPropertyValidator propertyValidator) {
			_innerContext = innerContext;
			ErrorSource = errorSource;
			PropertyValidator = propertyValidator;
		}

		public IPropertyValidator PropertyValidator { get; }

		public IStringSource ErrorSource { get; }

		public IValidationContext ParentContext => _innerContext.ParentContext;

		public PropertyRule Rule => _innerContext.Rule;

		public string PropertyName => _innerContext.PropertyName;

		public string DisplayName => _innerContext.DisplayName;

		public MessageFormatter MessageFormatter => _innerContext.MessageFormatter;

		public object PropertyValue => _innerContext.PropertyValue;

		public string GetDefaultMessage() {
			return MessageFormatter.BuildMessage(ErrorSource.GetString(_innerContext));
		}

		public static implicit operator PropertyValidatorContext(MessageBuilderContext ctx) {
			return ctx._innerContext;
		}

		public Dictionary<string, object> RootContextData => _innerContext.RootContextData;
		public PropertyChain PropertyChain => _innerContext.PropertyChain;

		public object InstanceToValidate => _innerContext.InstanceToValidate;

		IValidatorSelector IValidationContext.Selector => _innerContext.Selector;
		bool IValidationContext.IsAsync => _innerContext.IsAsync;

		void IValidationContext.AddFailure(ValidationFailure failure) {
			_innerContext.AddFailure(failure);
		}
	}
}