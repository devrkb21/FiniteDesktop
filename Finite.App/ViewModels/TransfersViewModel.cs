using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finite.Core;
using Finite.Services;

namespace Finite.App.ViewModels;

public partial class TransfersViewModel(
    TransferService transfers,
    AccountService accounts) : ObservableObject
{
    public ObservableCollection<Transfer> Items { get; } = [];
    public ObservableCollection<Account> AccountOptions { get; } = [];

    [ObservableProperty] private Account? _fromAccount;
    [ObservableProperty] private Account? _toAccount;
    [ObservableProperty] private string _amount = "";
    [ObservableProperty] private string _fee = "0";
    [ObservableProperty] private string _description = "";
    [ObservableProperty] private string _statusText = "";

    [RelayCommand]
    private async Task LoadAsync()
    {
        AccountOptions.Clear();
        foreach (var account in await accounts.GetAllAsync()) AccountOptions.Add(account);

        Items.Clear();
        foreach (var t in await transfers.GetRecentAsync(100)) Items.Add(t);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (FromAccount is null || ToAccount is null) { StatusText = "Select both accounts."; return; }
        if (!decimal.TryParse(Amount, out var parsedAmount) || parsedAmount <= 0)
        {
            StatusText = "Amount must be a positive number.";
            return;
        }
        decimal.TryParse(Fee, out var parsedFee);

        try
        {
            await transfers.CreateAsync(new Transfer
            {
                FromAccountId = FromAccount.Id,
                ToAccountId = ToAccount.Id,
                Amount = parsedAmount,
                Fee = parsedFee,
                Description = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
            });

            Amount = "";
            Fee = "0";
            Description = "";
            StatusText = "Transfer saved.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Transfer? t)
    {
        if (t is null) return;
        try
        {
            await transfers.DeleteAsync(t.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }
}
