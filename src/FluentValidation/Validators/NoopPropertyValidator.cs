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

namespace FluentValidation.Validators {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Threading;
	using System.Threading.Tasks;
	using FluentValidation.Internal;
	using Resources;
	using Results;

	[Obsolete("NoOpPropertyValidator is deprecated and will be removed in FluentValidation 9.0. Please inherit from ValidatorBase instead. For information on upgrading to FluentValidation 8, please see https://fluentvalidation.net/upgrading-to-fluentvalidation-8")]
	public abstract class NoopPropertyValidator : IPropertyValidator, IValidationWorker {
		public IStringSource ErrorMessageSource {
			get { return null; }
			set { }
		}

		public IStringSource ErrorCodeSource {
			get { return null; }
			set { }
		}

		public abstract IEnumerable<ValidationFailure> Validate(PropertyValidatorContext context);

#pragma warning disable 1998
		public virtual async Task<IEnumerable<ValidationFailure>> ValidateAsync(PropertyValidatorContext context, CancellationToken cancellation) {
			return Validate(context);
		}
#pragma warning restore 1998

		public virtual ICollection<Func<object, object, object>> CustomMessageFormatArguments {
			get { return new List<Func<object, object, object>>(); }
		}

		public Func<PropertyValidatorContext, object> CustomStateProvider {
			get { return null; }
			set { }
		}

		public Severity Severity { get; set; }

		public virtual bool Validate(IValidationContext context) {
			var result = Validate((PropertyValidatorContext)context).ToList();
			result.ForEach(context.AddFailure);
			return !result.Any();
		}

		public virtual async Task<bool> ValidateAsync(IValidationContext context, CancellationToken cancellationToken) {
			var result = (await ValidateAsync((PropertyValidatorContext) context, cancellationToken)).ToList();
			result.ForEach(context.AddFailure);
			return !result.Any();
		}

		public virtual bool ShouldValidateAsync(IValidationContext context) {
			return false;
		}
	}
}