using Hercules.Controls.AvalonEdit;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using System.Windows.Threading;

namespace Hercules.Controls
{
    public class FoldingBehavior : Behavior<TextEditor>
    {
        BraceFoldingStrategy? foldingStrategy;
        FoldingManager? foldingManager;
        DispatcherTimer? foldingUpdateTimer;

        protected override void OnAttached()
        {
            AssociatedObject.TextArea.DocumentChanged += TextArea_DocumentChanged;
            if (AssociatedObject.TextArea.Document != null)
                Install();
        }

        void TextArea_DocumentChanged(object? sender, EventArgs e)
        {
            Uninstall();
            if (AssociatedObject.TextArea.Document != null)
                Install();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.TextArea.DocumentChanged -= TextArea_DocumentChanged;
            Uninstall();
        }

        bool IsInstalled => foldingManager != null;

        void Install()
        {
            foldingManager = FoldingManager.Install(AssociatedObject.TextArea);
            AssociatedObject.IsVisibleChanged += AssociatedObject_IsVisibleChanged;
            foldingStrategy = new BraceFoldingStrategy();
            if (AssociatedObject.IsVisible)
                StartTimer();
        }

        void Uninstall()
        {
            if (IsInstalled)
            {
                StopTimer();
                FoldingManager.Uninstall(foldingManager);
                AssociatedObject.IsVisibleChanged -= AssociatedObject_IsVisibleChanged;
                foldingStrategy = null;
            }
        }

        void AssociatedObject_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
                StartTimer();
            else if (foldingUpdateTimer != null)
                StopTimer();
        }

        void StartTimer()
        {
            if (foldingUpdateTimer == null)
            {
                UpdateFoldings();
                foldingUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                foldingUpdateTimer.Tick += delegate { UpdateFoldings(); };
                foldingUpdateTimer.Start();
            }
        }

        void StopTimer()
        {
            if (foldingUpdateTimer != null)
            {
                foldingUpdateTimer.Stop();
                foldingUpdateTimer = null;
            }
        }

        void UpdateFoldings()
        {
            foldingStrategy!.UpdateFoldings(foldingManager!, AssociatedObject.Document);
        }
    }
}
