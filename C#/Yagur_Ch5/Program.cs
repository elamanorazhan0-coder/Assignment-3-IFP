using Yagur.Ch5;

var game = new TowerDefenseGame(gold: 100);

// Exercise 1
Console.WriteLine("=== Exercise 1: UpgradeTower returns Result<bool, TowerUpgradeError> ===");

var towers = new[]
{
    new Tower("Archer",   Level: 1, MaxLevel: 3, UpgradeCost: 40, IsDestroyed: false),
    new Tower("Cannon",   Level: 3, MaxLevel: 3, UpgradeCost: 60, IsDestroyed: false),
    new Tower("Mage",     Level: 1, MaxLevel: 3, UpgradeCost: 40, IsDestroyed: true),
    new Tower("Ballista", Level: 2, MaxLevel: 3, UpgradeCost: 90, IsDestroyed: false),
};

foreach (var t in towers)
{
    var upgrade = game.UpgradeTower(t);
    Console.WriteLine(upgrade.IsSuccess
        ? $"{t.Name}: upgraded! Gold left: {game.Gold}"
        : $"{t.Name}: upgrade failed -> {upgrade.Error}");
}

// Exercise 2
Console.WriteLine();
Console.WriteLine("=== Exercise 2: ROP pipeline Parse -> Validate -> Spawn ===");

foreach (var input in new[] { "Goblin;5;2", "Goblin;abc;2", "Dragon;1;2", "Orc;500;1", "Orc;3" })
{
    var spawn = EnemySpawner.ProcessEnemySpawn(input);
    Console.WriteLine(spawn.IsSuccess
        ? $"'{input}' -> spawned {spawn.Value.Count}x {spawn.Value.Type} in lane {spawn.Value.Lane}"
        : $"'{input}' -> {spawn.Error}");
}

// Exercise 3
Console.WriteLine();
Console.WriteLine("=== Exercise 3: retry a flaky TowerFire ===");

var tower = towers[0];
var enemy = new Enemy("Goblin", Health: 30);

// Seeded so the demo output is reproducible; each new Random() would differ.
var flaky = new FlakyFiring(new Random(7), successChance: 0.3);

foreach (var maxRetries in new[] { 1, 5, 10 })
{
    var fire = flaky.TryTowerFire(tower, enemy, maxRetries);
    Console.WriteLine(fire.IsSuccess
        ? $"maxRetries={maxRetries}: hit! (took {flaky.LastAttemptCount} attempt(s))"
        : $"maxRetries={maxRetries}: {fire.Error}");
}

// Domain types
public record Tower(string Name, int Level, int MaxLevel, int UpgradeCost, bool IsDestroyed);
public record Enemy(string Type, int Health);

// Exercise 1 - custom error type instead of a bare "false"
public enum TowerUpgradeError
{
    InsufficientResources,
    MaxLevelReached,
    TowerDestroyed
}

public class TowerDefenseGame(int gold)
{
    public int Gold { get; private set; } = gold;

    public Result<bool, TowerUpgradeError> UpgradeTower(Tower tower)
    {
        // Guard clauses: each failure says exactly WHY it failed.
        if (tower.IsDestroyed)
            return Result.Fail<bool, TowerUpgradeError>(TowerUpgradeError.TowerDestroyed);

        if (tower.Level >= tower.MaxLevel)
            return Result.Fail<bool, TowerUpgradeError>(TowerUpgradeError.MaxLevelReached);

        if (Gold < tower.UpgradeCost)
            return Result.Fail<bool, TowerUpgradeError>(TowerUpgradeError.InsufficientResources);

        Gold -= tower.UpgradeCost; // the actual upgrade
        return Result.Ok<bool, TowerUpgradeError>(true);
    }
}

// Exercise 2 - Railway-Oriented Programming
public record EnemySpawnError(string Stage, string Message)
{
    public override string ToString() => $"[{Stage}] {Message}";
}

public record ParsedEnemyData(string Type, int Count, int Lane);
public record ValidatedEnemy(string Type, int Count, int Lane);

public static class EnemySpawner
{
    // Before: nested ifs, invalid input silently ignored, returns void.
    // After : a flat pipeline; the first failure skips everything after it.
    public static Result<ValidatedEnemy, EnemySpawnError> ProcessEnemySpawn(string enemyData) =>
        ParseEnemyData(enemyData)
            .Bind(ValidateEnemySpawn)
            .Bind(SpawnEnemy);

    public static Result<ParsedEnemyData, EnemySpawnError> ParseEnemyData(string data)
    {
        var parts = data.Split(';');
        if (parts.Length != 3)
            return Fail<ParsedEnemyData>("Parse", "Expected format 'Type;Count;Lane'.");

        if (!int.TryParse(parts[1], out var count))
            return Fail<ParsedEnemyData>("Parse", $"Count '{parts[1]}' is not a number.");

        if (!int.TryParse(parts[2], out var lane))
            return Fail<ParsedEnemyData>("Parse", $"Lane '{parts[2]}' is not a number.");

        return Result.Ok<ParsedEnemyData, EnemySpawnError>(new ParsedEnemyData(parts[0].Trim(), count, lane));
    }

    private static readonly string[] KnownTypes = ["Goblin", "Orc", "Troll"];

    public static Result<ValidatedEnemy, EnemySpawnError> ValidateEnemySpawn(ParsedEnemyData data)
    {
        if (!KnownTypes.Contains(data.Type))
            return Fail<ValidatedEnemy>("Validate", $"Unknown enemy type '{data.Type}'.");

        if (data.Count is < 1 or > 100)
            return Fail<ValidatedEnemy>("Validate", $"Count {data.Count} must be between 1 and 100.");

        if (data.Lane is < 1 or > 3)
            return Fail<ValidatedEnemy>("Validate", $"Lane {data.Lane} must be 1, 2 or 3.");

        return Result.Ok<ValidatedEnemy, EnemySpawnError>(new ValidatedEnemy(data.Type, data.Count, data.Lane));
    }

    // Final step: the only place with a side effect (here we just print).
    public static Result<ValidatedEnemy, EnemySpawnError> SpawnEnemy(ValidatedEnemy enemy)
    {
        // ... real spawning logic would go here ...
        return Result.Ok<ValidatedEnemy, EnemySpawnError>(enemy);
    }

    private static Result<T, EnemySpawnError> Fail<T>(string stage, string message) =>
        Result.Fail<T, EnemySpawnError>(new EnemySpawnError(stage, message));
}

// Exercise 3 - retry mechanism returning a Result
public class FlakyFiring(Random random, double successChance)
{
    public int LastAttemptCount { get; private set; }

    // The flaky operation from the task: sometimes true, sometimes false.
    public bool TowerFire(Tower tower, Enemy enemy) => random.NextDouble() < successChance;

    public Result<bool, string> TryTowerFire(Tower tower, Enemy enemy, int maxRetries)
    {
        if (maxRetries < 1)
            return Result.Fail<bool, string>("maxRetries must be at least 1.");

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            LastAttemptCount = attempt;
            if (TowerFire(tower, enemy))
                return Result.Ok<bool, string>(true);
        }

        return Result.Fail<bool, string>(
            $"Tower '{tower.Name}' failed to fire at {enemy.Type} after {maxRetries} attempt(s).");
    }
}
