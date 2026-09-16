// C ABI boundary for the pinned DmitryUlyanov/Multicore-TSNE implementation.
// Original algorithm is compiled separately, unchanged. See LICENSE.txt.
#include <algorithm>
#include <cmath>
#include <cstdint>
#include <cstdio>
#include <exception>
#include <limits>
#include <mutex>
#include <omp.h>

#ifndef _OPENMP
#error Multicore-TSNE must be compiled with OpenMP enabled.
#endif

extern "C" void tsne_run_double(double*, int, int, double*, int, double, double,
    int, int, int, int, bool, int, double, double, double*, int);

namespace {
    std::mutex run_mutex; // upstream srand/rand share native CRT state
    int fail(int code, const char* message, char* buffer, int capacity) {
        if (buffer && capacity > 0) snprintf(buffer, capacity, "%s", message);
        return code;
    }
    struct OmpSettings {
        int threads = omp_get_max_threads();
        int dynamic = omp_get_dynamic();
        ~OmpSettings() { omp_set_num_threads(threads); omp_set_dynamic(dynamic); }
    };
}

extern "C" __declspec(dllexport) int __cdecl tas_tsne_abi_version() { return 1; }

// Flat row-major buffers; caller owns all memory. No C++ bool or exception
// crosses this boundary. Return 0 on success, otherwise fill the error buffer.
extern "C" __declspec(dllexport) int __cdecl tas_tsne_run(
    double* x, int input_length, int rows, int columns,
    double* y, int output_length, double perplexity, double theta,
    int threads, int iterations, int seed, double learning_rate,
    double* final_error, int* actual_threads, char* error, int error_capacity)
{
    if (error && error_capacity > 0) error[0] = '\0';
    if (!x || !y || !final_error || !actual_threads || rows < 4 || columns < 2 ||
        static_cast<int64_t>(rows) * columns != input_length ||
        static_cast<int64_t>(rows) * 2 != output_length)
        return fail(1, "Invalid input/output buffers or dimensions.", error, error_capacity);
    if (!std::isfinite(perplexity) || perplexity < 1 || 3 * perplexity > rows - 1 ||
        !std::isfinite(theta) || theta <= 0 || theta > 1 ||
        !std::isfinite(learning_rate) || learning_rate <= 0 || iterations < 1 ||
        seed < 0 || threads < 1 || threads > omp_get_num_procs() ||
        2.0 * rows * std::floor(3 * perplexity) > std::numeric_limits<int>::max())
        return fail(1, "Invalid t-SNE settings or native integer capacity exceeded.", error, error_capacity);
    bool varied = false;
    const double safe_magnitude = std::numeric_limits<double>::max() / (4.0 * rows);
    for (int i = 0; i < input_length; ++i) {
        if (!std::isfinite(x[i]) || std::abs(x[i]) > safe_magnitude)
            return fail(1, "Input must be finite and safe to center. Standardize features first.", error, error_capacity);
        if (i >= columns && x[i] != x[i % columns]) varied = true;
    }
    if (!varied) return fail(1, "Input needs variation between rows.", error, error_capacity);

    std::lock_guard<std::mutex> guard(run_mutex);
    OmpSettings previous;
    omp_set_dynamic(0);
    omp_set_num_threads(threads);
    *actual_threads = 0;
    *final_error = std::numeric_limits<double>::quiet_NaN();
    std::fill(y, y + output_length, std::numeric_limits<double>::quiet_NaN());
    try {
        // Report the runtime team size, not merely the requested setting.
        #pragma omp parallel
        {
            #pragma omp single
            *actual_threads = omp_get_num_threads();
        }
        tsne_run_double(x, rows, columns, y, 2, perplexity, theta, threads,
            iterations, 250, seed, false, 0, 12, learning_rate, final_error, 1);
        if (!std::isfinite(*final_error))
            return fail(2, "Multicore-TSNE did not produce a finite final KL divergence.", error, error_capacity);
        for (int i = 0; i < output_length; ++i)
            if (!std::isfinite(y[i]))
                return fail(2, "Multicore-TSNE returned non-finite coordinates.", error, error_capacity);
        return 0;
    }
    catch (const std::exception& e) { return fail(3, e.what(), error, error_capacity); }
    catch (...) { return fail(3, "Unexpected native t-SNE exception.", error, error_capacity); }
}
