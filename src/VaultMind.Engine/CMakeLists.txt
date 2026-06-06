cmake_minimum_required(VERSION 3.20)
project(VaultMind.Engine VERSION 0.1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# ONNX Runtime (uncomment when ready in Month 3)
# find_package(onnxruntime REQUIRED)

add_library(vaultmind_engine SHARED
    src/engine.cpp
    src/preprocessor.cpp
)

target_include_directories(vaultmind_engine PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}/include
)

# target_link_libraries(vaultmind_engine PRIVATE onnxruntime::onnxruntime)

# Export symbols for P/Invoke interop with .NET
set_target_properties(vaultmind_engine PROPERTIES
    WINDOWS_EXPORT_ALL_SYMBOLS ON
    OUTPUT_NAME "vaultmind_engine"
)
