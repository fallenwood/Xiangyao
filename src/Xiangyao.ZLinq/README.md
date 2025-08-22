# Xiangyao.ZLinq - Zero-Allocation LINQ

Xiangyao.ZLinq is a high-performance, zero-allocation alternative to System.Linq for performance-critical paths in the Xiangyao reverse proxy.

## Purpose

This library provides optimized implementations of common LINQ operations specifically designed for:

- **Label parsing** - Processing Docker container labels with minimal allocations
- **Container data processing** - Hot paths in Docker container discovery
- **Performance-critical routing operations** - High-frequency operations in reverse proxy routing

## Features

- **Zero-allocation FirstOrDefault** for arrays and spans with predicates
- **Optimized counting and filtering** for arrays 
- **In-place operations** to minimize memory allocations
- **Span-based operations** for maximum performance

## Usage

```csharp
using Xiangyao.ZLinq;

// Zero-allocation search in label arrays
var enabledLabel = labels.FirstOrDefault(e => 
    string.Equals(e.Name, "xiangyao.enable", StringComparison.OrdinalIgnoreCase));

// Count matching items without allocations
int count = labels.Count(e => e.Name.StartsWith("xiangyao."));

// Filter items in-place to avoid allocations
var buffer = new Label[10];
int filteredCount = labels.WhereInPlace(e => e.Name.StartsWith("xiangyao."), buffer);
```

## Performance Benefits

ZLinq provides significant performance improvements over System.Linq in hot paths:

- **No delegate allocations** for simple predicates
- **No intermediate collections** created during operations
- **Optimized loop structures** for maximum CPU cache efficiency
- **Span-based operations** where possible to eliminate bounds checking

## API Reference

### FirstOrDefault Operations
- `FirstOrDefault<T>(this T[] source)` - Get first element or default
- `FirstOrDefault<T>(this T[] source, Func<T, bool> predicate)` - Get first matching element
- `FirstOrDefault<T>(this ReadOnlySpan<T> source, Func<T, bool> predicate)` - Span-based search

### Counting Operations
- `Count<T>(this T[] source, Func<T, bool> predicate)` - Count matching elements
- `Any<T>(this T[] source, Func<T, bool> predicate)` - Check if any elements match

### In-Place Operations
- `WhereInPlace<T>(this T[] source, Func<T, bool> predicate, T[] buffer)` - Filter into pre-allocated buffer
- `SelectInPlace<TSource, TResult>(this TSource[] source, Func<TSource, TResult> selector, TResult[] buffer)` - Transform into pre-allocated buffer

## Design Principles

1. **Minimal allocations** - Avoid creating unnecessary objects or collections
2. **Array-focused** - Optimized for array operations common in the codebase
3. **Simple API** - Drop-in replacement for common System.Linq patterns
4. **Performance-first** - Designed for hot paths, not general-purpose use