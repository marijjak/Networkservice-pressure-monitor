using NetworkService.Model;
using NetworkService.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NetworkService.Behaviors
{
    public static class DragDropBehavior
    {
        private static readonly DependencyProperty DragStartProperty =
            DependencyProperty.RegisterAttached("DragStart", typeof(Point),
                typeof(DragDropBehavior), new PropertyMetadata(new Point()));

        private static void SetDragStart(DependencyObject o, Point v) => o.SetValue(DragStartProperty, v);
        private static Point GetDragStart(DependencyObject o) => (Point)o.GetValue(DragStartProperty);

        private static readonly DependencyProperty IsDraggingProperty =
            DependencyProperty.RegisterAttached("IsDragging", typeof(bool),
                typeof(DragDropBehavior), new PropertyMetadata(false));

        private static void SetIsDragging(DependencyObject o, bool v) => o.SetValue(IsDraggingProperty, v);
        private static bool GetIsDragging(DependencyObject o) => (bool)o.GetValue(IsDraggingProperty);

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.RegisterAttached("ViewModel", typeof(NetworkDisplayViewModel),
                typeof(DragDropBehavior), new PropertyMetadata(null));

        public static void SetViewModel(DependencyObject o, NetworkDisplayViewModel v) => o.SetValue(ViewModelProperty, v);
        public static NetworkDisplayViewModel GetViewModel(DependencyObject o) => (NetworkDisplayViewModel)o.GetValue(ViewModelProperty);

        private static bool PassedThreshold(Point start, Point now) =>
            Math.Abs(start.X - now.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(start.Y - now.Y) > SystemParameters.MinimumVerticalDragDistance;

        public static readonly DependencyProperty TreeEnabledProperty =
            DependencyProperty.RegisterAttached("TreeEnabled", typeof(bool),
                typeof(DragDropBehavior), new PropertyMetadata(false, OnTreeEnabledChanged));

        public static void SetTreeEnabled(DependencyObject o, bool v) => o.SetValue(TreeEnabledProperty, v);
        public static bool GetTreeEnabled(DependencyObject o) => (bool)o.GetValue(TreeEnabledProperty);

        private static void OnTreeEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is TreeView tree) || !(bool)e.NewValue) return;

            tree.SelectedItemChanged += (s, args) =>
            {
                if (tree.DataContext is NetworkDisplayViewModel vm)
                    vm.SelectedTreeItem = args.NewValue as Ventil;
            };

            tree.PreviewMouseLeftButtonDown += (s, args) =>
            {
                SetDragStart(tree, args.GetPosition(null));
                SetIsDragging(tree, false);
            };

            tree.PreviewMouseMove += (s, args) =>
            {
                if (args.LeftButton != MouseButtonState.Pressed || GetIsDragging(tree)) return;
                if (!(tree.DataContext is NetworkDisplayViewModel vm) || vm.SelectedTreeItem == null) return;
                if (!PassedThreshold(GetDragStart(tree), args.GetPosition(null))) return;

                SetIsDragging(tree, true);
                DragDrop.DoDragDrop(tree, vm.SelectedTreeItem, DragDropEffects.Move);
                SetIsDragging(tree, false);
            };

            tree.Drop += (s, args) =>
            {
                if (!(tree.DataContext is NetworkDisplayViewModel vm)) return;
                if (args.Data.GetData(typeof(NetworkItem)) is NetworkItem sourceCell && sourceCell.Ventil != null)
                {
                    vm.SelectedNetworkItem = sourceCell;
                    if (vm.RemoveFromGridCommand.CanExecute(null))
                        vm.RemoveFromGridCommand.Execute(null);
                }
            };
        }

        public static readonly DependencyProperty CellEnabledProperty =
            DependencyProperty.RegisterAttached("CellEnabled", typeof(bool),
                typeof(DragDropBehavior), new PropertyMetadata(false, OnCellEnabledChanged));

        public static void SetCellEnabled(DependencyObject o, bool v) => o.SetValue(CellEnabledProperty, v);
        public static bool GetCellEnabled(DependencyObject o) => (bool)o.GetValue(CellEnabledProperty);

        private static void OnCellEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FrameworkElement cell) || !(bool)e.NewValue) return;

            cell.MouseLeftButtonDown += Cell_Down;
            cell.MouseMove += Cell_Move;
            cell.Drop += Cell_Drop;
        }

        private static void Cell_Down(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is FrameworkElement cell)) return;
            if (!(cell.DataContext is NetworkItem item)) return;
            var vm = GetViewModel(cell);
            if (vm == null) return;

            vm.SelectedNetworkItem = item;
            SetDragStart(cell, e.GetPosition(null));
            SetIsDragging(cell, false);

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                if (item.Ventil != null)
                {
                    int idx = vm.NetworkItems.IndexOf(item);
                    vm.HandleConnectionClick(idx);
                }
                e.Handled = true;
            }
        }

        private static void Cell_Move(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (!(sender is FrameworkElement cell)) return;
            if (GetIsDragging(cell)) return;
            if (!(cell.DataContext is NetworkItem item)) return;
            var vm = GetViewModel(cell);
            if (vm == null) return;
            if (!PassedThreshold(GetDragStart(cell), e.GetPosition(null))) return;

            SetIsDragging(cell, true);
            if (item.Ventil != null)
                DragDrop.DoDragDrop(cell, item, DragDropEffects.Move);
            else if (vm.SelectedTreeItem != null)
                DragDrop.DoDragDrop(cell, vm.SelectedTreeItem, DragDropEffects.Move);
            SetIsDragging(cell, false);
        }

        private static void Cell_Drop(object sender, DragEventArgs e)
        {
            if (!(sender is FrameworkElement cell)) return;
            if (!(cell.DataContext is NetworkItem targetCell)) return;
            var vm = GetViewModel(cell);
            if (vm == null) return;

            if (e.Data.GetData(typeof(Ventil)) is Ventil ventil && targetCell.IsEmpty)
            {
                vm.SelectedTreeItem = ventil;
                vm.DropCommand.Execute(targetCell);
            }
            else if (e.Data.GetData(typeof(NetworkItem)) is NetworkItem sourceCell
                     && targetCell.IsEmpty && sourceCell != targetCell)
            {
                vm.MoveItem(sourceCell, targetCell);
            }
        }
    }
}
