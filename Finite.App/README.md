# Finite.App

The WPF application: shell, 12 pages, view models, custom controls, theming, DI host.

## Root files

| File | Purpose |
|---|---|
| `App.xaml` | App resources. **Contains `<ui:ThemesDictionary Theme="Dark" />` — do not remove**: `ApplicationThemeManager.Apply` *replaces* this dictionary, so without it theme switching silently does nothing. Also hosts the shared converters |
| `App.xaml.cs` | Generic host: DI registrations (DbContext, 12 services, 12 VMs, 12 pages), `DbInitializer.Initialize`, applies persisted theme on startup, global `DispatcherUnhandledException` → friendly error dialog instead of crash |
| `AppSettings.cs` | `AppThemeChoice` (Dark/Light/System) persisted to `%APPDATA%\FiniteDesktop\settings.json`; `App.ApplyTheme(choice)` calls WPF-UI `ApplicationThemeManager.Apply` |
| `DbPaths.cs` | `%APPDATA%\FiniteDesktop\` — database dir, db path, settings live next to it |
| `NavigationPageProvider.cs` | Implements WPF-UI `IPageService` resolving pages from the DI container — this is why every page takes its VM via constructor |
| `AssemblyInfo.cs` | `[ThemeInfo]` → custom control default styles are looked up in `Themes/generic.xaml` |
| `Finite.App.csproj` | net8.0-windows, WPF-UI, CommunityToolkit.Mvvm, Hosting; version metadata |

## Views/

`MainWindow.xaml(.cs)` — `FluentWindow` + `NavigationView`: grouped sidebar (core / management / reports), footer Settings item, `RootNavigation.SetPageService(new NavigationPageProvider(App.Services))`, navigates to Dashboard on load. Navigation items use `TargetPageType="{x:Type pages:XPage}"`.

## Pages/ — one folder pair per feature

Code-behind is always 10 lines: constructor takes the VM, `InitializeComponent()`, `DataContext = vm`.

| Page | VM | Feature notes |
|---|---|---|
| `DashboardPage` | `DashboardViewModel` | Net worth / in-out / cash-flow / daily-average cards, 30-day trend chart, budget alerts, goal progress, recent transactions. **Runs `RecurrenceEngine.ProcessDueAsync` on load** — that's the recurring catch-up trigger |
| `TransactionsPage` | `TransactionsViewModel` | Filter by account/type, add form, income green / expense red |
| `AccountsPage` | `AccountsViewModel` | Conditional fields per account type (wallet provider / bank / credit limit), **ColorPickerButton** bound to `Color` string, per-row color swatch via `HexToBrushConverter` |
| `TransfersPage` | `TransfersViewModel` | From/to/amount/fee form; fee note shown |
| `BudgetsPage` | `BudgetsViewModel` | Cards with `ColoredProgressBar` — green < threshold, yellow near, red over |
| `DebtsPage` | `DebtsViewModel` | Receivable/payable badges (pre-computed brushes), overdue red, Record Payment + Settle actions |
| `SavingsGoalsPage` | `SavingsGoalsViewModel` | Progress bars colored per goal, ACHIEVED state |
| `RecurringPage` | `RecurringViewModel` | Overdue highlight, Create Now (respects end date), pause/resume |
| `CategoriesPage` | `CategoriesViewModel` | System categories delete-protected; ColorPickerButton for custom ones |
| `AnalyticsPage` | `AnalyticsViewModel` | Bar chart (6-month in/out), line chart (30-day), top categories with colored mini bars, top merchants, health score. **Pre-computes category brushes in the VM** — hex-string binding in DataTemplates is unreliable |
| `BillCalendarPage` | `BillCalendarViewModel` | Monday-start month grid. Layout rule: outer ItemsControl = weeks (stack), inner ItemsControl = days with `UniformGrid Columns=7 Rows=1` ItemsPanel — **never wrap the inner grid in another UniformGrid** (that collapsed the calendar into a left column). Chips use pre-computed `ChipBrush`/`TextBrush` |
| `SettingsPage` | `SettingsViewModel` | Theme ComboBox (live apply + persist), db path + open folder, Backup/Restore (closes db connection first), About |

## ViewModels/

CommunityToolkit.Mvvm: `[ObservableProperty]` for fields, `[RelayCommand]` for async actions, `partial void OnXChanged()` for dependent flags (e.g. `ShowWalletField`). Pattern: `LoadAsync` clears + repopulates `ObservableCollection`; actions catch `ValidationException` → `StatusText`.

## Controls/ — custom, dependency-free

| Control | Notes |
|---|---|
| `ChartBase` | Base for charts. Subscribes to `INotifyCollectionChanged` so `OnRender` re-runs when items are **added** (not just when the reference changes — that's why charts were blank). Also re-renders all charts on theme change via `ApplicationThemeManager.Changed` (subscribe on Loaded, unsubscribe on Unloaded) |
| `SimpleBarChart` | `Series` (label, hex color, values) + `Labels`; grouped bars, legend, hover tooltips |
| `SimpleLineChart` | Line + gradient area. **Build line and area in ONE pass, two separate `StreamGeometryContext`s** — re-opening a frozen/cloned geometry throws "BeginFigure must be called before this API" and kills the whole chart. Zero baseline for negative dips |
| `ColoredProgressBar` | Progress bar honoring `Foreground` (WPF-UI's themed ProgressBar ignores it). `DefaultStyleKeyProperty.OverrideMetadata` **must be in the static ctor** — in the instance ctor it throws on every instantiation. Template lives in `Themes/generic.xaml` |
| `ColorPickerButton` | Button + popup: saturation/value box, hue slider, 10 presets, hex entry, live preview. Two-way `Color` + `HexText` (string, binds straight to VM properties). **Open on MouseUp, not MouseDown** — `StaysOpen=False` popup captures the mouse on open, so the same click's MouseUp registers as an outside click and closes it instantly. SV/hue drags capture the mouse so picking works outside the box |

## Converters/

`InverseBoolConverter` · `BoolToRedConverter` · `BoolToVisibilityConverter` · `HexToBrushConverter` (throw-safe: bad/missing hex → neutral gray) · `CalendarConverters` (today brush, out-of-month opacity). Registered as keys in `App.xaml` resources.

## Themes/generic.xaml

Default styles for `ColoredProgressBar` and `ColorPickerButton` (found via `[ThemeInfo]`). Custom `Control` classes need a style here **or** an explicit `Style` — otherwise the template is null and template parts never apply.

## Recipes

**Add a page:** VM (`ViewModels/`) → Page XAML + code-behind (constructor injection) → register VM + page in `App.xaml.cs` → `NavigationViewItem` in `MainWindow.xaml` → done.

**Add a chart:** subclass `ChartBase`, register `DependencyProperty`(ies) with `AffectsRender`, call `Watch(...)` in the property-changed callback, draw in `OnRender` with `FindBackgroundBrush()` + theme-consistent alpha brushes.

**Change the theme behavior:** everything flows through `App.ApplyTheme` — keep the `ThemesDictionary` in `App.xaml`.

**Bind a color from a VM:** pre-compute a `Brush` in the VM (see `AnalyticsViewModel`/`BillCalendarViewModel`) or use `HexToBrushConverter`; don't bind raw hex strings to `Fill`/`Background`.
