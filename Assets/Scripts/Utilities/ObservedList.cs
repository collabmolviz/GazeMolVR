using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace UMol {
[Serializable]
public class ObservedList<T> : List<T>
{
    public event Action<int> Changed;

    public new void Add(T item)
    {
        base.Add(item);
        InvokeChanged();
    }

    public new void Remove(T item)
    {
        base.Remove(item);
        InvokeChanged();
    }
    public new void AddRange(IEnumerable<T> collection)
    {
        base.AddRange(collection);
        InvokeChanged();
    }
    public new void RemoveRange(int index, int count)
    {
        base.RemoveRange(index, count);
        InvokeChanged();
    }
    public new void Clear()
    {
        base.Clear();
        InvokeChanged();
    }
    public new void Insert(int index, T item)
    {
        base.Insert(index, item);
        InvokeChanged();
    }
    public new void InsertRange(int index, IEnumerable<T> collection)
    {
        base.InsertRange(index, collection);
        InvokeChanged();
    }
    public new void RemoveAll(Predicate<T> match)
    {
        base.RemoveAll(match);
        InvokeChanged();
    }

    public new T this[int index]
    {
        get
        {
            return base[index];
        }
        set
        {
            base[index] = value;
            Changed(index);
        }
    }

    void InvokeChanged()
    {
        if (Changed != null)
            Changed(this.Count);
    }
}
}