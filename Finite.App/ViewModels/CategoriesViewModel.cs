using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finite.Core;
using Finite.Services;

namespace Finite.App.ViewModels;

public partial class CategoriesViewModel(CategoryService categories) : ObservableObject
{
    public ObservableCollection<Category> Items { get; } = [];

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private CategoryType _selectedType = CategoryType.Expense;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _color = "#3B82F6";

    public Array CategoryTypes { get; } = Enum.GetValues<CategoryType>();

    [RelayCommand]
    private async Task LoadAsync()
    {
        Items.Clear();
        foreach (var category in await categories.GetAllAsync(includeInactive: true)) Items.Add(category);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        try
        {
            await categories.CreateAsync(new Category
            {
                Name = Name.Trim(),
                Type = SelectedType,
                Color = Color,
            });
            Name = "";
            StatusText = "Category created.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Category? category)
    {
        if (category is null) return;
        try
        {
            await categories.DeleteAsync(category.Id);
            StatusText = "Category deleted.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }
}
