using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Behaviors
{
    /// <summary>
    /// Attached behavior that lets the user scroll straight through a nested scrollable list (e.g. a
    /// ListBox with a MaxHeight, sitting inside a page's own ScrollViewer) instead of getting stuck --
    /// the mouse wheel keeps scrolling the inner list normally until it hits the top/bottom of its own
    /// content, then forwards further scrolling to the nearest ancestor ScrollViewer.
    /// </summary>
    public static class ScrollBubbling
    {
        public static readonly DependencyProperty PassThroughProperty =
            DependencyProperty.RegisterAttached(
                "PassThrough",
                typeof(bool),
                typeof(ScrollBubbling),
                new PropertyMetadata(false, OnPassThroughChanged));

        public static bool GetPassThrough(DependencyObject obj) => (bool)obj.GetValue(PassThroughProperty);
        public static void SetPassThrough(DependencyObject obj, bool value) => obj.SetValue(PassThroughProperty, value);

        private static void OnPassThroughChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element)
                return;

            element.PreviewMouseWheel -= OnPreviewMouseWheel;

            if ((bool)e.NewValue)
                element.PreviewMouseWheel += OnPreviewMouseWheel;
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not DependencyObject source)
                return;

            ScrollViewer? innerScrollViewer = FindDescendantScrollViewer(source);

            bool atScrollLimit = innerScrollViewer == null ||
                (e.Delta > 0 && innerScrollViewer.VerticalOffset <= 0) ||
                (e.Delta < 0 && innerScrollViewer.VerticalOffset >= innerScrollViewer.ScrollableHeight - 0.5);

            if (!atScrollLimit)
                return; // still room to scroll the inner list itself -- let it handle the wheel as usual

            ScrollViewer? outerScrollViewer = FindAncestorScrollViewer(source);
            if (outerScrollViewer == null)
                return;

            e.Handled = true;
            outerScrollViewer.ScrollToVerticalOffset(outerScrollViewer.VerticalOffset - e.Delta);
        }

        private static ScrollViewer? FindDescendantScrollViewer(DependencyObject parent)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is ScrollViewer scrollViewer)
                    return scrollViewer;

                ScrollViewer? found = FindDescendantScrollViewer(child);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static ScrollViewer? FindAncestorScrollViewer(DependencyObject current)
        {
            DependencyObject? node = VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current);
            while (node != null)
            {
                if (node is ScrollViewer scrollViewer)
                    return scrollViewer;

                node = VisualTreeHelper.GetParent(node) ?? LogicalTreeHelper.GetParent(node);
            }
            return null;
        }
    }
}
