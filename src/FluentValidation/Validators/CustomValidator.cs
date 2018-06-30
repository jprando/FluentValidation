namespace FluentValidation.Validators {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Internal;
	using Results;
	/// <summary>
	/// Custom validator that allows for manual/direct creation of ValidationFailure instances. 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class CustomValidator<T> : PropertyValidator, IShouldValidateAsync {
		private readonly Action<T, IValidationContext> _action;
		private Func<T, IValidationContext, CancellationToken, Task> _asyncAction;
		private readonly bool _isAsync;

		[Obsolete]
		public override bool IsAsync => _isAsync;

		/// <summary>
		/// Creates a new instance of the CustomValidator
		/// </summary>
		/// <param name="action"></param>
		public CustomValidator(Action<T, IValidationContext> action) : base(string.Empty) {
			_isAsync = false;
			_action = action;
			_asyncAction = (x, ctx, cancel) => TaskHelpers.RunSynchronously(() =>_action(x, ctx), cancel);
		}

		/// <summary>
		/// Creates a new isntance of the CutomValidator.
		/// </summary>
		/// <param name="asyncAction"></param>
		public CustomValidator(Func<T, IValidationContext, CancellationToken, Task> asyncAction) : base(string.Empty) {
			_isAsync = true;
			_asyncAction = asyncAction;
			_action = (x, ctx) => Task.Run(() => _asyncAction(x, ctx, new CancellationToken())).GetAwaiter().GetResult();
		}

		public override IEnumerable<ValidationFailure> Validate(PropertyValidatorContext context) {
			_action((T) context.PropertyValue, context);
			return context.ParentContext.Failures;
		}

		public override async Task<IEnumerable<ValidationFailure>> ValidateAsync(PropertyValidatorContext context, CancellationToken cancellation) {
			await _asyncAction((T)context.PropertyValue, context, cancellation);
			return context.ParentContext.Failures;
		}

		protected override bool IsValid(PropertyValidatorContext context) {
			throw new NotImplementedException();
		}

		public bool ShouldValidateAsync(ValidationContext context) {
			return _isAsync && context.IsAsync();
		}
	}
}