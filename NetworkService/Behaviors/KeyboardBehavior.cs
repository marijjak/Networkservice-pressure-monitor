using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace NetworkService.Behaviors
{
    public static class KeyboardBehavior
    {
        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.RegisterAttached("DeleteCommand", typeof(ICommand),
                typeof(KeyboardBehavior), new PropertyMetadata(null, OnAnyCommandSet));
        public static void SetDeleteCommand(DependencyObject o, ICommand v) => o.SetValue(DeleteCommandProperty, v);
        public static ICommand GetDeleteCommand(DependencyObject o) => (ICommand)o.GetValue(DeleteCommandProperty);

        public static readonly DependencyProperty EscapeCommandProperty =
            DependencyProperty.RegisterAttached("EscapeCommand", typeof(ICommand),
                typeof(KeyboardBehavior), new PropertyMetadata(null, OnAnyCommandSet));
        public static void SetEscapeCommand(DependencyObject o, ICommand v) => o.SetValue(EscapeCommandProperty, v);
        public static ICommand GetEscapeCommand(DependencyObject o) => (ICommand)o.GetValue(EscapeCommandProperty);

        public static readonly DependencyProperty EnterCommandProperty =
            DependencyProperty.RegisterAttached("EnterCommand", typeof(ICommand),
                typeof(KeyboardBehavior), new PropertyMetadata(null, OnAnyCommandSet));
        public static void SetEnterCommand(DependencyObject o, ICommand v) => o.SetValue(EnterCommandProperty, v);
        public static ICommand GetEnterCommand(DependencyObject o) => (ICommand)o.GetValue(EnterCommandProperty);

        public static readonly DependencyProperty CtrlEnterCommandProperty =
            DependencyProperty.RegisterAttached("CtrlEnterCommand", typeof(ICommand),
                typeof(KeyboardBehavior), new PropertyMetadata(null, OnAnyCommandSet));
        public static void SetCtrlEnterCommand(DependencyObject o, ICommand v) => o.SetValue(CtrlEnterCommandProperty, v);
        public static ICommand GetCtrlEnterCommand(DependencyObject o) => (ICommand)o.GetValue(CtrlEnterCommandProperty);

        public static readonly DependencyProperty CtrlECommandProperty =
            DependencyProperty.RegisterAttached("CtrlECommand", typeof(ICommand),
                typeof(KeyboardBehavior), new PropertyMetadata(null, OnAnyCommandSet));
        public static void SetCtrlECommand(DependencyObject o, ICommand v) => o.SetValue(CtrlECommandProperty, v);
        public static ICommand GetCtrlECommand(DependencyObject o) => (ICommand)o.GetValue(CtrlECommandProperty);

        private static readonly DependencyProperty HookedProperty =
            DependencyProperty.RegisterAttached("Hooked", typeof(bool),
                typeof(KeyboardBehavior), new PropertyMetadata(false));

        private static void OnAnyCommandSet(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is UIElement el)) return;
            if ((bool)el.GetValue(HookedProperty)) return;
            el.SetValue(HookedProperty, true);
            el.PreviewKeyDown += OnPreviewKeyDown;
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var el = (DependencyObject)sender;
            var mods = Keyboard.Modifiers;
            bool inTextBox = Keyboard.FocusedElement is TextBoxBase;
            ICommand cmd = null;

            if (e.Key == Key.Enter && mods == ModifierKeys.Control)
                cmd = GetCtrlEnterCommand(el);
            else if (e.Key == Key.Enter && mods == ModifierKeys.None)
                cmd = GetEnterCommand(el);
            else if (e.Key == Key.Delete && mods == ModifierKeys.None && !inTextBox)
                cmd = GetDeleteCommand(el);
            else if (e.Key == Key.Escape && mods == ModifierKeys.None)
                cmd = GetEscapeCommand(el);
            else if (e.Key == Key.E && mods == ModifierKeys.Control)
                cmd = GetCtrlECommand(el);

            if (cmd != null && cmd.CanExecute(null))
            {
                cmd.Execute(null);
                e.Handled = true;
            }
        }
    }
}
