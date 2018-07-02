#region License
// Copyright (c) Jeremy Skinner (http://www.jeremyskinner.co.uk)
// 
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
// 
// http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
// 
// The latest version of this file can be found at https://github.com/jeremyskinner/FluentValidation
#endregion

using System.Threading;

namespace FluentValidation.Validators {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using FluentValidation.Internal;
	using Resources;
	using Results;

	public abstract class DefaultPropertyValidator : IValidationWorker, IConfigurable<ValidatorMetadata, ValidatorMetadata> {
		
		public ValidatorMetadata Metadata { get; } = new ValidatorMetadata();

		bool IValidationWorker.Validate(IValidationContext context) {
			if (!(context is PropertyValidatorContext ctx)) {
				throw new ArgumentException("Must pass a PropertyValidatorContext to ValidateAsync", nameof(context));
			}
			Validate(ctx);
			return !ctx.HasFailures;
		}

		async Task<bool> IValidationWorker.ValidateAsync(IValidationContext context, CancellationToken cancellationToken) {
			if (!(context is PropertyValidatorContext ctx)) {
				throw new ArgumentException("Must pass a PropertyValidatorContext to ValidateAsync", nameof(context));
			}
			await ValidateAsync(ctx, cancellationToken);
			return !ctx.HasFailures;
		}

		
		protected DefaultPropertyValidator(IStringSource errorMessageSource) {
			if(errorMessageSource == null) errorMessageSource = new StaticStringSource("No default error message has been specified.");
			Metadata.ErrorMessageSource = errorMessageSource;
		}

		protected DefaultPropertyValidator(string errorMessageResourceName, Type errorMessageResourceType) {
			errorMessageResourceName.Guard("errorMessageResourceName must be specified.", nameof(errorMessageResourceName));
			errorMessageResourceType.Guard("errorMessageResourceType must be specified.", nameof(errorMessageResourceType));

			Metadata.ErrorMessageSource = new LocalizedStringSource(errorMessageResourceType, errorMessageResourceName);
		}

		protected DefaultPropertyValidator(string errorMessage) {
			Metadata.ErrorMessageSource = new StaticStringSource(errorMessage);
		}
		
		public virtual bool ShouldValidateAsync(IValidationContext context) {
			return false; // context.IsAsync; ????
		}

		public virtual void Validate(PropertyValidatorContext context) {
		}
		
#pragma warning disable 1998 
		public virtual async Task ValidateAsync(PropertyValidatorContext context, CancellationToken cancellationToken) {
			Validate(context);
		}
#pragma warning restore 1998
		ValidatorMetadata IConfigurable<ValidatorMetadata, ValidatorMetadata>.Configure(Action<ValidatorMetadata> configurator) {
			configurator.Guard("Configuration action cannot be null.", nameof(configurator));
			configurator(Metadata);
			return Metadata;
		}
	} 

	[Obsolete("PropertyValidator is deprecated. For custom validators, you should either call RuleFor(..).Custom(expression) or inherit from DefaultPropertyValidator. For more information on upgrading, see https://fluentvalidation.net/upgrading-to-fluentvalidation-8")]
	public abstract class PropertyValidator : IPropertyValidator, IValidationWorker, IConfigurable<ValidatorMetadata, ValidatorMetadata> {
		private ValidatorMetadata _metadata = new ValidatorMetadata();

		public Func<PropertyValidatorContext, object> CustomStateProvider {
			get => _metadata.CustomStateProvider;
			set => _metadata.CustomStateProvider = value;
		}

		public Severity Severity {
			get => _metadata.Severity;
			set => _metadata.Severity = value;
		}

		protected PropertyValidator(IStringSource errorMessageSource) {
			if(errorMessageSource == null) errorMessageSource = new StaticStringSource("No default error message has been specified.");
			_metadata.ErrorMessageSource = errorMessageSource;
		}

		protected PropertyValidator(string errorMessageResourceName, Type errorMessageResourceType) {
			errorMessageResourceName.Guard("errorMessageResourceName must be specified.", nameof(errorMessageResourceName));
			errorMessageResourceType.Guard("errorMessageResourceType must be specified.", nameof(errorMessageResourceType));

			_metadata.ErrorMessageSource = new LocalizedStringSource(errorMessageResourceType, errorMessageResourceName);
		}

		protected PropertyValidator(string errorMessage) {
			_metadata.ErrorMessageSource = new StaticStringSource(errorMessage);
		}

		public IStringSource ErrorMessageSource {
			get { return _metadata.ErrorMessageSource; }
			set {
				if (value == null) {
					throw new ArgumentNullException("value");
				}

				_metadata.ErrorMessageSource = value;
			}
		}

		public virtual IEnumerable<ValidationFailure> Validate(PropertyValidatorContext context) {
			if (!IsValid(context)) {
				PrepareMessageFormatterForValidationError(context);
				return new[] { CreateValidationError(context) };
			}

			return Enumerable.Empty<ValidationFailure>();
		}

		public virtual Task<IEnumerable<ValidationFailure>> ValidateAsync(PropertyValidatorContext context, CancellationToken cancellation) {
			return
				IsValidAsync(context, cancellation)
				.Then(valid => {
					    if (valid) {
						    return Enumerable.Empty<ValidationFailure>();
					    }

						PrepareMessageFormatterForValidationError(context);
						return new[] { CreateValidationError(context) }.AsEnumerable();
				      },
					runSynchronously: true
				);
		}

		protected abstract bool IsValid(PropertyValidatorContext context);

#pragma warning disable 1998
		protected virtual async Task<bool> IsValidAsync(PropertyValidatorContext context, CancellationToken cancellation) {
			return IsValid(context);
		}
#pragma warning restore 1998

		/// <summary>
		/// Prepares the <see cref="MessageFormatter"/> of <paramref name="context"/> for an upcoming <see cref="ValidationFailure"/>.
		/// </summary>
		/// <param name="context">The validator context</param>
		protected virtual void PrepareMessageFormatterForValidationError(PropertyValidatorContext context) {
			context.MessageFormatter.AppendPropertyName(context.DisplayName);
			context.MessageFormatter.AppendPropertyValue(context.PropertyValue);
		}

		/// <summary>
		/// Creates an error validation result for this validator.
		/// </summary>
		/// <param name="context">The validator context</param>
		/// <returns>Returns an error validation result.</returns>
		protected virtual ValidationFailure CreateValidationError(PropertyValidatorContext context) {
			var messageBuilderContext = new MessageBuilderContext(context, _metadata.ErrorMessageSource, this);

			var error = context.Rule.MessageBuilder != null 
				? context.Rule.MessageBuilder(messageBuilderContext) 
				: messageBuilderContext.GetDefaultMessage();

			var failure = new ValidationFailure(context.PropertyName, error, context.PropertyValue);
			failure.FormattedMessageArguments = context.MessageFormatter.AdditionalArguments;
			failure.FormattedMessagePlaceholderValues = context.MessageFormatter.PlaceholderValues;
			failure.ResourceName = _metadata.ErrorMessageSource.ResourceName;
			failure.ErrorCode = (_metadata.ErrorCodeSource != null)
				? _metadata.ErrorCodeSource.GetString(context)
				: ValidatorOptions.ErrorCodeResolver(this);

			if (CustomStateProvider != null) {
				failure.CustomState = CustomStateProvider(context);
			}

			failure.Severity = Severity;
			return failure;
		}


		public IStringSource ErrorCodeSource {
			get => _metadata.ErrorCodeSource;
			set => _metadata.ErrorCodeSource = value;
		}

		bool IValidationWorker.Validate(IValidationContext context) {
			var failures = Validate((PropertyValidatorContext) context).ToList();
			failures.ForEach(context.AddFailure);
			return !failures.Any();
		}

		async Task<bool> IValidationWorker.ValidateAsync(IValidationContext context, CancellationToken cancellationToken) {
			var failures = (await ValidateAsync((PropertyValidatorContext) context, cancellationToken)).ToList();
			failures.ForEach(context.AddFailure);
			return !failures.Any();
		}

		public virtual bool ShouldValidateAsync(IValidationContext context) {
			return false;
		}
		
		ValidatorMetadata IConfigurable<ValidatorMetadata, ValidatorMetadata>.Configure(Action<ValidatorMetadata> configurator) {
			configurator.Guard("Configuration action cannot be null.", nameof(configurator));
			configurator(_metadata);
			return _metadata;
		}

	}
}