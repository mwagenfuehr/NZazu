﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace NZazu.Layout
{
    public class GridLayoutStrategy : INZazuLayoutStrategy
    {
        public void DoLayout(ContentControl container, IEnumerable<INZazuField> fields)
        {
            if (container == null) throw new ArgumentNullException("container");
            if (fields == null) throw new ArgumentNullException("fields");

            var grid = new Grid();

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

            var row = 0;
            foreach (var field in fields)
            {
                var labelElement = field.LabelControl;
                var valueElement = field.ValueControl;
                if (labelElement == null && valueElement == null) continue;

                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
                
                if (labelElement != null)
                {
                    Grid.SetColumn(labelElement, 0);
                    Grid.SetRow(labelElement, row);
                    grid.Children.Add(labelElement);
                }

                if (valueElement != null)
                {
                    valueElement.Margin = new Thickness(5, 0, 5, 0);
                    //valueElement.HorizontalAlignment = HorizontalAlignment.Left;
                    valueElement.VerticalAlignment = VerticalAlignment.Center;

                    Grid.SetColumn(valueElement, 1);
                    Grid.SetRow(valueElement, row);
                    grid.Children.Add(valueElement);

                }

                row++;
            }

            container.Content = grid;
        }
    }
}