# Praetor Engine

A high-performance, turn-based grand strategy game engine built with C# and MonoGame. Designed for games similar to Total War's campaign map, focusing on performance and efficiency.

**Current Version:** Phase 2 Complete (v0.2.0)  
**Author:** NathanGr33n  
**Target Framework:** .NET 8.0  
**Graphics:** MonoGame 3.8+ (DesktopGL)

## 🎯 Project Goals

- **Performance**: Handle 10,000+ entities at 60 FPS
- **Zero Allocations**: Minimize GC pressure through object pooling
- **Data-Oriented Design**: Cache-friendly ECS architecture
- **Security**: Input validation and safe memory management

## ✅ Phase 1 Complete

### Core Systems

1. **Entity Component System** - Generation-based entity lifecycle, O(1) component access
2. **Memory Management** - Object pooling (98.3% allocation reduction), zero GC
3. **Rendering System** - Camera2D with smooth interpolation, visibility culling
4. **Hexagonal Grid** - Axial coordinates, distance calculation, neighbor queries
5. **Performance Profiler** - Hierarchical timing with frame budget tracking

## ✅ Phase 2 Complete

### Map & Simulation Systems

1. **Terrain System** - 8 terrain types with movement costs, passability, and procedural generation
2. **Chunk Management** - 16x16 hex chunk streaming based on camera view
3. **Pathfinding** - A* algorithm optimized for hex grids with terrain awareness
4. **Movement System** - Turn-based movement with path validation and range calculation
5. **Settlement System** - Cities/towns with population and procedural naming
6. **Turn Manager** - Turn-based flow control with phases and event system
7. **Integration Demo** - 64x64 maps with 100 units, pathfinding, and settlements

## 📊 Performance Results

### Phase 1
- ✅ 10,000 entities in <1ms
- ✅ 98.3% allocation reduction with pooling
- ✅ Zero GC collections

### Phase 2
- ✅ 4096 hex tiles (64x64 map) generation
- ✅ Pathfinding <5ms for typical paths (10-20 hexes)
- ✅ Chunk streaming with zero frame drops
- ✅ Zero allocations during pathfinding operations
- ✅ 100+ units with movement at 60 FPS

## 🚀 Quick Start

```bash
cd PraetorEngine/PraetorEngine
dotnet run
```

## 🏗️ Architecture

```
Core/
├── ECS/          # Entity Component System
├── Memory/       # Object pooling & allocation tracking
├── Rendering/    # Camera2D system
└── Profiling/    # Performance profiler
World/
├── HexGrid.cs    # Hexagonal grid utilities
├── Terrain.cs    # Terrain types and generation
├── MapChunk.cs   # Chunk management and streaming
└── Settlement.cs # Settlement/city system
Systems/
├── Pathfinding/  # A* pathfinding for hex grids
├── Movement/     # Turn-based unit movement
└── TurnManager.cs # Turn flow control
```

## 📈 Roadmap

**Phase 2**: ✅ Complete - Map systems, pathfinding, simulation  
**Phase 3**: Combat, factions, save/load  
**Phase 4**: AI, diplomacy, technology trees

---

**Built with performance and maintainability in mind.**  
*Phase 2 Complete - December 2025*
