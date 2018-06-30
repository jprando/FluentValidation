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
// The latest version of this file can be found at https://github.com/JeremySkinner/FluentValidation
#endregion

namespace FluentValidation {
	using System;
	using System.Collections.Generic;
	using Validators;

	/// <summary>
	/// Extension methods for collection validation rules 
	/// </summary>
	public static class CollectionValidatorExtensions {
		/// <summary>
		/// Associates an instance of IValidator with the current property rule and is used to validate each item within the collection.
		/// </summary>
		/// <param name="ruleBuilder">Rule builder</param>
		/// <param name="validator">The validator to use</param>
		[Obsolete("SetcollectionValidator is deprecated and will be removed in FluentValidation 9.0. Please inherit use RuleForEach(..).SetValidator() instead. For information on upgrading to FluentValidation 8, please see https://fluentvalidation.net/upgrading-to-fluentvalidation-8")]
		public static IRuleBuilderOptions<T, IEnumerable<TCollectionElement>> SetCollectionValidator<T, TCollectionElement>(this IRuleBuilder<T, IEnumerable<TCollectionElement>> ruleBuilder, IValidator<TCollectionElement> validator) {
			// Just delegate to the RuleForEach implementation to do the work.
			return ruleBuilder.SetValidator(new ValidationWorkerWrapper(x => validator));
		}

		/// <summary>
		/// Uses a provider to instantiate a validator instance to be associated with a collection
		/// </summary>
		/// <param name="ruleBuilder"></param>
		/// <param name="validator"></param>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TCollectionElement"></typeparam>
		/// <typeparam name="TValidator"></typeparam>
		/// <returns></returns>
		[Obsolete("SetcollectionValidator is deprecated and will be removed in FluentValidation 9.0. Please inherit use RuleForEach(..).SetValidator() instead. For information on upgrading to FluentValidation 8, please see https://fluentvalidation.net/upgrading-to-fluentvalidation-8")]
		public static IRuleBuilderOptions<T, IEnumerable<TCollectionElement>> SetCollectionValidator<T, TCollectionElement, TValidator>(this IRuleBuilder<T, IEnumerable<TCollectionElement>> ruleBuilder, Func<T, TValidator> validator)
			where TValidator : IValidator<TCollectionElement> {
			// Just delegate to the RuleForEach implementation to do the work.
			return ruleBuilder.SetValidator(new ValidationWorkerWrapper(x => validator((T) x)));
		}
	}
}