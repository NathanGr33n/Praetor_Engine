# Praetor Engine

A high-performance, turn-based grand strategy game engine built with C# and MonoGame. Designed for games similar to Total War's campaign map, focusing on performance and efficiency.

**Current Version:** Phase 1 Complete (v0.1.0)  
**Author:** NathanGr33n  
**Target Framework:** .NET 8.0  
**Graphics:** MonoGame 3.8+ (DesktopGL)

## 🎯 Project Goals

- **Performance**: Handle 10,000+ entities at 60 FPS
- **Zero Allocations**: Minimize GC pressure through object pooling
- **Data-Oriented Design**: Cache-friendly ECS architecture
- **Security**: Input validation and safe memory management

## ✅ Phase 1 Complete

### Systems Implemented

1. **Entity Component System** - Generation-based entity lifecycle, O(1) component access
2. **Memory Management** - Object pooling (98.3% allocation reduction), zero GC
3. **Rendering System** - Camera2D with smooth interpolation, visibility culling
4. **Hexagonal Grid** - Axial coordinates, distance calculation, neighbor queries
5. **Performance Profiler** - Hierarchical timing with frame budget tracking
6. **Integration Demo** - All systems working together

## 📊 Performance Results

- ✅ 10,000 entities in <1ms
- ✅ 98.3% allocation reduction with pooling
- ✅ Zero GC collections
- ✅ 6.20ms for 100 hex entities (meets 60 FPS target)

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
└── HexGrid.cs    # Hexagonal grid utilities
```

## 📈 Roadmap

**Phase 2**: Map chunking, pathfinding, simulation systems  
**Phase 3**: Combat, factions, save/load  
**Phase 4**: AI, diplomacy, technology trees

---

**Built with performance and maintainability in mind.**  
*Phase 1 Complete - December 2025*
