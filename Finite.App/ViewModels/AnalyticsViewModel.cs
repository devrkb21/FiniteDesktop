using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finite.App.Controls;
using Finite.Services;

namespace Finite.App.ViewModels;

public partial class AnalyticsViewModel(
    ReportService reports,
    BalanceService balances,
    BudgetEvaluator budgetEvaluator) : ObservableObject
{
    public ObservableCollection<SimpleBarChart.NamedSeries> MonthlySeries { get; } = [];
    public ObservableCollection<string> MonthLabels { get; } = [];

    public ObservableCollection<decimal> TrendValues { get; } = [];
    public ObservableCollection<string> TrendLabels { get; } = [];

    public ObservableCollection<CategoryRow> Categories { get; } = [];
    public ObservableCollection<CategoryRow> Merchants { get; } = [];
    public ObservableCollection<BudgetAlertRow> BudgetAlerts { get; } = [];

    [ObservableProperty] private string _healthScore = "—";
    [ObservableProperty] private string _healthLabel = "";
    [ObservableProperty] private string _healthColor = "#9CA3AF";
    [ObservableProperty] private string _dailyAverage = "৳ 0.00";
    [ObservableProperty] private string _savingsRate = "0%";
    [ObservableProperty] private string _netWorth = "৳ 0.00";

    public bool HasNoAlerts => BudgetAlerts.Count == 0;

    public decimal MaxCategoryTotal => Categories.Count == 0 ? 1 : Categories.Max(c => c.Total);

    [RelayCommand]
    private async Task LoadAsync()
    {
        // Monthly income vs expense (6 months)
        var flow = await reports.GetMonthlyFlowAsync(6);
        MonthLabels.Clear();
        foreach (var m in flow) MonthLabels.Add(m.Label);
        MonthlySeries.Clear();
        MonthlySeries.Add(new SimpleBarChart.NamedSeries("Income", "#4ADE80", flow.Select(f => f.Income).ToList()));
        MonthlySeries.Add(new SimpleBarChart.NamedSeries("Expense", "#F87171", flow.Select(f => f.Expense).ToList()));

        // 30-day balance trend
        var trend = await reports.GetBalanceTrendAsync(30);
        TrendValues.Clear();
        TrendLabels.Clear();
        foreach (var point in trend)
        {
            TrendValues.Add(point.Balance);
            TrendLabels.Add(point.Date.ToString("dd MMM"));
        }

        // Category breakdown
        Categories.Clear();
        foreach (var c in await reports.GetCategoryBreakdownAsync(8))
            Categories.Add(new CategoryRow(c.Name, c.Color, c.Total));

        // Top merchants
        Merchants.Clear();
        foreach (var m in await reports.GetTopMerchantsAsync(8))
            Merchants.Add(new CategoryRow(m.Name, "#9CA3AF", m.Total));

        // Budget alerts
        BudgetAlerts.Clear();
        foreach (var status in await budgetEvaluator.EvaluateActiveAsync())
        {
            if (status.ShouldAlert)
                BudgetAlerts.Add(new BudgetAlertRow(status.Budget.Name, status.PercentUsed, status.IsOver));
        }
        OnPropertyChanged(nameof(HasNoAlerts));
        OnPropertyChanged(nameof(MaxCategoryTotal));

        // Health + averages
        var (income, expense) = await reports.GetCurrentMonthFlowAsync();
        var worth = await balances.GetNetWorthAsync();
        NetWorth = $"৳ {worth.NetWorth:N2}";

        var daysElapsed = DateTime.Today.Day;
        DailyAverage = $"৳ {(daysElapsed > 0 ? expense / daysElapsed : 0):N2}";

        SavingsRate = income > 0 ? $"{(double)((income - expense) / income) * 100:0}%" : "0%";

        var score = ComputeHealthScore(worth.NetWorth, income, expense, daysElapsed);
        HealthScore = $"{score}/100";
        HealthColor = score >= 70 ? "#4ADE80" : score >= 40 ? "#F59E0B" : "#F87171";
        HealthLabel = score switch
        {
            >= 70 => "Good — saving consistently",
            >= 40 => "Fair — watch your spending",
            _ => "Struggling — expenses exceed income",
        };
    }

    private static int ComputeHealthScore(decimal netWorth, decimal income, decimal expense, int daysElapsed)
    {
        if (daysElapsed == 0 || income <= 0) return netWorth >= 0 ? 50 : 10;
        var rate = (double)((income - expense) / income); // savings rate
        var score = 50 + rate * 100;                      // ±50 by savings rate
        if (netWorth > 0) score += 10;
        if (netWorth < 0) score -= 20;
        return Math.Clamp((int)Math.Round(score), 0, 100);
    }
}

public record CategoryRow(string Name, string Color, decimal Total)
{
    public string TotalText => $"৳{Total:N2}";

    // Pre-parsed brush — binding to the raw hex string failed inside DataTemplates.
    public System.Windows.Media.Brush Brush
    {
        get
        {
            try
            {
                return new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Color));
            }
            catch
            {
                return System.Windows.Media.Brushes.Gray;
            }
        }
    }
}

public record BudgetAlertRow(string Name, double Percent, bool IsOver)
{
    public string Text => IsOver
        ? $"{Name} — OVER by {Percent - 100:0}%"
        : $"{Name} — {Percent:0}% used";
    public string Color => IsOver ? "#F87171" : "#F59E0B";
}
