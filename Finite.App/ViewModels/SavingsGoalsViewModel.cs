using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finite.Core;
using Finite.Services;

namespace Finite.App.ViewModels;

public partial class SavingsGoalsViewModel(SavingsGoalService goals, AccountService accounts) : ObservableObject
{
    public ObservableCollection<GoalRow> Items { get; } = [];
    public ObservableCollection<Account> AccountOptions { get; } = [];

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _targetAmount = "";
    [ObservableProperty] private Account? _selectedAccount;
    [ObservableProperty] private DateTime? _targetDate;
    [ObservableProperty] private string _statusText = "";

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (AccountOptions.Count == 0)
        {
            foreach (var account in await accounts.GetAllAsync()) AccountOptions.Add(account);
        }

        Items.Clear();
        foreach (var view in await goals.GetAllAsync()) Items.Add(GoalRow.From(view));
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(Name)) { StatusText = "Enter a goal name."; return; }
        if (!decimal.TryParse(TargetAmount, out var target) || target <= 0) { StatusText = "Target must be positive."; return; }
        if (SelectedAccount is null) { StatusText = "Select the savings account."; return; }

        try
        {
            await goals.CreateAsync(new SavingsGoal
            {
                Name = Name.Trim(),
                TargetAmount = target,
                AccountId = SelectedAccount.Id,
                TargetDate = TargetDate.HasValue ? DateOnly.FromDateTime(TargetDate.Value) : null,
            });
            Name = "";
            TargetAmount = "";
            TargetDate = null;
            StatusText = "Goal created.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(GoalRow? row)
    {
        if (row is null) return;
        try
        {
            await goals.DeleteAsync(row.Id);
            StatusText = "Goal deleted.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }
}

public class GoalRow
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string AccountName { get; init; } = "";
    public string CurrentText { get; init; } = "";
    public string TargetText { get; init; } = "";
    public double Progress { get; init; }
    public string ProgressText { get; init; } = "";
    public string Color { get; init; } = "#10B981";
    public bool Completed { get; init; }
    public string TargetDateText { get; init; } = "";

    public static GoalRow From(SavingsGoalView view) => new()
    {
        Id = view.Goal.Id,
        Name = view.Goal.Name,
        AccountName = view.Goal.Account?.Name ?? "—",
        CurrentText = $"৳{view.Current:N2}",
        TargetText = $"of ৳{view.Goal.TargetAmount:N2}",
        Progress = view.Progress,
        ProgressText = $"{view.Progress:0}%",
        Color = view.Completed ? "#4ADE80" : view.Goal.Color,
        Completed = view.Completed,
        TargetDateText = view.Goal.TargetDate is DateOnly d ? $"by {d:dd MMM yyyy}" : "",
    };
}
