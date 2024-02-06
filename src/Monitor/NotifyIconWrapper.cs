﻿using System.ComponentModel;
using System.Reflection;
using System.Windows;
using Application = System.Windows.Application;

namespace Monitor;

public class NotifyIconWrapper : FrameworkElement, IDisposable
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(NotifyIconWrapper), new PropertyMetadata(
            (d, e) =>
            {
                var notifyIcon = ((NotifyIconWrapper)d)._notifyIcon;
                if (notifyIcon == null)
                    return;
                notifyIcon.Text = (string)e.NewValue;
            }));

    private static readonly DependencyProperty NotifyRequestProperty =
        DependencyProperty.Register("NotifyRequest", typeof(NotifyRequestRecord), typeof(NotifyIconWrapper),
            new PropertyMetadata(
                (d, e) =>
                {
                    var r = (NotifyRequestRecord)e.NewValue;
                    ((NotifyIconWrapper)d)._notifyIcon?.ShowBalloonTip(r.Duration, r.Title, r.Text, r.Icon);
                }));

    private static readonly RoutedEvent OpenSelectedEvent = EventManager.RegisterRoutedEvent("OpenSelected",
        RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(NotifyIconWrapper));

    private static readonly RoutedEvent ExitSelectedEvent = EventManager.RegisterRoutedEvent("ExitSelected",
        RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(NotifyIconWrapper));

    private NotifyIcon? _notifyIcon;

    public NotifyIconWrapper()
    {
        if (DesignerProperties.GetIsInDesignMode(this))
            return;
        _notifyIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location),
            Visible = true,
            ContextMenuStrip = CreateContextMenu()
        };
        _notifyIcon.DoubleClick += OpenItemOnClick;
        Application.Current.Exit += (obj, args) => { _notifyIcon.Dispose(); };
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public NotifyRequestRecord NotifyRequest
    {
        get => (NotifyRequestRecord)GetValue(NotifyRequestProperty);
        set => SetValue(NotifyRequestProperty, value);
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
    }

    public event RoutedEventHandler OpenSelected
    {
        add => AddHandler(OpenSelectedEvent, value);
        remove => RemoveHandler(OpenSelectedEvent, value);
    }

    public event RoutedEventHandler ExitSelected
    {
        add => AddHandler(ExitSelectedEvent, value);
        remove => RemoveHandler(ExitSelectedEvent, value);
    }

    public void SetIcon(string icon)
    {
        _notifyIcon!.Icon = new Icon(@"Resources\" + icon);
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var openItem = new ToolStripMenuItem("Open");
        openItem.Click += OpenItemOnClick;
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += ExitItemOnClick;
        var contextMenu = new ContextMenuStrip { Items = { openItem, exitItem } };
        return contextMenu;
    }

    private void OpenItemOnClick(object? sender, EventArgs eventArgs)
    {
        var args = new RoutedEventArgs(OpenSelectedEvent);
        RaiseEvent(args);
    }

    private void ExitItemOnClick(object? sender, EventArgs eventArgs)
    {
        var args = new RoutedEventArgs(ExitSelectedEvent);
        RaiseEvent(args);
    }

    public class NotifyRequestRecord
    {
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        public int Duration { get; set; } = 1000;
        public ToolTipIcon Icon { get; set; } = ToolTipIcon.Info;
    }
}
