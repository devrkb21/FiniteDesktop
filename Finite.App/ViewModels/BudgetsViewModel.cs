using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finite.Core;
using Finite.Data;
using Finite.Services;
using Microsoft.EntityFrameworkCore;

namespace Finite.App.ViewModels;

public partial class BudgetsViewModel(
    BudgetService budgets,
    BudgetEvaluator evaluator,
    FiniteDbContext db) : ObservableObject
{
    public ObservableCollection<BudgetRow> Items { get; } = [];
    public ObservableCollection<Category> CategoryOptions { get; } = [];

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private string _amount = "";
    [ObservableProperty] private BudgetPeriod _selectedPeriod = BudgetPeriod.Monthly;
    [ObservableProperty] private string _statusText = "";

    public Array Periods { get; } = Enum.GetValues<BudgetPeriod>();

    [RelayCommand]
    private async Task LoadAsync()
    {
        CategoryOptions.Clear();
        var cats = await db.Categories.AsNoTracking()
            .Where(c => c.IsActive && c.Type != CategoryType.Income)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
        foreach (var category in cats) CategoryOptions.Add(category);

        var statuses = await evaluator.EvaluateActiveAsync();
        var all = await budgets.GetAllAsync();
        var statusById = statuses.ToDictionary(s => s.Budget.Id);

        Items.Clear();
        foreach (var budget in all)
        {
            if (statusById.TryGetValue(budget.Id, out var status))
                Items.Add(BudgetRow.From(budget, status.Spent, status.PercentUsed));
            else
                Items.Add(BudgetRow.From(budget, 0, 0));
        }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (SelectedCategory is null) { StatusText = "Select a category."; return; }
        if (!decimal.TryParse(Amount, out var parsed) || parsed <= 0) { StatusText = "Amount must be positive."; return; }

        try
        {
            await budgets.CreateAsync(new Budget
            {
                Name = string.IsNullOrWhiteSpace(Name) ? $"{SelectedCategory.Name} budget" : Name.Trim(),
                CategoryId = SelectedCategory.Id,
                Amount = parsed,
                Period = SelectedPeriod,
            });
            Name = "";
            Amount = "";
            StatusText = "Budget created.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(BudgetRow? row)
    {
        if (row is null) return;
        try
        {
            await budgets.DeleteAsync(row.Id);
            StatusText = "Budget deleted.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }
}

public class BudgetRow
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string CategoryName { get; init; } = "";
    public string AmountText { get; init; } = "";
    public string SpentText { get; init; } = "";
    public string RemainingText { get; init; } = "";
    public double PercentUsed { get; init; }
    public string PercentText { get; init; } = "";
    public string ProgressColor { get; init; } = "#10B981";
    public bool Alert { get; init; }

    public static BudgetRow From(Budget budget, decimal spent, double percent) => new()
    {
        Id = budget.Id,
        Name = budget.Name,
        CategoryName = budget.Category?.Name ?? "—",
        AmountText = $"৳{budget.Amount:N2}",
        SpentText = $"৳{spent:N2}",
        RemainingText = $"৳{Math.Max(0, budget.Amount - spent):N2}",
        PercentUsed = percent,
        PercentText = $"{percent:0}%",
        ProgressColor = percent >= 100 ? "#EF4444" : percent >= budget.AlertThreshold ? "#F59E0B" : "#10B981",
        Alert = percent >= budget.AlertThreshold,
    };
}
