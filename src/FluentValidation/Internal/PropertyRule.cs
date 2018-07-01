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

namespace FluentValidation.Internal {
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Threading;
	using System.Threading.Tasks;
	using Resources;
	using Results;
	using Validators;

	/// <summary>
	/// Defines a rule associated with a property.
	/// </summary>
	public class PropertyRule : IValidationRule {
		readonly List<RuleElement> _validators = new List<RuleElement>();
		Func<CascadeMode> _cascadeModeThunk = () => ValidatorOptions.CascadeMode;
		string _propertyDisplayName;
		string _propertyName;
		private string[] _ruleSet = new string[0];
		private Func<IValidationWorker, RuleElement> _ruleElementFactory;

		/// <summary>
		/// Property associated with this rule.
		/// </summary>
		public MemberInfo Member { get; }

		/// <summary>
		/// Function that can be invoked to retrieve the value of the property.
		/// </summary>
		public Func<object, object> PropertyFunc { get; private set; }

		/// <summary>
		/// Expression that was used to create the rule.
		/// </summary>
		public LambdaExpression Expression { get; private set; }

		/// <summary>
		/// String source that can be used to retrieve the display name (if null, falls back to the property name)
		/// </summary>
		public IStringSource DisplayName { get; set; }

		/// <summary>
		/// Rule set that this rule belongs to (if specified)
		/// </summary>
		public string[] RuleSets {
			get => _ruleSet;
			set => _ruleSet = value ?? new string[0];
		}

		/// <summary>
		/// Function that will be invoked if any of the validators associated with this rule fail.
		/// </summary>
		public Action<object> OnFailure { get; set; }

		/// <summary>
		/// The current validator being configured by this rule.
		/// </summary>
		public RuleElement CurrentValidator { get; private set; }

		/// <summary>
		/// Type of the property being validated
		/// </summary>
		public Type TypeToValidate { get; private set; }

		/// <summary>
		/// Cascade mode for this rule.
		/// </summary>
		public CascadeMode CascadeMode {
			get { return _cascadeModeThunk(); }
			set { _cascadeModeThunk = () => value; }
		}

		/// <summary>
		/// Validators associated with this rule.
		/// </summary>
		public IEnumerable<RuleElement> Validators => _validators;

		/// <summary>
		/// Defines the factory used to create rule elements.
		/// </summary>
		public Func<IValidationWorker, RuleElement> RuleElementFactory {
			get => _ruleElementFactory;
			set => _ruleElementFactory = value ?? throw new ArgumentNullException("Cannot pass null to RuleElementFactory", nameof(value));
		}

		/// <summary>
		/// Creates a new property rule.
		/// </summary>
		/// <param name="member">Property</param>
		/// <param name="propertyFunc">Function to get the property value</param>
		/// <param name="expression">Lambda expression used to create the rule</param>
		/// <param name="cascadeModeThunk">Function to get the cascade mode.</param>
		/// <param name="typeToValidate">Type to validate</param>
		/// <param name="containerType">Container type that owns the property</param>
		public PropertyRule(MemberInfo member, Func<object, object> propertyFunc, LambdaExpression expression, Func<CascadeMode> cascadeModeThunk, Type typeToValidate, Type containerType) {
			Member = member;
			PropertyFunc = propertyFunc;
			Expression = expression;
			OnFailure = x => { };
			TypeToValidate = typeToValidate;
			_cascadeModeThunk = cascadeModeThunk;
			_ruleElementFactory = worker => new RuleElement(worker, new ValidatorMetadata(), this);

			var t = (m: Member, e: expression);
			
			DependentRules = new List<IValidationRule>();
			PropertyName = ValidatorOptions.PropertyNameResolver(containerType, member, expression);
			DisplayName = new LazyStringSource(x =>  ValidatorOptions.DisplayNameResolver(containerType, member, expression));
		}

		/// <summary>
		/// Creates a new property rule from a lambda expression.
		/// </summary>
		public static PropertyRule Create<T, TProperty>(Expression<Func<T, TProperty>> expression) {
			return Create(expression, () => ValidatorOptions.CascadeMode);
		}

		/// <summary>
		/// Creates a new property rule from a lambda expression.
		/// </summary>
		public static PropertyRule Create<T, TProperty>(Expression<Func<T, TProperty>> expression, Func<CascadeMode> cascadeModeThunk) {
			var member = expression.GetMember();
			var compiled = AccessorCache<T>.GetCachedAccessor(member, expression);
			return new PropertyRule(member, compiled.CoerceToNonGeneric(), expression, cascadeModeThunk, typeof(TProperty), typeof(T));
		}

		/// <summary>
		/// Creates a new property rule from a lambda expression for a child collection.
		/// </summary>
		public static PropertyRule CreateForCollection<T, TCollectionElement>(Expression<Func<T, IEnumerable<TCollectionElement>>> expression, Func<CascadeMode> cascadeModeThunk) {
			var member = expression.GetMember();
			var compiled = expression.Compile();
			var rule = new PropertyRule(member, compiled.CoerceToNonGeneric(), expression, cascadeModeThunk, typeof(TCollectionElement), typeof(T));
			// Override the rule element factory with a version that handles collection indicies etc.
			rule.RuleElementFactory = x => new CollectionRuleElement<TCollectionElement>(x, new ValidatorMetadata(), rule);
			return rule;
		}

		/// <summary>
		/// Adds a validator to the rule.
		/// </summary>
		public void AddValidator(IValidationWorker validator) {
			CurrentValidator = _ruleElementFactory(validator);
			_validators.Add(CurrentValidator);
		}

		/// <summary>
		/// Returns the property name for the property being validated.
		/// Returns null if it is not a property being validated (eg a method call)
		/// </summary>
		public string PropertyName {
			get { return _propertyName; }
			set {
				_propertyName = value;
				_propertyDisplayName = _propertyName.SplitPascalCase();
			}
		}

		/// <summary>
		/// Allows custom creation of an error message
		/// </summary>
		public Func<MessageBuilderContext, string> MessageBuilder { get; set; }

		/// <summary>
		/// Dependent rules
		/// </summary>
		public List<IValidationRule> DependentRules { get; private set; }

		public Func<object, object> Transformer { get; set; }

		/// <summary>
		/// Display name for the property. 
		/// </summary>
		public string GetDisplayName() {
			string result = null;

			if (DisplayName != null) {
				result = DisplayName.GetString(null /*We don't have a model object at this point*/);
			}

			if (result == null) {
				result = _propertyDisplayName;
			}

			return result;
		}

		/// <summary>
		/// Display name for the property. 
		/// </summary>
		public string GetDisplayName(object model) {
			string result = null;

			if (DisplayName != null) {
				result = DisplayName.GetString(model);
			}

			if (result == null) {
				result = _propertyDisplayName;
			}

			return result;
		}

		/// <summary>
		/// Performs validation using a validation context and returns a collection of Validation Failures.
		/// </summary>
		/// <param name="context">Validation Context</param>
		/// <returns>A collection of validation failures</returns>
		public virtual void Validate(IValidationContext context) {
			string displayName = GetDisplayName(context.Model);

			if (PropertyName == null && displayName == null) {
				//No name has been specified. Assume this is a model-level rule, so we should use empty string instead. 
				displayName = string.Empty;
			}

			// Construct the full name of the property, taking into account overriden property names and the chain (if we're in a nested validator)
			string propertyName = context.PropertyChain.BuildPropertyName(PropertyName ?? displayName);

			// Ensure that this rule is allowed to run. 
			// The validatselector has the opportunity to veto this before any of the validators execute.
			if (!context.Selector.CanExecute(this, propertyName, context)) {
				return;
			}

			var cascade = _cascadeModeThunk();
			bool hasAnyFailure = false;

			// Invoke each validator and collect its results.
			foreach (var validator in _validators) {
				var results = new List<ValidationFailure>();
				bool success;
				if (validator.ShouldValidateAsync(context))
					success = validator.ValidateAsync(context, propertyName, default(CancellationToken)).GetAwaiter().GetResult();
				else
					success = validator.Validate(context, propertyName);

				if (!success) {
					hasAnyFailure = true;
				}

				// If there has been at least one failure, and our CascadeMode has been set to StopOnFirst
				// then don't continue to the next rule
				if (cascade == FluentValidation.CascadeMode.StopOnFirstFailure && hasAnyFailure) {
					break;
				}
			}

			if (hasAnyFailure) {
				// Callback if there has been at least one property validator failed.
				OnFailure(context.Model);
			}
			else {
				foreach (var dependentRule in DependentRules) {
					dependentRule.Validate(context);
				}
			}
		}

		/// <summary>
		/// Performs asynchronous validation using a validation context and returns a collection of Validation Failures.
		/// </summary>
		/// <param name="context">Validation Context</param>
		/// <param name="cancellation"></param>
		/// <returns>A collection of validation failures</returns>
		public virtual Task ValidateAsync(IValidationContext context, CancellationToken cancellation) {
			try {
				if (!context.IsAsync) {
					context.RootContextData["__FV_IsAsyncExecution"] = true;
				}

				var displayName = GetDisplayName(context.Model);

				if (PropertyName == null && displayName == null)
				{
					//No name has been specified. Assume this is a model-level rule, so we should use empty string instead. 
					displayName = string.Empty;
				}

				// Construct the full name of the property, taking into account overriden property names and the chain (if we're in a nested validator)
				var propertyName = context.PropertyChain.BuildPropertyName(PropertyName ?? displayName);

				// Ensure that this rule is allowed to run. 
				// The validatselector has the opportunity to veto this before any of the validators execute.
				if (!context.Selector.CanExecute(this, propertyName, context)) {
					return TaskHelpers.Completed();
				}

				var cascade = _cascadeModeThunk();
				var fastExit = false;
				bool hasFailures = false;

				// Firstly, invoke all syncronous validators and collect their results.
				foreach (var validator in _validators.Where(v => !v.Worker.ShouldValidateAsync(context))) {

					if (cancellation.IsCancellationRequested) {
						return TaskHelpers.Canceled();
					}

					bool success = validator.Validate(context, propertyName);

					if (!success) {
						hasFailures = true;
					}
					
					// If there has been at least one failure, and our CascadeMode has been set to StopOnFirst
					// then don't continue to the next rule
					if (fastExit = (cascade == CascadeMode.StopOnFirstFailure && hasFailures)) {
						break;
					}
				}

				//if StopOnFirstFailure triggered then we exit
				if (fastExit && hasFailures) {
					// Callback if there has been at least one property validator failed.
					OnFailure(context.Model);
					return TaskHelpers.Completed();
				}

				var asyncValidators = _validators.Where(v => v.ShouldValidateAsync(context)).ToList();
                
				// if there's no async validators then we exit
				if (asyncValidators.Count == 0) {
					if (hasFailures) {
						// Callback if there has been at least one property validator failed.
						OnFailure(context.Model);
						return TaskHelpers.Completed();
					}

					return RunDependentRulesAsync(context, cancellation);
				}

				//Then call asyncronous validators in non-blocking way
				var resultTasks = asyncValidators
					.Select(v => v.ValidateAsync(context, propertyName, cancellation))
					.IterateAsync(cancellation, breakCondition: t => cascade == CascadeMode.StopOnFirstFailure && !t.Result);

				return resultTasks.Then(runSynchronously: true, continuation: tasks => {
					bool failed = tasks.Any(x => x.IsCanceled || x.IsFaulted || x.Result == false);

					if (failed) {
						OnFailure(context.Model);
						return TaskHelpers.Completed();
						//return tasks;
					}

					return RunDependentRulesAsync(context, cancellation);
				});
			}
			catch (Exception ex) {
				return TaskHelpers.FromError(ex);
			}
		}

		public bool ShouldValidateAsync(IValidationContext context) {
			return context.IsAsync;
		}

		private Task RunDependentRulesAsync(IValidationContext context, CancellationToken cancellation) {
			var validations = DependentRules.Select(v => v.ValidateAsync(context, cancellation));
			return TaskHelpers.Iterate(validations, cancellationToken: cancellation);
		}

		/// <summary>
		/// Applies a condition to the rule
		/// </summary>
		/// <param name="predicate"></param>
		/// <param name="applyConditionTo"></param>
		public void ApplyCondition(Func<IValidationContext, bool> predicate, ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators) {
			// Default behaviour for When/Unless as of v1.3 is to apply the condition to all previous validators in the chain.
			if (applyConditionTo == ApplyConditionTo.AllValidators) {
				foreach (var validator in Validators.ToList()) {
					validator.Worker = new DelegatingValidator(predicate, validator.Worker);
				}

				foreach (var dependentRule in DependentRules.ToList()) {
					dependentRule.ApplyCondition(predicate, applyConditionTo);
				}
			}
			else {
				CurrentValidator.Worker = new DelegatingValidator(predicate, CurrentValidator.Worker);
			}
		}

		/// <summary>
		/// Applies the condition to the rule asynchronously
		/// </summary>
		/// <param name="predicate"></param>
		/// <param name="applyConditionTo"></param>
		public void ApplyAsyncCondition(Func<IValidationContext, CancellationToken, Task<bool>> predicate, ApplyConditionTo applyConditionTo = ApplyConditionTo.AllValidators) {
			// Default behaviour for When/Unless as of v1.3 is to apply the condition to all previous validators in the chain.
			if (applyConditionTo == ApplyConditionTo.AllValidators) {
				foreach (var validator in Validators) {
					validator.Worker = new DelegatingValidator(predicate, validator.Worker);
				}

				foreach (var dependentRule in DependentRules.ToList()) {
					dependentRule.ApplyAsyncCondition(predicate, applyConditionTo);
				}
			}
			else {
				CurrentValidator.Worker = new DelegatingValidator(predicate, CurrentValidator.Worker);
			}
		}
	}
	
	/// <summary>
	/// Holds a validation worker and its metadata.
	/// </summary>
	public class RuleElement {
		private IValidationWorker _worker;

		protected PropertyRule Rule { get; }

		/// <summary>
		/// Creates a new Container
		/// </summary>
		/// <param name="worker"></param>
		/// <param name="metadata"></param>
		/// <param name="rule"></param>
		public RuleElement(IValidationWorker worker, ValidatorMetadata metadata, PropertyRule rule) {
			_worker = worker;
			Metadata = metadata;
			Rule = rule;
		}

		/// <summary>
		/// A validator to run.
		/// </summary>
		public IValidationWorker Worker {
			get => _worker;
			set => _worker = value ?? throw new ArgumentNullException(nameof(value), "Cannot set Worker to null");
		}

		/// <summary>
		/// Metadata for this validator.
		/// </summary>
		public ValidatorMetadata Metadata { get; }

		/// <summary>
		/// Invokes the validator asynchronously 
		/// </summary>
		/// <returns></returns>
		public virtual async Task<bool> ValidateAsync(IValidationContext context, string propertyName, CancellationToken cancellation) {
			var propertyValidatorContext = new PropertyValidatorContext(context, Rule, propertyName);
			await _worker.ValidateAsync(propertyValidatorContext, cancellation);
			return !propertyValidatorContext.HasFailures;
		}

		/// <summary>
		/// Invokes a property validator using the specified validation context.
		/// </summary>
		public virtual bool Validate(IValidationContext context, string propertyName) {
			var propertyContext = new PropertyValidatorContext(context, Rule, propertyName);
			_worker.Validate(propertyContext);
			return !propertyContext.HasFailures;
		}
		
		public virtual bool ShouldValidateAsync(IValidationContext context) {
			return _worker.ShouldValidateAsync(context);
		}
	}

}