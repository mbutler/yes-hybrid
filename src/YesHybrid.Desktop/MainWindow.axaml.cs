using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using YesHybrid.Desktop.ViewModels;
using YesHybrid.Engine.Game;
using YesHybrid.Engine.Uci;
using BoardPosition = YesHybrid.Engine.Game.Position;

namespace YesHybrid.Desktop;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();
    private UciEngine? _engine;
    private GameSession? _session;
    private readonly Border?[,] _borders = new Border[Variant.Files, Variant.Ranks];
    private readonly global::Avalonia.Controls.Shapes.Path?[,] _paths = new global::Avalonia.Controls.Shapes.Path[Variant.Files, Variant.Ranks];
    private readonly TextBlock?[,] _labels = new TextBlock[Variant.Files, Variant.Ranks];

    private (int x, int y)? _selected;
    private HashSet<(int x, int y)> _legalDests = new();
    private IReadOnlyList<string> _allLegals = Array.Empty<string>();
    /// <summary>Serialized board + engine use: one in-flight path (clicks, new game, engine plies). Prevents overlapping <see cref="OnCellClickAsync"/> while <c>go perft</c> awaits.</summary>
    private readonly SemaphoreSlim _boardInteraction = new(1, 1);
    private bool _gameOver;
    private string? _endMessage;

    private static readonly object ClickDebugLogLock = new();
    /// <summary>Set env <c>YES_HYBRID_DEBUG_CLICKS=1</c> to append one JSON line per cell click to temp and to <c>reports/yes-hybrid-click-debug.log</c> under the repo when found (so Cursor can read it).</summary>
    private const string ClickDebugEnv = "YES_HYBRID_DEBUG_CLICKS";
    private const string ClickDebugFile = "yes-hybrid-click-debug.log";

    private static readonly IBrush LightSq = new SolidColorBrush(Color.Parse("#EEE8DC"));
    private static readonly IBrush DarkSq = new SolidColorBrush(Color.Parse("#A0826D"));
    private static readonly IBrush SelectedSq = new SolidColorBrush(Color.Parse("#5C7F5C"));
    private static readonly Color LegalTint = Color.Parse("#4A7A9A");
    private static readonly IBrush PartyPiece = new SolidColorBrush(Color.Parse("#1a3a6e"));
    private static readonly IBrush HordePiece = new SolidColorBrush(Color.Parse("#6b1c1e"));
    private static readonly IBrush BlockedPiece = new SolidColorBrush(Color.Parse("#333333"));
    private static readonly IBrush PartyStroke = new SolidColorBrush(Color.FromArgb(140, 5, 18, 45));
    private static readonly IBrush HordeStroke = new SolidColorBrush(Color.FromArgb(150, 45, 8, 12));
    private static readonly IBrush BlockedStroke = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0));
    private static readonly IBrush BoardLabelFore = new SolidColorBrush(Color.Parse("#6a635c"));

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        ModeCombo.Items.Add(new ComboBoxItem
        {
            Content = "Human vs Engine",
            Tag = DesktopPlayMode.HumanVsEngine,
        });
        ModeCombo.Items.Add(new ComboBoxItem
        {
            Content = "Human vs Human",
            Tag = DesktopPlayMode.HumanVsHuman,
        });
        ModeCombo.SelectedIndex = 0;
        SideCombo.Items.Add(new ComboBoxItem { Content = "Party (white)", Tag = DesktopHumanSide.Party });
        SideCombo.Items.Add(new ComboBoxItem { Content = "Horde (black)", Tag = DesktopHumanSide.Horde });
        SideCombo.SelectedIndex = 0;
        NewGameButton.Click += async (_, _) => await NewGameAsync();
        RulesButton.Click += OnRulesClick;
        ModeCombo.SelectionChanged += async (_, _) => await OnModeChangedAsync();
        SideCombo.SelectionChanged += async (_, _) => await OnHumanSideChangedAsync();
        BuildBoardGrid();
        Opened += async (_, _) => await InitializeEngineAndBoardAsync();
        Closing += async (_, _) => await OnWindowClosingAsync();
    }

    private void OnRulesClick(object? sender, RoutedEventArgs e)
    {
        var path = RuntimePaths.FindRulesMd();
        if (path is null)
        {
            new RulesWindow(null, "RULES.md not found. Run the app with working directory at the yes-hybrid repo root.").Show(this);
            return;
        }
        try
        {
            var text = File.ReadAllText(path);
            new RulesWindow(text, null).Show(this);
        }
        catch (Exception ex)
        {
            new RulesWindow(null, ex.Message).Show(this);
        }
    }

    private async Task OnModeChangedAsync()
    {
        if (_session is null) return;
        if (ModeCombo.SelectedItem is not ComboBoxItem { Tag: DesktopPlayMode mode })
            return;
        _session.Mode = mode;
        ApplyHumanSideFromUi();
        UpdateSideComboEnabled();
        _gameOver = false;
        _endMessage = null;
        _selected = null;
        _legalDests.Clear();
        _allLegals = Array.Empty<string>();
        await WithBoardExclusiveAsync(async () =>
        {
            await _session.NewGameAsync();
            await AfterAnyPlyAsync();
        });
    }

    private async Task OnHumanSideChangedAsync()
    {
        if (_session is null) return;
        if (ModeCombo.SelectedItem is not ComboBoxItem { Tag: DesktopPlayMode mode } || mode != DesktopPlayMode.HumanVsEngine)
            return;
        ApplyHumanSideFromUi();
        _gameOver = false;
        _endMessage = null;
        _selected = null;
        _legalDests.Clear();
        _allLegals = Array.Empty<string>();
        await WithBoardExclusiveAsync(async () =>
        {
            await _session.NewGameAsync();
            await AfterAnyPlyAsync();
        });
    }

    private void UpdateSideComboEnabled()
    {
        if (ModeCombo.SelectedItem is not ComboBoxItem { Tag: DesktopPlayMode mode })
        {
            SideCombo.IsEnabled = false;
            return;
        }
        var hve = mode == DesktopPlayMode.HumanVsEngine;
        SideCombo.IsEnabled = hve;
        YouPlayLabel.Opacity = hve ? 0.85 : 0.35;
    }

    private void ApplyHumanSideFromUi()
    {
        if (_session is null) return;
        if (SideCombo.SelectedItem is ComboBoxItem { Tag: DesktopHumanSide hs })
            _session.HumanSide = hs;
    }

    private void BuildBoardGrid()
    {
        const double labelColWidth = 24;
        const double labelRowHeight = 22;
        var outer = new Grid
        {
            MinWidth = labelColWidth + Variant.Files * 40,
            MinHeight = Variant.Ranks * 40 + labelRowHeight,
        };
        outer.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(labelColWidth, GridUnitType.Pixel)));
        for (int c = 0; c < Variant.Files; c++)
            outer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        for (int r = 0; r < Variant.Ranks; r++)
            outer.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        outer.RowDefinitions.Add(new RowDefinition(new GridLength(labelRowHeight, GridUnitType.Pixel)));

        for (int row = 0; row < Variant.Ranks; row++)
        {
            int rank = Variant.Ranks - row;
            var rankLabel = new TextBlock
            {
                Text = rank.ToString(),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                Foreground = BoardLabelFore,
            };
            outer.Children.Add(rankLabel);
            Grid.SetRow(rankLabel, row);
            Grid.SetColumn(rankLabel, 0);
        }
        for (int f = 0; f < Variant.Files; f++)
        {
            var fileLabel = new TextBlock
            {
                Text = ((char)('a' + f)).ToString(),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                Foreground = BoardLabelFore,
            };
            outer.Children.Add(fileLabel);
            Grid.SetRow(fileLabel, Variant.Ranks);
            Grid.SetColumn(fileLabel, 1 + f);
        }
        // Corner below rank column (keeps file row alignment)
        var corner = new Border { Background = Brushes.Transparent };
        outer.Children.Add(corner);
        Grid.SetRow(corner, Variant.Ranks);
        Grid.SetColumn(corner, 0);

        for (int col = 0; col < Variant.Files; col++)
        for (int row = 0; row < Variant.Ranks; row++)
        {
            int x = col;
            int y = (Variant.Ranks - 1) - row;
            var path = new global::Avalonia.Controls.Shapes.Path
            {
                Stretch = Stretch.Uniform,
                MaxWidth = 32,
                MaxHeight = 32,
                IsVisible = false,
                // Route hits to the parent Border; piece geometry can extend past the cell.
                IsHitTestVisible = false,
            };
            var text = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.Black,
                IsVisible = false,
                IsHitTestVisible = false,
            };
            var panel = new Grid
            {
                // Without a background, many Avalonia hit-test paths do not let the parent Border own pointer events on the full cell.
                Background = Brushes.Transparent,
            };
            panel.Children.Add(path);
            panel.Children.Add(text);
            var border = new Border
            {
                Child = panel,
                MinWidth = 36,
                MinHeight = 36,
                // Same: a Border with null Background is often not hittable in the cell interior, so the wrong square can receive the event.
                Background = Brushes.Transparent,
                Focusable = true,
                Cursor = new Cursor(StandardCursorType.Hand),
            };
            int fx = x, fy = y;
            border.PointerPressed += async (_, e) =>
            {
                e.Handled = true;
                await OnCellClickAsync(fx, fy);
            };
            outer.Children.Add(border);
            Grid.SetColumn(border, 1 + col);
            Grid.SetRow(border, row);
            _borders[x, y] = border;
            _paths[x, y] = path;
            _labels[x, y] = text;
        }

        BoardHost.Content = outer;
    }

    private async Task NewGameAsync()
    {
        if (_session is null) return;
        ApplyHumanSideFromUi();
        _gameOver = false;
        _endMessage = null;
        _selected = null;
        _legalDests.Clear();
        _allLegals = Array.Empty<string>();
        await WithBoardExclusiveAsync(async () =>
        {
            await _session.NewGameAsync();
            await AfterAnyPlyAsync();
        });
    }

    private async Task InitializeEngineAndBoardAsync()
    {
        var engPath = RuntimePaths.FindEngine();
        var varPath = RuntimePaths.FindYeshybridIni();
        if (engPath is null)
        {
            _vm.ErrorText =
                "Fairy-Stockfish not found. Place a binary in engine/ or runtimes/{rid}/native/ (see README), or set working directory to the repo root.";
            return;
        }
        if (varPath is null)
        {
            _vm.ErrorText = "variants/yeshybrid.ini not found. Run the app with the working directory at the yes-hybrid repo root.";
            return;
        }

        try
        {
            _engine = await UciEngine.StartAsync(engPath, varPath, Variant.Name);
        }
        catch (Exception ex)
        {
            _vm.ErrorText = ex.Message;
            return;
        }

        _session = new GameSession(_engine)
        {
            Mode = ModeCombo.SelectedItem is ComboBoxItem { Tag: DesktopPlayMode m } ? m : DesktopPlayMode.HumanVsEngine,
            HumanSide = SideCombo.SelectedItem is ComboBoxItem { Tag: DesktopHumanSide h } ? h : DesktopHumanSide.Party,
        };
        UpdateSideComboEnabled();
        _vm.ErrorText = "";
        await WithBoardExclusiveAsync(async () =>
        {
            await _session.NewGameAsync();
            await AfterAnyPlyAsync();
        });
    }

    private async Task OnWindowClosingAsync()
    {
        if (_engine is null) return;
        try
        {
            await _engine.DisposeAsync();
        }
        catch
        { /* best effort */ }
        _engine = null;
        _session = null;
    }

    private async Task OnCellClickAsync(int x, int y)
    {
        if (_session is null || _engine is null || _gameOver) return;

        await _boardInteraction.WaitAsync();
        try
        {
            if (_session is null || _engine is null || _gameOver) return;
            var selectionBeforeClick = _selected;

            // One UCI transaction: replay full plies, perft 1, then "d". FEN and legals cannot disagree.
            var allLegals = await _session.GetLegalMovesAuthoritativeAsync();
            _allLegals = allLegals;

            var pos = _session.GetPosition();
            var st = pos.Squares[x, y];
            var stm = pos.SideToMove;

            string? tryMoveUci = null;
            if (_selected is { } sel0)
            {
                var fromSq = Algeb(sel0.x, sel0.y);
                var toSq = Algeb(x, y);
                tryMoveUci = fromSq + toSq;
                var match = _allLegals.FirstOrDefault(m => m.StartsWith(tryMoveUci, StringComparison.Ordinal));
                if (match is not null)
                {
                    TryAppendClickDebug(
                        x, y, pos, st, stm, allLegals, fromPrefix: null, legalDestCount: 0,
                        selectionBeforeClick, tryMoveUci, committedUci: match);
                    await RunPlayerMoveAndResolveCoreAsync(match);
                    return;
                }
            }

            string? fromPrefix = null;
            int legalDestCount = 0;
            if (st is not ('.' or '*' or '#') && IsSidePiece(st, stm))
            {
                var pick = (x, y);
                _selected = pick;
                fromPrefix = Algeb(pick.x, pick.y);
                _legalDests = new HashSet<(int, int)>();
                foreach (var m in _allLegals)
                {
                    if (!m.StartsWith(fromPrefix, StringComparison.Ordinal) || m.Length < 4) continue;
                    var to = m.Substring(2, 2);
                    if (TryParseAlgeb(to, out var tx, out var ty))
                        _legalDests.Add((tx, ty));
                }
                legalDestCount = _legalDests.Count;
                if (legalDestCount == 0)
                {
                    _selected = null;
                    _legalDests.Clear();
                    _vm.StatusText = stm == 'b'
                        ? "That Horde piece has no legal move from here (often blocked or pinned). Select another."
                        : "That Party piece has no legal move from here. Select another.";
                }
            }
            else
            {
                if (tryMoveUci is not null)
                    _vm.StatusText = "Not a legal move. Choose a destination that was highlighted, or another piece.";
                _selected = null;
                _legalDests.Clear();
                _allLegals = Array.Empty<string>();
            }

            TryAppendClickDebug(
                x, y, pos, st, stm, allLegals, fromPrefix, legalDestCount, selectionBeforeClick, tryMoveUci,
                committedUci: null);

            RefreshView(_session.GetPosition());
        }
        finally
        {
            _boardInteraction.Release();
        }
    }

    private void TryAppendClickDebug(
        int x,
        int y,
        BoardPosition pos,
        char st,
        char stm,
        IReadOnlyList<string> allLegals,
        string? fromPrefix,
        int legalDestCount,
        (int x, int y)? selectionBeforeClick,
        string? tryMoveUci,
        string? committedUci)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(ClickDebugEnv), "1", StringComparison.Ordinal))
            return;
        if (_session is null) return;

        var matchesFromPrefix = 0;
        if (fromPrefix is not null)
        {
            foreach (var m in allLegals)
            {
                if (m.StartsWith(fromPrefix, StringComparison.Ordinal) && m.Length >= 4)
                    matchesFromPrefix++;
            }
        }

        var tryKeyMatches = 0;
        if (tryMoveUci is not null)
        {
            foreach (var m in allLegals)
            {
                if (m.StartsWith(tryMoveUci, StringComparison.Ordinal)) tryKeyMatches++;
            }
        }

        var sample = allLegals.Take(12).ToArray();
        var line = JsonSerializer.Serialize(new
        {
            t = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
            x,
            y,
            algeb = Algeb(x, y),
            stm = stm.ToString(),
            st = st.ToString(),
            posStm = pos.SideToMove.ToString(),
            fen = _session.Fen,
            pliesN = _session.MoveUci.Count,
            uci = string.Join(" ", _session.MoveUci),
            legalsN = allLegals.Count,
            fromPrefix,
            legalDestCount,
            matchesFromPrefix,
            tryMoveUci,
            tryKeyMatches,
            selBefore = selectionBeforeClick is { } s ? $"{s.x},{s.y}" : null,
            committedUci,
            sample,
        });
        lock (ClickDebugLogLock)
        {
            var path = Path.Combine(Path.GetTempPath(), ClickDebugFile);
            File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
            var root = RuntimePaths.TryGetRepoRoot();
            if (root is not null)
            {
                var reports = Path.Combine(root, "reports");
                try
                {
                    Directory.CreateDirectory(reports);
                    File.AppendAllText(Path.Combine(reports, ClickDebugFile), line + Environment.NewLine, Encoding.UTF8);
                }
                catch
                { /* best effort */ }
            }
        }
    }

    private static bool IsSidePiece(char c, char sideToMove)
    {
        if (c is '.' or '*' or '#') return false;
        if (sideToMove == 'w') return char.IsUpper(c) && char.IsLetter(c);
        if (sideToMove == 'b') return char.IsLower(c) && char.IsLetter(c);
        return false;
    }

    /// <summary>In HvE, the engine plays the color the human does not play.</summary>
    private static bool IsEngineToMove(DesktopHumanSide humanSide, char sideToMove) =>
        humanSide == DesktopHumanSide.Party ? sideToMove == 'b' : sideToMove == 'w';

    /// <summary>Applies a human UCI and runs post-ply; caller must already hold the board <see cref="SemaphoreSlim"/> in <see cref="OnCellClickAsync"/>.</summary>
    private async Task RunPlayerMoveAndResolveCoreAsync(string uci)
    {
        if (_session is null) return;
        _selected = null;
        _legalDests.Clear();
        _allLegals = Array.Empty<string>();
        try
        {
            await _session.ApplyUserMoveUciAsync(uci);
            await AfterAnyPlyAsync();
        }
        catch (InvalidOperationException ex)
        {
            _vm.ErrorText = ex.Message;
        }
    }

    private async Task WithBoardExclusiveAsync(Func<Task> work)
    {
        await _boardInteraction.WaitAsync();
        try
        {
            await work();
        }
        finally
        {
            _boardInteraction.Release();
        }
    }

    private async Task AfterAnyPlyAsync()
    {
        if (_session is null) return;

        while (true)
        {
            var legals = await _session.GetLegalMovesAuthoritativeAsync();
            var pos = _session.GetPosition();
            RebuildMoveList();
            RefreshView(pos);
            UpdateResultDisplay(pos);

            if (GameResultHelper.TryTerminalMessage(pos, out var term))
            {
                _gameOver = true;
                _endMessage = term;
                _vm.StatusText = term;
                _vm.ResultText = term;
                return;
            }

            if (DrawRules.IsHalfmoveDraw(pos))
            {
                const string d = "Draw: 50-move rule (100 halfmoves with no reset).";
                _gameOver = true;
                _endMessage = d;
                _vm.StatusText = d;
                _vm.ResultText = d;
                return;
            }

            if (_session.IsTripleRepetitionDraw)
            {
                const string d = "Draw: same position (board + turn + rights) three times.";
                _gameOver = true;
                _endMessage = d;
                _vm.StatusText = d;
                _vm.ResultText = d;
                return;
            }

            if (legals.Count == 0)
            {
                var noMove = GameResultHelper.NoLegalMovesForSideToMoveMessage(pos.SideToMove);
                _gameOver = true;
                _endMessage = noMove;
                _vm.StatusText = noMove;
                _vm.ResultText = noMove;
                return;
            }

            if (_session.Mode == DesktopPlayMode.HumanVsEngine
                && IsEngineToMove(_session.HumanSide, pos.SideToMove))
            {
                _vm.StatusText = pos.SideToMove == 'w'
                    ? "Party (engine) thinking…"
                    : "Horde (engine) thinking…";
                var sideBeforeEngine = pos.SideToMove;
                var mv = await _session.EngineBestMoveAsync();

                if (mv is "(none)" or "0000")
                {
                    var noMove = GameResultHelper.NoLegalMovesForSideToMoveMessage(sideBeforeEngine);
                    _gameOver = true;
                    _endMessage = noMove;
                    _vm.StatusText = noMove;
                    RebuildMoveList();
                    RefreshView(_session.GetPosition());
                    UpdateResultDisplay(_session.GetPosition());
                    _vm.ResultText = noMove;
                    return;
                }

                pos = _session.GetPosition();
                if (GameResultHelper.TryTerminalMessage(pos, out term))
                {
                    _gameOver = true;
                    _endMessage = term;
                    _vm.StatusText = term;
                    RebuildMoveList();
                    RefreshView(pos);
                    _vm.ResultText = term;
                    return;
                }

                if (DrawRules.IsHalfmoveDraw(pos))
                {
                    const string d = "Draw: 50-move rule (100 halfmoves with no reset).";
                    _gameOver = true;
                    _endMessage = d;
                    _vm.StatusText = d;
                    _vm.ResultText = d;
                    return;
                }

                if (_session.IsTripleRepetitionDraw)
                {
                    const string d = "Draw: same position (board + turn + rights) three times.";
                    _gameOver = true;
                    _endMessage = d;
                    _vm.StatusText = d;
                    _vm.ResultText = d;
                    return;
                }

                var leg2 = await _session.GetLegalMovesAuthoritativeAsync();
                if (leg2.Count == 0)
                {
                    var nm = GameResultHelper.NoLegalMovesForSideToMoveMessage(pos.SideToMove);
                    _gameOver = true;
                    _endMessage = nm;
                    _vm.StatusText = nm;
                    _vm.ResultText = nm;
                    return;
                }

                continue;
            }

            SetStatusForSide(pos);
            return;
        }
    }

    private void SetStatusForSide(BoardPosition pos)
    {
        if (pos.SideToMove == 'w')
            _vm.StatusText = "Party to move. Select a piece (Guard, Scout, …).";
        else
            _vm.StatusText = "Horde to move. Select a piece (Trove, Wraith, …).";
    }

    private void RebuildMoveList()
    {
        if (_session is null)
        {
            _vm.RebuildHistory(Array.Empty<string>());
            return;
        }
        var m = _session.MoveUci;
        var lines = new List<string>();
        for (int i = 0, n = 1; i < m.Count; n++)
        {
            if (i + 1 < m.Count) lines.Add($"{n,3}.  {m[i++]}  {m[i++]}");
            else lines.Add($"{n,3}.  {m[i++]}  …");
        }
        _vm.RebuildHistory(lines);
    }

    private void UpdateResultDisplay(BoardPosition pos)
    {
        if (_gameOver && _endMessage is not null)
        {
            _vm.ResultText = _endMessage;
            return;
        }
        if (GameResultHelper.TryTerminalMessage(pos, out var term))
        {
            _vm.ResultText = term;
            return;
        }
        _vm.ResultText = "Ongoing";
    }

    private void RefreshView(BoardPosition pos)
    {
        for (int x = 0; x < Variant.Files; x++)
        for (int y = 0; y < Variant.Ranks; y++)
        {
            var b = _borders[x, y]!;
            var label = _labels[x, y]!;
            var pathControl = _paths[x, y]!;
            bool light = (x + y) % 2 == 0;
            IBrush bg = light ? LightSq : DarkSq;
            if (_selected is { } s && s.x == x && s.y == y) bg = SelectedSq;
            else if (_legalDests.Contains((x, y)))
                bg = new SolidColorBrush(LegalTint, 0.45);
            b.Background = bg;
            b.BorderThickness = new Thickness(0);
            b.BorderBrush = null;

            var ch = pos.Squares[x, y];
            if (ch is '.')
            {
                pathControl.IsVisible = false;
                label.IsVisible = false;
                continue;
            }

            var pieceIcon = PiecePathGeometry.ForFenChar(ch);
            if (pieceIcon.Geometry is { } geom)
            {
                pathControl.Data = geom;
                pathControl.IsVisible = true;
                label.IsVisible = false;
                // Lucide 24px stroke icons: outline only, party/horde color on square
                pathControl.Fill = Brushes.Transparent;
                pathControl.Stroke = ch is '*' or '#'
                    ? BlockedPiece
                    : char.IsUpper(ch) && char.IsLetter(ch)
                        ? PartyPiece
                        : char.IsLower(ch) && char.IsLetter(ch)
                            ? HordePiece
                            : Brushes.SlateGray;
                pathControl.StrokeThickness = ch is '*' or '#' ? 1.35 : 1.9;
            }
            else
            {
                pathControl.IsVisible = false;
                label.IsVisible = true;
                label.Text = PiecePresentation.ShortLabel(ch);
                label.Foreground = Brushes.Black;
            }

            if (PiecePresentation.TooltipText(ch) is { } tip)
            {
                ToolTip.SetTip(b, new TextBlock
                {
                    Text = tip,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 320,
                });
            }
            else ToolTip.SetTip(b, (object?)null);
        }
    }

    private static string Algeb(int x, int y) => string.Concat((char)('a' + x), (y + 1).ToString());

    private static bool TryParseAlgeb(string two, out int x, out int y)
    {
        x = y = 0;
        if (two.Length != 2) return false;
        x = char.ToLowerInvariant(two[0]) - 'a';
        if (x is < 0 or > 11) return false;
        int rank = two[1] - '0';
        if (rank is < 1 or > 8) return false;
        y = rank - 1;
        return y < Variant.Ranks;
    }
}
