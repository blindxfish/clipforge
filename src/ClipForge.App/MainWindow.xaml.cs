using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ClipForge.App.ViewModels;
using ClipForge.Core.Models;

namespace ClipForge.App;

/// <summary>
/// Interaction logic for MainWindow.xaml. Closing hides to tray rather than
/// exiting; the app is shut down explicitly from the tray menu.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Refresh();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Keep running in the tray instead of terminating.
        e.Cancel = true;
        Hide();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    // Open a clip's "…" context menu on left-click (it's normally right-click only).
    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { ContextMenu: { } menu } button)
        {
            menu.PlacementTarget = button;
            menu.IsOpen = true;
        }
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    private void EntriesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Vm is { } vm) vm.SelectedCount = EntriesList.SelectedItems.Count;
    }

    private void SelectAllButton_Click(object sender, RoutedEventArgs e) => EntriesList.SelectAll();

    private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (Vm is not { } vm) return;
        var selected = EntriesList.SelectedItems.Cast<ClipEntry>().ToList();
        vm.DeleteEntries(selected);
    }

    // Double-click: images open in the default viewer; other clips copy.
    private void EntriesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FindAncestor<ButtonBase>(e.OriginalSource as DependencyObject) is not null) return;
        if (Vm is not { SelectedEntry: { } entry } vm) return;
        if (entry.Type == ClipType.Image) vm.OpenCommand.Execute(entry);
        else vm.CopyCommand.Execute(entry);
    }

    // --- Drag out: drag a clip (image files or text) to Explorer / other apps ---
    private Point _dragStart;

    private void EntriesList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        _dragStart = e.GetPosition(null);

    private void EntriesList_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var pos = e.GetPosition(null);
        if (System.Math.Abs(pos.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            System.Math.Abs(pos.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        var origin = e.OriginalSource as DependencyObject;
        if (FindAncestor<ButtonBase>(origin) is not null) return;              // let star/… buttons work
        if (FindAncestor<ListBoxItem>(origin) is not { DataContext: ClipEntry pressed }) return;
        if (Vm is not { } vm) return;

        // Drag the whole selection if the pressed row is part of it; otherwise just that row.
        var entries = EntriesList.SelectedItems.Cast<ClipEntry>().ToList();
        if (!entries.Contains(pressed)) entries = new List<ClipEntry> { pressed };

        if (vm.BuildDragData(entries) is { } data)
            DragDrop.DoDragDrop(EntriesList, data, DragDropEffects.Copy);
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match) return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
