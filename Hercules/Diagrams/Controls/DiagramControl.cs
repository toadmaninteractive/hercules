using Hercules.Diagrams.View;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Telerik.Windows.Controls;
using Telerik.Windows.Diagrams.Core;

namespace Hercules.Diagrams
{
    public class DiagramControl : RadDiagram
    {
        Point previousPosition;

        public DiagramControl()
        {
            this.MouseMove += MouseMoveHandler;
            this.PreviewMouseDown += PreviewMouseDownHandler;
            this.PreviewMouseUp += PreviewMouseUpHandler;
            this.ServiceLocator.Register(new DisabledUndoRedoService() as IUndoRedoService);
            AllowCopy = false;
            AllowCut = false;
            AllowPaste = false;
            AllowDelete = false;

            OverrideInputBindings();
        }

        private static bool inputBindingsInitialized = false;

        private void OverrideInputBindings()
        {
            if (inputBindingsInitialized)
                return;
            RegisterInputBinding(ApplicationCommands.Undo);
            RegisterInputBinding(ApplicationCommands.Redo);
            RegisterInputBinding(ApplicationCommands.Copy);
            RegisterInputBinding(ApplicationCommands.Cut);
            RegisterInputBinding(ApplicationCommands.Delete);
            RegisterInputBinding(ApplicationCommands.Paste);
            inputBindingsInitialized = true;
        }

        private void RegisterInputBinding(RoutedUICommand command)
        {
            foreach (var gesture in command.InputGestures.Cast<InputGesture>())
                CommandManager.RegisterClassInputBinding(GetType(), new InputBinding(command, gesture));
        }

        void PreviewMouseUpHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton != MouseButtonState.Pressed)
                Mouse.OverrideCursor = default;
        }

        void PreviewMouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                previousPosition = e.GetPosition(this);
                Mouse.OverrideCursor = Cursors.Hand;
            }
        }

        void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(this);
                Position += currentPosition - previousPosition;
                previousPosition = currentPosition;
            }
        }

        protected override IShape GetShapeContainerForItemOverride(object item)
        {
            if (item is BlockBase block)
                return new BlockView(block.Prototype);
            else
                return base.GetShapeContainerForItemOverride(item);
        }
    }
}