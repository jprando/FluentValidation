namespace FluentValidation {
	using System;
	using Internal;
	using Resources;
	using Validators;

	/// <summary>
	/// Validator metadata.
	/// </summary>
	public class ValidatorMetadata {
		private IStringSource _errorSource;
		private IStringSource _errorCodeSource;
		
		/// <summary>
		/// Function used to retrieve custom state for the validator
		/// </summary>
		public Func<PropertyValidatorContext, object> CustomStateProvider { get; set; }

		/// <summary>
		/// Severity of error.
		/// </summary>
		public Severity Severity { get; set; }
		
		/// <summary>
		/// Retrieves the unformatted error message template.
		/// </summary>
		public IStringSource ErrorMessageSource {
			get => _errorSource;
			set => _errorSource = value ?? throw new ArgumentNullException(nameof(value));
		}
		
		/// <summary>
		/// Retrieves the error code.
		/// </summary>
		public IStringSource ErrorCodeSource {
			get => _errorCodeSource;
			set => _errorCodeSource = value ?? throw new ArgumentNullException(nameof(value));
		}
	}
}