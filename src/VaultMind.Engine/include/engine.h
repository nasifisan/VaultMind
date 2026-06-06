#pragma once

#ifdef _WIN32
    #define VAULTMIND_API __declspec(dllexport)
#else
    #define VAULTMIND_API __attribute__((visibility("default")))
#endif

extern "C" {
    /// Initialize the inference engine with a model path.
    /// Returns 0 on success, non-zero on failure.
    VAULTMIND_API int vm_engine_init(const char* model_path);

    /// Run inference on input text.
    /// Result is written to output_buffer (up to buffer_size bytes).
    /// Returns the number of bytes written, or -1 on error.
    VAULTMIND_API int vm_engine_infer(
        const char* input_text,
        char* output_buffer,
        int buffer_size
    );

    /// Free all engine resources.
    VAULTMIND_API void vm_engine_shutdown();
}
