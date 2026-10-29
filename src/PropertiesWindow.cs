using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace LTOG;

/// <summary>Explorer-style properties of a search result: General tab, then its Cartridge.</summary>
internal static class PropertiesWindow
{
    public static Window Open(CatalogNode node)
    {
        string name = node.Parent == null ? "全部磁带" : node.Name;
        var w = new Window { Title = $"{name} 的属性", SystemBackdrop = new MicaBackdrop() };
        w.AppWindow.Resize(new Windows.Graphics.SizeInt32(480, 640));
        var ico = Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico");
        if (File.Exists(ico)) w.AppWindow.SetIcon(ico);

        // the same path can be on several tapes; each copy has its own file and cartridge details
        var copy = new ComboBox
        {
            Header = "磁带上的副本",
            ItemsSource = node.Copies.Select(c => c.Tape.Label).ToList(),
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Visibility = node.Copies.Count > 1 ? Visibility.Visible : Visibility.Collapsed,
            Margin = new Thickness(0, 0, 0, 12),
        };
        var general = new StackPanel { Spacing = 8 };
        var cartridge = new StackPanel { Spacing = 8 };
        var tabs = new TabView
        {
            IsAddTabButtonVisible = false, CanReorderTabs = false, CanDragTabs = false,
            TabWidthMode = TabViewWidthMode.SizeToContent,
        };
        foreach (var (header, body) in new[] { ("常规", general), ("磁带", cartridge) })
            tabs.TabItems.Add(new TabViewItem
            {
                Header = header, IsClosable = false,
                Content = new ScrollViewer
                {
                    Content = body, Padding = new Thickness(16),
                    Background = (Brush)Application.Current.Resources["TabViewItemHeaderBackgroundSelected"],
                    BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                    BorderThickness = new Thickness(1, 0, 1, 1),
                    CornerRadius = new CornerRadius(0, 0, 8, 8),
                },
            });
        tabs.SelectedIndex = 0;
        var ok = new Button
        {
            Content = "确定", MinWidth = 90, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0),
            Style = (Style)Application.Current.Resources["AccentButtonStyle"],
        };
        ok.KeyboardAccelerators.Add(new KeyboardAccelerator { Key = Windows.System.VirtualKey.Enter });
        ok.Click += (_, _) => w.Close();

        void Render()
        {
            general.Children.Clear();
            cartridge.Children.Clear();
            if (node.Copies.Count == 0) return;
            var (tape, src) = node.Copies[Math.Max(0, copy.SelectedIndex)];
            GeneralTab(general, node, src, name);
            CartridgeTab(cartridge, tape);
        }
        copy.SelectionChanged += (_, _) => Render();
        Render();

        // no RowSpacing: it would still pad around the collapsed copy picker
        var root = new Grid { Language = "zh-CN", Padding = new Thickness(16) };
        var esc = new KeyboardAccelerator { Key = Windows.System.VirtualKey.Escape };
        esc.Invoked += (_, a) => { a.Handled = true; w.Close(); };
        root.KeyboardAccelerators.Add(esc);
        foreach (var h in new[] { GridLength.Auto, new GridLength(1, GridUnitType.Star), GridLength.Auto })
            root.RowDefinitions.Add(new RowDefinition { Height = h });
        foreach (var (el, row) in new (FrameworkElement, int)[] { (copy, 0), (tabs, 1), (ok, 2) })
        {
            Grid.SetRow(el, row);
            root.Children.Add(el);
        }
        w.Content = root;
        w.Activate();
        return w;
    }

    private static void GeneralTab(Panel p, CatalogNode node, SchemaNode src, string name)
    {
        Head(p, node.IsDirectory ? "" : "", name);
        Divider(p);
        string ext = Path.GetExtension(node.Name).TrimStart('.').ToUpperInvariant();
        Row(p, "类型", node.IsDirectory ? "文件夹" : ext == "" ? "文件" : $"{ext} 文件");
        Row(p, "位置", node.Parent?.Path);
        Row(p, "大小", SchemaSnapshot.FormatBytes(node.IsDirectory ? node.Size : src.Length ?? 0));
        if (node.IsDirectory)
        {
            int dirs = node.Descendants().Count(n => n.IsDirectory), files = node.Descendants().Count() - dirs;
            Row(p, "包含", $"{files:N0} 个文件，{dirs:N0} 个文件夹");
        }
        Row(p, "所在磁带", string.Join(", ", node.Copies.Select(c => c.Tape.Label)));
        Divider(p);
        Row(p, "创建时间", SchemaDisplay.Time(src.CreationTime));
        Row(p, "修改时间", SchemaDisplay.Time(src.ModifyTime));
        Row(p, "访问时间", SchemaDisplay.Time(src.AccessTime));
        Row(p, "备份时间", SchemaDisplay.Time(src.BackupTime));
        Divider(p);
        Row(p, "文件 ID", src.FileUid);
        if (!node.IsDirectory) Row(p, "磁带位置", SchemaDisplay.Location(src.FirstLocationText));
        p.Children.Add(new CheckBox { Content = "只读", IsChecked = src.ReadOnly, IsEnabled = false });
    }

    private static void CartridgeTab(Panel p, CatalogTape tape)
    {
        var s = tape.Snapshot;
        var rec = tape.Record;
        Head(p, "", tape.Label);
        Divider(p);
        Row(p, "卷标", s.Root.Name is "/" or "(empty index)" ? null : s.Root.Name);
        Row(p, "条码", rec?.Barcode);
        Row(p, "序列号", rec?.Serial);
        Row(p, "容量", rec == null ? null : SchemaSnapshot.FormatBytes(rec.Capacity));
        Row(p, "可用空间", rec == null ? null : SchemaSnapshot.FormatBytes(rec.Free));
        Row(p, "代际", rec?.Generation);
        Row(p, "空间记录时间", rec?.SeenUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"));
        Divider(p);
        Row(p, "卷 UUID", s.VolumeUuid);
        Row(p, "格式版本", s.FormatVersion);
        Row(p, "索引写入程序", s.Creator);
        Row(p, "索引修订号", s.GenerationNumber);
        Row(p, "更新时间", SchemaDisplay.Time(s.UpdatedText));
        Row(p, "最大文件 ID", s.HighestFileUid);
        Row(p, "策略覆盖", SchemaDisplay.Policy(s.PolicyOverrideText));
        Row(p, "卷锁定状态", s.VolumeLockState);
        Divider(p);
        Row(p, "索引文件", s.Path);
    }

    private static void Head(Panel p, string glyph, string title)
    {
        var g = new Grid { ColumnSpacing = 16 };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        g.ColumnDefinitions.Add(new ColumnDefinition());
        g.Children.Add(new FontIcon { Glyph = glyph, FontSize = 32, HorizontalAlignment = HorizontalAlignment.Left });
        var box = new TextBox { Text = title, IsReadOnly = true, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(box, 1);
        g.Children.Add(box);
        p.Children.Add(g);
    }

    private static void Row(Panel p, string label, string? value)
    {
        var g = new Grid { ColumnSpacing = 16 };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        g.ColumnDefinitions.Add(new ColumnDefinition());
        g.Children.Add(new TextBlock { Text = label + ":", Opacity = 0.75 });
        var v = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(value) ? "未指定" : value,
            TextWrapping = TextWrapping.Wrap, IsTextSelectionEnabled = true,
        };
        Grid.SetColumn(v, 1);
        g.Children.Add(v);
        p.Children.Add(g);
    }

    private static void Divider(Panel p) => p.Children.Add(new Border
    {
        Height = 1, Margin = new Thickness(0, 4, 0, 4),
        Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
    });
}
