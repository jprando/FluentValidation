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

		[Obsolete("Use Container instead.")]
		public object Instance => _innerContext.Instance;

		public MessageFormatter MessageFormatter => _innerContext.MessageFormatter;

		public object PropertyValue => _innerContext.PropertyValue;

		public string GetDefaultMessage() {
			// For backwards compatibility, only pass in the PropertyValidatorContext if the string source implements IContextAwareStringSource
			// otherwise fall back to old behaviour of passing the instance. 
			object stringSourceContext = ErrorSource is IContextAwareStringSource ? _innerContext: _innerContext.Instance;

			string error = MessageFormatter.BuildMessage(ErrorSource.GetString(stringSourceContext));
			return error;
		}

		public static implicit operator PropertyValidatorContext(MessageBuilderContext ctx) {
			return ctx._innerContext;
		}

		public Dictionary<string, object> RootContextData => _innerContext.RootContextData;
		public PropertyChain PropertyChain => _innerContext.PropertyChain;

		public object Container => _innerContext.Container;

		object IValidationContext.Model => _innerContext.PropertyValue;
		string IValidationContext.ModelName => _innerContext.ModelName;
		IValidatorSelector IValidationContext.Selector => _innerContext.Selector;
		bool IValidationContext.IsAsync => _innerContext.IsAsync;

		void IValidationContext.AddFailure(ValidationFailure failure) {
			_innerContext.AddFailure(failure);
		}
	}
}