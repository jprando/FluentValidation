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
	using Internal;

	public class PropertyValidatorContext : IValidationContext {
		private MessageFormatter _messageFormatter;
		private readonly Lazy<object> _propertyValueContainer;

		public ValidationContext ParentContext { get; }
		public PropertyRule Rule { get; }
		
		public string ModelName { get; }
		public string PropertyName => ModelName;
		public string DisplayName => Rule.GetDisplayName(Container);

		[Obsolete("Use the Container property instead.")]
		public object Instance => ParentContext.Model;
		public object Container => ParentContext.Model;

		public Dictionary<string, object> RootContextData => ParentContext.RootContextData;
		public PropertyChain PropertyChain => ParentContext.PropertyChain;

		public MessageFormatter MessageFormatter => _messageFormatter ?? (_messageFormatter = ValidatorOptions.MessageFormatterFactory());

		//Lazily load the property value
		//to allow the delegating validator to cancel validation before value is obtained
		object IValidationContext.Model => _propertyValueContainer.Value;
		public object PropertyValue => _propertyValueContainer.Value;

		public PropertyValidatorContext(ValidationContext parentContext, PropertyRule rule, string propertyName) {
			ParentContext = parentContext;
			Rule = rule;
			ModelName = propertyName;
			_propertyValueContainer = new Lazy<object>( () => {
				var value = rule.PropertyFunc(parentContext.Model);
				if (rule.Transformer != null) value = rule.Transformer(value);
				return value;
			});
		}

		public PropertyValidatorContext(ValidationContext parentContext, PropertyRule rule, string propertyName, object propertyValue) {
			ParentContext = parentContext;
			Rule = rule;
			ModelName = propertyName;
			_propertyValueContainer = new Lazy<object>(() => propertyValue);
		}

	
		public IValidatorSelector Selector => ParentContext.Selector;
	}
}