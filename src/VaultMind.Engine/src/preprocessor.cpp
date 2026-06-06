#include <string>
#include <algorithm>
#include <vector>

// TODO (Month 3): Implement real tokenization and data preprocessing

namespace vaultmind {

/// Normalize input text: lowercase, trim whitespace.
std::string normalize_text(const std::string& input) {
    std::string result = input;

    // Trim leading/trailing whitespace
    auto start = result.find_first_not_of(" \t\n\r");
    auto end = result.find_last_not_of(" \t\n\r");
    if (start == std::string::npos) return "";
    result = result.substr(start, end - start + 1);

    // Lowercase
    std::transform(result.begin(), result.end(), result.begin(), ::tolower);

    return result;
}

/// Split text into chunks for batch processing.
std::vector<std::string> chunk_text(const std::string& text, size_t max_chunk_size) {
    std::vector<std::string> chunks;
    for (size_t i = 0; i < text.size(); i += max_chunk_size) {
        chunks.push_back(text.substr(i, max_chunk_size));
    }
    return chunks;
}

} // namespace vaultmind
