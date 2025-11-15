using System;
using System.Text.Json.Nodes;

namespace JSONMorph;

internal readonly struct PathLocation
{
    public PathLocation(JsonNode parent, string? propertyName, int? arrayIndex)
    {
        Parent = parent;
        PropertyName = propertyName;
        ArrayIndex = arrayIndex;
    }

    public JsonNode Parent { get; }
    public string? PropertyName { get; }
    public int? ArrayIndex { get; }
}

internal ref struct PathReader
{
    private readonly ReadOnlySpan<char> _span;
    private int _position;
    private readonly bool _isEmpty;

    public PathReader(string path)
    {
        _span = path.AsSpan();
        if (_span.Length == 0)
        {
            _isEmpty = true;
            _position = 0;
            return;
        }

        if (_span[0] != '/')
        {
            throw new InvalidOperationException("Path must start with '/'.");
        }

        _isEmpty = false;
        _position = 1;
    }

    public bool TryRead(out ReadOnlySpan<char> segment)
    {
        if (_isEmpty)
        {
            segment = default;
            return false;
        }

        if (_position > _span.Length)
        {
            segment = default;
            return false;
        }

        int start = _position;
        while (_position < _span.Length && _span[_position] != '/')
        {
            _position++;
        }

        segment = _span.Slice(start, _position - start);

        if (_position < _span.Length && _span[_position] == '/')
        {
            _position++;
        }

        return true;
    }

    public bool IsAtEnd => _isEmpty || _position >= _span.Length;
}
