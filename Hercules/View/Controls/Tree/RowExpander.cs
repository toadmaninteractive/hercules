﻿using System.Windows;
using System.Windows.Controls;

namespace Hercules.Controls.Tree
{
    public class RowExpander : Control
    {
        static RowExpander()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RowExpander), new FrameworkPropertyMetadata(typeof(RowExpander)));
        }
    }
}
