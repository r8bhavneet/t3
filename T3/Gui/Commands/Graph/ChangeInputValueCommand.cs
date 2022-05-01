﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using T3.Core.Animation;
using T3.Core.Logging;
using T3.Core.Operator;
using T3.Gui;
using T3.Gui.Commands;

namespace t3.Gui.Commands.Graph
{
    public class ChangeInputValueCommand : ICommand
    {
        public string Name => "Change Input Value";
        public bool IsUndoable => true;

        public ChangeInputValueCommand(Symbol inputParentSymbol, Guid symbolChildId, SymbolChild.Input input, InputValue newValue)
        {
            _inputParentSymbolId = inputParentSymbol.Id;
            _childId = symbolChildId;
            _inputId = input.InputDefinition.Id;
            _isAnimated = inputParentSymbol.Animator.IsAnimated(_childId, _inputId);
            _wasDefault = input.IsDefault;
            _animationTime = EvaluationContext.GlobalTimeForKeyframes;

            OriginalValue = input.Value.Clone();
            NewValue = newValue == null ? input.Value.Clone() : newValue.Clone();

            if (_isAnimated)
            {
                var animator = inputParentSymbol.Animator;
                _originalKeyframes = animator.GetTimeKeys(symbolChildId, _inputId, _animationTime).ToList();
            }
        }

        public void Undo()
        {
            // Log.Debug($"Undo  {ValueAsString(NewValue)} -> {ValueAsString(OriginalValue)}");
            var inputParentSymbol = SymbolRegistry.Entries[_inputParentSymbolId];
            if (_isAnimated)
            {
                var wasNewKeyframe = false;
                foreach (var v in _originalKeyframes)
                {
                    if (v == null)
                    {
                        wasNewKeyframe = true;
                        break;
                    }
                }

                var animator = inputParentSymbol.Animator;
                if (wasNewKeyframe)
                {
                    animator.SetTimeKeys(_childId, _inputId,_animationTime, _originalKeyframes); // Remove keyframes
                    var symbolChild = inputParentSymbol.Children.Single(child => child.Id == _childId);
                    InvalidateInstances(inputParentSymbol, symbolChild);
                }
                else
                {
                    animator.SetTimeKeys(_childId, _inputId,_animationTime, _originalKeyframes);
                    AssignValue(OriginalValue, false);
                }
            }
            else
            {
                if (_wasDefault)
                {
                    var symbolChild = inputParentSymbol.Children.Single(child => child.Id == _childId);
                    var input = symbolChild.InputValues[_inputId];
                    input.ResetToDefault();
                    InvalidateInstances(inputParentSymbol, symbolChild);
                }
                else
                {
                    AssignValue(OriginalValue, false);
                }
            }
        }

        public void Do()
        {
            // Log.Debug($"Do  {ValueAsString(OriginalValue)} -> {ValueAsString(NewValue)}");
            AssignValue(NewValue);
        }

        private string ValueAsString(InputValue v)
        {
            if (v is InputValue<float> f)
            {
                return f.Value.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                return v.ToString();
            }
        }

        public void AssignValue(InputValue valueToSet, bool updateNewValue = true)
        {
            if (updateNewValue)
            {
                NewValue.Assign(valueToSet);
            }
            
            var inputParentSymbol = SymbolRegistry.Entries[_inputParentSymbolId];
            var symbolChild = inputParentSymbol.Children.Single(child => child.Id == _childId);
            
            var input = symbolChild.InputValues[_inputId];
            input.Value.Assign(NewValue);
            
            if (_isAnimated)
            {
                var symbolUi = SymbolUiRegistry.Entries[symbolChild.Symbol.Id];
                var inputUi = symbolUi.InputUis[_inputId];
                var animator = inputParentSymbol.Animator;

                foreach (var parentInstance in inputParentSymbol.InstancesOfSymbol)
                {
                    var instance = parentInstance.Children.Single(child => child.SymbolChildId == symbolChild.Id);
                    var inputSlot = instance.Inputs.Single(slot => slot.Id == _inputId);
                    inputUi.ApplyValueToAnimation(inputSlot, valueToSet, animator);
                    inputSlot.DirtyFlag.Invalidate(true);
                }
            }
            else
            {
                input.IsDefault = false;
                InvalidateInstances(inputParentSymbol, symbolChild);
            }
        }

        private void InvalidateInstances(Symbol inputParentSymbol, SymbolChild symbolChild)
        {
            foreach (var parentInstance in inputParentSymbol.InstancesOfSymbol)
            {
                var instance = parentInstance.Children.Single(child => child.SymbolChildId == symbolChild.Id);
                var inputSlot = instance.Inputs.Single(slot => slot.Id == _inputId);
                inputSlot.DirtyFlag.Invalidate(true);
            }
        }

        private InputValue OriginalValue { get; set; }
        public InputValue NewValue { get; private set; }

        private readonly Guid _inputParentSymbolId;
        private readonly Guid _childId;
        private readonly Guid _inputId;
        private readonly bool _wasDefault;
        private readonly bool _isAnimated;
        private readonly double _animationTime;
        private readonly List<VDefinition> _originalKeyframes;
    }
}