using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FontAwesome.Sharp;
using NEdifis.Attributes;
using NZazu.Contracts;
using NZazu.Contracts.Checks;
using NZazu.Fields.Controls;

namespace NZazu.Fields
{
    public class NZazuDataTableField
        : NZazuField
        , IRequireFactory
    {
        #region crappy code to create a new row after tabbing the last field

#pragma warning disable 612
        [Obsolete]
        private UIElement _lastAddedField;

        private void ChangeLastAddedFieldTo(UIElement newField)
        {
            if (_lastAddedField != null)
                _lastAddedField.PreviewKeyDown -= LastAddedFieldOnPreviewKeyDown;
            _lastAddedField = newField;
            if (_lastAddedField != null)
                _lastAddedField.PreviewKeyDown += LastAddedFieldOnPreviewKeyDown;
        }

        [ExcludeFromCodeCoverage]
        internal void LastAddedFieldOnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // double check sender
            if (!ReferenceEquals(sender, _lastAddedField)) return;

            // check shortcut
            var binding = new KeyBinding { Key = System.Windows.Input.Key.Tab };
            if (!binding.Gesture.Matches(sender, e)) return;

            // add rows and handle key
            AddNewRow();
        }
#pragma warning restore 612

        #endregion

        #region shortcuts and insert/delete line

        private readonly KeyBinding _addRowAboveShortcut1 = new KeyBinding { Key = System.Windows.Input.Key.OemPlus, Modifiers = ModifierKeys.Control };
        private readonly KeyBinding _addRowAboveShortcut2 = new KeyBinding { Key = System.Windows.Input.Key.Insert, Modifiers = ModifierKeys.Control };
        private readonly KeyBinding _deleteRowShortcut1 = new KeyBinding { Key = System.Windows.Input.Key.OemMinus, Modifiers = ModifierKeys.Control };
        private readonly KeyBinding _deleteRowShortcut2 = new KeyBinding { Key = System.Windows.Input.Key.Delete, Modifiers = ModifierKeys.Control };

        private void AttachShortcutsTo(UIElement ctrl)
        {
            if (ctrl == null) throw new ArgumentNullException(nameof(ctrl));
            ctrl.PreviewKeyDown += ValueControl_PreviewKeyDown;
        }

        private void RemoveShortcutsFrom(UIElement ctrl)
        {
            if (ctrl == null) throw new ArgumentNullException(nameof(ctrl));
            ctrl.PreviewKeyDown -= ValueControl_PreviewKeyDown;
        }

        [ExcludeFromCodeCoverage]
        [Because("I need to find out how to test shortcuts with modifier keys")]
        private void ValueControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_addRowAboveShortcut1.Gesture.Matches(sender, e) || _addRowAboveShortcut2.Gesture.Matches(sender, e))
                AddRowAbove(sender);
            if (_deleteRowShortcut1.Gesture.Matches(sender, e) || _deleteRowShortcut2.Gesture.Matches(sender, e))
                DeleteRow(sender);
        }

        internal void DeleteRow(object sender)
        {
            var ctrl = sender as Control;
            if (ctrl == null) return;

            var row = Grid.GetRow(ctrl);
            if (row == 0) return; // cannot delete header
            if (_clientControl.LayoutGrid.RowDefinitions.Count <= 2) return; // cannot delete last input row

            var controlsToDelete = _clientControl
                .LayoutGrid.Children.Cast<UIElement>()
                .Where(e => Grid.GetRow(e) == row)
                .ToList();
            controlsToDelete.ForEach(delegate (UIElement control)
            {
                RemoveShortcutsFrom(control);
                _clientControl.LayoutGrid.Children.Remove(control);
                _fields.Remove(_fields.First(x => Equals(x.Value.ValueControl, control)));
            });

            RecalculateFieldKeys();

            var row2Delete = _clientControl.LayoutGrid.RowDefinitions.Count - 1;
            _clientControl.LayoutGrid.RowDefinitions.RemoveAt(row2Delete);

            // lets assume the last control in _fields is the lastAddedField
            ChangeLastAddedFieldTo(_clientControl.LayoutGrid.Children.Cast<UIElement>()
                .First(x => Grid.GetRow(x) == _clientControl.LayoutGrid.RowDefinitions.Count - 1 &&
                            Grid.GetColumn(x) == _clientControl.LayoutGrid.ColumnDefinitions.Count - 1));
        }

        // renumber all the fields so they are back in order again
        private void RecalculateFieldKeys()
        {
            var newFields = new Dictionary<string, INZazuWpfField>();
            var lastIndex = string.Empty;
            var index = 0;
            foreach (var field in _fields.OrderBy(x => int.Parse(x.Key.Split(new[] { "__" }, StringSplitOptions.None)[1])))
            {
                var splits = field.Key.Split(new[] { "__" }, StringSplitOptions.None);

                if (lastIndex != splits[1])
                {
                    index++;
                    lastIndex = splits[1];
                }

                var newKey = splits[0] + "__" + index;

                // set new field and move the control to the current row (index)
                field.Value.ValueControl.Name = newKey;
                newFields.Add(newKey, field.Value);
                Grid.SetRow(field.Value.ValueControl, index);
            }

            _fields.Clear();
            newFields.ToList().ForEach(x => _fields.Add(x));
        }

        internal void AddRowAbove(object sender)
        {
            var ctrl = sender as Control;
            if (ctrl == null) return;

            var row = Grid.GetRow(ctrl);
            if (row == 0) return; // cannot insert above header


            var fieldsAboveInsert =
                _fields.Where(x => int.Parse(x.Key.Split(new[] { "__" }, StringSplitOptions.None)[1]) < row).ToArray();
            var fieldsBelowInsert =
                _fields.Where(x => int.Parse(x.Key.Split(new[] { "__" }, StringSplitOptions.None)[1]) >= row).ToArray();
            var fieldsBelowWithNewName = new Dictionary<string, INZazuWpfField>();

            // now we need to move the below fields to the next row (a little like Hilberts Hotel)
            _clientControl.LayoutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24) });
            foreach (var field in fieldsBelowInsert.OrderBy(x => int.Parse(x.Key.Split(new[] { "__" }, StringSplitOptions.None)[1])))
            {
                // move to next row
                var currentRow = Grid.GetRow(field.Value.ValueControl);
                Grid.SetRow(field.Value.ValueControl, currentRow + 1);

                // change fields index and control name (for serialization)
                var splits = field.Key.Split(new[] { "__" }, StringSplitOptions.None);
                var newKey = splits[0] + "__" + (currentRow + 1);
                field.Value.ValueControl.Name = newKey;
                fieldsBelowWithNewName.Add(newKey, field.Value);
            }

            // ok, lets fill the  fields
            _fields.Clear();
            fieldsAboveInsert.ToList().ForEach(_fields.Add);
            AddNewRow(row); // this adds the new controls to the field list
            fieldsBelowWithNewName.ToList().ForEach(_fields.Add);
        }

        #endregion

        public INZazuWpfFieldFactory FieldFactory { get; set; }

        private DynamicDataTable _clientControl;
        private readonly IDictionary<string, INZazuWpfField> _fields = new Dictionary<string, INZazuWpfField>();
        private int _tabOrder;

        private Button _addBtn;
        private Button _delBtn;

        public NZazuDataTableField(FieldDefinition definition) : base(definition) { }

        #region buttons

        [ExcludeFromCodeCoverage]
        private void AddBtnOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            AddNewRow();
        }

        [ExcludeFromCodeCoverage]
        private void DelBtnOnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var lastField = _clientControl.LayoutGrid.Children.Cast<UIElement>()
                .First(x => Grid.GetRow(x) == _clientControl.LayoutGrid.RowDefinitions.Count - 1 &&
                            Grid.GetColumn(x) == _clientControl.LayoutGrid.ColumnDefinitions.Count - 1);

            DeleteRow(lastField);
        }

        private void AddNewRow(int row = -1)
        {
            var grid = _clientControl.LayoutGrid;

            int rowNo;
            if (row == -1) // we add a row at the end
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24) });
                rowNo = grid.RowDefinitions.Count - 1;
            }
            else
                rowNo = row;

            var columnCounter = 0;
            foreach (var field in (Definition.Fields ?? Enumerable.Empty<FieldDefinition>()).ToArray())
            {
                // we create a first empty row :)
                var ctrl = FieldFactory.CreateField(field);
                ctrl.ValueControl.Name = field.Key + "__" + rowNo;
                ctrl.ValueControl.TabIndex = _tabOrder++;
                AttachShortcutsTo(ctrl.ValueControl);

                grid.Children.Add(ctrl.ValueControl);
                Grid.SetRow(ctrl.ValueControl, rowNo);
                Grid.SetColumn(ctrl.ValueControl, columnCounter);

                _fields.Add(ctrl.ValueControl.Name, ctrl);
                if (row == -1) // only if added new row
                    ChangeLastAddedFieldTo(ctrl.ValueControl);

                columnCounter++;
            }

            if (_addBtn != null)
                _addBtn.TabIndex = _tabOrder + 1;
            if (_delBtn != null)
                _delBtn.TabIndex = _tabOrder + 2;
        }

        #endregion

        public override bool IsEditable => true;

        protected override void SetStringValue(string value)
        {
            UpdateGridValues(value);
        }

        protected override string GetStringValue()
        {
            return GetGridValues();
        }

        private string GetGridValues()
        {
            var data = _clientControl.LayoutGrid.Children
                .Cast<Control>()
                .Where(x =>
                    !string.IsNullOrEmpty(x.Name) &&
                    Definition.Fields.SingleOrDefault(
                        y => y.Key == x.Name.Split(new[] { "__" }, StringSplitOptions.RemoveEmptyEntries)[0]) != null)
                .ToDictionary(
                    child => child.Name,
                    child => _fields.Single(x => Equals(x.Value.ValueControl, child)).Value.StringValue
                 );

            return FieldFactory.Serializer.Serialize(data);
        }

        private void UpdateGridValues(string value)
        {
            Dictionary<string, string> newDict;
            try
            {
                newDict = FieldFactory.Serializer.Deserialize(value);

            }
            catch (Exception ex)
            {
                throw new SerializationException("NZazu.NZazuDataTable.UpdateGridValues(): data cannot be parsed. therefore the list will be empty", ex);
            }

            var iterations = 0;
            if (newDict.Count > 0)
                iterations = newDict
                    .Max(x => int.Parse(x.Key.Split(new[] { "__" }, StringSplitOptions.RemoveEmptyEntries)[1]));

            while (_clientControl.LayoutGrid.RowDefinitions.Count <= iterations)
                AddNewRow();

            foreach (var field in _fields)
            {
                var kv = newDict.FirstOrDefault(x => x.Key == field.Key);
                if (string.IsNullOrEmpty(kv.Key)) continue;

                field.Value.StringValue = kv.Value;
            }

        }

        public override DependencyProperty ContentProperty => null;
        public override string Type => "datatable";

        protected override Control GetValue()
        {
            if (_clientControl != null) return _clientControl;

            _clientControl = new DynamicDataTable();
            CreateClientControlsOn(_clientControl.LayoutGrid);
            // somewhere here i need to attach the behaviour
            CreateButtonsOn(_clientControl.ButtonPanel);
            return _clientControl;
        }

        private void CreateClientControlsOn(Grid grid)
        {
            // header
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24.0) });
            foreach (var field in Definition.Fields ?? Enumerable.Empty<FieldDefinition>())
            {
                // create column with default width
                var width = 135; // default Width
                if (field.Settings != null && field.Settings.ContainsKey("Width")) width = int.Parse(field.Settings["Width"]);
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width) });
                var column = grid.ColumnDefinitions.Count - 1;

                // set the header column ;)
                var lbl = new Label()
                {
                    Content = field.Prompt,
                    ToolTip = field.Description,
                    Background = Brushes.Silver,
                    FontWeight = FontWeights.Bold
                };
                if (string.IsNullOrEmpty(Description)) lbl.ToolTip = field.Prompt;
                grid.Children.Add(lbl);
                Grid.SetRow(lbl, 0);
                Grid.SetColumn(lbl, column); // the last one ;)
            }

            AddNewRow();
        }

        #region create _clientControl

        private void CreateButtonsOn(Panel panel)
        {
            // add button
            _addBtn = new Button
            {
                Content = new IconBlock { Icon = IconChar.PlusCircle, Foreground = Brushes.DarkGreen },
                TabIndex = _tabOrder + 1,
                FontFamily = new FontFamily("/FontAwesome.Sharp;component/fonts/#FontAwesome"),
                Width = 24
            };
            _addBtn.Click += AddBtnOnClick;
            panel.Children.Add(_addBtn);

            // del button
            _delBtn = new Button
            {
                Content = new IconBlock { Icon = IconChar.MinusCircle, Foreground = Brushes.DarkRed },
                TabIndex = _tabOrder + 2,
                FontFamily = new FontFamily("/FontAwesome.Sharp;component/fonts/#FontAwesome"),
                Width = 24
            };
            _delBtn.Click += DelBtnOnClick;
            panel.Children.Add(_delBtn);
        }

        #endregion

        public override ValueCheckResult Validate()
        {
            var result = base.Validate();

            foreach (var field in _fields)
            {
                if (!field.Value.IsEditable) continue;

                var iterRes = field.Value.Validate();
                if (!iterRes.IsValid)
                    result = new ValueCheckResult(false, iterRes.Error);
            }
            return result;
        }
    }
}