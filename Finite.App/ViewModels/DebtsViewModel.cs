using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finite.Core;
using Finite.Services;

namespace Finite.App.ViewModels;

public partial class DebtsViewModel(DebtService debts, AccountService accounts) : ObservableObject
{
    public ObservableCollection<DebtRow> Items { get; } = [];
    public ObservableCollection<Account> AccountOptions { get; } = [];

    [ObservableProperty] private DebtType _selectedType = DebtType.Receivable;
    [ObservableProperty] private string _personName = "";
    [ObservableProperty] private string _amount = "";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private Account? _selectedAccount;
    [ObservableProperty] private DateTime? _dueDate;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _totalsText = "";

    public Array DebtTypes { get; } = Enum.GetValues<DebtType>();

    [RelayCommand]
    private async Task LoadAsync()
    {
        AccountOptions.Clear();
        foreach (var account in await accounts.GetAllAsync()) AccountOptions.Add(account);

        Items.Clear();
        var list = await debts.GetAllAsync(activeOnly: true);
        foreach (var debt in list) Items.Add(DebtRow.From(debt));

        var receivables = list.Where(d => d.Type == DebtType.Receivable).Sum(d => d.Remaining);
        var payables = list.Where(d => d.Type == DebtType.Payable).Sum(d => d.Remaining);
        TotalsText = $"Owed to me: ৳{receivables:N2}   ·   I owe: ৳{payables:N2}   ·   Net: ৳{receivables - payables:N2}";
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(PersonName)) { StatusText = "Enter a person/company name."; return; }
        if (!decimal.TryParse(Amount, out var parsed) || parsed <= 0) { StatusText = "Amount must be positive."; return; }

        try
        {
            await debts.CreateAsync(new Debt
            {
                Type = SelectedType,
                PersonName = PersonName.Trim(),
                Amount = parsed,
                AccountId = SelectedAccount?.Id,
                DueDate = DueDate.HasValue ? DateOnly.FromDateTime(DueDate.Value) : null,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            });
            PersonName = "";
            Amount = "";
            Description = "";
            DueDate = null;
            StatusText = "Debt recorded.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private async Task RecordPaymentAsync(DebtRow? row)
    {
        if (row is null) return;
        try
        {
            await debts.RecordPaymentAsync(row.Id, row.Remaining);
            StatusText = $"Payment of ৳{row.Remaining:N2} recorded.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(DebtRow? row)
    {
        if (row is null) return;
        try
        {
            await debts.DeleteAsync(row.Id);
            StatusText = "Debt deleted.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }
}

public class DebtRow
{
    public int Id { get; init; }
    public string TypeLabel { get; init; } = "";
    public string TypeColor { get; init; } = "";

    // Pre-parsed brush for the type badge (raw hex string binding failed).
    public System.Windows.Media.Brush TypeBrush => new System.Windows.Media.SolidColorBrush(
        TypeColor == "#4ADE80"
            ? System.Windows.Media.Color.FromRgb(0x4A, 0xDE, 0x80)
            : System.Windows.Media.Color.FromRgb(0xF8, 0x71, 0x71));
    public string PersonName { get; init; } = "";
    public string Description { get; init; } = "";
    public string AmountText { get; init; } = "";
    public string RemainingText { get; init; } = "";
    public string DueText { get; init; } = "";
    public string StatusText { get; init; } = "";
    public decimal Remaining { get; init; }
    public bool IsOverdue { get; init; }

    public static DebtRow From(Debt debt) => new()
    {
        Id = debt.Id,
        TypeLabel = debt.Type == DebtType.Receivable ? "Owed to Me" : "I Owe",
        TypeColor = debt.Type == DebtType.Receivable ? "#4ADE80" : "#F87171",
        PersonName = debt.PersonName,
        Description = debt.Description ?? "",
        AmountText = $"৳{debt.Amount:N2}",
        RemainingText = $"৳{debt.Remaining:N2}",
        Remaining = debt.Remaining,
        DueText = debt.DueDate is DateOnly due
            ? (debt.IsOverdue ? $"Overdue ({due:dd MMM})" : due.ToString("dd MMM"))
            : "—",
        StatusText = debt.Status.ToString(),
        IsOverdue = debt.IsOverdue,
    };
}
