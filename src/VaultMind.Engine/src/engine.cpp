#include "engine.h"
#include <cstring>
#include <string>

// TODO (Month 3): Replace with ONNX Runtime implementation

static bool s_initialized = false;

VAULTMIND_API int vm_engine_init(const char* model_path) {
    // Placeholder: will load ONNX model here
    s_initialized = true;
    return 0;
}

VAULTMIND_API int vm_engine_infer(
    const char* input_text,
    char* output_buffer,
    int buffer_size
) {
    if (!s_initialized) return -1;

    // Placeholder: echo back a response
    std::string response = "[VaultMind Engine] Processed: " + std::string(input_text);

    int len = static_cast<int>(response.size());
    if (len >= buffer_size) len = buffer_size - 1;

    std::memcpy(output_buffer, response.c_str(), len);
    output_buffer[len] = '\0';
    return len;
}

VAULTMIND_API void vm_engine_shutdown() {
    s_initialized = false;
}
