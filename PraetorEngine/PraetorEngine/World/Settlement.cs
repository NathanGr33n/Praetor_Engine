using System;
using System.Runtime.CompilerServices;

namespace PraetorEngine.World
{
    /// <summary>
    /// Component for settlement/city entities.
    /// Must be unmanaged for ECS compatibility.
    /// </summary>
    public struct SettlementComponent
    {
        // Use fixed-size buffer for name (32 chars max)
        private unsafe fixed byte _nameBytes[32];
        
        public int Population;
        public int OwnerId; // Faction ID (0 = neutral, Phase 3 will add faction system)
        public HexCoord Location;
        public SettlementType Type;

        public SettlementComponent(string name, HexCoord location, int population = 1000, int ownerId = 0, SettlementType type = SettlementType.Town)
        {
            Population = population;
            OwnerId = ownerId;
            Location = location;
            Type = type;
            
            // Initialize name buffer
            unsafe
            {
                fixed (byte* ptr = _nameBytes)
                {
                    for (int i = 0; i < 32; i++)
                        ptr[i] = 0;
                    
                    if (!string.IsNullOrEmpty(name))
                    {
                        var bytes = System.Text.Encoding.UTF8.GetBytes(name);
                        int len = Math.Min(bytes.Length, 31); // Reserve 1 for null terminator
                        for (int i = 0; i < len; i++)
                            ptr[i] = bytes[i];
                    }
                }
            }
        }

        /// <summary>
        /// Gets the settlement name as a string.
        /// </summary>
        public readonly string GetName()
        {
            unsafe
            {
                fixed (byte* ptr = _nameBytes)
                {
                    int length = 0;
                    while (length < 32 && ptr[length] != 0)
                        length++;
                    
                    return System.Text.Encoding.UTF8.GetString(ptr, length);
                }
            }
        }

        /// <summary>
        /// Gets display string for settlement size based on population.
        /// </summary>
        public readonly string GetSizeDescription()
        {
            return Population switch
            {
                < 500 => "Hamlet",
                < 2000 => "Village",
                < 5000 => "Town",
                < 20000 => "City",
                _ => "Metropolis"
            };
        }
    }

    /// <summary>
    /// Settlement type categorization.
    /// </summary>
    public enum SettlementType : byte
    {
        Hamlet = 0,
        Village = 1,
        Town = 2,
        City = 3,
        Capital = 4,
        Fortress = 5
    }

    /// <summary>
    /// Helper utilities for creating and managing settlements.
    /// </summary>
    public static class SettlementHelper
    {
        private static readonly Random _random = new Random();
        private static readonly string[] _namesPrefixes = new[]
        {
            "North", "South", "East", "West", "New", "Old", "Great", "Little",
            "High", "Low", "Upper", "Lower", "Fort", "Castle", "Port"
        };

        private static readonly string[] _namesSuffixes = new[]
        {
            "ville", "ton", "burg", "ford", "ham", "shire", "dale", "haven",
            "ridge", "field", "wood", "shore", "mount", "bridge", "gate"
        };

        private static readonly string[] _namesBase = new[]
        {
            "Oak", "Stone", "River", "Iron", "Silver", "Gold", "Green", "Red",
            "White", "Black", "Storm", "Sun", "Moon", "Star", "Crown", "Dragon"
        };

        /// <summary>
        /// Generates a random settlement name.
        /// </summary>
        public static string GenerateSettlementName(int seed = 0)
        {
            var rng = seed != 0 ? new Random(seed) : _random;
            
            int style = rng.Next(3);
            return style switch
            {
                0 => _namesBase[rng.Next(_namesBase.Length)] + _namesSuffixes[rng.Next(_namesSuffixes.Length)],
                1 => _namesPrefixes[rng.Next(_namesPrefixes.Length)] + _namesBase[rng.Next(_namesBase.Length)],
                _ => _namesBase[rng.Next(_namesBase.Length)] + " " + _namesSuffixes[rng.Next(_namesSuffixes.Length)]
            };
        }

        /// <summary>
        /// Generates a random population for a settlement type.
        /// </summary>
        public static int GeneratePopulation(SettlementType type, int seed = 0)
        {
            var rng = seed != 0 ? new Random(seed) : _random;
            
            return type switch
            {
                SettlementType.Hamlet => rng.Next(100, 500),
                SettlementType.Village => rng.Next(500, 2000),
                SettlementType.Town => rng.Next(2000, 5000),
                SettlementType.City => rng.Next(5000, 20000),
                SettlementType.Capital => rng.Next(20000, 100000),
                SettlementType.Fortress => rng.Next(500, 3000),
                _ => 1000
            };
        }

        /// <summary>
        /// Determines if a hex location is suitable for a settlement.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSuitableForSettlement(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Plains => true,
                TerrainType.Forest => true,
                TerrainType.Hills => true,
                TerrainType.Desert => true,
                TerrainType.Mountain => false,
                TerrainType.Water => false,
                TerrainType.Swamp => false,
                TerrainType.Snow => false,
                _ => false
            };
        }

        /// <summary>
        /// Gets settlement type based on population.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SettlementType GetTypeFromPopulation(int population)
        {
            return population switch
            {
                < 500 => SettlementType.Hamlet,
                < 2000 => SettlementType.Village,
                < 5000 => SettlementType.Town,
                < 20000 => SettlementType.City,
                _ => SettlementType.Capital
            };
        }
    }
}
