// The application: owns the telemetry, the overlays (pedals, sectors, fuel, electronics, tyres), the tray icon, the hotkeys and the settings window.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

using System.Windows.Threading;
using SD = System.Drawing;
using WF = System.Windows.Forms;

namespace SimracingBR
{
    public class OverlayApp
    {
        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, uint mod, uint vk);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        static readonly uint OwnPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;
        const string App = "SimracingBR", Ver = "0.2.0";

        readonly Settings cfg;
        readonly Tele tele = new Tele();
        readonly GameSources games;
        readonly DemoSource demo = new DemoSource();
        readonly PedalsWindow pedals;
        readonly SectorsWindow sectors;
        readonly FuelWindow fuel;
        readonly ElecWindow elec;
        // readonly TyreWindow tyre;          // tyre overlay: switched off for now, it is not working well yet (see TyreWindow.cs)
        readonly HashSet<OverlayWindow> beta;   // overlays shown with a BETA label
        readonly OverlayWindow[] overlays;
        readonly DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Render);
        HwndSource hotkeys;            // message-only window: the hotkeys work whichever overlays are on
        OverlayWindow lastTarget;      // the overlay the resize hotkeys act on (the one under the mouse, else the last one used)

        WF.NotifyIcon tray;
        WF.ToolStripMenuItem miLock, miDemo, miAutoHide;
        readonly Dictionary<OverlayWindow, WF.ToolStripMenuItem> miOn = new Dictionary<OverlayWindow, WF.ToolStripMenuItem>();
        readonly Dictionary<OverlayWindow, List<WF.ToolStripMenuItem>> miSizes = new Dictionary<OverlayWindow, List<WF.ToolStripMenuItem>>();
        static readonly int[] SizePcts = { 50, 75, 100, 125, 150, 200, 250 };

        bool locked = true, useDemo;
        // Settings window: while it is open the overlays stay visible and show demo data as a live preview
        Window settingsWin;
        bool settingsOpen;
        readonly List<Action> refreshers = new List<Action>();

        // auto-hide outside of driving (menus, replays, pause, setup in the pit box, game not focused)
        bool autoHide, hideInPitBox, hideUnfocused, sawLiveStatus;
        ITelemetrySource lastSource;
        uint lastFgPid; bool lastFgIsGame;

        public OverlayApp(Settings s, string dir, bool startDemo)
        {
            cfg = s; useDemo = startDemo || s.Bool("Demo", false);
            autoHide = s.Bool("AutoHide", true); hideInPitBox = s.Bool("HideInPitBox", true);
            hideUnfocused = s.Bool("HideWhenGameNotFocused", true);
            games = new GameSources(s);

            pedals = PedalsWindow.Create(s, dir, tele);
            sectors = SectorsWindow.Create(s, tele);
            fuel = FuelWindow.Create(s, tele);
            elec = ElecWindow.Create(s, tele);
            // tyre = TyreWindow.Create(s, tele);
            overlays = new OverlayWindow[] { pedals, sectors, fuel, elec /*, tyre */ };
            beta = new HashSet<OverlayWindow> { sectors, fuel };
            lastTarget = pedals;
            foreach (OverlayWindow w in overlays)
            {
                OverlayWindow ww = w;
                ww.LockToggleRequested += ToggleLock;
                ww.ScaleChanged += UpdateSizeMenu;
            }

            BuildTray();
            BuildHotkeys();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--settings") >= 0)
                Application.Current.Dispatcher.BeginInvoke(new Action(ShowSettings));
            timer.Interval = TimeSpan.FromMilliseconds(16);
            timer.Tick += Tick;
            timer.Start();
        }

        // ---- hotkeys ----------------------------------------------------------------------
        void BuildHotkeys()
        {
            var p = new HwndSourceParameters("SimracingBRHotkeys") { ParentWindow = new IntPtr(-3), Width = 0, Height = 0, WindowStyle = 0 };   // HWND_MESSAGE
            hotkeys = new HwndSource(p);
            IntPtr h = hotkeys.Handle;
            if (!RegisterHotKey(h, 1, 0x0002 | 0x0004, 0x4C)) // Ctrl+Shift+L
                tray.ShowBalloonTip(5000, App, "Ctrl+Shift+L is used by another app (is another SimracingBR open?). " +
                    "Double-click this tray icon to lock/unlock instead.", WF.ToolTipIcon.Warning);
            // Ctrl+Shift + / -  (main keys and numpad) to resize without unlocking
            RegisterHotKey(h, 2, 0x0006, 0xBB); RegisterHotKey(h, 3, 0x0006, 0xBD);
            RegisterHotKey(h, 4, 0x0006, 0x6B); RegisterHotKey(h, 5, 0x0006, 0x6D);
            hotkeys.AddHook(delegate (IntPtr hw, int msg, IntPtr w, IntPtr l, ref bool handled)
            {
                if (msg == 0x0312)
                {
                    int id = w.ToInt32();
                    if (id == 1) { ToggleLock(); handled = true; }
                    else if (id == 2 || id == 4) { ResizeTarget().Step(1.1); handled = true; }
                    else if (id == 3 || id == 5) { ResizeTarget().Step(1 / 1.1); handled = true; }
                }
                return IntPtr.Zero;
            });
        }

        OverlayWindow ResizeTarget()
        {
            foreach (OverlayWindow w in overlays) if (w.Hovered) lastTarget = w;
            return lastTarget;
        }

        // ---- lock -------------------------------------------------------------------------
        void ToggleLock()
        {
            locked = !locked;
            foreach (OverlayWindow w in overlays) w.SetLocked(locked);
            if (locked) SaveAll();
            miLock.Text = locked ? L.T("Unlock position (Ctrl+Shift+L)", "Destravar posição (Ctrl+Shift+L)")
                                 : L.T("Lock position (Ctrl+Shift+L)", "Travar posição (Ctrl+Shift+L)");
            tray.ShowBalloonTip(2500, App, locked ? L.T("Locked. Position and size saved.", "Travado. Posição e tamanho salvos.") :
                L.T("Unlocked: drag to move, scroll or drag the bottom-right corner to resize. Ctrl+Shift+L (or double-click the tray icon) to lock.",
                    "Destravado: arraste para mover, use a rodinha ou arraste o canto inferior direito para redimensionar. Ctrl+Shift+L (ou duplo clique no ícone) para travar."),
                WF.ToolTipIcon.None);
        }

        void SaveAll()
        {
            cfg.Set("Demo", useDemo ? "1" : "0");
            foreach (OverlayWindow w in overlays) w.SaveState();
        }

        void UpdateSizeMenu()
        {
            foreach (OverlayWindow w in overlays)
            {
                List<WF.ToolStripMenuItem> items;
                if (!miSizes.TryGetValue(w, out items)) continue;
                for (int i = 0; i < items.Count; i++) items[i].Checked = Math.Abs(w.Scale * 100 - SizePcts[i]) < 1.0;
            }
        }

        // ---- settings window -------------------------------------------------------------
        static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(225, 6, 0));
        static readonly Brush Muted = new SolidColorBrush(Color.FromRgb(150, 150, 160));

        void ShowSettings()
        {
            if (settingsWin != null) { settingsWin.Activate(); return; }
            refreshers.Clear();
            var w = new Window
            {
                Title = L.T("SimracingBR - Settings", "SimracingBR - Configurações"),
                Width = 700, Height = Math.Min(640, SystemParameters.WorkArea.Height - 40), ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen, Topmost = true,
                Background = new SolidColorBrush(Color.FromRgb(22, 22, 28)), Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI"), FontSize = 13
            };
            var shell = new Grid();
            shell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });
            shell.ColumnDefinitions.Add(new ColumnDefinition());
            w.Content = shell;

            // left: one row per overlay (name + on/off switch)
            var side = new StackPanel();
            side.Children.Add(new TextBlock { Text = "OVERLAYS", FontWeight = FontWeights.Bold, FontSize = 11, Foreground = Muted, Margin = new Thickness(18, 18, 0, 10) });
            var sideBox = new Border { Background = new SolidColorBrush(Color.FromRgb(15, 15, 20)), Child = side };
            Grid.SetColumn(sideBox, 0);
            shell.Children.Add(sideBox);

            // right: header, the selected overlay's settings (scrolls), buttons
            var right = new Grid { Margin = new Thickness(24, 16, 24, 16) };
            right.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            right.RowDefinitions.Add(new RowDefinition());
            right.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetColumn(right, 1);
            shell.Children.Add(right);

            var header = new StackPanel();
            var title = new TextBlock { FontSize = 20, FontWeight = FontWeights.Bold };
            var titleBeta = BetaBadge(true);
            var titleRow = new StackPanel { Orientation = Orientation.Horizontal };
            titleRow.Children.Add(title); titleRow.Children.Add(titleBeta);
            header.Children.Add(titleRow);
            header.Children.Add(new TextBlock
            {
                Text = L.T("Changes apply instantly. The overlays show demo data while this window is open.",
                           "As mudanças valem na hora. Os overlays mostram dados de demonstração enquanto esta janela estiver aberta."),
                Foreground = Muted, TextWrapping = TextWrapping.Wrap, FontSize = 12, Margin = new Thickness(0, 4, 0, 0)
            });
            right.Children.Add(header);

            var pageHost = new StackPanel();
            var scroll = new ScrollViewer { Content = pageHost, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 4, -8, 0), Padding = new Thickness(0, 0, 8, 0) };
            Grid.SetRow(scroll, 1);
            right.Children.Add(scroll);

            var pages = new StackPanel[overlays.Length];
            var rows = new Border[overlays.Length];
            int selected = 0;
            Action<int> select = delegate (int idx)
            {
                selected = idx;
                for (int i = 0; i < pages.Length; i++)
                {
                    pages[i].Visibility = i == idx ? Visibility.Visible : Visibility.Collapsed;
                    rows[i].Background = i == idx ? new SolidColorBrush(Color.FromRgb(34, 34, 42)) : Brushes.Transparent;
                    rows[i].BorderBrush = i == idx ? Accent : Brushes.Transparent;
                }
                title.Text = overlays[idx].Title2;
                titleBeta.Visibility = beta.Contains(overlays[idx]) ? Visibility.Visible : Visibility.Collapsed;
                scroll.ScrollToTop();
            };
            for (int i = 0; i < overlays.Length; i++)
            {
                int idx = i;
                OverlayWindow o = overlays[i];
                var line = new DockPanel { LastChildFill = false };
                var sw = Switch(() => o.Enabled, v => { o.SetEnabled(v); miOn[o].Checked = v; });
                DockPanel.SetDock(sw, Dock.Right);
                line.Children.Add(sw);
                var nameBox = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                nameBox.Children.Add(new TextBlock { Text = o.Title2, FontSize = 14, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
                if (beta.Contains(o)) nameBox.Children.Add(BetaBadge(false));
                line.Children.Add(nameBox);
                rows[i] = new Border { Child = line, Padding = new Thickness(15, 12, 14, 12), BorderThickness = new Thickness(3, 0, 0, 0), Cursor = Cursors.Hand, Background = Brushes.Transparent };
                rows[i].MouseLeftButtonUp += delegate { select(idx); };
                side.Children.Add(rows[i]);
                pages[i] = new StackPanel();
                pageHost.Children.Add(pages[i]);
            }
            BuildPedalsPage(pages[0]);
            BuildSectorsPage(pages[1]);
            BuildFuelPage(pages[2]);
            BuildElecPage(pages[3]);
            // BuildTyrePage(pages[4]);

            var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
            var reset = new Button { Content = L.T("Reset this overlay", "Restaurar este overlay"), Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(0, 0, 8, 0) };
            reset.Click += delegate
            {
                if (selected == 0) pedals.ResetDefaults(); else if (selected == 1) sectors.ResetDefaults(); else if (selected == 2) fuel.ResetDefaults(); else elec.ResetDefaults();   // else tyre.ResetDefaults();
                foreach (Action r in refreshers) r();
            };
            var close = new Button { Content = L.T("Close", "Fechar"), Padding = new Thickness(18, 5, 18, 5), IsDefault = true };
            close.Click += delegate { w.Close(); };
            buttons.Children.Add(reset); buttons.Children.Add(close);
            Grid.SetRow(buttons, 2);
            right.Children.Add(buttons);

            select(0);          // the pedals overlay (first in the list) is open by default
            w.Closed += delegate { settingsWin = null; settingsOpen = false; SaveAll(); try { cfg.Save(); } catch { } };
            settingsWin = w; settingsOpen = true;
            w.Show(); w.Activate();
        }

        // on / off switch
        UIElement Switch(Func<bool> get, Action<bool> set)
        {
            var knob = new Border { Width = 16, Height = 16, CornerRadius = new CornerRadius(8), Background = Brushes.White, VerticalAlignment = VerticalAlignment.Center };
            var track = new Border { Width = 40, Height = 22, CornerRadius = new CornerRadius(11), Child = knob, Cursor = Cursors.Hand };
            Action paint = delegate
            {
                bool on = get();
                track.Background = new SolidColorBrush(on ? Color.FromRgb(51, 214, 138) : Color.FromRgb(70, 70, 80));
                knob.HorizontalAlignment = on ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                knob.Margin = on ? new Thickness(0, 0, 3, 0) : new Thickness(3, 0, 0, 0);
            };
            track.MouseLeftButtonUp += delegate (object s, MouseButtonEventArgs e) { set(!get()); paint(); e.Handled = true; };
            refreshers.Add(paint);
            paint();
            return track;
        }

        void BuildPedalsPage(Panel root)
        {
            HudView v = pedals.View;
            Action after = pedals.LayoutChanged;
            AddSection(root, L.T("PEDALS", "PEDAIS"));
            AddCheck(root, L.T("Input graph", "Gráfico dos pedais"), "ShowGraph", () => v.ShowGraph, x => v.ShowGraph = x, after);
            AddCheck(root, L.T("Pedal bars", "Barras dos pedais"), "ShowBars", () => v.ShowBars, x => v.ShowBars = x, after);
            AddCheck(root, L.T("Clutch (bar and graph line)", "Embreagem (barra e linha do gráfico)"), "ShowClutch", () => v.ShowClutch, x => v.ShowClutch = x, after);
            AddCheck(root, L.T("ABS / TC levels and activity", "Níveis e atuação de ABS / TC"), "ShowAids", () => v.ShowAids, x => v.ShowAids = x, after);
            AddCheck(root, L.T("Brake bias (when the game publishes it)", "Balança de freio (quando o jogo publica)"), "ShowBias", () => v.ShowBias, x => v.ShowBias = x, after);

            AddSection(root, L.T("DASH", "PAINEL"));
            AddCheck(root, L.T("Gear", "Marcha"), "ShowGear", () => v.ShowGear, x => v.ShowGear = x, after);
            AddCheck(root, L.T("Speed", "Velocidade"), "ShowSpeed", () => v.ShowSpeed, x => v.ShowSpeed = x, after);
            AddCheck(root, L.T("Shift lights", "Luzes de troca de marcha"), "ShowShiftLights", () => v.ShowShiftLights, x => v.ShowShiftLights = x, after);
            AddCheck(root, L.T("Steering wheel", "Volante"), "ShowWheel", () => v.ShowWheel, x => v.ShowWheel = x, after);

            AddSection(root, L.T("FUEL", "COMBUSTÍVEL"));
            AddCheck(root, L.T("Fuel in the tank", "Combustível no tanque"), "ShowFuel", () => v.ShowFuel, x => v.ShowFuel = x, after);
            AddCheck(root, L.T("Consumption per lap", "Consumo por volta"), "ShowFuelPerLap", () => v.ShowFuelPerLap, x => v.ShowFuelPerLap = x, after);
            AddCheck(root, L.T("Laps remaining", "Voltas restantes"), "ShowLapsLeft", () => v.ShowLapsLeft, x => v.ShowLapsLeft = x, after);

            AddAppearance(root, pedals);
        }

        void BuildSectorsPage(Panel root)
        {
            SectorsView v = sectors.View;
            Action after = sectors.LayoutChanged;
            AddSection(root, L.T("SECTORS", "SETORES"));
            AddSlider(root, L.T("Sub-sectors in each sector", "Subsetores em cada setor"), 1, 12, 1, () => sectors.Subs,
                x => sectors.SetSubs((int)Math.Round(x)), x => Math.Round(x).ToString(CultureInfo.InvariantCulture));
            AddCheck(root, L.T("Paint my best purple when the game does not show other drivers (AC EVO, ACC)", "Pintar meu melhor de roxo quando o jogo não mostra os outros pilotos (AC EVO, ACC)"), "SectorsPurpleBest", () => sectors.AssumePurple, x => sectors.SetAssumePurple(x), null);
            AddCheck(root, L.T("Sector time inside S1 / S2 / S3", "Tempo do setor dentro de S1 / S2 / S3"), "SectorsShowSectorTimes", () => v.ShowSectorTimes, x => v.ShowSectorTimes = x, after);

            AddSection(root, L.T("LAP TIMES", "TEMPOS DE VOLTA"));
            AddCheck(root, L.T("Current / best / last lap", "Volta atual / melhor / última"), "SectorsShowTimes", () => v.ShowTimes, x => v.ShowTimes = x, after);
            AddCheck(root, L.T("Leader's best lap (LMU and AMS2 only)", "Melhor volta do líder (só LMU e AMS2)"), "SectorsShowLeader", () => v.ShowLeader, x => v.ShowLeader = x, after);

            AddAppearance(root, sectors);

            AddSection(root, L.T("COLORS", "CORES"));
            root.Children.Add(Legend(v.CPurple, L.T("Fastest of the session (all drivers where the game shows it, else your own best)", "Mais rápido da sessão (de todos os pilotos onde o jogo informa, senão o seu melhor)")));
            root.Children.Add(Legend(v.CGreen, L.T("Your own best there, but another driver was faster", "O seu melhor no trecho, mas outro piloto foi mais rápido")));
            root.Children.Add(Legend(v.CSlower, L.T("Slower than your best there", "Mais lento que o seu melhor no trecho")));
            root.Children.Add(Legend(Color.FromRgb(36, 36, 44), L.T("Not driven yet in this lap", "Ainda não passou por aqui nesta volta")));
        }

        // small "BETA" label: the overlay works but is still being tuned
        static UIElement BetaBadge(bool big)
        {
            var amber = new SolidColorBrush(Color.FromRgb(255, 200, 0));
            return new Border
            {
                Child = new TextBlock { Text = "BETA", FontSize = big ? 11 : 9, FontWeight = FontWeights.Bold, Foreground = amber },
                BorderBrush = amber, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3),
                Padding = new Thickness(big ? 6 : 5, 1, big ? 6 : 5, 1), Margin = new Thickness(big ? 10 : 8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center
            };
        }

        string MenuName(OverlayWindow o) { return o.Title2 + (beta.Contains(o) ? " (beta)" : ""); }


        // where an overlay is shown: on the track, on the game's setup screen, or both
        void AddVisChoice(Panel p, OverlayWindow o)
        {
            AddSection(p, L.T("WHEN TO SHOW", "QUANDO MOSTRAR"));
            string[] names = { L.T("On track", "Na pista"), L.T("Setup screen", "Tela de setup"), L.T("Both", "Os dois") };
            var row = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            var cells = new Border[3];
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                row.ColumnDefinitions.Add(new ColumnDefinition());
                var tb = new TextBlock { Text = names[i], FontWeight = FontWeights.SemiBold, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center };
                cells[i] = new Border { Child = tb, Padding = new Thickness(0, 7, 0, 7), Cursor = Cursors.Hand, BorderThickness = new Thickness(1), Margin = new Thickness(i == 0 ? 0 : 4, 0, 0, 0) };
                cells[i].MouseLeftButtonUp += delegate { o.SetVisMode(idx); foreach (Action r in refreshers) r(); };
                Grid.SetColumn(cells[i], i);
                row.Children.Add(cells[i]);
            }
            Action paint = delegate
            {
                for (int i = 0; i < 3; i++)
                {
                    bool on = o.VisMode == i;
                    cells[i].Background = new SolidColorBrush(on ? Color.FromRgb(40, 40, 50) : Color.FromRgb(26, 26, 33));
                    cells[i].BorderBrush = on ? Accent : new SolidColorBrush(Color.FromRgb(50, 50, 58));
                    ((TextBlock)cells[i].Child).Foreground = on ? Brushes.White : Muted;
                }
            };
            refreshers.Add(paint); paint();
            p.Children.Add(row);
            p.Children.Add(new TextBlock
            {
                Text = L.T("On the setup screen the overlay appears where you place it (unlock with Ctrl+Shift+L to drag it) and goes away when you leave that screen.",
                           "Na tela de setup o overlay aparece onde você o posicionar (destrave com Ctrl+Shift+L para arrastar) e some quando você sai dessa tela."),
                Foreground = Muted, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0)
            });
        }


        void BuildFuelPage(Panel root)
        {
            FuelView v = fuel.View;
            Action after = fuel.LayoutChanged;
            AddVisChoice(root, fuel);
            AddSection(root, L.T("FUEL", "COMBUSTÍVEL"));
            AddCheck(root, L.T("Fuel in the tank (with a level bar)", "Combustível no tanque (com barra de nível)"), "FuelShowFuel", () => v.ShowFuel, x => v.ShowFuel = x, after);
            AddCheck(root, L.T("Consumption per lap", "Consumo por volta"), "FuelShowPerLap", () => v.ShowPerLap, x => v.ShowPerLap = x, after);
            AddCheck(root, L.T("Laps left on the tank", "Voltas restantes no tanque"), "FuelShowLapsLeft", () => v.ShowLapsLeft, x => v.ShowLapsLeft = x, after);
            AddCheck(root, L.T("Fuel for the whole race (suggestion)", "Combustível para a corrida inteira (sugestão)"), "FuelShowSuggested", () => v.ShowSuggested, x => v.ShowSuggested = x, after);

            AddSection(root, L.T("RACE", "CORRIDA"));
            root.Children.Add(new TextBlock
            {
                Text = L.T("The suggestion is the fuel for the WHOLE race: consumption per lap x the race laps (or the race time divided by your fastest valid lap, remembered from practice and qualifying) plus the margin. Set the race length below, or leave it on auto to use what the game says during the race session.",
                           "A sugestão é o combustível para a corrida INTEIRA: consumo por volta x voltas da corrida (ou o tempo da corrida dividido pela sua volta válida mais rápida, lembrada do treino e do qualy) mais a margem. Defina a duração da corrida abaixo, ou deixe em automático para usar o que o jogo informa na sessão de corrida."),
                Foreground = Muted, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0)
            });
            AddSlider(root, L.T("Safety margin (laps of fuel)", "Margem de segurança (voltas de combustível)"), 0, 3, 0.5, () => fuel.Margin,
                x => fuel.SetMargin(x), x => x.ToString("0.0", CultureInfo.InvariantCulture));
            AddSlider(root, L.T("Race length in laps (auto = what the game says)", "Duração da corrida em voltas (auto = o que o jogo informa)"), 0, 200, 1, () => fuel.RaceLaps,
                x => fuel.SetRaceLaps((int)Math.Round(x)), x => Math.Round(x) <= 0 ? L.T("auto", "auto") : Math.Round(x).ToString(CultureInfo.InvariantCulture));
            AddSlider(root, L.T("Race length in minutes (for timed races; auto = what the game says)", "Duração da corrida em minutos (corrida por tempo; auto = o que o jogo informa)"), 0, 240, 5, () => fuel.RaceMinutes,
                x => fuel.SetRaceMinutes((int)Math.Round(x)), x => Math.Round(x) <= 0 ? L.T("auto", "auto") : Math.Round(x).ToString(CultureInfo.InvariantCulture) + " min");

            AddAppearance(root, fuel);
        }

        /* Tyre overlay settings page: switched off for now, it is not working well yet.
        void BuildTyrePage(Panel root)
        {
            TyreView v = tyre.View;
            Action after = tyre.LayoutChanged;
            AddVisChoice(root, tyre);
            root.Children.Add(new TextBlock
            {
                Text = L.T("After the valid laps asked for, it tells you the COLD pressure to put in the setup so that the hot pressure ends up at the target: target x cold / hot. Example: setup 25.5, hot 27.9 -> 24.7. The cold value is the pressure the car shows at the start of the session, standing in the pits (or the value typed below). Needs a game that publishes tyre pressure (AC EVO, ACC).",
                           "Depois das voltas válidas pedidas, ele diz a pressão A FRIO para colocar no setup para que a pressão a quente fique no alvo: alvo x frio / quente. Exemplo: setup 25,5, quente 27,9 -> 24,7. O valor a frio é a pressão que o carro mostra no começo da sessão, parado no box (ou o valor digitado abaixo). Precisa de um jogo que publique a pressão dos pneus (AC EVO, ACC)."),
                Foreground = Muted, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0)
            });
            AddSection(root, L.T("TARGET", "ALVO"));
            AddSlider(root, L.T("Hot pressure target (psi)", "Pressão alvo a quente (psi)"), 20, 35, 0.1, () => tyre.Target,
                x => tyre.SetTarget(x), x => x.ToString("0.0", CultureInfo.InvariantCulture));
            AddSlider(root, L.T("Valid laps before the suggestion", "Voltas válidas antes da sugestão"), 1, 5, 1, () => tyre.LapsNeeded,
                x => tyre.SetLapsNeeded((int)Math.Round(x)), x => Math.Round(x).ToString(CultureInfo.InvariantCulture));
            AddSection(root, L.T("SHOW", "MOSTRAR"));
            AddCheck(root, L.T("Tyre core temperature", "Temperatura do núcleo do pneu"), "TyreShowTemps", () => v.ShowTemps, x => v.ShowTemps = x, after);
            AddCheck(root, L.T("Suggested cold pressure", "Pressão a frio sugerida"), "TyreShowAdvice", () => v.ShowAdvice, x => v.ShowAdvice = x, after);
            AddSection(root, L.T("SETUP PRESSURE (OPTIONAL)", "PRESSÃO DO SETUP (OPCIONAL)"));
            root.Children.Add(new TextBlock
            {
                Text = L.T("Only if the overlay can't tell the setup pressure itself (for example you opened it after leaving the pits). 0 = automatic.",
                           "Só se o overlay não conseguir saber a pressão do setup sozinho (por exemplo, você o abriu depois de sair do box). 0 = automático."),
                Foreground = Muted, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0)
            });
            string[] names = { L.T("Front left", "Dianteiro esquerdo"), L.T("Front right", "Dianteiro direito"), L.T("Rear left", "Traseiro esquerdo"), L.T("Rear right", "Traseiro direito") };
            for (int i = 0; i < 4; i++)
            {
                int w = i;
                AddSlider(root, names[i], 0, 35, 0.1, () => tyre.ColdOverride(w), x => tyre.SetColdOverride(w, x),
                    x => x < 0.05 ? L.T("auto", "auto") : x.ToString("0.0", CultureInfo.InvariantCulture));
            }
            AddAppearance(root, tyre);
        }
        */


        void BuildElecPage(Panel root)
        {
            ElecView v = elec.View;
            Action after = elec.LayoutChanged;
            root.Children.Add(new TextBlock
            {
                Text = L.T("Only what the car and the game publish is shown: items the car doesn't have take no space.",
                           "Só aparece o que o carro e o jogo publicam: o que o carro não tem não ocupa espaço."),
                Foreground = Muted, FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0)
            });
            AddSection(root, L.T("DRIVER AIDS", "AUXÍLIOS"));
            AddCheck(root, "ABS", "ElecShowAbs", () => v.ShowAbs, x => v.ShowAbs = x, after);
            AddCheck(root, "TC", "ElecShowTc", () => v.ShowTc, x => v.ShowTc = x, after);
            AddCheck(root, L.T("TC cut (TCC)", "Corte do TC (TCC)"), "ElecShowTcc", () => v.ShowTcc, x => v.ShowTcc = x, after);
            AddCheck(root, "ESC", "ElecShowEsc", () => v.ShowEsc, x => v.ShowEsc = x, after);
            AddSection(root, L.T("CAR SETUP", "AJUSTES DO CARRO"));
            AddCheck(root, L.T("Brake bias", "Balança de freio"), "ElecShowBias", () => v.ShowBias, x => v.ShowBias = x, after);
            AddCheck(root, L.T("Engine map", "Mapa do motor"), "ElecShowMap", () => v.ShowMap, x => v.ShowMap = x, after);
            AddCheck(root, L.T("Differential (power / coast)", "Diferencial (potência / freio motor)"), "ElecShowDiff", () => v.ShowDiff, x => v.ShowDiff = x, after);
            AddSection(root, L.T("HYBRID / ENERGY", "HÍBRIDO / ENERGIA"));
            AddCheck(root, L.T("Battery (hybrid / ERS)", "Bateria (híbrido / ERS)"), "ElecShowBattery", () => v.ShowBattery, x => v.ShowBattery = x, after);
            AddCheck(root, L.T("Virtual energy (LMU)", "Energia virtual (LMU)"), "ElecShowEnergy", () => v.ShowEnergy, x => v.ShowEnergy = x, after);
            AddCheck(root, "DRS", "ElecShowDrs", () => v.ShowDrs, x => v.ShowDrs = x, after);
            AddAppearance(root, elec);
        }


        static UIElement Legend(Color c, string text)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0) };
            row.Children.Add(new Border { Width = 28, Height = 12, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush(c), Margin = new Thickness(0, 2, 10, 0), VerticalAlignment = VerticalAlignment.Top });
            row.Children.Add(new TextBlock { Text = text, Foreground = Muted, FontSize = 12, TextWrapping = TextWrapping.Wrap, MaxWidth = 320 });
            return row;
        }

        void AddAppearance(Panel p, OverlayWindow o)
        {
            AddSection(p, L.T("APPEARANCE", "APARÊNCIA"));
            AddSlider(p, L.T("Size", "Tamanho"), OverlayWindow.MinScale * 100, OverlayWindow.MaxScale * 100, 5, () => o.Scale * 100,
                v => { o.ApplyScale(v / 100.0, true); o.QueueSave(); }, v => Math.Round(v) + "%");
            AddSlider(p, L.T("Transparency (lower = more see-through)", "Transparência (menor = mais transparente)"), 20, 100, 5, () => o.Opacity * 100,
                v => o.SetOpacity(v / 100.0), v => Math.Round(v) + "%");
        }

        static void AddSection(Panel p, string title)
        {
            p.Children.Add(new TextBlock
            {
                Text = title, FontWeight = FontWeights.Bold, FontSize = 11, Margin = new Thickness(0, 16, 0, 2), Foreground = Accent
            });
        }

        // key "" = the value is not stored by the checkbox itself (set() does it)
        void AddCheck(Panel p, string label, string key, Func<bool> get, Action<bool> set, Action after)
        {
            var cb = new CheckBox { Content = new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap, MaxWidth = 410 }, IsChecked = get(), Margin = new Thickness(0, 4, 0, 2), Foreground = Brushes.White };
            RoutedEventHandler h = delegate
            {
                bool v = cb.IsChecked == true;
                set(v);
                if (key.Length > 0) cfg.Set(key, v ? "1" : "0");
                if (after != null) after();
            };
            cb.Checked += h; cb.Unchecked += h;
            refreshers.Add(delegate { cb.IsChecked = get(); });
            p.Children.Add(cb);
        }

        void AddSlider(Panel p, string label, double min, double max, double tick, Func<double> get, Action<double> apply, Func<double, string> fmt)
        {
            var head = new DockPanel { Margin = new Thickness(0, 8, 0, 0) };
            var val = new TextBlock { Foreground = Accent, FontWeight = FontWeights.Bold };
            DockPanel.SetDock(val, Dock.Right);
            head.Children.Add(val);
            head.Children.Add(new TextBlock { Text = label });
            var sl = new Slider { Minimum = min, Maximum = max, Value = get(), Margin = new Thickness(0, 4, 0, 0), TickFrequency = tick, IsSnapToTickEnabled = true };
            val.Text = fmt(sl.Value);
            sl.ValueChanged += delegate { val.Text = fmt(sl.Value); apply(sl.Value); };
            refreshers.Add(delegate { sl.Value = get(); });
            p.Children.Add(head); p.Children.Add(sl);
        }

        // ---- tray -------------------------------------------------------------------------
        void BuildTray()
        {
            tray = new WF.NotifyIcon();
            tray.Text = App;
            try
            {
                var bmp = new SD.Bitmap(32, 32);
                using (var g = SD.Graphics.FromImage(bmp))
                {
                    g.Clear(SD.Color.FromArgb(14, 14, 18));
                    g.FillRectangle(new SD.SolidBrush(SD.Color.FromArgb(0, 156, 59)), 0, 0, 6, 32);
                    g.FillRectangle(new SD.SolidBrush(SD.Color.FromArgb(255, 223, 0)), 0, 12, 6, 8);
                    g.DrawString("S", new SD.Font("Segoe UI", 16, SD.FontStyle.Bold), SD.Brushes.White, 8, 3);
                }
                tray.Icon = SD.Icon.FromHandle(bmp.GetHicon());
            }
            catch { tray.Icon = SD.SystemIcons.Application; }
            var menu = new WF.ContextMenuStrip();
            miLock = new WF.ToolStripMenuItem(L.T("Unlock position (Ctrl+Shift+L)", "Destravar posição (Ctrl+Shift+L)"), null, delegate { ToggleLock(); });
            miDemo = new WF.ToolStripMenuItem(L.T("Demo mode", "Modo demonstração"), null, delegate { useDemo = !useDemo; miDemo.Checked = useDemo; });
            miDemo.Checked = useDemo;
            var miSettings = new WF.ToolStripMenuItem(L.T("Settings...", "Configurações..."), null, delegate { ShowSettings(); });
            miSettings.Font = new SD.Font(menu.Font, SD.FontStyle.Bold);
            menu.Items.Add(miSettings);
            menu.Items.Add(miLock);

            var overlaysMenu = new WF.ToolStripMenuItem(L.T("Overlays", "Overlays"));
            var size = new WF.ToolStripMenuItem(L.T("Size", "Tamanho"));
            foreach (OverlayWindow ov in overlays)
            {
                OverlayWindow o = ov;
                var on = new WF.ToolStripMenuItem(MenuName(o), null, delegate { o.SetEnabled(!o.Enabled); miOn[o].Checked = o.Enabled; if (settingsWin != null) foreach (Action r in refreshers) r(); });
                on.Checked = o.Enabled;
                miOn[o] = on; overlaysMenu.DropDownItems.Add(on);

                var sub = new WF.ToolStripMenuItem(MenuName(o));
                var items = new List<WF.ToolStripMenuItem>();
                foreach (int pct in SizePcts)
                {
                    int p = pct;
                    var mi = new WF.ToolStripMenuItem(p + "%", null, delegate { o.ApplyScale(p / 100.0, true); o.SaveState(); });
                    sub.DropDownItems.Add(mi); items.Add(mi);
                }
                sub.DropDownItems.Add(new WF.ToolStripSeparator());
                sub.DropDownItems.Add(new WF.ToolStripMenuItem(L.T("Bigger (Ctrl+Shift++)", "Maior (Ctrl+Shift++)"), null, delegate { o.Step(1.1); }));
                sub.DropDownItems.Add(new WF.ToolStripMenuItem(L.T("Smaller (Ctrl+Shift+-)", "Menor (Ctrl+Shift+-)"), null, delegate { o.Step(1 / 1.1); }));
                size.DropDownItems.Add(sub);
                miSizes[o] = items;
            }
            menu.Items.Add(overlaysMenu);
            menu.Items.Add(size);
            menu.Items.Add(new WF.ToolStripMenuItem(L.T("Reset positions", "Restaurar posições"), null, delegate { foreach (OverlayWindow w in overlays) w.ResetPosition(); }));
            menu.Items.Add(miDemo);
            miAutoHide = new WF.ToolStripMenuItem(L.T("Hide when not driving", "Esconder quando não está pilotando"), null, delegate
            {
                autoHide = !autoHide; miAutoHide.Checked = autoHide;
                cfg.Set("AutoHide", autoHide ? "1" : "0"); SaveAll();
            });
            miAutoHide.Checked = autoHide;
            menu.Items.Add(miAutoHide);
            menu.Items.Add(new WF.ToolStripMenuItem(L.T("Quit", "Sair"), null, delegate { Quit(); }));
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { ToggleLock(); };
            tray.Visible = true;
            UpdateSizeMenu();
        }

        void Quit()
        {
            SaveAll();
            tray.Visible = false; tray.Dispose();
            if (hotkeys != null) { for (int id = 1; id <= 5; id++) UnregisterHotKey(hotkeys.Handle, id); hotkeys.Dispose(); }
            foreach (OverlayWindow w in overlays) w.Close();
            Application.Current.Shutdown();
        }

        // ---- frame ------------------------------------------------------------------------
        void Tick(object sender, EventArgs e)
        {
            Tele t = tele;
            bool preview = useDemo || settingsOpen;
            if (preview) demo.Read(t);
            else
            {
                bool ok = games.Read(t);
                if (!ok || !t.Live)
                {
                    t.Gas = t.Brake = t.Clutch = 0; t.AbsOn = t.TcOn = t.ShiftUp = false;
                }
                if (games.Active != lastSource) SourceChanged(games.Active);
            }
            pedals.Tick(t, preview || t.HasTcCut);
            sectors.Tick(t, preview);
            fuel.Tick(t);
            elec.Tick(t);
            // tyre.Tick(t, preview);
            bool onTrack = ShouldShow(t), force = !locked || useDemo || settingsOpen;
            bool inSetup = t.Live && t.InSetup && (!hideUnfocused || GameFocused());
            foreach (OverlayWindow w in overlays) { w.Present(force || w.WantShown(onTrack, inSetup)); w.PollHover(); }
        }

        bool ShouldShow(Tele t)
        {
            if (!locked || useDemo || !autoHide || settingsOpen) return true;
            if (!t.Live) return false;                          // game closed, loading or frozen
            if (t.Status == 2) sawLiveStatus = true;
            if (t.Status == 1 || t.Status == 3) return false;   // replay / pause
            if (t.Status == 0 && sawLiveStatus) return false;   // back to menus (only trusted once we've seen "live")
            if (hideInPitBox && t.InPitBox && t.SpeedKmh < 2) return false;   // setup screen / garage
            if (hideUnfocused && !GameFocused()) return false;
            return true;
        }

        bool GameFocused()
        {
            uint pid;
            GetWindowThreadProcessId(GetForegroundWindow(), out pid);
            if (pid == OwnPid) return true;
            if (pid != lastFgPid)
            {
                lastFgPid = pid;
                lastFgIsGame = false;
                try
                {
                    string name = System.Diagnostics.Process.GetProcessById((int)pid).ProcessName;
                    foreach (ITelemetrySource s in games.All)
                        if (lastSource == null || s == lastSource)
                            foreach (string p in s.Processes)
                                if (name.Equals(p, StringComparison.OrdinalIgnoreCase)) lastFgIsGame = true;
                }
                catch { }
            }
            return lastFgIsGame;
        }

        // Another game took over: forget what we learned about the previous one.
        void SourceChanged(ITelemetrySource s)
        {
            lastSource = s;
            sawLiveStatus = false;
            lastFgPid = 0;
            pedals.SourceChanged();
            if (s != null) tray.Text = App + " " + Ver + "  -  " + s.Name;
        }
    }
}
