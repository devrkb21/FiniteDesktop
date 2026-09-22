using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finite.Core;
using Finite.Data;
using Microsoft.EntityFrameworkCore;

namespace Finite.App.ViewModels;

public partial class BillCalendarViewModel(FiniteDbContext db) : ObservableObject
{
    public ObservableCollection<CalendarWeek> Weeks { get; } = [];

    [ObservableProperty] private string _monthLabel = "";
    [ObservableProperty] private string _monthTotalText = "";

    private int _year = DateTime.Today.Year;
    private int _month = DateTime.Today.Month;

    [RelayCommand]
    private Task LoadAsync() => LoadAsyncCore();

    [RelayCommand]
    private async Task PreviousMonthAsync()
    {
        (_year, _month) = _month == 1 ? (_year - 1, 12) : (_year, _month - 1);
        await LoadAsyncCore();
    }

    [RelayCommand]
    private async Task NextMonthAsync()
    {
        (_year, _month) = _month == 12 ? (_year + 1, 1) : (_year, _month + 1);
        await LoadAsyncCore();
    }

    [RelayCommand]
    private async Task GoTodayAsync()
    {
        _year = DateTime.Today.Year;
        _month = DateTime.Today.Month;
        await LoadAsyncCore();
    }

    private async Task LoadAsyncCore()
    {
        var first = new DateOnly(_year, _month, 1);
        MonthLabel = first.ToString("MMMM yyyy");

        // Monday-start grid (Bangladesh convention), 6 rows max.
        var gridStart = first.AddDays(-((int)first.DayOfWeek + 6) % 7);
        var lastDay = new DateOnly(_year, _month, DateTime.DaysInMonth(_year, _month));
        var gridEnd = lastDay.AddDays(7 - (((int)lastDay.DayOfWeek + 6) % 7) - 1);

        // Items due in the visible range.
        var recurring = await db.RecurringTransactions.AsNoTracking()
            .Where(r => r.IsActive && r.NextOccurrence >= gridStart && r.NextOccurrence <= gridEnd)
            .ToListAsync();
        var debts = await db.Debts.AsNoTracking()
            .Where(d => d.DueDate != null && d.DueDate >= gridStart && d.DueDate <= gridEnd
                        && d.Status != DebtStatus.Settled && d.Status != DebtStatus.Cancelled)
            .ToListAsync();

        var itemsByDate = new Dictionary<DateOnly, List<BillItem>>();
        void Add(DateOnly date, BillItem item)
        {
            if (!itemsByDate.TryGetValue(date, out var list)) itemsByDate[date] = list = [];
            list.Add(item);
        }

        foreach (var r in recurring)
            Add(r.NextOccurrence, new BillItem(r.Name, r.Amount, r.Type == TransactionType.Income));
        foreach (var d in debts)
            Add(d.DueDate!.Value, new BillItem(d.PersonName, d.Remaining, d.Type == DebtType.Receivable, true));

        Weeks.Clear();
        var monthTotal = 0m;
        var cursor = gridStart;
        while (cursor <= gridEnd)
        {
            var days = new List<CalendarDay>(7);
            for (var i = 0; i < 7; i++)
            {
                var date = cursor;
                var items = itemsByDate.GetValueOrDefault(date) ?? [];
                monthTotal += date.Month == _month ? items.Sum(x => x.IsIncoming ? 0 : x.Amount) : 0;
                days.Add(new CalendarDay(date, date.Month == _month, date == DateOnly.FromDateTime(DateTime.Today), items));
                cursor = cursor.AddDays(1);
            }
            Weeks.Add(new CalendarWeek(days));
        }

        MonthTotalText = $"Upcoming bills this month: ৳{monthTotal:N2}";
    }
}

public record CalendarWeek(IReadOnlyList<CalendarDay> Days);

public record CalendarDay(DateOnly Date, bool InMonth, bool IsToday, IReadOnlyList<BillItem> Items)
{
    public string DayNumber => Date.Day.ToString();
    public string DateFull => Date.ToString("dd MMM");
}

public record BillItem(string Label, decimal Amount, bool IsIncoming, bool IsDebt = false)
{
    public string Text => $"{Label} ৳{Amount:N0}";

    // Pre-computed brushes (XAML ColorConverter on enum-typed values broke color coding).
    public System.Windows.Media.Brush ChipBrush => new System.Windows.Media.SolidColorBrush(
        IsIncoming
            ? System.Windows.Media.Color.FromArgb(0x2E, 0x4A, 0xDE, 0x80)   // green tint
            : IsDebt
                ? System.Windows.Media.Color.FromArgb(0x2E, 0xF8, 0x71, 0x71) // red tint
                : System.Windows.Media.Color.FromArgb(0x2E, 0xF5, 0x9E, 0x0B)); // amber tint

    public System.Windows.Media.Brush TextBrush => new System.Windows.Media.SolidColorBrush(
        IsIncoming
            ? System.Windows.Media.Color.FromRgb(0x4A, 0xDE, 0x80)
            : IsDebt
                ? System.Windows.Media.Color.FromRgb(0xF8, 0x71, 0x71)
                : System.Windows.Media.Color.FromRgb(0xF5, 0x9E, 0x0B));
}
