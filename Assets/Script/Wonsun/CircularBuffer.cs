using UnityEngine;
using System.Collections;

public class CircularBuffer<T>
{
    private T[] bufferArray;
    private int start;
    private int end;
    private int size;

    public CircularBuffer(int _size)
    {
        if (_size > 0)
        {
            size = _size;
            bufferArray = new T[size];
            start = 0;
            end = 0;
        }
    }

    public void Push(T obj)
    {
        if (end == null) { return; }
        if ((end + 1) % size == start)
        {
            start = (start + 1) % size;
        }

        bufferArray[end] = obj;
        end = (end + 1) % size;
    }

    public T Pop()
    {
        if (end != start)
        {
            end = (end - 1 + size) % size;
            return bufferArray[end];
        }
        return default(T);
    }

    public void Clear()
    {
        for (int iLoop = 0; iLoop < size; iLoop++)
        {
            bufferArray[iLoop] = default(T);
        }

        start = 0;
        end = 0;
    }

    public int Count
    {
        get
        {
            return (end - start + size) % size;
        }
    }

    public T[] Values
    {
        get
        {
            return bufferArray;
        }
    }

    public T getValue(int index)
    {
        return bufferArray[index];
    }
}