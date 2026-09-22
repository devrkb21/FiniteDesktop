using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finite.Core;
using Finite.Services;

namespace Finite.App.ViewModels;

public partial class DashboardViewModel(
    BalanceService balances,
    ReportService reports,
    TransactionService transactions,
    RecurrenceEngine recurrence,
    BudgetEvaluator budgetEvaluator,
    SavingsGoalService goals) : ObservableObject
{
    [ObservableProperty] private string _netWorth = "৳ 0.00";
    [ObservableProperty] private string _assets = "৳ 0.00";
    [ObservableProperty] private string _liabilities = "৳ 0.00";
    [ObservableProperty] private string _monthlyIncome = "৳ 0.00";
    [ObservableProperty] private string _monthlyExpense = "৳ 0.00";
    [ObservableProperty] private string _monthlyFlow = "৳ 0.00";
    [ObservableProperty] private string _dailyAverage = "৳ 0.00";
    [ObservableProperty] private string _lastRun = "";

    public ObservableCollection<decimal> TrendValues { get; } = [];
    public ObservableCollection<string> TrendLabels { get; } = [];

    public ObservableCollection<Transaction> RecentTransactions { get; } = [];
    public ObservableCollection<AlertRow> BudgetAlerts { get; } = [];
    public ObservableCollection<GoalMiniRow> GoalProgress { get; } = [];

    public bool HasNoAlerts => BudgetAlerts.Count == 0;

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        var worth = await balances.GetNetWorthAsync(ct);
        NetWorth = Format(worth.NetWorth);
        Assets = Format(worth.Assets);
        Liabilities = Format(worth.Liabilities);

        var (income, expense) = await reports.GetCurrentMonthFlowAsync(ct);
        MonthlyIncome = Format(income);
        MonthlyExpense = Format(expense);
        MonthlyFlow = Format(income - expense);
        var daysElapsed = DateTime.Today.Day;
        DailyAverage = Format(daysElapsed > 0 ? expense / daysElapsed : 0);

        // Catch up recurring items (cheap; idempotent).
        var created = await recurrence.ProcessDueAsync(ct: ct);
        LastRun = created.Count > 0 ? $"Recurring engine created {created.Count} transaction(s) just now." : "";

        // 30-day trend
        var trend = await reports.GetBalanceTrendAsync(30, ct);
        TrendValues.Clear();
        TrendLabels.Clear();
        foreach (var point in trend)
        {
            TrendValues.Add(point.Balance);
            TrendLabels.Add(point.Date.ToString("dd MMM"));
        }

        // Budget alerts (threshold reached)
        BudgetAlerts.Clear();
        foreach (var status in await budgetEvaluator.EvaluateActiveAsync(ct: ct))
        {
            if (status.ShouldAlert)
                BudgetAlerts.Add(new AlertRow(status.Budget.Name, status.PercentUsed, status.IsOver));
        }
        OnPropertyChanged(nameof(HasNoAlerts));

        // Goal progress
        GoalProgress.Clear();
        foreach (var view in await goals.GetAllAsync(ct: ct))
            GoalProgress.Add(new GoalMiniRow(view.Goal.Name, view.Current, view.Goal.TargetAmount, view.Progress, view.Completed));

        RecentTransactions.Clear();
        foreach (var t in await transactions.GetRecentAsync(10, ct))
            RecentTransactions.Add(t);
    }

    private static string Format(decimal value) =>
        (value < 0 ? "− ৳ " : "৳ ") + Math.Abs(value).ToString("N2");
}

public record AlertRow(string Name, double Percent, bool IsOver)
{
    public string Text => IsOver
        ? $"{Name} — OVER budget by {Percent - 100:0}%"
        : $"{Name} — {Percent:0}% used";
    public string Color => IsOver ? "#F87171" : "#F59E0B";
}

public record GoalMiniRow(string Name, decimal Current, decimal Target, double Progress, bool Completed)
{
    public string Text => Completed ? $"{Name} ✓" : $"{Name} — {Progress:0}%";
    public string Color => Completed ? "#4ADE80" : "#3B82F6";
}
