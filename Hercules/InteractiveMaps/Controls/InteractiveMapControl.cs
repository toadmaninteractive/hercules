using Json;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Telerik.Windows.Controls.Diagrams;
using Telerik.Windows.Diagrams.Core;
using ICommand = System.Windows.Input.ICommand;

namespace Hercules.InteractiveMaps
{
    public class InteractiveMapControl : RadDiagram
    {
        public static readonly DependencyProperty IsDrawingNewShapeProperty = DependencyProperty.Register("IsDrawingNewShape", typeof(bool), typeof(InteractiveMapControl), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty CreateNewBlockCommandProperty = DependencyProperty.Register("CreateNewBlockCommand", typeof(ICommand), typeof(InteractiveMapControl), new FrameworkPropertyMetadata(null));

        public bool IsDrawingNewShape
        {
            get => (bool)GetValue(IsDrawingNewShapeProperty);
            set => SetValue(IsDrawingNewShapeProperty, value);
        }

        public ICommand CreateNewBlockCommand
        {
            get => (ICommand)GetValue(CreateNewBlockCommandProperty);
            set => SetValue(CreateNewBlockCommandProperty, value);
        }

        Point selectionStartPosition;
        int lastZIndex = 1;
        private static bool inputBindingsInitialized = false;

        public InteractiveMapControl()
        {
            // Fix zoom
            DiagramConstants.MinimumZoom = 1;
            DiagramConstants.MaximumZoom = 1;

            PreviewMouseDown += PreviewMouseDownHandler;    // TODO: unsubscribe
            PreviewMouseUp += PreviewMouseUpHandler;        // TODO: unsubscribe
            ItemsChanging += ItemsChangingHandler;          // TODO: unsubscribe
            SizeChanged += SizeChangedHandler;              // TODO: unsubscribe
            ServiceLocator.Register(new DisabledUndoRedoService() as IUndoRedoService);
            OverrideInputBindings();
        }

        private void OverrideInputBindings()
        {
            if (inputBindingsInitialized)
                return;

            CommandManager.RegisterClassInputBinding(GetType(),
                new InputBinding(ApplicationCommands.Undo, new KeyGesture(Key.Z, ModifierKeys.Control)));

            CommandManager.RegisterClassInputBinding(GetType(),
                new InputBinding(ApplicationCommands.Redo, new KeyGesture(Key.Y, ModifierKeys.Control)));

            inputBindingsInitialized = true;
        }

        private double NormalizeXAxisValue(double value)
        {
            return ActualWidth > 0 ? value / ActualWidth : 0.0;
        }

        private double NormalizeYAxisValue(double value)
        {
            return ActualHeight > 0 ? value / ActualHeight : 0.0;
        }

        public double ScaleXAxisValue(double value)
        {
            return value * ActualWidth;
        }

        public double ScaleYAxisValue(double value)
        {
            return value * ActualHeight;
        }

        void PreviewMouseUpHandler(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt) || IsDrawingNewShape)
            {
                // Get current position
                var currentPosition = e.GetPosition(this);

                // Define top left point
                var topLeft = new Point(
                    currentPosition.X < selectionStartPosition.X ? currentPosition.X : selectionStartPosition.X,
                    currentPosition.Y < selectionStartPosition.Y ? currentPosition.Y : selectionStartPosition.Y);

                // Define bottom right point
                var bottomRight = new Point(
                    currentPosition.X > selectionStartPosition.X ? currentPosition.X : selectionStartPosition.X,
                    currentPosition.Y > selectionStartPosition.Y ? currentPosition.Y : selectionStartPosition.Y);

                // Turn off shape drawing mode
                IsDrawingNewShape = false;

                // Define new block
                var propBlockJson = new JsonObject()
                {
                    { "pos", new JsonObject()
                        {
                            { "left", NormalizeXAxisValue(topLeft.X) },
                            { "top", NormalizeYAxisValue(topLeft.Y) },
                            { "right", NormalizeXAxisValue(bottomRight.X) },
                            { "bottom", NormalizeYAxisValue(bottomRight.Y) }
                        }
                    },
                    { "z_index", lastZIndex++ }
                };

                // Execute related command
                if (CreateNewBlockCommand != null && CreateNewBlockCommand.CanExecute(propBlockJson))
                    CreateNewBlockCommand.Execute(propBlockJson);
            }
        }

        void PreviewMouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt) || IsDrawingNewShape)
                selectionStartPosition = e.GetPosition(this);
        }

        void SizeChangedHandler(object sender, SizeChangedEventArgs e)
        {
            if (GraphSource?.Items == null)
                return;

            foreach (var b in GraphSource.Items.OfType<PropertyBlockShape>())
            {
                b.UpdateScalingFactors(ActualWidth, ActualHeight);
            }
        }

        void ItemsChangingHandler(object? sender, DiagramItemsChangingEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var b in e.NewItems.OfType<PropertyBlockShape>())
                    {
                        b.UpdateScalingFactors(ActualWidth, ActualHeight);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
            }
        }
    }
}
