using NetworkService.Model;
using NetworkService.ViewModel;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NetworkService.Behaviors
{
    public static class ConnectionsBehavior
    {
        public static readonly DependencyProperty ConnectionsProperty =
            DependencyProperty.RegisterAttached("Connections", typeof(IEnumerable),
                typeof(ConnectionsBehavior), new PropertyMetadata(null, OnConnectionsChanged));

        public static void SetConnections(DependencyObject o, IEnumerable v) => o.SetValue(ConnectionsProperty, v);
        public static IEnumerable GetConnections(DependencyObject o) => (IEnumerable)o.GetValue(ConnectionsProperty);

        public static readonly DependencyProperty ItemsProperty =
            DependencyProperty.RegisterAttached("Items", typeof(IEnumerable),
                typeof(ConnectionsBehavior), new PropertyMetadata(null, OnItemsChanged));

        public static void SetItems(DependencyObject o, IEnumerable v) => o.SetValue(ItemsProperty, v);
        public static IEnumerable GetItems(DependencyObject o) => (IEnumerable)o.GetValue(ItemsProperty);

        public static readonly DependencyProperty ItemsHostProperty =
            DependencyProperty.RegisterAttached("ItemsHost", typeof(ItemsControl),
                typeof(ConnectionsBehavior), new PropertyMetadata(null, OnItemsHostChanged));

        public static void SetItemsHost(DependencyObject o, ItemsControl v) => o.SetValue(ItemsHostProperty, v);
        public static ItemsControl GetItemsHost(DependencyObject o) => (ItemsControl)o.GetValue(ItemsHostProperty);

        private static readonly DependencyProperty HookedProperty =
            DependencyProperty.RegisterAttached("Hooked", typeof(bool),
                typeof(ConnectionsBehavior), new PropertyMetadata(false));

        private static void EnsureHooks(Canvas canvas)
        {
            if ((bool)canvas.GetValue(HookedProperty)) return;
            canvas.SetValue(HookedProperty, true);
            canvas.SizeChanged += (s, _) => Redraw(canvas);
            canvas.Loaded += (s, _) => Redraw(canvas);
        }

        private static void OnConnectionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is Canvas canvas)) return;
            EnsureHooks(canvas);
            if (e.OldValue is INotifyCollectionChanged oldColl)
                oldColl.CollectionChanged -= GetHandler(canvas);
            if (e.NewValue is INotifyCollectionChanged newColl)
                newColl.CollectionChanged += GetHandler(canvas);
            Redraw(canvas);
        }

        private static NotifyCollectionChangedEventHandler GetHandler(Canvas canvas)
        {
            return (s, _) => Redraw(canvas);
        }

        private static void OnItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is Canvas canvas)) return;
            EnsureHooks(canvas);
            if (e.NewValue is IEnumerable items)
                foreach (var it in items)
                    if (it is INotifyPropertyChanged inpc)
                        inpc.PropertyChanged += (s, _) => Redraw(canvas);
            Redraw(canvas);
        }

        private static void OnItemsHostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Canvas canvas) { EnsureHooks(canvas); Redraw(canvas); }
        }

        private static int IndexOfVentil(ItemsControl host, Ventil v)
        {
            for (int i = 0; i < host.Items.Count; i++)
                if (host.Items[i] is NetworkItem ni && ni.Ventil == v) return i;
            return -1;
        }

        private static void Redraw(Canvas canvas)
        {
            canvas.Children.Clear();

            var host = GetItemsHost(canvas);
            var connections = GetConnections(canvas);
            if (host == null || connections == null) return;

            var reference = VisualTreeHelper.GetParent(canvas) as Visual;
            if (reference == null) return;

            if (host.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
            {
                canvas.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, (Action)(() => Redraw(canvas)));
                return;
            }

            foreach (var obj in connections)
            {
                if (!(obj is ConnectionLine conn)) continue;

                int fromIdx = IndexOfVentil(host, conn.From);
                int toIdx = IndexOfVentil(host, conn.To);
                if (fromIdx < 0 || toIdx < 0) continue;

                var fromContainer = host.ItemContainerGenerator.ContainerFromIndex(fromIdx) as FrameworkElement;
                var toContainer = host.ItemContainerGenerator.ContainerFromIndex(toIdx) as FrameworkElement;
                if (fromContainer == null || toContainer == null) continue;

                try
                {
                    var fromCenter = fromContainer.TransformToAncestor(reference)
                        .Transform(new Point(fromContainer.ActualWidth / 2, fromContainer.ActualHeight / 2));
                    var toCenter = toContainer.TransformToAncestor(reference)
                        .Transform(new Point(toContainer.ActualWidth / 2, toContainer.ActualHeight / 2));

                    var lineBrush = new LinearGradientBrush(
                        Color.FromRgb(79, 70, 229), Color.FromRgb(168, 85, 247),
                        new Point(0, 0), new Point(1, 1));

                    canvas.Children.Add(new Line
                    {
                        X1 = fromCenter.X, Y1 = fromCenter.Y,
                        X2 = toCenter.X, Y2 = toCenter.Y,
                        Stroke = new SolidColorBrush(Color.FromArgb(55, 99, 102, 241)),
                        StrokeThickness = 8,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                        SnapsToDevicePixels = true
                    });

                    canvas.Children.Add(new Line
                    {
                        X1 = fromCenter.X, Y1 = fromCenter.Y,
                        X2 = toCenter.X, Y2 = toCenter.Y,
                        Stroke = lineBrush,
                        StrokeThickness = 2.5,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeDashArray = new DoubleCollection { 6, 4 },
                        SnapsToDevicePixels = true
                    });

                    var mid = new Point((fromCenter.X + toCenter.X) / 2, (fromCenter.Y + toCenter.Y) / 2);
                    var node = new Ellipse
                    {
                        Width = 13, Height = 13,
                        Fill = new SolidColorBrush(Colors.White),
                        Stroke = new SolidColorBrush(Color.FromRgb(99, 102, 241)),
                        StrokeThickness = 2.5
                    };
                    Canvas.SetLeft(node, mid.X - 6.5);
                    Canvas.SetTop(node, mid.Y - 6.5);
                    canvas.Children.Add(node);
                }
                catch { }
            }
        }
    }
}
