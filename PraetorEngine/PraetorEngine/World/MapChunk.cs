using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using PraetorEngine.Core.ECS;
using PraetorEngine.Core.Memory;

namespace PraetorEngine.World
{
    /// <summary>
    /// Chunk coordinate for spatial partitioning of the map.
    /// </summary>
    public readonly struct ChunkCoord : IEquatable<ChunkCoord>
    {
        public readonly int X;
        public readonly int Y;

        public ChunkCoord(int x, int y)
        {
            X = x;
            Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ChunkCoord other) => X == other.X && Y == other.Y;

        public override bool Equals(object? obj) => obj is ChunkCoord other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(X, Y);

        public static bool operator ==(ChunkCoord left, ChunkCoord right) => left.Equals(right);
        public static bool operator !=(ChunkCoord left, ChunkCoord right) => !left.Equals(right);

        public override string ToString() => $"Chunk({X}, {Y})";
    }

    /// <summary>
    /// A chunk of the map containing hex tiles.
    /// Chunks enable efficient streaming and memory management for large maps.
    /// </summary>
    public class MapChunk
    {
        public const int ChunkSize = 16; // 16x16 hex tiles per chunk

        public ChunkCoord Coord { get; private set; }
        public bool IsLoaded { get; private set; }
        
        // Hex tile entities within this chunk
        private readonly Entity[] _tiles;
        private readonly HexCoord[] _hexCoords;
        
        public MapChunk(ChunkCoord coord)
        {
            Coord = coord;
            IsLoaded = false;
            _tiles = new Entity[ChunkSize * ChunkSize];
            _hexCoords = new HexCoord[ChunkSize * ChunkSize];
        }

        /// <summary>
        /// Converts chunk-local index to hex coordinate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public HexCoord GetHexCoord(int index)
        {
            return _hexCoords[index];
        }

        /// <summary>
        /// Gets entity at chunk-local index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity GetTile(int index)
        {
            return _tiles[index];
        }

        /// <summary>
        /// Sets tile data at chunk-local index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTile(int index, Entity entity, HexCoord hexCoord)
        {
            _tiles[index] = entity;
            _hexCoords[index] = hexCoord;
        }

        /// <summary>
        /// Loads chunk data and creates tile entities.
        /// </summary>
        public void Load(Core.ECS.World world, int mapSeed)
        {
            if (IsLoaded) return;

            int index = 0;
            for (int localY = 0; localY < ChunkSize; localY++)
            {
                for (int localX = 0; localX < ChunkSize; localX++)
                {
                    // Convert chunk + local coords to global hex coords
                    int globalQ = Coord.X * ChunkSize + localX;
                    int globalR = Coord.Y * ChunkSize + localY;
                    var hexCoord = new HexCoord(globalQ, globalR);

                    // Generate terrain for this tile
                    var terrainType = TerrainGenerator.GenerateTerrainAt(hexCoord, mapSeed);
                    var elevation = TerrainGenerator.GenerateElevation(hexCoord, terrainType, mapSeed);
                    
                    // Create tile entity with terrain component
                    var entity = world.CreateEntity();
                    world.AddComponent(entity, new TerrainComponent(terrainType, elevation));
                    
                    // Store world position for rendering
                    var worldPos = HexGrid.HexToWorld(hexCoord);
                    world.AddComponent(entity, new Core.ECS.Components.PositionComponent(
                        worldPos.X, worldPos.Y, 0f));

                    SetTile(index, entity, hexCoord);
                    index++;
                }
            }

            IsLoaded = true;
        }

        /// <summary>
        /// Unloads chunk data and destroys tile entities.
        /// </summary>
        public void Unload(Core.ECS.World world)
        {
            if (!IsLoaded) return;

            for (int i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i].Id != 0)
                {
                    world.DestroyEntity(_tiles[i]);
                    _tiles[i] = default;
                }
            }

            IsLoaded = false;
        }

        /// <summary>
        /// Resets chunk for reuse (pooling support).
        /// </summary>
        public void Reset()
        {
            IsLoaded = false;
            Array.Clear(_tiles, 0, _tiles.Length);
            Array.Clear(_hexCoords, 0, _hexCoords.Length);
        }
    }

    /// <summary>
    /// Manages chunk loading/unloading based on camera view.
    /// Implements streaming for large maps.
    /// </summary>
    public class ChunkManager
    {
        private readonly Core.ECS.World _world;
        private readonly Dictionary<ChunkCoord, MapChunk> _activeChunks;
        private readonly ObjectPool<MapChunk> _chunkPool;
        private readonly int _mapSeed;

        public int LoadedChunkCount => _activeChunks.Count;
        public int MapSeed => _mapSeed;

        public ChunkManager(Core.ECS.World world, int mapSeed = 12345)
        {
            _world = world;
            _mapSeed = mapSeed;
            _activeChunks = new Dictionary<ChunkCoord, MapChunk>();
            
            // Pool chunks to avoid allocations during streaming
            _chunkPool = new ObjectPool<MapChunk>(
                () => new MapChunk(default),
                chunk => chunk.Reset(),
                preAllocate: 16 // Pre-allocate for typical view
            );
        }

        /// <summary>
        /// Converts hex coordinate to chunk coordinate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChunkCoord HexToChunk(HexCoord hex)
        {
            int chunkX = hex.Q / MapChunk.ChunkSize;
            int chunkY = hex.R / MapChunk.ChunkSize;
            
            // Handle negative coordinates correctly
            if (hex.Q < 0 && hex.Q % MapChunk.ChunkSize != 0) chunkX--;
            if (hex.R < 0 && hex.R % MapChunk.ChunkSize != 0) chunkY--;
            
            return new ChunkCoord(chunkX, chunkY);
        }

        /// <summary>
        /// Loads a chunk if not already loaded.
        /// </summary>
        public MapChunk LoadChunk(ChunkCoord coord)
        {
            if (_activeChunks.TryGetValue(coord, out var existingChunk))
            {
                return existingChunk;
            }

            var chunk = _chunkPool.Rent();
            // Update coord (pooled chunks may have old coords)
            var newChunk = new MapChunk(coord);
            newChunk.Load(_world, _mapSeed);
            
            _activeChunks[coord] = newChunk;
            return newChunk;
        }

        /// <summary>
        /// Unloads a chunk and returns it to pool.
        /// </summary>
        public void UnloadChunk(ChunkCoord coord)
        {
            if (_activeChunks.TryGetValue(coord, out var chunk))
            {
                chunk.Unload(_world);
                _activeChunks.Remove(coord);
                _chunkPool.Return(chunk);
            }
        }

        /// <summary>
        /// Gets a loaded chunk, or null if not loaded.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MapChunk? GetChunk(ChunkCoord coord)
        {
            _activeChunks.TryGetValue(coord, out var chunk);
            return chunk;
        }

        /// <summary>
        /// Updates chunk streaming based on camera view bounds.
        /// Loads visible chunks, unloads distant ones.
        /// </summary>
        public void UpdateStreaming(Rectangle viewBounds, int loadDistance = 1)
        {
            // Calculate chunk bounds from view bounds
            var minHex = HexGrid.WorldToHex(new Vector2(viewBounds.Left, viewBounds.Top));
            var maxHex = HexGrid.WorldToHex(new Vector2(viewBounds.Right, viewBounds.Bottom));
            
            var minChunk = HexToChunk(minHex);
            var maxChunk = HexToChunk(maxHex);

            // Expand by load distance
            int minX = minChunk.X - loadDistance;
            int maxX = maxChunk.X + loadDistance;
            int minY = minChunk.Y - loadDistance;
            int maxY = maxChunk.Y + loadDistance;

            // Track chunks that should be loaded
            var desiredChunks = new HashSet<ChunkCoord>();
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var coord = new ChunkCoord(x, y);
                    desiredChunks.Add(coord);
                    
                    // Load if not already loaded
                    if (!_activeChunks.ContainsKey(coord))
                    {
                        LoadChunk(coord);
                    }
                }
            }

            // Unload chunks outside desired set
            var toUnload = new List<ChunkCoord>();
            foreach (var coord in _activeChunks.Keys)
            {
                if (!desiredChunks.Contains(coord))
                {
                    toUnload.Add(coord);
                }
            }

            foreach (var coord in toUnload)
            {
                UnloadChunk(coord);
            }
        }

        /// <summary>
        /// Gets terrain at a specific hex coordinate.
        /// Returns null if chunk not loaded.
        /// </summary>
        public TerrainComponent? GetTerrainAt(HexCoord hex)
        {
            var chunkCoord = HexToChunk(hex);
            var chunk = GetChunk(chunkCoord);
            
            if (chunk == null || !chunk.IsLoaded)
                return null;

            // Calculate local index within chunk
            int localQ = hex.Q - (chunkCoord.X * MapChunk.ChunkSize);
            int localR = hex.R - (chunkCoord.Y * MapChunk.ChunkSize);
            int index = localR * MapChunk.ChunkSize + localQ;

            if (index < 0 || index >= MapChunk.ChunkSize * MapChunk.ChunkSize)
                return null;

            var entity = chunk.GetTile(index);
            if (entity.Id == 0 || !_world.IsEntityAlive(entity))
                return null;

            if (_world.HasComponent<TerrainComponent>(entity))
            {
                return _world.GetComponent<TerrainComponent>(entity);
            }

            return null;
        }

        /// <summary>
        /// Clears all loaded chunks.
        /// </summary>
        public void Clear()
        {
            var allCoords = new List<ChunkCoord>(_activeChunks.Keys);
            foreach (var coord in allCoords)
            {
                UnloadChunk(coord);
            }
            _activeChunks.Clear();
        }
    }
}
