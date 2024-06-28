using System;
using System.Threading;

namespace RingBuffer;

/// <summary>
/// Warning: The implementation of this ring buffer is not thread-safe.
/// </summary>
/// <typeparam name="T">The type of elements in the ring buffer.</typeparam>
public sealed class RingBuffer<T>
{
    private int _readIndex = 0;
    private int _writeIndex = 0;

    public bool IsFull => (_writeIndex + 1) % _capacity == _readIndex;

    public bool IsEmpty => _readIndex == _writeIndex;

    private readonly Memory<T> _buffer;
    private readonly int _capacity;

    /// <summary>
    /// Initializes a new instance of the RingBuffer class with the specified capacity.
    /// The capacity must be a multiple of the system's page size.
    /// </summary>
    /// <param name="capacity">The capacity of the ring buffer.</param>
    /// <exception cref="ArgumentException">Thrown when the capacity is not a multiple of the system's page size.</exception>
    public RingBuffer(int capacity)
    {
        if (capacity % Environment.SystemPageSize != 0)
        {
            throw new ArgumentException("capacity must be a multiple of the system's page size");
        }

        _capacity = capacity;
        _buffer = new T[capacity];
    }

    /// <summary>
    /// Gets the next element from the ring buffer.
    /// </summary>
    /// <returns>The next element from the ring buffer.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the ring buffer is empty.</exception>
    public T Get()
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException("ring buffer is empty");
        }

        var item = _buffer.Span[_readIndex];
        _readIndex = (_readIndex + 1) % _capacity;
        return item;
    }

    /// <summary>
    /// Attempts to get the next element from the ring buffer within the specified timeout period.
    /// </summary>
    /// <param name="timeout">The timeout period.</param>
    /// <returns>The next element from the ring buffer.</returns>
    /// <exception cref="TimeoutException">Thrown when the ring buffer is empty within the timeout period.</exception>
    public T Get(TimeSpan timeout)
    {
        var expiry = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < expiry)
        {
            if (!IsEmpty)
            {
                return Get();
            }
        }

        throw new TimeoutException("ring buffer is empty");
    }

    /// <summary>
    /// Attempts to get the next element from the ring buffer within the specified timeout period,
    /// or throws an exception if the operation is canceled.
    /// </summary>
    /// <param name="timeout">The timeout period.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The next element from the ring buffer.</returns>
    /// <exception cref="TimeoutException">Thrown when the ring buffer is empty within the timeout period.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public T Get(TimeSpan timeout, CancellationToken cancellationToken)
    {
        var expiry = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < expiry)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsEmpty)
            {
                return Get();
            }
        }

        throw new TimeoutException("ring buffer is empty");
    }

    /// <summary>
    /// Tries to get the next element from the ring buffer.
    /// </summary>
    /// <param name="item">The next element from the ring buffer.</param>
    /// <returns>True if an element was retrieved; otherwise, false.</returns>
    public bool TryGet(out T item)
    {
        if (IsEmpty)
        {
            item = default;
            return false;
        }

        item = _buffer.Span[_readIndex];
        _readIndex = (_readIndex + 1) % _capacity;
        return true;
    }

    /// <summary>
    /// Tries to get the next element from the ring buffer within the specified timeout period.
    /// </summary>
    /// <param name="timeout">The timeout period.</param>
    /// <param name="item">The next element from the ring buffer.</param>
    /// <returns>True if an element was retrieved within the timeout period; otherwise, false.</returns>
    public bool TryGet(TimeSpan timeout, out T item)
    {
        var expiry = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < expiry)
        {
            if (!IsEmpty)
            {
                item = Get();
                return true;
            }
        }

        item = default;
        return false;
    }

    /// <summary>
    /// Tries to get the next element from the ring buffer within the specified timeout period,
    /// or throws an exception if the operation is canceled.
    /// </summary>
    /// <param name="timeout">The timeout period.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="item">The next element from the ring buffer.</param>
    /// <returns>True if an element was retrieved within the timeout period; otherwise, false.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public bool TryGet(TimeSpan timeout, CancellationToken cancellationToken, out T item)
    {
        var expiry = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < expiry)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsEmpty)
            {
                item = Get();
                return true;
            }
        }

        item = default;
        return false;
    }

    /// <summary>
    /// Puts an element into the ring buffer.
    /// </summary>
    /// <param name="item">The element to add to the ring buffer.</param>
    /// <exception cref="InvalidOperationException">Thrown when the ring buffer is full.</exception>
    public void Put(T item)
    {
        if (IsFull)
        {
            throw new InvalidOperationException("ring buffer is full");
        }

        _buffer.Span[_writeIndex] = item;
        _writeIndex = (_writeIndex + 1) % _capacity;
    }

    /// <summary>
    /// Attempts to put an element into the ring buffer within the specified timeout period.
    /// </summary>
    /// <param name="item">The element to add to the ring buffer.</param>
    /// <param name="timeout">The timeout period.</param>
    /// <exception cref="TimeoutException">Thrown when the ring buffer is full within the timeout period.</exception>
    public void Put(T item, TimeSpan timeout)
    {
        var expiry = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < expiry)
        {
            if (!IsFull)
            {
                Put(item);
                return;
            }
        }

        throw new TimeoutException("ring buffer is full");
    }

    /// <summary>
    /// Attempts to put an element into the ring buffer within the specified timeout period,
    /// or throws an exception if the operation is canceled.
    /// </summary>
    /// <param name="item">The element to add to the ring buffer.</param>
    /// <param name="timeout">The timeout period.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <exception cref="TimeoutException">Thrown when the ring buffer is full within the timeout period.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public void Put(T item, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var expiry = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < expiry)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsFull)
            {
                Put(item);
                return;
            }
        }

        throw new TimeoutException("ring buffer is full");
    }

    /// <summary>
    /// Tries to put an element into the ring buffer.
    /// </summary>
    /// <param name="item">The element to add to the ring buffer.</param>
    /// <returns>True if the element was added; otherwise, false.</returns>
    public bool TryPut(T item)
    {
        if (IsFull)
        {
            return false;
        }

        _buffer.Span[_writeIndex] = item;
        _writeIndex = (_writeIndex + 1) % _capacity;
        return true;
    }

    /// <summary>
    /// Tries to put an element into the ring buffer within the specified timeout period.
    /// </summary>
    /// <param name="item">The element to add to the ring buffer.</param>
    /// <param name="timeout">The timeout period.</param>
    /// <returns>True if the element was added within the timeout period; otherwise, false.</returns>
    public bool TryPut(T item, TimeSpan timeout)
    {
        var expiry = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < expiry)
        {
            if (!IsFull)
            {
                Put(item);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Tries to put an element into the ring buffer within the specified timeout period,
    /// or throws an exception if the operation is canceled.
    /// </summary>
    /// <param name="item">The element to add to the ring buffer.</param>
    /// <param name="timeout">The timeout period.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>True if the element was added within the timeout period; otherwise, false.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public bool TryPut(T item, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var expiry = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < expiry)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsFull)
            {
                Put(item);
                return true;
            }
        }

        return false;
    }
}