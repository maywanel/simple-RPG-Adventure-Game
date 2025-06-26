using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Rendering;

public enum ItemType { Potion, Armor, Weapon, Spell, Boost }

public record Item(string Name, ItemType Type, int Value, string Description = "");

public class Player
{
    public string Name { get; set; } = "Hero";
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public int ExperienceToNext => Level * 100;
    public Stats Stats { get; set; }
    public List<Item> Inventory { get; set; } = new();
    public int Gold { get; set; } = 50;
    public DateTime RestCooldownUntil { get; set; } = DateTime.MinValue;
    
    public Player()
    {
        Stats = new Stats(10, 10);
    }
    
    public void GainExperience(int exp)
    {
        Experience += exp;
        while (Experience >= ExperienceToNext)
        {
            Experience -= ExperienceToNext;
            LevelUp();
        }
    }
    
    private void LevelUp()
    {
        Level++;
        Stats.Strength += 2;
        Stats.Agility += 2;
        Stats.MaxHealth += 20;
        Stats.Health = Stats.MaxHealth;
        Stats.MaxArmor += 10;
        Stats.Armor = Stats.MaxArmor;
    }
    
    public bool IsAlive => Stats.Health > 0;
    public bool IsResting => DateTime.Now < RestCooldownUntil;
    public TimeSpan RemainingRestTime => RestCooldownUntil > DateTime.Now ? RestCooldownUntil - DateTime.Now : TimeSpan.Zero;
}

public class Enemy
{
    public string Name { get; set; }
    public Stats Stats { get; set; }
    public int ExperienceReward { get; set; }
    public int GoldReward { get; set; }
    public List<Item> PossibleDrops { get; set; } = new();
    
    public Enemy(string name, int level)
    {
        Name = name;
        var baseHealth = 30 + (level * 15);
        var baseStrength = 5 + (level * 3);
        Stats = new Stats(baseStrength, level * 2) 
        { 
            Health = baseHealth, 
            MaxHealth = baseHealth,
            Armor = level * 5,
            MaxArmor = level * 5
        };
        ExperienceReward = level * 25;
        GoldReward = level * 10 + Random.Shared.Next(5, 15);
    }
    
    public bool IsAlive => Stats.Health > 0;
}

public class Stats
{
    public int Strength { get; set; }
    public int Agility { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int Armor { get; set; }
    public int MaxArmor { get; set; }

    public Stats(int str, int agi)
    {
        Strength = str;
        Agility = agi;
        Health = MaxHealth = 10;
        Armor = MaxArmor = 50;
    }

    public void ApplyItem(Item item)
    {
        switch (item.Type)
        {
            case ItemType.Potion:
                Health += item.Value;
                if (Health > MaxHealth) Health = MaxHealth;
                break;
            case ItemType.Armor:
                Armor += item.Value;
                if (Armor > MaxArmor) Armor = MaxArmor;
                break;
            case ItemType.Boost:
                Agility += item.Value;
                break;
            case ItemType.Spell:
                Strength += item.Value;
                break;
        }
    }
    
    public int CalculateDamage(int weaponValue = 0)
    {
        return Strength + weaponValue + Random.Shared.Next(1, 6);
    }
    
    public void TakeDamage(int damage)
    {
        if (Armor > 0)
        {
            var armorReduction = Math.Min(Armor, damage / 2);
            Armor -= armorReduction;
            damage -= armorReduction;
        }
        
        Health -= damage;
        if (Health < 0) Health = 0;
        if (Armor < 0) Armor = 0;
    }
}

public enum GameState { Exploring, Combat, Victory, Defeat, Shopping, Resting, Finished }

public class GameEngine
{
    public Player Player { get; set; }
    public Enemy? CurrentEnemy { get; set; }
    public GameState State { get; set; } = GameState.Exploring;
    public List<string> CombatLog { get; set; } = new();
    public int Floor { get; set; } = 1;

    private readonly List<string> _enemyNames = new()
    {
        "Goblin", "Orc", "Skeleton", "Spider", "Wolf", "Bandit", "Troll", "Dragon"
    };
    
    private readonly List<Item> _shopItems = new()
    {
        new("Health Potion", ItemType.Potion, 30, "Restores 30 HP") { },
        new("Steel Armor", ItemType.Armor, 20, "Adds 20 armor"),
        new("Iron Sword", ItemType.Weapon, 15, "Deals extra 15 damage"),
        new("Agility Boost", ItemType.Boost, 5, "Permanently increases agility"),
        new("Power Spell", ItemType.Spell, 8, "Permanently increases strength")
    };

    public GameEngine()
    {
        Player = new Player();
        AddStartingItems();
    }
    
    private void AddStartingItems()
    {
        Player.Inventory.AddRange(new[]
        {
            new Item("Small Potion", ItemType.Potion, 20, "Restores 20 HP"),
            new Item("Leather Armor", ItemType.Armor, 10, "Adds 10 armor"),
            new Item("Basic Sword", ItemType.Weapon, 8, "Deals extra 8 damage")
        });
    }
    
    public void StartCombat()
    {
        if (Player.IsResting)
        {
            CombatLog.Add($"You can't explore while resting! {Player.RemainingRestTime.TotalSeconds:F0} seconds remaining.");
            return;
        }
        
        var enemyLevel = Math.Max(1, Floor + Random.Shared.Next(-1, 2));
        var enemyName = _enemyNames[Random.Shared.Next(_enemyNames.Count)];
        CurrentEnemy = new Enemy($"Level {enemyLevel} {enemyName}", enemyLevel);
        State = GameState.Combat;
        CombatLog.Clear();
        CombatLog.Add($"A {CurrentEnemy.Name} appears!");
    }
    
    public void PlayerAttack()
    {
        if (Player.IsResting)
        {
            CombatLog.Add($"You can't attack while resting! {Player.RemainingRestTime.TotalSeconds:F0} seconds remaining.");
            return;
        }
        
        if (CurrentEnemy == null)
        {
            CombatLog.Add("No enemy to attack!");
            return;
        }
        
        if (!CurrentEnemy.IsAlive)
        {
            CombatLog.Add("The enemy is already defeated!");
            return;
        }
        
        var weapon = Player.Inventory.FirstOrDefault(i => i.Type == ItemType.Weapon);
        var damage = Player.Stats.CalculateDamage(weapon?.Value ?? 0);
        CurrentEnemy.Stats.TakeDamage(damage);
        
        CombatLog.Add($"You attack for {damage} damage!");
        
        if (!CurrentEnemy.IsAlive)
        {
            Victory();
            return;
        }
        
        EnemyAttack();
    }
    
    private void EnemyAttack()
    {
        if (CurrentEnemy == null) return;
        
        var damage = CurrentEnemy.Stats.CalculateDamage();
        Player.Stats.TakeDamage(damage);
        
        CombatLog.Add($"{CurrentEnemy.Name} attacks for {damage} damage!");
        
        if (!Player.IsAlive)
        {
            State = GameState.Defeat;
            CombatLog.Add("You have been defeated!");
        }
    }
    
    private void Victory()
    {
        if (CurrentEnemy == null) return;
        
        Player.GainExperience(CurrentEnemy.ExperienceReward);
        Player.Gold += CurrentEnemy.GoldReward;
        
        CombatLog.Add($"Victory! Gained {CurrentEnemy.ExperienceReward} XP and {CurrentEnemy.GoldReward} gold!");
        
        // Random item drop
        if (Random.Shared.Next(100) < 30)
        {
            var dropItem = GenerateRandomItem();
            Player.Inventory.Add(dropItem);
            CombatLog.Add($"Found: {dropItem.Name}!");
        }
        
        CurrentEnemy = null;
        Floor++;

        // Check if game is finished
        if (Floor >= 101)
        {
            State = GameState.Finished;
            CombatLog.Add("🏁 Congratulations! You have completed all 100 floors!");
            Floor = 100;
        }
        else
            State = GameState.Exploring;
    }
    
    private Item GenerateRandomItem()
    {
        var types = new[] { ItemType.Potion, ItemType.Armor, ItemType.Weapon, ItemType.Boost, ItemType.Spell };
        var type = types[Random.Shared.Next(types.Length)];
        var value = Floor * 5 + Random.Shared.Next(5, 15);
        
        return type switch
        {
            ItemType.Potion => new Item($"Greater Potion", type, value, $"Restores {value} HP"),
            ItemType.Armor => new Item($"Enhanced Armor", type, value, $"Adds {value} armor"),
            ItemType.Weapon => new Item($"Magic Weapon", type, value, $"Deals extra {value} damage"),
            ItemType.Boost => new Item($"Agility Elixir", type, value, $"Increases agility by {value}"),
            ItemType.Spell => new Item($"Strength Rune", type, value, $"Increases strength by {value}"),
            _ => new Item("Unknown Item", type, value)
        };
    }
    
    public void UseItem(Item item)
    {
        if (Player.IsResting)
        {
            CombatLog.Add($"You can't use items while resting! {Player.RemainingRestTime.TotalSeconds:F0} seconds remaining.");
            return;
        }
        
        Player.Stats.ApplyItem(item);
        Player.Inventory.Remove(item);
        CombatLog.Add($"Used {item.Name}!");
    }
    
    public void Rest(int speedup)
    {
        if (Player.IsResting)
        {
            CombatLog.Add($"You are still resting! {Player.RemainingRestTime.TotalSeconds:F0} seconds remaining.");
            return;
        }
        
        var healthDeficit = Player.Stats.MaxHealth - Player.Stats.Health;
        var armorDeficit = Player.Stats.MaxArmor - Player.Stats.Armor;
        
        var restTimeSeconds = Math.Max(3, healthDeficit + (armorDeficit / 2)) - speedup;
        Player.RestCooldownUntil = DateTime.Now.AddSeconds(restTimeSeconds);
        Player.Stats.Health = Player.Stats.MaxHealth;
        Player.Stats.Armor = Player.Stats.MaxArmor;
        CombatLog.Add($"You rest and recover fully. Resting for {restTimeSeconds} seconds...");
    }
    
    public List<Item> GetShopItems() => _shopItems;
    
    public bool BuyItem(Item item, int price)
    {
        if (Player.Gold >= price)
        {
            Player.Gold -= price;
            Player.Inventory.Add(item);
            return true;
        }
        return false;
    }
}
