using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finite.Core;
using Finite.Data;
using Finite.Services;
using Microsoft.EntityFrameworkCore;

namespace Finite.App.ViewModels;

public partial class TransactionsViewModel(
    TransactionService transactions,
    AccountService accounts,
    FiniteDbContext db) : ObservableObject
{
    public ObservableCollection<Transaction> Items { get; } = [];
    public ObservableCollection<Account> AccountOptions { get; } = [];
    public ObservableCollection<Category> CategoryOptions { get; } = [];

    [ObservableProperty] private Account? _selectedAccount;
    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private TransactionType _selectedType = TransactionType.Expense;
    [ObservableProperty] private string _amount = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private DateTime _date = DateTime.Today;
    [ObservableProperty] private string _statusText = "";

    public Array TransactionTypes { get; } = Enum.GetValues<TransactionType>();

    [RelayCommand]
    private async Task LoadAsync()
    {
        foreach (var account in await accounts.GetAllAsync()) AccountOptions.Add(account);

        CategoryOptions.Clear();
        var cats = await db.Categories.AsNoTracking()
            .Where(c => c.IsActive)
            .Where(c => c.Type == CategoryType.Expense || c.Type == CategoryType.Income || c.Type == CategoryType.Both)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
        foreach (var category in cats) CategoryOptions.Add(category);

        Items.Clear();
        foreach (var t in await transactions.GetRecentAsync(100)) Items.Add(t);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (SelectedAccount is null) { StatusText = "Select an account."; return; }
        if (!decimal.TryParse(Amount, out var parsedAmount) || parsedAmount <= 0)
        {
            StatusText = "Amount must be a positive number.";
            return;
        }

        try
        {
            await transactions.CreateAsync(new Transaction
            {
                AccountId = SelectedAccount.Id,
                CategoryId = SelectedCategory?.Id,
                Type = SelectedType,
                Amount = parsedAmount,
                Date = DateOnly.FromDateTime(Date),
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            });

            Amount = "";
            Description = "";
            StatusText = "Transaction saved.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Transaction? t)
    {
        if (t is null) return;
        try
        {
            await transactions.DeleteAsync(t.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }
}
