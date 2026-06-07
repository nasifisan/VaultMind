# VaultMind.Engine

> C++20 inference engine for VaultMind — local model execution using ONNX Runtime. **(Planned — Phase 3)**

## Status: 🚧 Not Yet Implemented

This project is scaffolded but not yet built. It is part of **Phase 3 (Weeks 9-12)** of the VaultMind roadmap.

## What This Will Do

The engine will:

1. Load quantized ONNX models directly into memory (no server overhead)
2. Run inference at millisecond latency on CPU
3. Expose a C shared library (`.dll` / `.so`) for .NET interop via P/Invoke
4. Support INT8 and FP16 quantization for speed/accuracy tradeoffs

## Planned Architecture

```
VaultMind.API (C#)
      │
      │ P/Invoke (.dll)
      ▼
VaultMind.Engine (C++)
      │
      │ ONNX Runtime C++ API
      ▼
  Quantized .onnx Model
```

## Current Files

```
VaultMind.Engine/
├── CMakeLists.txt     # CMake build configuration (scaffolded)
├── include/           # Header files (empty)
└── src/               # Source files (empty)
```

## Prerequisites (When Ready)

- C++20 compatible compiler (MSVC, GCC 12+, or Clang 15+)
- CMake 3.20+
- [ONNX Runtime](https://onnxruntime.ai/) C++ SDK
