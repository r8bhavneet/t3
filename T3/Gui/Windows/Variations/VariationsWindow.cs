﻿using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using T3.Gui.Graph;
using T3.Gui.Graph.Interaction;
using T3.Gui.Interaction.Variations;
using T3.Gui.Interaction.Variations.Model;

namespace T3.Gui.Windows.Variations
{
    public class VariationsWindow : Window
    {
        public VariationsWindow()
        {
            _presetCanvas = new PresetCanvas();
            _snapshotCanvas = new SnapshotCanvas();
            Config.Title = "Variations";
            WindowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
        }

        protected override void DrawContent()
        {
            DrawWindowContent();
        }

        private ViewModes _viewMode = 0;
        private int _selectedNodeCount = 0;

        private void DrawWindowContent()
        {
            // Delete actions need be deferred to prevent collection modification during iteration
            if (_variationsToBeDeletedNextFrame.Count > 0)
            {
                _poolWithVariationToBeDeleted.DeleteVariations(_variationsToBeDeletedNextFrame);
                _variationsToBeDeletedNextFrame.Clear();
            }

            var compositionHasVariations = VariationHandling.ActivePoolForSnapshots != null && VariationHandling.ActivePoolForSnapshots.Variations.Count > 0;
            var oneChildSelected = NodeSelection.Selection.Count == 1;
            var selectionChanged = NodeSelection.Selection.Count != _selectedNodeCount;

            if (selectionChanged)
            {
                _selectedNodeCount = NodeSelection.Selection.Count;
                
                if (oneChildSelected)
                {
                    _viewMode = ViewModes.Presets;
                }
                else if (compositionHasVariations && _selectedNodeCount == 0) 
                {
                    _viewMode = ViewModes.Snapshots;
                }
            }

            var drawList = ImGui.GetWindowDrawList();
            var keepCursorPos = ImGui.GetCursorScreenPos();

            drawList.ChannelsSplit(2);
            drawList.ChannelsSetCurrent(1);
            {
                ImGui.BeginChild("header", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight()));

                var viewModeIndex = (int)_viewMode;
                if (CustomComponents.DrawSegmentedToggle(ref viewModeIndex, _options))
                {
                    _viewMode = (ViewModes)viewModeIndex;
                }

                ImGui.SameLine();
                
                switch (_viewMode)
                {
                    case ViewModes.Presets:
                        _presetCanvas.DrawToolbarFunctions();
                        break;
                        
                    case ViewModes.Snapshots:
                        _snapshotCanvas.DrawToolbarFunctions();
                        break;
                }

                ImGui.EndChild();
            }

            drawList.ChannelsSetCurrent(0);
            {
                ImGui.SetCursorScreenPos(keepCursorPos);

                if (_viewMode == ViewModes.Presets)
                {
                    if (VariationHandling.ActivePoolForPresets == null 
                        || VariationHandling.ActiveInstanceForPresets == null 
                        || VariationHandling.ActivePoolForPresets.Variations.Count == 0)
                    {
                        CustomComponents.EmptyWindowMessage("No presets yet.");
                    }
                    else
                    {
                        _presetCanvas.Draw(drawList);
                    }
                }
                else
                {
                    if (VariationHandling.ActivePoolForSnapshots == null 
                        || VariationHandling.ActiveInstanceForSnapshots == null 
                        || VariationHandling.ActivePoolForSnapshots.Variations.Count == 0)
                    {
                        CustomComponents.EmptyWindowMessage("No Snapshots yet.\n\nSnapshots save parameters for selected\nOperators in the current composition.");
                    }
                    else
                    {
                        _snapshotCanvas.Draw(drawList);
                    }
                }
            }

            drawList.ChannelsMerge();
        }

        private enum ViewModes
        {
            Presets,
            Snapshots,
        }

        private static readonly List<string> _options = new() { "Presets", "Snapshots" };

        public override List<Window> GetInstances()
        {
            return new List<Window>();
        }

        public static void DeleteVariationsFromPool(SymbolVariationPool pool, IEnumerable<Variation> selectionSelection)
        {
            
            _poolWithVariationToBeDeleted = pool;
            _variationsToBeDeletedNextFrame.AddRange(selectionSelection); // TODO: mixing Snapshots and variations in same list is dangerous
            pool.StopHover();
            pool.SaveVariationsToFile();
        }

        private static readonly List<Variation> _variationsToBeDeletedNextFrame = new(20);
        private static SymbolVariationPool _poolWithVariationToBeDeleted;
        private readonly PresetCanvas _presetCanvas;
        private readonly SnapshotCanvas _snapshotCanvas;
    }
}