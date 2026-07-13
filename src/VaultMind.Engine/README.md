# VaultMind.Engine

> C++20 inference engine for VaultMind — local model execution using ONNX Runtime. **(Phase 3 — Up Next)**

## Status: 🔜 Up Next

This project is scaffolded with placeholder implementations. It is part of **Phase 3 (Weeks 9-12)** of the VaultMind roadmap.

## What This Will Do

The engine will:

1. Load quantized ONNX models directly into memory (no server overhead)
2. Run text classification inference at millisecond latency on CPU
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
      ├── tokenizer.cpp     → Vocab loading, text tokenization
      ├── engine.cpp         → ONNX Runtime session management
      ├── preprocessor.cpp   → Text normalization
      │
      │ ONNX Runtime C++ API
      ▼
  Quantized .onnx Model (INT8/FP16)
```

## Current Files

```
VaultMind.Engine/
├── CMakeLists.txt          # CMake build configuration (scaffolded)
├── README.md               # This file
├── include/
│   └── engine.h            # C API header (vm_engine_init, vm_engine_infer, vm_engine_shutdown)
└── src/
    ├── engine.cpp           # Placeholder — will load ONNX model and run inference
    └── preprocessor.cpp     # Text normalization and chunking utilities
```

## Prerequisites (When Ready)

- C++20 compatible compiler (MSVC, GCC 12+, or Clang 15+)
- CMake 3.20+
- [ONNX Runtime](https://onnxruntime.ai/) C++ SDK (v1.18+)
- A pre-trained ONNX model (e.g., DistilBERT for text classification)
