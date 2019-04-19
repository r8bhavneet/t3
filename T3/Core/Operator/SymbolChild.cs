﻿using System;
using System.Collections.Generic;

using InputDefinitionId = System.Guid;

namespace T3.Core.Operator
{
    /// <summary>
    /// Represents an sinstance of a <see cref="Symbol"/> within a combined symbol group.
    /// </summary>
    public class SymbolChild
    {
        /// <summary>
        /// A reference to the <see cref="Symbol"/> this is an instance from.
        /// </summary>
        public Symbol Symbol { get; }
        public Guid Id { get; }

        /// <summary>
        /// Map input id to actual input value 
        /// </summary>
        //TODO: It would by much desired to store this as list, because we frequently have to iterate this list to draw inputs
        public Dictionary<InputDefinitionId, Input> InputValues { get; } = new Dictionary<Guid, Input>();

        public SymbolChild(Symbol symbol)
        {
            Symbol = symbol;
            Id = Guid.NewGuid();

            foreach (var inputDefinition in symbol.InputDefinitions)
            {
                InputValues.Add(inputDefinition.Id, new Input(inputDefinition.DefaultValue));
            }
        }

        #region sub classes =============================================================
        public class Input
        {
            /// <summary>
            /// A reference to the default value defined in corresponding <see cref="Symbol"/>
            /// </summary>
            public InputValue DefaultValue { get; }

            /// <summary>
            /// The input value used for this symbol child
            /// </summary>
            public InputValue Value { get; }
            public bool IsDefault { get; set; }

            public Input(InputValue defaultValue)
            {
                DefaultValue = defaultValue;
                Value = DefaultValue.Clone();
                IsDefault = true;
            }

            public void SetCurrentValueAsDefault()
            {
                DefaultValue.Assign(Value);
                IsDefault = true;
            }

            public void ResetToDefault()
            {
                Value.Assign(DefaultValue);
                IsDefault = true;
            }
        }
        #endregion
    }
}
