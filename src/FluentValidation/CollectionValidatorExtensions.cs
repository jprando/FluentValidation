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
	using Internal;
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
		[Obsolete("SetcollectionValidator is deprecated and will be removed in FluentValidation 9.0. Please use RuleForEach(...).SetValidator() instead. For information on upgrading to FluentValidation 8, please see https://fluentvalidation.net/upgrading-to-fluentvalidation-8")]
		public static ICollectionValidatorRuleBuilder<T, TCollectionElement> SetCollectionValidator<T, TCollectionElement>(this IRuleBuilder<T, IEnumerable<TCollectionElement>> ruleBuilder, IValidator<TCollectionElement> validator) {
			var adaptor = new ChildCollectionValidatorAdaptor(validator);
			ruleBuilder.SetValidator(adaptor);
			IValidator<T> parentValidator = null;

			if (ruleBuilder is IExposesParentValidator<T> exposesParentValidator) {
				parentValidator = exposesParentValidator.ParentValidator;
			}

			return new CollectionValidatorRuleBuilder<T, TCollectionElement>(ruleBuilder, adaptor, parentValidator);
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
		[Obsolete("SetcollectionValidator is deprecated and will be removed in FluentValidation 9.0. Please use RuleForEach(...).SetValidator() instead. For information on upgrading to FluentValidation 8, please see https://fluentvalidation.net/upgrading-to-fluentvalidation-8")]
		public static ICollectionValidatorRuleBuilder<T, TCollectionElement> SetCollectionValidator<T, TCollectionElement, TValidator>(this IRuleBuilder<T, IEnumerable<TCollectionElement>> ruleBuilder, Func<T, TValidator> validator)
			where TValidator : IValidator<TCollectionElement> {
			var adaptor = new ChildCollectionValidatorAdaptor(parent => validator((T) parent), typeof(TValidator));
			ruleBuilder.SetValidator(adaptor);

			IValidator<T> parentValidator= null;

			if (ruleBuilder is IExposesParentValidator<T> exposesParentValidator) {
				parentValidator = exposesParentValidator.ParentValidator;
			}

			return new CollectionValidatorRuleBuilder<T, TCollectionElement>(ruleBuilder, adaptor, parentValidator);
		}

		/// <summary>
		/// Collection rule builder syntax
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TCollectionElement"></typeparam>
		public interface ICollectionValidatorRuleBuilder<T,TCollectionElement> : IRuleBuilderOptions<T, IEnumerable<TCollectionElement>> {
			/// <summary>
			/// Defines a condition to be used to determine if validation should run
			/// </summary>
			/// <param name="predicate"></param>
			/// <returns></returns>
			ICollectionValidatorRuleBuilder<T,TCollectionElement> Where(Func<TCollectionElement, bool> predicate);
		}

		[Obsolete]
		private class CollectionValidatorRuleBuilder<T,TCollectionElement> : ICollectionValidatorRuleBuilder<T,TCollectionElement>, IExposesParentValidator<T> {
			IRuleBuilder<T, IEnumerable<TCollectionElement>> ruleBuilder;
			ChildCollectionValidatorAdaptor adaptor;

			public CollectionValidatorRuleBuilder(IRuleBuilder<T, IEnumerable<TCollectionElement>> ruleBuilder, ChildCollectionValidatorAdaptor adaptor, IValidator<T> parent) {
				this.ruleBuilder = ruleBuilder;
				this.adaptor = adaptor;
				ParentValidator = parent;
			}

			public IRuleBuilderOptions<T, IEnumerable<TCollectionElement>> SetValidator(IValidationWorker validator) {
				return ruleBuilder.SetValidator(validator);
			}

			public IRuleBuilderOptions<T, IEnumerable<TCollectionElement>> SetValidator(IValidator<IEnumerable<TCollectionElement>> validator) {
				return ruleBuilder.SetValidator(validator);
			}

			public IRuleBuilderOptions<T, IEnumerable<TCollectionElement>> SetValidator<TValidator>(Func<T, TValidator> validatorProvider)
				where TValidator : IValidator<IEnumerable<TCollectionElement>> {
				return ruleBuilder.SetValidator(validatorProvider);
			}

			public IRuleBuilderOptions<T, IEnumerable<TCollectionElement>> Configure(Action<PropertyRule> configurator) {
				return ((IRuleBuilderOptions<T, IEnumerable<TCollectionElement>>)ruleBuilder).Configure(configurator);
			}

			public ICollectionValidatorRuleBuilder<T, TCollectionElement> Where(Func<TCollectionElement, bool> predicate) {
				predicate.Guard("Cannot pass null to Where.", nameof(predicate));
				adaptor.Predicate = x => predicate((TCollectionElement)x);
				return this;
			}

			public IValidator<T> ParentValidator { get; }
		}
	}
}

namespace FluentValidation.Validators {
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Threading;
	using System.Threading.Tasks;
	using Results;

	[Obsolete("Use of ChildCollcetionValidatorAdaptor is deprecated. Use RuleForEach(...).SetValidator() instead.")]
	public class ChildCollectionValidatorAdaptor : NoopPropertyValidator {
		static readonly IEnumerable<ValidationFailure> EmptyResult = Enumerable.Empty<ValidationFailure>();
		static readonly Task<IEnumerable<ValidationFailure>> AsyncEmptyResult = TaskHelpers.FromResult(Enumerable.Empty<ValidationFailure>());
		readonly Func<object, IValidator> _childValidatorProvider;

		public Type ChildValidatorType { get; }

		public Func<object, bool> Predicate { get; set; }

		public ChildCollectionValidatorAdaptor(IValidator childValidator) {
			_childValidatorProvider = _ => childValidator;
			ChildValidatorType = childValidator.GetType();
		}

		public ChildCollectionValidatorAdaptor(Func<object, IValidator> childValidatorProvider, Type childValidatorType) {
			_childValidatorProvider = childValidatorProvider;
			ChildValidatorType = childValidatorType;
		}

		public override IEnumerable<ValidationFailure> Validate(PropertyValidatorContext context) {
			return ValidateInternal(
				context,
				items => items.Select(item => {
					var (ctx, validator) = item;
					return validator.Validate(ctx).Errors;
				}).SelectMany(errors => errors),
				EmptyResult
			);
		}

		public override Task<IEnumerable<ValidationFailure>> ValidateAsync(PropertyValidatorContext context, CancellationToken cancellation) {
			return ValidateInternal(
				context,
				items => {
					var failures = new List<ValidationFailure>();
					var tasks = items.Select(item => {
						var (ctx, validator) = item;
						return validator.ValidateAsync(ctx, cancellation).Then(res => failures.AddRange(res.Errors), runSynchronously: true, cancellationToken: cancellation);
					});
					return TaskHelpers.Iterate(tasks, cancellation).Then(() => failures.AsEnumerable(), runSynchronously: true, cancellationToken: cancellation);
				},
				AsyncEmptyResult
			);
		}

		TResult ValidateInternal<TResult>(
			PropertyValidatorContext context,
			Func<IEnumerable<(ValidationContext ctx, IValidator validator)>, TResult> validatorApplicator,
			TResult emptyResult
		) {
			var collection = context.PropertyValue as IEnumerable;

			if (collection == null) {
				return emptyResult;
			}

			var predicate = Predicate ?? (x => true);

			string propertyName = context.Rule.PropertyName;

			if (string.IsNullOrEmpty(propertyName)) {
				propertyName = InferPropertyName(context.Rule.Expression);
			}

			var itemsToValidate = collection
				.Cast<object>()
				.Select((item, index) => new { item, index })
				.Where(a => a.item != null && predicate(a.item))
				.Select(a => {
					var newContext = ((ValidationContext)context.ParentContext).CloneForChildValidator(a.item);
					newContext.PropertyChain.Add(propertyName);
					newContext.PropertyChain.AddIndexer(a.item is IIndexedCollectionItem ? ((IIndexedCollectionItem)a.item).Index : a.index.ToString());

					var validator = _childValidatorProvider(context.InstanceToValidate);

					return (newContext, validator);
				});

			return validatorApplicator(itemsToValidate);
		}

		private string InferPropertyName(LambdaExpression expression) {
			var paramExp = expression.Body as ParameterExpression;

			if (paramExp == null) {
				throw new InvalidOperationException("Could not infer property name for expression: " + expression + ". Please explicitly specify a property name by calling OverridePropertyName as part of the rule chain. Eg: RuleFor(x => x).SetCollectionValidator(new MyFooValidator()).OverridePropertyName(\"MyProperty\")");
			}

			return paramExp.Name;
		}

		public override bool ShouldValidateAsync(IValidationContext context) {
			return context.IsAsync;
		}
	}
	
	[Obsolete]
	public interface IIndexedCollectionItem {
		string Index { get; }
	}
}