using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finite.Core;
using Finite.Data;
using Finite.Services;
using Microsoft.EntityFrameworkCore;

namespace Finite.App.ViewModels;

public partial class RecurringViewModel(RecurringTransactionService recurring, AccountService accounts, FiniteDbContext db) : ObservableObject
{
    public ObservableCollection<RecurringRow> Items { get; } = [];
    public ObservableCollection<Account> AccountOptions { get; } = [];
    public ObservableCollection<Category> CategoryOptions { get; } = [];

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private TransactionType _selectedType = TransactionType.Expense;
    [ObservableProperty] private string _amount = "";
    [ObservableProperty] private Account? _selectedAccount;
    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private Frequency _selectedFrequency = Frequency.Monthly;
    [ObservableProperty] private string _dayOfMonth = "";
    [ObservableProperty] private DateTime _startDate = DateTime.Today;
    [ObservableProperty] private DateTime? _endDate;
    [ObservableProperty] private string _statusText = "";

    public Array Frequencies { get; } = Enum.GetValues<Frequency>();
    public Array TransactionTypes { get; } = Enum.GetValues<TransactionType>();

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (AccountOptions.Count == 0)
        {
            foreach (var account in await accounts.GetAllAsync()) AccountOptions.Add(account);
        }
        if (CategoryOptions.Count == 0)
        {
            foreach (var category in await db.Categories.AsNoTracking()
                         .Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToListAsync())
                CategoryOptions.Add(category);
        }

        Items.Clear();
        var today = DateOnly.FromDateTime(DateTime.Today);
        foreach (var r in await recurring.GetAllAsync())
            Items.Add(RecurringRow.From(r, today));
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(Name)) { StatusText = "Enter a name."; return; }
        if (!decimal.TryParse(Amount, out var parsed) || parsed <= 0) { StatusText = "Amount must be positive."; return; }
        if (SelectedAccount is null) { StatusText = "Select an account."; return; }

        int? day = null;
        if (SelectedFrequency is Frequency.Monthly or Frequency.Quarterly or Frequency.Yearly
            && int.TryParse(DayOfMonth, out var d) && d is >= 1 and <= 31)
            day = d;

        try
        {
            await recurring.CreateAsync(new RecurringTransaction
            {
                Name = Name.Trim(),
                Type = SelectedType,
                Amount = parsed,
                AccountId = SelectedAccount.Id,
                CategoryId = SelectedCategory?.Id,
                Frequency = SelectedFrequency,
                DayOfMonth = day,
                StartDate = DateOnly.FromDateTime(StartDate),
                EndDate = EndDate.HasValue ? DateOnly.FromDateTime(EndDate.Value) : null,
                NextOccurrence = DateOnly.FromDateTime(StartDate),
            });
            Name = "";
            Amount = "";
            DayOfMonth = "";
            StatusText = "Recurring transaction created.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private async Task CreateNowAsync(RecurringRow? row)
    {
        if (row is null) return;
        try
        {
            await recurring.CreateNowAsync(row.Id);
            StatusText = $"Transaction created for {row.Name}.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(RecurringRow? row)
    {
        if (row is null) return;
        try
        {
            await recurring.DeleteAsync(row.Id);
            StatusText = "Deleted.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }
}

public class RecurringRow
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string TypeLabel { get; init; } = "";
    public string TypeColor { get; init; } = "";

    // Pre-parsed brush for the type badge.
    public System.Windows.Media.Brush TypeBrush => new System.Windows.Media.SolidColorBrush(
        TypeColor == "#4ADE80"
            ? System.Windows.Media.Color.FromRgb(0x4A, 0xDE, 0x80)
            : System.Windows.Media.Color.FromRgb(0xF8, 0x71, 0x71));
    public string AmountText { get; init; } = "";
    public string FrequencyLabel { get; init; } = "";
    public string AccountName { get; init; } = "";
    public string CategoryName { get; init; } = "";
    public string NextDueText { get; init; } = "";
    public bool IsOverdue { get; init; }
    public bool AutoCreate { get; init; }

    public static RecurringRow From(RecurringTransaction r, DateOnly today) => new()
    {
        Id = r.Id,
        Name = r.Name,
        TypeLabel = r.Type.ToString(),
        TypeColor = r.Type == TransactionType.Income ? "#4ADE80" : "#F87171",
        AmountText = $"৳{r.Amount:N2}",
        FrequencyLabel = r.Frequency.ToString(),
        AccountName = r.Account?.Name ?? "—",
        CategoryName = r.Category?.Name ?? "—",
        NextDueText = r.NextOccurrence.ToString("dd MMM yyyy"),
        IsOverdue = r.NextOccurrence < today,
        AutoCreate = r.AutoCreate,
    };
}
