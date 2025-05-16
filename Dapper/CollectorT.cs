using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Dapper;

/// <summary>
/// Allows efficient collection of data into lists, arrays, etc.
/// </summary>
/// <remarks>This is a mutable struct; treat with caution.</remarks>
/// <typeparam name="T"></typeparam>
[DebuggerDisplay($"{{{nameof(ToString)}(),nq}}")]
[SuppressMessage("Usage", "CA2231:Overload operator equals on overriding value type Equals", Justification = "Equality not supported")]
public struct Collector<T>
{
    /// <summary>
    /// Create a new collector using a size hint for the number of elements expected.
    /// </summary>
    public Collector(int capacityHint)
    {
        oversized = capacityHint > 0 ? ArrayPool<T>.Shared.Rent(capacityHint) : [];
        capacity = oversized.Length;
    }

    /// <inheritdoc/>
    public readonly override string ToString() => $"Count: {count}";

    /// <inheritdoc/>
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public readonly override bool Equals([NotNullWhen(true)] object? obj) => throw new NotSupportedException();

    /// <inheritdoc/>
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public readonly override int GetHashCode() => throw new NotSupportedException();

    private T[] oversized;
    private int count, capacity;

    /// <summary>
    /// Gets the current capacity of the backing buffer of this instance.
    /// </summary>
    internal readonly int Capacity => capacity;

    /// <summary>
    /// Gets the number of elements represented by this instance.
    /// </summary>
    public readonly int Count => count;

    /// <summary>
    /// Gets the underlying elements represented by this instance.
    /// </summary>
    public readonly Span<T> Span => new(oversized, 0, count);

    /// <summary>
    /// Gets the underlying elements represented by this instance.
    /// </summary>
    public readonly ArraySegment<T> ArraySegment => new(oversized, 0, count);

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    public readonly ref T this[int index]
    {
        get
        {
            return ref index >= 0 & index < count ? ref oversized[index] : ref OutOfRange();

            static ref T OutOfRange() => throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    /// <summary>
    /// Add an element to the collection.
    /// </summary>
    public void Add(T value)
    {
        if (capacity == count) Expand();
        oversized[count++] = value;
    }

    /// <summary>
    /// Add elements to the collection.
    /// </summary>
    public void AddRange(ReadOnlySpan<T> values)
    {
        EnsureCapacity(count + values.Length);
        values.CopyTo(new(oversized, count, values.Length));
        count += values.Length;
    }

    private void EnsureCapacity(int minCapacity)
    {
        if (capacity < minCapacity)
        {
            var newBuffer = ArrayPool<T>.Shared.Rent(minCapacity);
            Span.CopyTo(newBuffer);
            var oldBuffer = oversized;
            oversized = newBuffer;
            capacity = newBuffer.Length;

            if (oldBuffer is not null)
            {
                ArrayPool<T>.Shared.Return(oldBuffer);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Expand() => EnsureCapacity(Math.Max(capacity * 2, 16));

    /// <summary>
    /// Release any resources associated with this instance.
    /// </summary>
    public void Clear()
    {
        count = 0;
        if (capacity != 0)
        {
            capacity = 0;
            ArrayPool<T>.Shared.Return(oversized);
            oversized = [];
        }
    }

    /// <summary>
    /// Create an array with the elements associated with this instance, and release any resources.
    /// </summary>
    public T[] ToArrayAndClear()
    {
        T[] result = [.. Span]; // let the compiler worry about the per-platform implementation
        Clear();
        return result;
    }

    /// <summary>
    /// Create an array with the elements associated with this instance, and release any resources.
    /// </summary>
    public List<T> ToListAndClear()
    {
        List<T> result = [.. Span]; // let the compiler worry about the per-platform implementation (net8+ in particular)
        Clear();
        return result;
    }
}
