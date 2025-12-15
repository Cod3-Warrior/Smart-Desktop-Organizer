using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace SmartDesktopOrganizer.Controls;

/// <summary>
/// Adorner that shows a semi-transparent copy of the dragged element following the cursor
/// </summary>
public class DragAdorner : Adorner
{
    private readonly VisualBrush _visualBrush;
    private readonly Size _size;
    private Point _location;

    public DragAdorner(UIElement adornedElement, UIElement draggedElement, Point startPoint) 
        : base(adornedElement)
    {
        _size = new Size(draggedElement.RenderSize.Width, draggedElement.RenderSize.Height);
        _location = startPoint;
        
        _visualBrush = new VisualBrush(draggedElement)
        {
            Opacity = 0.7,
            Stretch = Stretch.None
        };
        
        IsHitTestVisible = false;
    }

    public void UpdatePosition(Point location)
    {
        _location = location;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var rect = new Rect(
            _location.X - _size.Width / 2,
            _location.Y - _size.Height / 2,
            _size.Width,
            _size.Height);
        
        // Draw drop shadow
        var shadowRect = new Rect(rect.X + 3, rect.Y + 3, rect.Width, rect.Height);
        drawingContext.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(50, 0, 0, 0)),
            null,
            shadowRect);
        
        // Draw the dragged element
        drawingContext.DrawRectangle(_visualBrush, null, rect);
    }
}
