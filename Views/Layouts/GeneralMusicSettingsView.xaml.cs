using PlayniteSounds.Models;
using PlayniteSounds.Views.Models;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PlayniteSounds.Views.Layouts;

public partial class GeneralMusicSettingsView
{

    private bool _dragging;
    private Point _draggingStart;

    public GeneralMusicSettingsView()
    {
        InitializeComponent();
        DataContextChanged += CreateDownloaderItems;
    }

    // Heavily based on the listbox implementation by felixkmh: https://github.com/felixkmh/DuplicateHider/blob/master/source/DuplicateHiderSettingsView.xaml.cs
    public void CreateDownloaderItems(object sender, DependencyPropertyChangedEventArgs e)
    {
        var model = DataContext as PlayniteSoundsSettingsViewModel;

        var downloaders = model.Settings.Downloaders;
        foreach (var source in downloaders)
        {
            Downloaders.Items.Add(CreateDownloaderEntry(source, true));
        }

        foreach (Source source in Enum.GetValues(typeof(Source)))
        {
            if (source != Source.All && !downloaders.Contains(source))
            {
                Downloaders.Items.Add(CreateDownloaderEntry(source, false));
            }
        }
    }

    private class CustomListBoxItem : ListBoxItem
    {
        public Source Source;
    }

    public ListBoxItem CreateDownloaderEntry(Source source, bool enabled)
    {
        var buttonUp = new Button
        {
            Content = "▲",
            Margin = new Thickness(0, 0, 3, 0),
            Width = 20,
            Height = 20,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            ClipToBounds = false,
            Padding = new Thickness(0, 0, 0, 0),
            Cursor = Cursors.Arrow
        };
        buttonUp.Click += ButtonUp_Click;

        var buttonDown = new Button
        {
            Content = "▼",
            Margin = new Thickness(0, 0, 8, 0),
            Width = 20,
            Height = 20,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            ClipToBounds = false,
            Padding = new Thickness(0, 0, 0, 0),
            Cursor = Cursors.Arrow
        };
        buttonDown.Click += ButtonDown_Click;

        var toggle = new CheckBox { IsChecked = enabled };
        toggle.Checked += (_, _) => UpdateDownloaders();
        toggle.Unchecked += (_, _) => UpdateDownloaders();

        var label = new Label { Content = source };

        var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
        stackPanel.Children.Add(buttonUp);
        stackPanel.Children.Add(buttonDown);
        stackPanel.Children.Add(toggle);
        stackPanel.Children.Add(label);

        var item = new CustomListBoxItem
        {
            Content = stackPanel,
            Source = source,
            Tag = toggle,
            AllowDrop = true,
            Cursor = Cursors.SizeNS
        };

        item.Drop += Item_Drop;
        item.PreviewDragOver += Item_PreviewDragOver;
        item.PreviewMouseMove += Item_PreviewMouseMove;
        item.PreviewMouseLeftButtonDown += Item_MouseLeftButtonDown;
        item.PreviewMouseLeftButtonUp += Item_MouseLeftButtonUp;

        buttonUp.Tag = item;
        buttonDown.Tag = item;
        label.Tag = item;
        toggle.Tag = item;

        return item;
    }

    private void Item_Drop(object sender, DragEventArgs e)
    {
        Downloaders.SelectedItem = null;
    }

    private void Item_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
    }

    private void Item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = true;
        _draggingStart = e.GetPosition(Downloaders);
    }

    private void Item_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (sender is CustomListBoxItem draggedItem && e.LeftButton == MouseButtonState.Pressed)
        {
            var delta = Point.Subtract(e.GetPosition(Downloaders), _draggingStart);
            if (_dragging && delta.Length > 5)
            {
                DragDrop.DoDragDrop(draggedItem, draggedItem, DragDropEffects.Move);
                UpdateDownloaders();
            }
        }
    }

    private void Item_PreviewDragOver(object sender, DragEventArgs e)
    {
        if (sender is CustomListBoxItem hitItem && e.Data.GetData(typeof(ListBoxItem)) is ListBoxItem droppedItem)
        {
            var targetIdx = Downloaders.Items.IndexOf(hitItem);
            var removedIdx = Downloaders.Items.IndexOf(droppedItem);
            Downloaders.Items.RemoveAt(removedIdx);
            Downloaders.Items.Insert(targetIdx, droppedItem);
            Downloaders.SelectedItem = droppedItem;

            UpdateDownloaders();
        }
    }

    private void ButtonDown_Click(object sender, RoutedEventArgs e)
    {
        var item = (CustomListBoxItem)((Button)sender).Tag;
        item.IsSelected = false;
        var index = Downloaders.Items.IndexOf(item);
        Downloaders.Items.RemoveAt(index);
        if (index < Downloaders.Items.Count - 1)
        {
            Downloaders.Items.Insert(index + 1, item);
        }
        else
        {
            Downloaders.Items.Add(item);
        }

        UpdateDownloaders();
    }

    private void ButtonUp_Click(object sender, RoutedEventArgs e)
    {
        var item = (CustomListBoxItem)((Button)sender).Tag;
        var index = Downloaders.Items.IndexOf(item);
        if (index > 0)
        {
            Downloaders.Items.RemoveAt(index);
            Downloaders.Items.Insert(index - 1, item);
        }

        UpdateDownloaders();
    }

    private void UpdateDownloaders()
    {
        var enabledDownloaders = new HashSet<Source>();
        foreach (CustomListBoxItem item in Downloaders.Items)
        {
            var checkBox = item.Tag as CheckBox;
            if (checkBox.IsChecked ?? false)
            {
                enabledDownloaders.Add(item.Source);
            }
        }

        var model = DataContext as PlayniteSoundsSettingsViewModel;
        model.Settings.Downloaders = enabledDownloaders;
    }
}