using AlbionDpsMeter.Enums;
using AlbionDpsMeter.Models;
using AlbionDpsMeter.Services;
using AlbionDpsMeter.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;

namespace AlbionDpsMeter.Views;

public sealed class MainWindow : Window
{
    private readonly DamageMeterViewModel _viewModel;
    private readonly ItemController _itemController;
    private readonly ImageController _imageController;
    private readonly AppWindow _appWindow;
    private readonly OverlappedPresenter _presenter;
    private bool _isCompactOverlay;

    // Color palette
    private static readonly Color DamageColor = Color.FromArgb(0xFF, 0xFF, 0x6B, 0x6B);
    private static readonly Color HealColor = Color.FromArgb(0xFF, 0x51, 0xCF, 0x66);
    private static readonly Color TakenDamageColor = Color.FromArgb(0xFF, 0xFF, 0xD4, 0x3B);
    private static readonly Color FameColor = Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00);
    private static readonly Color CombatFameColor = Color.FromArgb(0xFF, 0xFF, 0x9A, 0x3C);
    private static readonly Color SilverColor = Color.FromArgb(0xFF, 0xC0, 0xC0, 0xC8);

    // Brushes
    private readonly SolidColorBrush _damageBrush;
    private readonly SolidColorBrush _healBrush;
    private readonly SolidColorBrush _barBgBrush;

    // Named elements for updates
    private readonly Grid _titleBar;
    private readonly FontIcon _pinIcon;
    private readonly FontIcon _compactIcon;
    private readonly FontIcon _startIcon;
    private readonly TextBlock _startText;
    private readonly TextBlock _totalDamageText;
    private readonly TextBlock _totalHealText;
    private readonly TextBlock _totalTakenDamageText;
    private readonly TextBlock _totalFameText;
    private readonly TextBlock _totalCombatFameText;
    private readonly TextBlock _totalSilverText;
    private readonly StackPanel _playerListPanel;
    private readonly TextBlock _statusText;
    private readonly TextBlock _errorText;
    private readonly TextBlock _playerNameText;
    private readonly TextBlock _guildNameText;
    private readonly TextBlock _partyCountText;
    private readonly Grid _profileBar;
    private readonly Ellipse _trackingIndicator;
    private Button _copyBtn = null!;

    // Player row tracking
    private readonly Dictionary<Guid, PlayerRow> _playerRows = new();

    public MainWindow(DamageMeterViewModel viewModel, ItemController itemController, ImageController imageController)
    {
        _viewModel = viewModel;
        _itemController = itemController;
        _imageController = imageController;

        // Create brushes
        _damageBrush = new SolidColorBrush(Color.FromArgb(0xDD, DamageColor.R, DamageColor.G, DamageColor.B));
        _healBrush = new SolidColorBrush(Color.FromArgb(0xDD, HealColor.R, HealColor.G, HealColor.B));
        _barBgBrush = new SolidColorBrush(Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF));

        // Initialize named elements
        _titleBar = new Grid();
        _pinIcon = new FontIcon { Glyph = "\uE718", FontSize = 13 };
        _compactIcon = new FontIcon { Glyph = "\uE740", FontSize = 13 };
        _startIcon = new FontIcon { Glyph = "\uE769", FontSize = 11 };
        _startText = new TextBlock { Text = "Pause", FontSize = 12 };
        _totalDamageText = new TextBlock { Foreground = new SolidColorBrush(DamageColor), Text = "0", FontWeight = FontWeights.SemiBold, FontSize = 13 };
        _totalHealText = new TextBlock { Foreground = new SolidColorBrush(HealColor), Text = "0", FontWeight = FontWeights.SemiBold, FontSize = 13 };
        _totalTakenDamageText = new TextBlock { Foreground = new SolidColorBrush(TakenDamageColor), Text = "0", FontWeight = FontWeights.SemiBold, FontSize = 13 };
        _totalFameText = new TextBlock { Foreground = new SolidColorBrush(FameColor), Text = "0", FontWeight = FontWeights.SemiBold, FontSize = 13 };
        _totalCombatFameText = new TextBlock { Foreground = new SolidColorBrush(CombatFameColor), Text = "0", FontWeight = FontWeights.SemiBold, FontSize = 13 };
        _totalSilverText = new TextBlock { Foreground = new SolidColorBrush(SilverColor), Text = "0", FontWeight = FontWeights.SemiBold, FontSize = 13 };
        _playerListPanel = new StackPanel { Spacing = 4 };
        _statusText = new TextBlock { Opacity = 0.4, FontSize = 11, Text = "Ready" };
        _errorText = new TextBlock { Foreground = new SolidColorBrush(DamageColor), FontSize = 11 };
        _playerNameText = new TextBlock { FontSize = 12, FontWeight = FontWeights.SemiBold };
        _guildNameText = new TextBlock { FontSize = 11, Opacity = 0.5 };
        _partyCountText = new TextBlock { FontSize = 11, Opacity = 0.5 };
        _profileBar = BuildProfileBar();
        _trackingIndicator = new Ellipse
        {
            Width = 7,
            Height = 7,
            Fill = new SolidColorBrush(Color.FromArgb(0xFF, 0x88, 0x88, 0x88)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 6, 0)
        };

        // Build the UI tree
        Content = BuildRootLayout();

        // Setup AppWindow
        IntPtr hWnd = WindowNative.GetWindowHandle(this);
        var wId = Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow = AppWindow.GetFromWindowId(wId);
        _presenter = (OverlappedPresenter)_appWindow.Presenter;
        _appWindow.Title = "Albion DPS Meter";
        _appWindow.Resize(new SizeInt32(420, 620));

        // Mica backdrop + custom title bar
        SystemBackdrop = new MicaBackdrop();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(_titleBar);

        // Subscribe to ViewModel events
        _viewModel.OnUiRefreshRequired += RefreshPlayerList;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // Auto-start tracking on launch
        _viewModel.StartTrackingCommand.Execute(null);

        // Cleanup on close
        Closed += (_, _) => _viewModel.StopTrackingCommand.Execute(null);
    }

    #region UI Building

    private Grid BuildRootLayout()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var titleBar = BuildTitleBar();
        Grid.SetRow(titleBar, 0);
        root.Children.Add(titleBar);

        var content = BuildContentArea();
        Grid.SetRow(content, 1);
        root.Children.Add(content);

        return root;
    }

    private Grid BuildTitleBar()
    {
        _titleBar.Height = 40;
        _titleBar.Padding = new Thickness(16, 0, 0, 0);
        _titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        // Reserve space for system caption buttons (minimize / maximize / close)
        _titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(138) });

        var title = new TextBlock
        {
            Text = "Albion DPS Meter",
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12,
            Opacity = 0.6,
            FontWeight = FontWeights.SemiBold
        };
        Grid.SetColumn(title, 0);
        _titleBar.Children.Add(title);

        var pinBtn = CreateTitleBarButton(_pinIcon, "Always on Top");
        pinBtn.Click += PinButton_Click;
        Grid.SetColumn(pinBtn, 1);
        _titleBar.Children.Add(pinBtn);

        var compactBtn = CreateTitleBarButton(_compactIcon, "Compact Overlay");
        compactBtn.Click += CompactButton_Click;
        Grid.SetColumn(compactBtn, 2);
        _titleBar.Children.Add(compactBtn);

        // Subtle bottom border
        var borderLine = new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.FromArgb(0x15, 0xFF, 0xFF, 0xFF)),
            VerticalAlignment = VerticalAlignment.Bottom
        };
        Grid.SetColumnSpan(borderLine, 4);
        _titleBar.Children.Add(borderLine);

        return _titleBar;
    }

    private static Button CreateTitleBarButton(FontIcon icon, string tooltip)
    {
        var btn = new Button
        {
            Width = 36,
            Height = 32,
            Padding = new Thickness(0),
            Background = new SolidColorBrush(Colors.Transparent),
            BorderThickness = new Thickness(0),
            Content = icon,
            Opacity = 0.7
        };
        ToolTipService.SetToolTip(btn, tooltip);
        return btn;
    }

    private Grid BuildContentArea()
    {
        var content = new Grid { Padding = new Thickness(10, 4, 10, 8) };
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var controls = BuildControlsBar();
        Grid.SetRow(controls, 0);
        content.Children.Add(controls);

        _profileBar.Visibility = Visibility.Collapsed;
        Grid.SetRow(_profileBar, 1);
        content.Children.Add(_profileBar);

        var totals = BuildTotalsHeader();
        Grid.SetRow(totals, 2);
        content.Children.Add(totals);

        var playerList = new ScrollViewer
        {
            Content = _playerListPanel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(0, 2, 0, 0)
        };
        Grid.SetRow(playerList, 3);
        content.Children.Add(playerList);

        var status = BuildStatusBar();
        Grid.SetRow(status, 4);
        content.Children.Add(status);

        return content;
    }

    private Grid BuildControlsBar()
    {
        var grid = new Grid
        {
            Margin = new Thickness(0, 2, 0, 6),
            Padding = new Thickness(6, 6, 6, 6),
            CornerRadius = new CornerRadius(6)
        };
        if (Application.Current.Resources.TryGetValue("CardBackgroundFillColorSecondaryBrush", out var bgObj) && bgObj is Brush bgBrush)
            grid.Background = bgBrush;

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Start/Stop
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Reset
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Copy
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Sort
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Spacer

        // Start/Stop button
        var startPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        startPanel.Children.Add(_startIcon);
        startPanel.Children.Add(_startText);

        var startBtn = new Button
        {
            Content = startPanel,
            Height = 32,
            Padding = new Thickness(14, 4, 14, 4),
            Margin = new Thickness(0, 0, 4, 0),
            CornerRadius = new CornerRadius(6)
        };
        if (Application.Current.Resources.TryGetValue("AccentButtonStyle", out var accentStyle))
            startBtn.Style = (Style)accentStyle;
        startBtn.Click += StartStopButton_Click;
        Grid.SetColumn(startBtn, 0);
        grid.Children.Add(startBtn);

        // Reset button
        var resetBtn = CreateIconButton("\uE72C", "Reset damage meter");
        resetBtn.Click += (_, _) => _viewModel.ResetDamageMeterCommand.Execute(null);
        Grid.SetColumn(resetBtn, 1);
        grid.Children.Add(resetBtn);

        // Copy button
        _copyBtn = CreateIconButton("\uE8C8", "Copy to clipboard");
        _copyBtn.Click += (_, _) => CopyDamageMeterToClipboard();
        Grid.SetColumn(_copyBtn, 2);
        grid.Children.Add(_copyBtn);

        // Sort combo
        var sortCombo = new ComboBox
        {
            Height = 32,
            CornerRadius = new CornerRadius(6),
            MinWidth = 90
        };
        sortCombo.Items.Add("Damage");
        sortCombo.Items.Add("DPS");
        sortCombo.Items.Add("Heal");
        sortCombo.Items.Add("HPS");
        sortCombo.Items.Add("Name");
        sortCombo.Items.Add("Taken Dmg");
        sortCombo.SelectedIndex = 0;
        ToolTipService.SetToolTip(sortCombo, "Sort by");
        sortCombo.SelectionChanged += (_, _) =>
        {
            if (sortCombo.SelectedIndex >= 0)
                _viewModel.SortType = (DamageMeterSortType)sortCombo.SelectedIndex;
        };
        Grid.SetColumn(sortCombo, 3);
        grid.Children.Add(sortCombo);

        return grid;
    }

    private static Button CreateIconButton(string glyph, string tooltip)
    {
        var btn = new Button
        {
            Content = new FontIcon { Glyph = glyph, FontSize = 12 },
            Height = 32,
            Width = 36,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 4, 0),
            CornerRadius = new CornerRadius(6)
        };
        ToolTipService.SetToolTip(btn, tooltip);
        return btn;
    }

    private Grid BuildProfileBar()
    {
        var grid = new Grid
        {
            Padding = new Thickness(10, 8, 10, 8),
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0, 0, 0, 4)
        };

        if (Application.Current.Resources.TryGetValue("CardBackgroundFillColorDefaultBrush", out var bgObj) && bgObj is Brush bgBrush)
            grid.Background = bgBrush;
        else
            grid.Background = new SolidColorBrush(Color.FromArgb(0x12, 0xFF, 0xFF, 0xFF));

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var namePanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        namePanel.Children.Add(new FontIcon { Glyph = "\uE77B", FontSize = 14, Opacity = 0.6 });
        namePanel.Children.Add(_playerNameText);
        Grid.SetColumn(namePanel, 0);
        grid.Children.Add(namePanel);

        var guildPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 12, 0) };
        guildPanel.Children.Add(new FontIcon { Glyph = "\uE902", FontSize = 11, Opacity = 0.4 });
        guildPanel.Children.Add(_guildNameText);
        Grid.SetColumn(guildPanel, 1);
        grid.Children.Add(guildPanel);

        var partyPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, VerticalAlignment = VerticalAlignment.Center };
        partyPanel.Children.Add(new FontIcon { Glyph = "\uE716", FontSize = 12, Opacity = 0.4 });
        partyPanel.Children.Add(_partyCountText);
        Grid.SetColumn(partyPanel, 2);
        grid.Children.Add(partyPanel);

        return grid;
    }

    private Grid BuildTotalsHeader()
    {
        var grid = new Grid
        {
            Padding = new Thickness(10, 8, 10, 8),
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0, 0, 0, 4)
        };

        if (Application.Current.Resources.TryGetValue("CardBackgroundFillColorDefaultBrush", out var bgObj) && bgObj is Brush bgBrush)
            grid.Background = bgBrush;
        else
            grid.Background = new SolidColorBrush(Color.FromArgb(0x12, 0xFF, 0xFF, 0xFF));

        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Row 0: combat stats
        var totalLabel = new TextBlock
        {
            Text = "TOTALS",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.4,
            CharacterSpacing = 60
        };
        Grid.SetRow(totalLabel, 0);
        Grid.SetColumn(totalLabel, 0);
        grid.Children.Add(totalLabel);

        var dmgPanel = BuildTotalColumn("DMG", DamageColor, _totalDamageText);
        Grid.SetRow(dmgPanel, 0); Grid.SetColumn(dmgPanel, 1);
        grid.Children.Add(dmgPanel);

        var healPanel = BuildTotalColumn("HEAL", HealColor, _totalHealText);
        Grid.SetRow(healPanel, 0); Grid.SetColumn(healPanel, 2);
        grid.Children.Add(healPanel);

        var takenPanel = BuildTotalColumn("TAKEN", TakenDamageColor, _totalTakenDamageText);
        Grid.SetRow(takenPanel, 0); Grid.SetColumn(takenPanel, 3);
        grid.Children.Add(takenPanel);

        // Row 1: separator
        var separator = new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF)),
            Margin = new Thickness(0, 4, 0, 2)
        };
        Grid.SetRow(separator, 1);
        Grid.SetColumnSpan(separator, 4);
        grid.Children.Add(separator);

        // Row 2: fame/silver stats
        var famePanel = BuildTotalColumn("FAME", FameColor, _totalFameText);
        Grid.SetRow(famePanel, 2); Grid.SetColumn(famePanel, 1);
        grid.Children.Add(famePanel);

        var combatFamePanel = BuildTotalColumn("C.FAME", CombatFameColor, _totalCombatFameText);
        Grid.SetRow(combatFamePanel, 2); Grid.SetColumn(combatFamePanel, 2);
        grid.Children.Add(combatFamePanel);

        var silverPanel = BuildTotalColumn("SILVER", SilverColor, _totalSilverText);
        Grid.SetRow(silverPanel, 2); Grid.SetColumn(silverPanel, 3);
        grid.Children.Add(silverPanel);

        return grid;
    }

    private static StackPanel BuildTotalColumn(string label, Color dotColor, TextBlock valueText)
    {
        var panel = new StackPanel
        {
            Spacing = 1,
            Margin = new Thickness(10, 0, 2, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var labelRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
        labelRow.Children.Add(new Ellipse
        {
            Width = 6,
            Height = 6,
            Fill = new SolidColorBrush(dotColor),
            VerticalAlignment = VerticalAlignment.Center
        });
        labelRow.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 9,
            Opacity = 0.5,
            FontWeight = FontWeights.SemiBold,
            CharacterSpacing = 40
        });
        panel.Children.Add(labelRow);

        valueText.VerticalAlignment = VerticalAlignment.Center;
        valueText.HorizontalAlignment = HorizontalAlignment.Center;
        valueText.Margin = new Thickness(0);
        panel.Children.Add(valueText);

        return panel;
    }

    private Grid BuildStatusBar()
    {
        var grid = new Grid
        {
            Padding = new Thickness(6, 6, 6, 2),
            Margin = new Thickness(0, 4, 0, 0)
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Grid.SetColumn(_trackingIndicator, 0);
        grid.Children.Add(_trackingIndicator);

        Grid.SetColumn(_statusText, 1);
        grid.Children.Add(_statusText);

        Grid.SetColumn(_errorText, 2);
        grid.Children.Add(_errorText);

        var credit = new TextBlock
        {
            Text = "Made by Akashi",
            FontSize = 10,
            Opacity = 0.45,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
            IsHitTestVisible = false
        };
        Grid.SetColumn(credit, 3);
        grid.Children.Add(credit);

        return grid;
    }

    #endregion

    #region Clipboard

    private void CopyDamageMeterToClipboard()
    {
        var fragments = _viewModel.Fragments;
        if (fragments.Count == 0) return;

        var sb = new StringBuilder();
        int counter = 1;

        foreach (var fragment in fragments)
        {
            double percentage = _viewModel.SortType switch
            {
                DamageMeterSortType.Heal or DamageMeterSortType.Hps => fragment.HealPercentage,
                DamageMeterSortType.TakenDamage => fragment.TakenDamagePercentage,
                _ => fragment.DamagePercentage
            };

            sb.AppendLine($"{counter}. {fragment.Name}: {percentage:F2}%");
            counter++;
        }

        var dataPackage = new DataPackage();
        dataPackage.SetText(sb.ToString().TrimEnd());
        Clipboard.SetContent(dataPackage);

        ShowCopyFeedback();
    }

    private async void ShowCopyFeedback()
    {
        ToolTipService.SetToolTip(_copyBtn, "Copied!");
        await System.Threading.Tasks.Task.Delay(1500);
        ToolTipService.SetToolTip(_copyBtn, "Copy to clipboard");
    }

    #endregion

    #region Event Handlers

    private void PinButton_Click(object sender, RoutedEventArgs e)
    {
        _presenter.IsAlwaysOnTop = !_presenter.IsAlwaysOnTop;
        _pinIcon.Glyph = _presenter.IsAlwaysOnTop ? "\uE841" : "\uE718";
    }

    private void CompactButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isCompactOverlay)
        {
            _appWindow.SetPresenter(AppWindowPresenterKind.Default);
            _appWindow.Resize(new SizeInt32(420, 620));
            _isCompactOverlay = false;
            _compactIcon.Glyph = "\uE740";
        }
        else
        {
            _appWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
            _appWindow.Resize(new SizeInt32(320, 400));
            _isCompactOverlay = true;
            _compactIcon.Glyph = "\uE73F";
        }
    }

    private void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.IsTrackingActive)
            _viewModel.StopTrackingCommand.Execute(null);
        else
            _viewModel.StartTrackingCommand.Execute(null);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DamageMeterViewModel.StatusText):
                _statusText.Text = _viewModel.StatusText;
                break;
            case nameof(DamageMeterViewModel.ErrorMessage):
                _errorText.Text = _viewModel.ErrorMessage ?? string.Empty;
                break;
            case nameof(DamageMeterViewModel.IsTrackingActive):
                _startIcon.Glyph = _viewModel.IsTrackingActive ? "\uE769" : "\uE768";
                _startText.Text = _viewModel.IsTrackingActive ? "Pause" : "Resume";
                _trackingIndicator.Fill = new SolidColorBrush(_viewModel.IsTrackingActive
                    ? Color.FromArgb(0xFF, 0x51, 0xCF, 0x66)
                    : Color.FromArgb(0xFF, 0x88, 0x88, 0x88));
                break;
            case nameof(DamageMeterViewModel.TotalDamage):
                _totalDamageText.Text = _viewModel.TotalDamage.ToShortNumberString();
                break;
            case nameof(DamageMeterViewModel.TotalHeal):
                _totalHealText.Text = _viewModel.TotalHeal.ToShortNumberString();
                break;
            case nameof(DamageMeterViewModel.TotalTakenDamage):
                _totalTakenDamageText.Text = _viewModel.TotalTakenDamage.ToShortNumberString();
                break;
            case nameof(DamageMeterViewModel.TotalFame):
                _totalFameText.Text = _viewModel.TotalFame.ToShortNumberString();
                break;
            case nameof(DamageMeterViewModel.TotalCombatFame):
                _totalCombatFameText.Text = _viewModel.TotalCombatFame.ToShortNumberString();
                break;
            case nameof(DamageMeterViewModel.TotalSilver):
                _totalSilverText.Text = _viewModel.TotalSilver.ToShortNumberString();
                break;
            case nameof(DamageMeterViewModel.PlayerName):
                _playerNameText.Text = _viewModel.PlayerName;
                UpdateProfileBarVisibility();
                break;
            case nameof(DamageMeterViewModel.GuildName):
                _guildNameText.Text = _viewModel.GuildName;
                break;
            case nameof(DamageMeterViewModel.PartyMemberCount):
                _partyCountText.Text = _viewModel.PartyMemberCount.ToString();
                UpdateProfileBarVisibility();
                break;
        }
    }

    #endregion

    #region UI Updates

    private void UpdateProfileBarVisibility()
    {
        _profileBar.Visibility = (!string.IsNullOrEmpty(_viewModel.PlayerName) || _viewModel.PartyMemberCount > 0)
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void RefreshPlayerList()
    {
        var fragments = _viewModel.Fragments;
        var activeGuids = new HashSet<Guid>();

        _playerListPanel.Children.Clear();

        int rank = 1;
        foreach (var fragment in fragments)
        {
            activeGuids.Add(fragment.UserGuid);

            if (!_playerRows.TryGetValue(fragment.UserGuid, out var row))
            {
                row = new PlayerRow(_damageBrush, _healBrush, _barBgBrush, _itemController, _imageController);
                _playerRows[fragment.UserGuid] = row;
            }

            row.Update(fragment, rank);
            _playerListPanel.Children.Add(row.Root);
            rank++;
        }

        // Remove stale cached rows
        var staleGuids = new List<Guid>();
        foreach (var kvp in _playerRows)
        {
            if (!activeGuids.Contains(kvp.Key))
                staleGuids.Add(kvp.Key);
        }
        foreach (var guid in staleGuids)
            _playerRows.Remove(guid);
    }

    #endregion

    #region PlayerRow

    private sealed class PlayerRow
    {
        public Grid Root { get; }

        private readonly ItemController _itemController;
        private readonly ImageController _imageController;
        private readonly TextBlock _rankText;
        private readonly Image _weaponIcon;
        private readonly TextBlock _nameText;
        private readonly TextBlock _combatTimeText;
        private readonly TextBlock _damageText;
        private readonly TextBlock _dpsText;
        private readonly TextBlock _damagePctText;
        private readonly TextBlock _healText;
        private readonly TextBlock _hpsText;
        private readonly ColumnDefinition _damageFilledCol;
        private readonly ColumnDefinition _damageEmptyCol;
        private readonly ColumnDefinition _healFilledCol;
        private readonly ColumnDefinition _healEmptyCol;
        private readonly Border _accentBorder;
        private readonly Grid _card;

        private int _lastMainHandIndex;

        public PlayerRow(SolidColorBrush damageBrush, SolidColorBrush healBrush, SolidColorBrush barBgBrush,
            ItemController itemController, ImageController imageController)
        {
            _itemController = itemController;
            _imageController = imageController;
            var whiteBrush = new SolidColorBrush(Colors.White);

            // Outer container with accent border
            Root = new Grid { Margin = new Thickness(0, 1, 0, 1) };
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3) });
            Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left accent border
            _accentBorder = new Border
            {
                Background = damageBrush,
                CornerRadius = new CornerRadius(3, 0, 0, 3),
                Width = 3
            };
            Grid.SetColumn(_accentBorder, 0);
            Root.Children.Add(_accentBorder);

            // Card content
            _card = new Grid
            {
                Padding = new Thickness(8, 6, 10, 6),
                CornerRadius = new CornerRadius(0, 6, 6, 0)
            };

            if (Application.Current.Resources.TryGetValue("CardBackgroundFillColorDefaultBrush", out var bgObj) && bgObj is Brush bg)
                _card.Background = bg;
            else
                _card.Background = new SolidColorBrush(Color.FromArgb(0x12, 0xFF, 0xFF, 0xFF));

            _card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _card.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Row 0: Rank + Weapon Icon + Name + Combat time
            var nameRow = new Grid();
            nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            nameRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _rankText = new TextBlock
            {
                FontSize = 11,
                Opacity = 0.35,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                MinWidth = 18
            };
            Grid.SetColumn(_rankText, 0);
            nameRow.Children.Add(_rankText);

            _weaponIcon = new Image
            {
                Width = 28,
                Height = 28,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_weaponIcon, 1);
            nameRow.Children.Add(_weaponIcon);

            _nameText = new TextBlock
            {
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(_nameText, 2);
            nameRow.Children.Add(_nameText);

            var timePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 3,
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.45
            };
            timePanel.Children.Add(new FontIcon { Glyph = "\uE823", FontSize = 10 });
            _combatTimeText = new TextBlock { FontSize = 10 };
            timePanel.Children.Add(_combatTimeText);
            Grid.SetColumn(timePanel, 3);
            nameRow.Children.Add(timePanel);

            Grid.SetRow(nameRow, 0);
            _card.Children.Add(nameRow);

            // Row 1: Damage bar (20px)
            _damageFilledCol = new ColumnDefinition { Width = new GridLength(0.001, GridUnitType.Star) };
            _damageEmptyCol = new ColumnDefinition { Width = new GridLength(99.999, GridUnitType.Star) };
            BuildBar(_card, 1, damageBrush, barBgBrush, whiteBrush,
                _damageFilledCol, _damageEmptyCol, 20, new Thickness(0, 5, 0, 0),
                out _damageText, out _dpsText, out _damagePctText);

            // Row 2: Heal bar (16px)
            _healFilledCol = new ColumnDefinition { Width = new GridLength(0.001, GridUnitType.Star) };
            _healEmptyCol = new ColumnDefinition { Width = new GridLength(99.999, GridUnitType.Star) };
            BuildBar(_card, 2, healBrush, barBgBrush, whiteBrush,
                _healFilledCol, _healEmptyCol, 16, new Thickness(0, 3, 0, 0),
                out _healText, out _hpsText, out _);

            Grid.SetColumn(_card, 1);
            Root.Children.Add(_card);

            // Pointer hover effect
            Root.PointerEntered += (_, _) =>
            {
                if (_card.Background is SolidColorBrush scb)
                {
                    var c = scb.Color;
                    _card.Background = new SolidColorBrush(Color.FromArgb((byte)Math.Min(255, c.A + 0x0A), c.R, c.G, c.B));
                }
            };
            Root.PointerExited += (_, _) =>
            {
                if (Application.Current.Resources.TryGetValue("CardBackgroundFillColorDefaultBrush", out var bgObj2) && bgObj2 is Brush bg2)
                    _card.Background = bg2;
                else
                    _card.Background = new SolidColorBrush(Color.FromArgb(0x12, 0xFF, 0xFF, 0xFF));
            };
        }

        private static void BuildBar(Grid parent, int row,
            SolidColorBrush fillBrush, SolidColorBrush bgBrush, SolidColorBrush textBrush,
            ColumnDefinition filledCol, ColumnDefinition emptyCol,
            double height, Thickness margin,
            out TextBlock valueText, out TextBlock rateText, out TextBlock pctText)
        {
            var barGrid = new Grid
            {
                Height = height,
                CornerRadius = new CornerRadius(4),
                Margin = margin
            };
            barGrid.ColumnDefinitions.Add(filledCol);
            barGrid.ColumnDefinitions.Add(emptyCol);

            var barBg = new Border { Background = bgBrush, CornerRadius = new CornerRadius(4) };
            Grid.SetColumnSpan(barBg, 2);
            barGrid.Children.Add(barBg);

            var barFill = new Border { Background = fillBrush, CornerRadius = new CornerRadius(4) };
            Grid.SetColumn(barFill, 0);
            barGrid.Children.Add(barFill);

            // Left text: value + rate
            var textPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Padding = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumnSpan(textPanel, 2);

            valueText = new TextBlock { FontSize = 10, Foreground = textBrush, FontWeight = FontWeights.SemiBold };
            rateText = new TextBlock { FontSize = 10, Foreground = textBrush, Opacity = 0.7, Margin = new Thickness(6, 0, 0, 0) };
            textPanel.Children.Add(valueText);
            textPanel.Children.Add(rateText);
            barGrid.Children.Add(textPanel);

            // Right text: percentage
            pctText = new TextBlock
            {
                FontSize = 10,
                Foreground = textBrush,
                Opacity = 0.8,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetColumnSpan(pctText, 2);
            barGrid.Children.Add(pctText);

            Grid.SetRow(barGrid, row);
            parent.Children.Add(barGrid);
        }

        public void Update(DamageMeterFragmentViewModel fragment, int rank)
        {
            _rankText.Text = $"#{rank}";
            _nameText.Text = fragment.Name;
            _combatTimeText.Text = fragment.CombatTimeString;

            _damageText.Text = fragment.Damage.ToShortNumberString();
            _dpsText.Text = $"{fragment.Dps:F1} DPS";
            if (_damagePctText != null)
                _damagePctText.Text = $"{fragment.DamagePercentage:F1}%";

            _healText.Text = fragment.Heal.ToShortNumberString();
            _hpsText.Text = $"{fragment.Hps:F1} HPS";

            var dmgPct = Math.Max(0.001, fragment.DamagePercentage);
            _damageFilledCol.Width = new GridLength(dmgPct, GridUnitType.Star);
            _damageEmptyCol.Width = new GridLength(Math.Max(0.001, 100 - dmgPct), GridUnitType.Star);

            var healPct = Math.Max(0.001, fragment.HealPercentage);
            _healFilledCol.Width = new GridLength(healPct, GridUnitType.Star);
            _healEmptyCol.Width = new GridLength(Math.Max(0.001, 100 - healPct), GridUnitType.Star);

            // Load weapon icon if changed (or if previous load failed)
            if (fragment.MainHandIndex > 0 && (fragment.MainHandIndex != _lastMainHandIndex || _weaponIcon.Source == null))
            {
                _ = LoadWeaponIconAsync(fragment.MainHandIndex);
            }
        }

        private async System.Threading.Tasks.Task LoadWeaponIconAsync(int mainHandIndex)
        {
            var uniqueName = _itemController.GetUniqueNameByIndex(mainHandIndex);
            if (uniqueName == null) return;

            var image = await _imageController.GetItemImageAsync(uniqueName);
            if (image != null)
            {
                _weaponIcon.Source = image;
                _lastMainHandIndex = mainHandIndex;
            }
        }
    }

    #endregion
}
