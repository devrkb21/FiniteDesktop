using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finite.Core;
using Finite.Services;

namespace Finite.App.ViewModels;

public partial class AccountsViewModel(
    AccountService accounts,
    BalanceService balances) : ObservableObject
{
    public ObservableCollection<AccountRow> Items { get; } = [];

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private AccountType _selectedType = AccountType.Cash;
    [ObservableProperty] private string _initialBalance = "0";
    [ObservableProperty] private string _walletProvider = "";
    [ObservableProperty] private string _bankName = "";
    [ObservableProperty] private string _accountNumber = "";
    [ObservableProperty] private string _creditLimit = "";
    [ObservableProperty] private string _color = "#3B82F6";
    [ObservableProperty] private string _statusText = "";

    public Array AccountTypes { get; } = Enum.GetValues<AccountType>();

    public bool ShowWalletField => SelectedType == AccountType.MobileWallet;
    public bool ShowBankField => SelectedType is AccountType.Bank or AccountType.DebitCard or AccountType.CreditCard or AccountType.Investment;
    public bool ShowCreditLimit => SelectedType == AccountType.CreditCard;

    partial void OnSelectedTypeChanged(AccountType value)
    {
        OnPropertyChanged(nameof(ShowWalletField));
        OnPropertyChanged(nameof(ShowBankField));
        OnPropertyChanged(nameof(ShowCreditLimit));
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        Items.Clear();
        var list = await accounts.GetAllAsync(includeInactive: true);
        var balanceMap = await balances.GetBalancesAsync();
        foreach (var account in list)
            Items.Add(new AccountRow(account, balanceMap.GetValueOrDefault(account.Id)));
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (!decimal.TryParse(InitialBalance, out var balance))
        {
            StatusText = "Initial balance must be a number.";
            return;
        }

        try
        {
            decimal? creditLimit = null;
            if (ShowCreditLimit && decimal.TryParse(CreditLimit, out var parsedLimit)) creditLimit = parsedLimit;

            await accounts.CreateAsync(new Account
            {
                Name = Name.Trim(),
                Type = SelectedType,
                InitialBalance = balance,
                WalletProvider = ShowWalletField && !string.IsNullOrWhiteSpace(WalletProvider) ? WalletProvider.Trim() : null,
                BankName = ShowBankField && !string.IsNullOrWhiteSpace(BankName) ? BankName.Trim() : null,
                AccountNumber = string.IsNullOrWhiteSpace(AccountNumber) ? null : AccountNumber.Trim(),
                CreditLimit = creditLimit,
                Color = string.IsNullOrWhiteSpace(Color) ? "#3B82F6" : Color.Trim(),
            });
            Name = "";
            InitialBalance = "0";
            WalletProvider = "";
            BankName = "";
            AccountNumber = "";
            CreditLimit = "";
            StatusText = "Account created.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(AccountRow? row)
    {
        if (row is null) return;
        try
        {
            await accounts.DeleteAsync(row.Account.Id);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }
}

public record AccountRow(Account Account, decimal Balance)
{
    public string Name => Account.Name;
    public string TypeLabel => Account.Type.ToString();
    public string BalanceText => (Balance < 0 ? "− ৳ " : "৳ ") + Math.Abs(Balance).ToString("N2");
}
