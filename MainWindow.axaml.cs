using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Linq;

namespace RPGInventoryGUI;

public partial class MainWindow : Window
{
    private GameEngine _gameEngine;
    private Item? _selectedShopItem;

    public MainWindow()
    {
        InitializeComponent();
        _gameEngine = new GameEngine();
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Update player info
        PlayerNameText.Text = _gameEngine.Player.Name;
        PlayerLevelText.Text = $"Level {_gameEngine.Player.Level}";
        HealthText.Text = $"Health: {_gameEngine.Player.Stats.Health}/{_gameEngine.Player.Stats.MaxHealth}";
        ArmorText.Text = $"Armor: {_gameEngine.Player.Stats.Armor}/{_gameEngine.Player.Stats.MaxArmor}";
        StatsText.Text = $"Str: {_gameEngine.Player.Stats.Strength} | Agi: {_gameEngine.Player.Stats.Agility}";
        ExperienceText.Text = $"XP: {_gameEngine.Player.Experience}/{_gameEngine.Player.ExperienceToNext}";
        GoldText.Text = $"Gold: {_gameEngine.Player.Gold}";
        FloorText.Text = $"Floor: {_gameEngine.Floor}";

        // Update inventory
        InventoryList.ItemsSource = _gameEngine.Player.Inventory.Select(item =>
            $"{item.Name} ({item.Type}) - {item.Description}").ToList();

        // Update combat log
        CombatLogText.Text = string.Join("\n", _gameEngine.CombatLog);
        if (CombatLogText.Parent is ScrollViewer scrollViewer)
            scrollViewer.ScrollToEnd();

        // Update game state panels
        ExploringPanel.IsVisible = _gameEngine.State == GameState.Exploring;
        CombatPanel.IsVisible = _gameEngine.State == GameState.Combat;
        ShopPanel.IsVisible = _gameEngine.State == GameState.Shopping;
        GameOverPanel.IsVisible = _gameEngine.State == GameState.Defeat;
        RestPanel.IsVisible = _gameEngine.State == GameState.Resting;
        GameFinishedPanel.IsVisible = _gameEngine.State == GameState.Finished;

        // Update enemy info if in combat
        if (_gameEngine.State == GameState.Combat && _gameEngine.CurrentEnemy != null)
        {
            EnemyNameText.Text = _gameEngine.CurrentEnemy.Name;
            EnemyHealthText.Text = $"Health: {_gameEngine.CurrentEnemy.Stats.Health}/{_gameEngine.CurrentEnemy.Stats.MaxHealth}";
        }

        // Update shop items
        if (_gameEngine.State == GameState.Shopping)
            ShopItemsList.ItemsSource = _gameEngine.GetShopItems().Select((item, index) =>
                $"{item.Name} - {item.Value * 2} gold - {item.Description}").ToList();
        
    }

    private void RegularRestButton_Click(object? sender, RoutedEventArgs e)
    {
        _gameEngine.Rest(0);
        UpdateUI();
    }

    private void SpeedRestButton_Click(object? sender, RoutedEventArgs e)
    {
        _gameEngine.Rest(10);
        UpdateUI();
    }

    private void LeaveRestButton_Click(object? sender, RoutedEventArgs e)
    {
        _gameEngine.State = GameState.Exploring;
        UpdateUI();
    }

    private void ExploreButton_Click(object? sender, RoutedEventArgs e)
    {
        _gameEngine.StartCombat();
        UpdateUI();
    }

    private void RestButton_Click(object? sender, RoutedEventArgs e)
    {
        _gameEngine.State = GameState.Resting;
        UpdateUI();
    }

    private void ShopButton_Click(object? sender, RoutedEventArgs e)
    {
        _gameEngine.State = GameState.Shopping;
        UpdateUI();
    }

    private void AttackButton_Click(object? sender, RoutedEventArgs e)
    {
        _gameEngine.PlayerAttack();
        UpdateUI();
    }

    private void UseItemButton_Click(object? sender, RoutedEventArgs e)
    {
        var usableItems = _gameEngine.Player.Inventory.Where(i => 
            i.Type == ItemType.Potion || i.Type == ItemType.Armor).ToList();
        
        if (usableItems.Any())
        {
            var item = usableItems.First();
            _gameEngine.UseItem(item);
            UpdateUI();
        }
    }

    private void UseInventoryItemButton_Click(object? sender, RoutedEventArgs e)
    {
        if (InventoryList.SelectedIndex >= 0 && InventoryList.SelectedIndex < _gameEngine.Player.Inventory.Count)
        {
            var item = _gameEngine.Player.Inventory[InventoryList.SelectedIndex];
            _gameEngine.UseItem(item);
            UpdateUI();
        }
    }

    private void InventoryList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (InventoryList.SelectedIndex >= 0 && InventoryList.SelectedIndex < _gameEngine.Player.Inventory.Count)
        {
            var item = _gameEngine.Player.Inventory[InventoryList.SelectedIndex];
            ItemDescriptionText.Text = $"{item.Name}\n{item.Description}\nValue: {item.Value}";
            UseInventoryItemButton.IsEnabled = true;
        }
        else
        {
            ItemDescriptionText.Text = "";
            UseInventoryItemButton.IsEnabled = false;
        }
    }

    private void ShopItemsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ShopItemsList.SelectedIndex >= 0)
        {
            var shopItems = _gameEngine.GetShopItems();
            if (ShopItemsList.SelectedIndex < shopItems.Count)
            {
                _selectedShopItem = shopItems[ShopItemsList.SelectedIndex];
                BuyButton.IsEnabled = _gameEngine.Player.Gold >= _selectedShopItem.Value * 2;
            }
        }
        else
        {
            _selectedShopItem = null;
            BuyButton.IsEnabled = false;
        }
    }

    private void BuyButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_selectedShopItem != null)
        {
            var price = _selectedShopItem.Value * 2;
            if (_gameEngine.BuyItem(_selectedShopItem, price))
            {
                _gameEngine.CombatLog.Add($"Bought {_selectedShopItem.Name} for {price} gold!");
                UpdateUI();
            }
        }
    }

    private void LeaveShopButton_Click(object? sender, RoutedEventArgs e)
    {
        _gameEngine.State = GameState.Exploring;
        UpdateUI();
    }

    private void RestartButton_Click(object? sender, RoutedEventArgs e)
    {
        _gameEngine = new GameEngine();
        UpdateUI();
    }

    private void OnReloadGameClick(object sender, RoutedEventArgs e)
    {
        _gameEngine = new GameEngine();
        UpdateUI();
    }
}
