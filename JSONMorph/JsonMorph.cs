using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JSONMorph;

/// <summary>
/// Provides helpers for generating and applying JSONMorph patches using the library's compact operation codes.
/// </summary>
/// <remarks>
/// The supported operations are add (<c>a</c>), remove (<c>rm</c>), replace (<c>rp</c>), and text diff (<c>td</c>). Text diff
/// operations target string values and encode start index, delete length, and replacement text instead of replacing the
/// entire value. The API parses and emits JSON text using <see cref="JsonNode"/> semantics so formatting is preserved only
/// where possible. All JSON parsing and serialization uses invariant culture formatting to keep patches stable across environments.
/// </remarks>
public static class JsonMorph
{
    private const string ReplaceOperationCode = "rp";
    private const string AddOperationCode = "a";
    private const string RemoveOperationCode = "rm";
    private const string TextDiffOperationCode = "td";
    private const string MoveOperationCode = "mv";
    private const string CopyOperationCode = "cp";
    private const string ListDiffOperationCode = "ld";

    private const string OpPropertyName = "op";
    private const string PathPropertyName = "p";
    private const string ValuePropertyName = "v";
    private const string FromPropertyName = "f";
    private const string TextDiffStartPropertyName = "s";
    private const string TextDiffDeleteLengthPropertyName = "dl";
    private const string TextDiffInsertTextPropertyName = "it";
    private const string ListDiffMovesPropertyName = "m";
    private const string ListDiffMoveToPropertyName = "t";

    private const int MaxReorderAnalysisLength = 256;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private enum PatchOperationKind
    {
        Replace,
        Add,
        Remove,
        TextDiff,
        Move,
        Copy,
        ListDiff
    }

    private sealed record class PatchOperation(PatchOperationKind Kind, string Path, JsonNode? Value, string? FromPath);

    /// <summary>
    /// Applies a JSON patch document to a JSON payload and returns the transformed JSON.
    /// </summary>
    /// <param name="jsonDocument">The JSON document that will receive the patch.</param>
    /// <param name="jsonPatch">The JSON patch to apply. The document must be an array of operations encoded with <c>op</c>, <c>path</c>, and optional <c>value</c> members. Text diff (<c>td</c>) operations expect an object payload with <c>start</c>, <c>deleteLength</c>, and <c>insertText</c>.</param>
    /// <returns>The patched JSON document serialized with indented formatting.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jsonDocument"/> or <paramref name="jsonPatch"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the JSON payload or patch cannot be parsed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the patch contains unsupported or invalid operations.</exception>
    public static string ApplyPatch(string jsonDocument, string jsonPatch)
    {
        ArgumentNullException.ThrowIfNull(jsonPatch);
        return ApplyPatches(jsonDocument, new[] { jsonPatch });
    }

    /// <summary>
    /// Applies a sequence of JSON patch documents to a JSON payload and returns the transformed JSON.
    /// </summary>
    /// <param name="jsonDocument">The JSON document that will receive the patches.</param>
    /// <param name="jsonPatches">The ordered collection of JSON patches to apply. Each entry must be a JSON array containing operations.</param>
    /// <returns>The patched JSON document serialized with indented formatting.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jsonDocument"/> or <paramref name="jsonPatches"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the sequence contains a <see langword="null"/> patch.</exception>
    /// <exception cref="InvalidOperationException">Thrown when any patch contains unsupported or invalid operations.</exception>
    public static string ApplyPatches(string jsonDocument, IEnumerable<string> jsonPatches)
    {
        ArgumentNullException.ThrowIfNull(jsonDocument);
        ArgumentNullException.ThrowIfNull(jsonPatches);

        JsonNode root = ParseJsonNode(jsonDocument, nameof(jsonDocument));

        foreach (string jsonPatch in jsonPatches)
        {
            if (jsonPatch is null)
            {
                throw new ArgumentException("Patch sequence cannot contain null entries.", nameof(jsonPatches));
            }

            JsonArray operationsArray = ParsePatchArray(jsonPatch);
            ApplyPatchOperations(ref root, operationsArray);
        }

        return root.ToJsonString(SerializerOptions);
    }

    /// <summary>
    /// Applies a sequence of JSON patch documents to a JSON payload and returns the transformed JSON.
    /// </summary>
    /// <param name="jsonDocument">The JSON document that will receive the patches.</param>
    /// <param name="jsonPatches">The ordered collection of JSON patches to apply. Each entry must be a JSON array containing operations.</param>
    /// <returns>The patched JSON document serialized with indented formatting.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="jsonPatches"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when any patch contains unsupported or invalid operations.</exception>
    public static string ApplyPatches(string jsonDocument, params string[] jsonPatches)
    {
        ArgumentNullException.ThrowIfNull(jsonPatches);
        return ApplyPatches(jsonDocument, (IEnumerable<string>)jsonPatches);
    }

    private static void ApplyPatchOperations(ref JsonNode root, JsonArray operationsArray)
    {
        foreach (PatchOperation operation in ToPatchOperations(operationsArray))
        {
            ApplyOperation(ref root, operation);
        }
    }

    /// <summary>
    /// Generates a JSON patch that transforms one JSON document into another. String differences are encoded using text diff operations.
    /// </summary>
    /// <param name="originalJson">The baseline JSON document.</param>
    /// <param name="modifiedJson">The updated JSON document to compare against the baseline.</param>
    /// <returns>A JSON patch array containing the minimal set of operations required to reach the modified document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="originalJson"/> or <paramref name="modifiedJson"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when either JSON document cannot be parsed.</exception>
    public static string GeneratePatch(string originalJson, string modifiedJson)
    {
        ArgumentNullException.ThrowIfNull(originalJson);
        ArgumentNullException.ThrowIfNull(modifiedJson);

        using JsonDocument originalDocument = ParseJsonDocument(originalJson, nameof(originalJson));
        using JsonDocument modifiedDocument = ParseJsonDocument(modifiedJson, nameof(modifiedJson));

        var collectedOperations = new List<JsonObject>();
        CollectDifferences(originalDocument.RootElement, modifiedDocument.RootElement, string.Empty, collectedOperations);

        var operationsArray = new JsonArray();
        foreach (JsonObject operation in collectedOperations)
        {
            operationsArray.Add(operation);
        }

        return operationsArray.ToJsonString(SerializerOptions);
    }

    private static JsonDocument ParseJsonDocument(string json, string parameterName)
    {
        try
        {
            return JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid JSON document.", parameterName, ex);
        }
    }

    private static void CollectDifferences(JsonElement? original, JsonElement? modified, string path, List<JsonObject> operations)
    {
        if (original.HasValue && modified.HasValue && ElementsEqual(original.Value, modified.Value))
        {
            return;
        }

        if (original.HasValue && modified.HasValue &&
            original.Value.ValueKind == JsonValueKind.Object &&
            modified.Value.ValueKind == JsonValueKind.Object)
        {
            var propertyOperations = new List<JsonObject>();
            DiffObject(original.Value, modified.Value, path, propertyOperations);

            if (propertyOperations.Count == 0)
            {
                return;
            }

            JsonObject replaceOperation = CreateReplaceOperation(path, ElementToNode(modified.Value));
            int replaceSize = CalculateOperationSize(replaceOperation);
            int propertySize = CalculateOperationsSize(propertyOperations);

            if (!string.IsNullOrEmpty(path) && replaceSize < propertySize)
            {
                operations.Add(replaceOperation);
            }
            else
            {
                operations.AddRange(propertyOperations);
            }

            return;
        }

        if (original.HasValue && modified.HasValue &&
            original.Value.ValueKind == JsonValueKind.Array &&
            modified.Value.ValueKind == JsonValueKind.Array)
        {
            DiffArray(original.Value, modified.Value, path, operations);
            return;
        }

        if (original.HasValue && modified.HasValue &&
            original.Value.ValueKind == JsonValueKind.String &&
            modified.Value.ValueKind == JsonValueKind.String)
        {
            AppendTextDiffOperations(path, original.Value.GetString() ?? string.Empty, modified.Value.GetString() ?? string.Empty, operations);
            return;
        }

        if (!original.HasValue && modified.HasValue)
        {
            operations.Add(CreateAddOperation(path, ElementToNode(modified.Value)));
            return;
        }

        if (original.HasValue && !modified.HasValue)
        {
            operations.Add(CreateRemoveOperation(path));
            return;
        }

        if (modified.HasValue)
        {
            operations.Add(CreateReplaceOperation(path, ElementToNode(modified.Value)));
        }
        else if (original.HasValue)
        {
            operations.Add(CreateRemoveOperation(path));
        }
    }

    private static void DiffObject(JsonElement original, JsonElement modified, string path, List<JsonObject> operations)
    {
        var processed = new HashSet<string>(StringComparer.Ordinal);

        foreach (JsonProperty property in modified.EnumerateObject())
        {
            processed.Add(property.Name);
            string childPath = AppendPath(path, property.Name);

            if (original.TryGetProperty(property.Name, out JsonElement originalValue))
            {
                CollectDifferences(originalValue, property.Value, childPath, operations);
            }
            else
            {
                operations.Add(CreateAddOperation(childPath, ElementToNode(property.Value)));
            }
        }

        foreach (JsonProperty property in original.EnumerateObject())
        {
            if (!processed.Contains(property.Name))
            {
                operations.Add(CreateRemoveOperation(AppendPath(path, property.Name)));
            }
        }
    }

    private static void DiffArray(JsonElement original, JsonElement modified, string path, List<JsonObject> operations)
    {
        List<JsonElement> originalItems = original.EnumerateArray().ToList();
        List<JsonElement> modifiedItems = modified.EnumerateArray().ToList();

        int sharedPrefixLength = CountSharedPrefix(originalItems, modifiedItems);
        int sharedSuffixLength = CountSharedSuffix(originalItems, modifiedItems, sharedPrefixLength);

        if (sharedPrefixLength == originalItems.Count && sharedPrefixLength == modifiedItems.Count)
        {
            return;
        }

        if (modifiedItems.Count == originalItems.Count + 1 && sharedPrefixLength + sharedSuffixLength == originalItems.Count)
        {
            int insertIndex = sharedPrefixLength;
            JsonNode valueToInsert = ElementToNode(modifiedItems[insertIndex]);
            if (insertIndex == originalItems.Count)
            {
                operations.Add(CreateAddOperation(path, valueToInsert));
            }
            else
            {
                operations.Add(CreateAddOperation(AppendIndex(path, insertIndex), valueToInsert));
            }

            return;
        }

        if (modifiedItems.Count + 1 == originalItems.Count && sharedPrefixLength + sharedSuffixLength == modifiedItems.Count)
        {
            int removeIndex = sharedPrefixLength;
            operations.Add(CreateRemoveOperation(AppendIndex(path, removeIndex)));
            return;
        }

        if (originalItems.Count == modifiedItems.Count &&
            TryEmitReorderOperations(originalItems, modifiedItems, path, operations))
        {
            return;
        }

        int minCount = Math.Min(originalItems.Count, modifiedItems.Count);
        for (int i = 0; i < minCount; i++)
        {
            string childPath = AppendIndex(path, i);
            CollectDifferences(originalItems[i], modifiedItems[i], childPath, operations);
        }

        if (modifiedItems.Count > originalItems.Count)
        {
            for (int i = originalItems.Count; i < modifiedItems.Count; i++)
            {
                operations.Add(CreateAddOperation(path, ElementToNode(modifiedItems[i])));
            }
        }
        else if (originalItems.Count > modifiedItems.Count)
        {
            for (int i = modifiedItems.Count; i < originalItems.Count; i++)
            {
                operations.Add(CreateRemoveOperation(AppendIndex(path, modifiedItems.Count)));
            }
        }
    }

    private static bool ElementsEqual(JsonElement left, JsonElement right)
    {
        if (left.ValueKind != right.ValueKind)
        {
            if (left.ValueKind == JsonValueKind.Null && right.ValueKind == JsonValueKind.Null)
            {
                return true;
            }

            return false;
        }

        switch (left.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var leftProperties = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                foreach (JsonProperty property in left.EnumerateObject())
                {
                    leftProperties[property.Name] = property.Value;
                }

                int matched = 0;
                foreach (JsonProperty property in right.EnumerateObject())
                {
                    if (!leftProperties.TryGetValue(property.Name, out JsonElement leftValue))
                    {
                        return false;
                    }

                    if (!ElementsEqual(leftValue, property.Value))
                    {
                        return false;
                    }

                    matched++;
                }

                return matched == leftProperties.Count;
            }

            case JsonValueKind.Array:
            {
                List<JsonElement> leftItems = left.EnumerateArray().ToList();
                List<JsonElement> rightItems = right.EnumerateArray().ToList();

                if (leftItems.Count != rightItems.Count)
                {
                    return false;
                }

                for (int i = 0; i < leftItems.Count; i++)
                {
                    if (!ElementsEqual(leftItems[i], rightItems[i]))
                    {
                        return false;
                    }
                }

                return true;
            }

            case JsonValueKind.String:
                return string.Equals(left.GetString(), right.GetString(), StringComparison.Ordinal);

            case JsonValueKind.Number:
                return string.Equals(left.GetRawText(), right.GetRawText(), StringComparison.Ordinal);

            case JsonValueKind.True:
            case JsonValueKind.False:
                return left.GetBoolean() == right.GetBoolean();

            case JsonValueKind.Null:
                return true;

            default:
                return string.Equals(left.GetRawText(), right.GetRawText(), StringComparison.Ordinal);
        }
    }

    private static int CountSharedPrefix(IReadOnlyList<JsonElement> original, IReadOnlyList<JsonElement> modified)
    {
        int comparisonLimit = Math.Min(original.Count, modified.Count);
        int prefixLength = 0;

        while (prefixLength < comparisonLimit && ElementsEqual(original[prefixLength], modified[prefixLength]))
        {
            prefixLength++;
        }

        return prefixLength;
    }

    private static int CountSharedSuffix(IReadOnlyList<JsonElement> original, IReadOnlyList<JsonElement> modified, int prefixLength)
    {
        int comparisonLimit = Math.Min(original.Count, modified.Count);
        if (prefixLength >= comparisonLimit)
        {
            return 0;
        }

        int suffixLength = 0;
        int originalIndex = original.Count - 1;
        int modifiedIndex = modified.Count - 1;

        while (suffixLength < comparisonLimit - prefixLength &&
               ElementsEqual(original[originalIndex - suffixLength], modified[modifiedIndex - suffixLength]))
        {
            suffixLength++;
        }

        return suffixLength;
    }

    private static bool TryEmitReorderOperations(List<JsonElement> original, List<JsonElement> modified, string path, List<JsonObject> operations)
    {
        if (original.Count == 0 || original.Count > MaxReorderAnalysisLength)
        {
            return false;
        }

        var workingElements = new List<JsonElement>(original);
        var workingFingerprints = new List<NodeFingerprint>(original.Count);
        foreach (JsonElement element in original)
        {
            workingFingerprints.Add(new NodeFingerprint(element));
        }

        var moveList = new List<(int From, int To)>();

        for (int targetIndex = 0; targetIndex < modified.Count; targetIndex++)
        {
            NodeFingerprint desiredFingerprint = new(modified[targetIndex]);

            if (targetIndex < workingElements.Count &&
                workingFingerprints[targetIndex].Equals(desiredFingerprint) &&
                ElementsEqual(workingElements[targetIndex], modified[targetIndex]))
            {
                continue;
            }

            int searchStart = Math.Min(targetIndex + 1, Math.Max(workingElements.Count - 1, 0));
            int currentIndex = FindMatchingIndex(workingElements, workingFingerprints, modified[targetIndex], desiredFingerprint, searchStart);

            if (currentIndex < 0)
            {
                return false;
            }

            JsonElement elementToMove = workingElements[currentIndex];
            workingElements.RemoveAt(currentIndex);
            workingFingerprints.RemoveAt(currentIndex);

            workingElements.Insert(targetIndex, elementToMove);
            workingFingerprints.Insert(targetIndex, desiredFingerprint);
            moveList.Add((currentIndex, targetIndex));
        }

        if (moveList.Count == 0)
        {
            return false;
        }

        var moveOperations = new List<JsonObject>(moveList.Count);
        foreach ((int from, int to) in moveList)
        {
            moveOperations.Add(CreateMoveOperation(AppendIndex(path, to), AppendIndex(path, from)));
        }

        JsonObject listDiffOperation = CreateListDiffOperation(path, moveList);
        int aggregateMoveSize = CalculateOperationsSize(moveOperations);
        int listDiffSize = CalculateOperationSize(listDiffOperation);

        if (listDiffSize < aggregateMoveSize)
        {
            operations.Add(listDiffOperation);
        }
        else
        {
            operations.AddRange(moveOperations);
        }

        return true;
    }

    private static int FindMatchingIndex(List<JsonElement> workingElements, List<NodeFingerprint> fingerprints, JsonElement target, NodeFingerprint targetFingerprint, int startIndex)
    {
        if (workingElements.Count == 0)
        {
            return -1;
        }

        int index = Math.Min(startIndex, workingElements.Count - 1);
        for (int i = index; i < workingElements.Count; i++)
        {
            if (!fingerprints[i].Equals(targetFingerprint))
            {
                continue;
            }

            if (ElementsEqual(workingElements[i], target))
            {
                return i;
            }
        }

        for (int i = 0; i < index; i++)
        {
            if (!fingerprints[i].Equals(targetFingerprint))
            {
                continue;
            }

            if (ElementsEqual(workingElements[i], target))
            {
                return i;
            }
        }

        return -1;
    }

    private readonly struct NodeFingerprint : IEquatable<NodeFingerprint>
    {
        private readonly JsonValueKind _kind;
        private readonly string? _text;
        private readonly bool? _boolean;

        public NodeFingerprint(JsonElement element)
        {
            _kind = element.ValueKind;
            switch (_kind)
            {
                case JsonValueKind.String:
                    _text = element.GetString();
                    _boolean = null;
                    break;
                case JsonValueKind.Number:
                    _text = element.GetRawText();
                    _boolean = null;
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    _boolean = element.GetBoolean();
                    _text = null;
                    break;
                case JsonValueKind.Null:
                    _text = null;
                    _boolean = null;
                    break;
                default:
                    _text = element.GetRawText();
                    _boolean = null;
                    break;
            }
        }

        public bool Equals(NodeFingerprint other)
        {
            if (_kind != other._kind)
            {
                return false;
            }

            return _kind switch
            {
                JsonValueKind.String or JsonValueKind.Number => string.Equals(_text, other._text, StringComparison.Ordinal),
                JsonValueKind.True or JsonValueKind.False => Nullable.Equals(_boolean, other._boolean),
                JsonValueKind.Null => true,
                _ => string.Equals(_text, other._text, StringComparison.Ordinal)
            };
        }

        public override bool Equals(object? obj) => obj is NodeFingerprint other && Equals(other);

        public override int GetHashCode()
        {
            return _kind switch
            {
                JsonValueKind.String or JsonValueKind.Number => HashCode.Combine(_kind, _text),
                JsonValueKind.True or JsonValueKind.False => HashCode.Combine(_kind, _boolean),
                JsonValueKind.Null => _kind.GetHashCode(),
                _ => HashCode.Combine(_kind, _text)
            };
        }
    }

    private static JsonNode ElementToNode(JsonElement element)
    {
        string rawText = element.GetRawText();
        JsonNode? node = JsonNode.Parse(rawText);
        return node ?? JsonValue.Create((object?)null)!;
    }

    private static int CalculateOperationsSize(IEnumerable<JsonObject> operations)
    {
        int total = 0;
        foreach (JsonObject operation in operations)
        {
            total += CalculateOperationSize(operation);
        }

        return total;
    }

    private static void AppendTextDiffOperations(string path, string original, string modified, ICollection<JsonObject> operations)
    {
        int prefixLength = CountSharedStringPrefix(original, modified);
        int suffixLength = CountSharedStringSuffix(original, modified, prefixLength);

        int deleteLength = original.Length - prefixLength - suffixLength;
        int insertLength = modified.Length - prefixLength - suffixLength;
        string insertText = insertLength > 0 ? modified.Substring(prefixLength, insertLength) : string.Empty;

        JsonObject textDiffOperation = CreateTextDiffOperation(path, prefixLength, deleteLength, insertText);
        JsonObject replaceOperation = CreateReplaceOperation(path, JsonValue.Create(modified)!);

        if (CalculateOperationSize(textDiffOperation) < CalculateOperationSize(replaceOperation))
        {
            operations.Add(textDiffOperation);
        }
        else
        {
            operations.Add(replaceOperation);
        }
    }

    private static JsonNode ParseJsonNode(string json, string parameterName) =>
        JsonNode.Parse(json) ?? throw new ArgumentException("Invalid JSON document.", parameterName);

    private static JsonArray ParsePatchArray(string jsonPatch)
    {
        JsonNode patchNode = ParseJsonNode(jsonPatch, nameof(jsonPatch));

        if (patchNode is JsonArray patchArray)
        {
            return patchArray;
        }

        throw new ArgumentException("Patch document must be a JSON array.", nameof(jsonPatch));
    }

    private static IEnumerable<PatchOperation> ToPatchOperations(JsonArray operations)
    {
        foreach (JsonNode? node in operations)
        {
            if (node is not JsonObject obj)
            {
                throw new InvalidOperationException("Patch operation must be a JSON object.");
            }

            string opCode = ReadRequiredString(obj, OpPropertyName);
            string path = ReadRequiredString(obj, PathPropertyName);
            obj.TryGetPropertyValue(ValuePropertyName, out JsonNode? valueNode);
            string? fromPath = ReadOptionalString(obj, FromPropertyName);

            yield return new PatchOperation(ParseOperationKind(opCode), path, valueNode, fromPath);
        }
    }

    private static string ReadRequiredString(JsonObject jsonObject, string propertyName)
    {
        if (!jsonObject.TryGetPropertyValue(propertyName, out JsonNode? valueNode) || valueNode is null)
        {
            throw new InvalidOperationException($"Patch operation missing '{propertyName}'.");
        }

        return valueNode.GetValue<string>();
    }

    private static string? ReadOptionalString(JsonObject jsonObject, string propertyName)
    {
        if (!jsonObject.TryGetPropertyValue(propertyName, out JsonNode? valueNode) || valueNode is null)
        {
            return null;
        }

        return valueNode.GetValue<string>();
    }

    private static int ReadRequiredInt(JsonObject jsonObject, string propertyName)
    {
        if (!jsonObject.TryGetPropertyValue(propertyName, out JsonNode? valueNode) || valueNode is null)
        {
            throw new InvalidOperationException($"Patch operation missing '{propertyName}'.");
        }

        return valueNode.GetValue<int>();
    }

    private static PatchOperationKind ParseOperationKind(string opCode) =>
        opCode switch
        {
            ReplaceOperationCode => PatchOperationKind.Replace,
            AddOperationCode => PatchOperationKind.Add,
            RemoveOperationCode => PatchOperationKind.Remove,
            TextDiffOperationCode => PatchOperationKind.TextDiff,
            MoveOperationCode => PatchOperationKind.Move,
            CopyOperationCode => PatchOperationKind.Copy,
            ListDiffOperationCode => PatchOperationKind.ListDiff,
            _ => throw new InvalidOperationException($"Unsupported operation '{opCode}'.")
        };

    private static void ApplyOperation(ref JsonNode root, PatchOperation operation)
    {
        switch (operation.Kind)
        {
            case PatchOperationKind.Replace:
                ApplyReplace(ref root, operation.Path, operation.Value);
                break;
            case PatchOperationKind.Add:
                ApplyAdd(ref root, operation.Path, operation.Value);
                break;
            case PatchOperationKind.Remove:
                ApplyRemove(ref root, operation.Path);
                break;
            case PatchOperationKind.TextDiff:
                ApplyTextDiff(ref root, operation.Path, operation.Value);
                break;
            case PatchOperationKind.Move:
                ApplyMove(ref root, operation.Path, operation.FromPath);
                break;
            case PatchOperationKind.Copy:
                ApplyCopy(ref root, operation.Path, operation.FromPath);
                break;
            case PatchOperationKind.ListDiff:
                ApplyListDiff(ref root, operation.Path, operation.Value);
                break;
            default:
                throw new InvalidOperationException($"Unsupported operation '{operation.Kind}'.");
        }
    }

    private static void ApplyReplace(ref JsonNode root, string path, JsonNode? valueNode)
    {
        if (valueNode is null)
        {
            throw new InvalidOperationException("Replace operation requires a value.");
        }

        if (IsRootPath(path))
        {
            root = CloneNode(valueNode);
            return;
        }

        PathLocation location = ResolveParent(root, path);
        JsonNode clone = CloneNode(valueNode);

        switch (location)
        {
            case { Parent: JsonObject obj, PropertyName: { } property }:
                obj[property] = clone;
                return;
            case { Parent: JsonArray array, ArrayIndex: int index }:
                EnsureIndexInRange(index, array.Count - 1);
                array[index] = clone;
                return;
            default:
                throw new InvalidOperationException("Replace target not found.");
        }
    }

    private static void ApplyAdd(ref JsonNode root, string path, JsonNode? valueNode)
    {
        if (valueNode is null)
        {
            throw new InvalidOperationException("Add operation requires a value.");
        }

        JsonNode clone = CloneNode(valueNode);

        if (IsRootPath(path))
        {
            root = clone;
            return;
        }

        PathLocation location = ResolveParent(root, path);

        switch (location)
        {
            case { Parent: JsonArray array, ArrayIndex: int index }:
                if ((uint)index > array.Count)
                {
                    throw new InvalidOperationException("Add index out of range.");
                }

                array.Insert(index, clone);
                return;

            case { Parent: JsonObject obj, PropertyName: { } propertyName }:
                if (!obj.TryGetPropertyValue(propertyName, out JsonNode? targetNode) || targetNode is null)
                {
                    obj[propertyName] = clone;
                    return;
                }

                if (targetNode is JsonArray targetArray)
                {
                    targetArray.Add(clone);
                    return;
                }

                obj[propertyName] = clone;
                return;

            default:
                throw new InvalidOperationException("Add target not found.");
        }
    }

    private static void ApplyRemove(ref JsonNode root, string path)
    {
        if (IsRootPath(path))
        {
            throw new InvalidOperationException("Removing the root is not supported.");
        }

        PathLocation location = ResolveParent(root, path);

        switch (location)
        {
            case { Parent: JsonObject obj, PropertyName: { } property }:
                if (!obj.Remove(property))
                {
                    throw new InvalidOperationException("Remove target not found.");
                }

                return;

            case { Parent: JsonArray array, ArrayIndex: int index }:
                EnsureIndexInRange(index, array.Count - 1);
                array.RemoveAt(index);
                return;

            default:
                throw new InvalidOperationException("Remove target not found.");
        }
    }

    

    private static PathLocation ResolveParent(JsonNode root, string path)
    {
        if (string.IsNullOrEmpty(path) || path[0] != '/')
        {
            throw new InvalidOperationException("Path must start with '/'.");
        }

        var reader = new PathReader(path);
        JsonNode? current = root;
        JsonNode? parent = null;
        ReadOnlySpan<char> finalSegment = default;

        while (reader.TryRead(out ReadOnlySpan<char> segment))
        {
            bool isLast = reader.IsAtEnd;
            if (isLast)
            {
                parent = current ?? throw new InvalidOperationException("Parent not found for path.");
                finalSegment = segment;
                break;
            }

            current = NavigateToChild(current, segment);
        }

        if (parent is null)
        {
            throw new InvalidOperationException("Invalid path – cannot resolve parent.");
        }

        if (parent is JsonObject)
        {
            string propertyName = DecodeSegment(finalSegment);
            return new PathLocation(parent, propertyName, null);
        }

        if (parent is JsonArray)
        {
            if (!TryParseIndex(finalSegment, out int index))
            {
                throw new InvalidOperationException("Array index expected in path.");
            }

            return new PathLocation(parent, null, index);
        }

        throw new InvalidOperationException("Path resolves to a value node without parent container.");
    }

    private static JsonNode NavigateToChild(JsonNode? current, ReadOnlySpan<char> segment)
    {
        if (current is JsonObject obj)
        {
            string key = DecodeSegment(segment);
            if (!obj.TryGetPropertyValue(key, out JsonNode? child) || child is null)
            {
                throw new InvalidOperationException($"Property '{key}' not found.");
            }

            return child;
        }

        if (current is JsonArray array)
        {
            if (!TryParseIndex(segment, out int index))
            {
                throw new InvalidOperationException("Array index expected in path.");
            }

            EnsureIndexInRange(index, array.Count - 1);
            return array[index] ?? throw new InvalidOperationException("Array element is null.");
        }

        throw new InvalidOperationException("Cannot navigate through a primitive value.");
    }

    private static JsonNode CloneNode(JsonNode? node) =>
        node is null ? JsonValue.Create((object?)null)! : node.DeepClone();

    private static JsonObject CreateReplaceOperation(string path, JsonNode value) => new()
    {
        [OpPropertyName] = ReplaceOperationCode,
        [PathPropertyName] = path,
        [ValuePropertyName] = value
    };

    private static JsonObject CreateAddOperation(string path, JsonNode value) => new()
    {
        [OpPropertyName] = AddOperationCode,
        [PathPropertyName] = path,
        [ValuePropertyName] = value
    };

    private static JsonObject CreateRemoveOperation(string path) => new()
    {
        [OpPropertyName] = RemoveOperationCode,
        [PathPropertyName] = path
    };

    private static JsonObject CreateTextDiffOperation(string path, int start, int deleteLength, string insertText)
    {
        var payload = new JsonObject
        {
            [TextDiffStartPropertyName] = start,
            [TextDiffDeleteLengthPropertyName] = deleteLength,
            [TextDiffInsertTextPropertyName] = insertText
        };

        return new JsonObject
        {
            [OpPropertyName] = TextDiffOperationCode,
            [PathPropertyName] = path,
            [ValuePropertyName] = payload
        };
    }

    private static JsonObject CreateMoveOperation(string path, string fromPath) => new()
    {
        [OpPropertyName] = MoveOperationCode,
        [PathPropertyName] = path,
        [FromPropertyName] = fromPath
    };

    private static JsonObject CreateCopyOperation(string path, string fromPath) => new()
    {
        [OpPropertyName] = CopyOperationCode,
        [PathPropertyName] = path,
        [FromPropertyName] = fromPath
    };

    private static JsonObject CreateListDiffOperation(string path, IReadOnlyList<(int From, int To)> moves)
    {
        var movesArray = new JsonArray();
        foreach ((int from, int to) in moves)
        {
            movesArray.Add(new JsonObject
            {
                [FromPropertyName] = from,
                [ListDiffMoveToPropertyName] = to
            });
        }

        var payload = new JsonObject
        {
            [ListDiffMovesPropertyName] = movesArray
        };

        return new JsonObject
        {
            [OpPropertyName] = ListDiffOperationCode,
            [PathPropertyName] = path,
            [ValuePropertyName] = payload
        };
    }

    private static int CalculateOperationSize(JsonObject operation)
    {
        string serialized = operation.ToJsonString();
        return serialized.Length;
    }

    private static void ApplyTextDiff(ref JsonNode root, string path, JsonNode? valueNode)
    {
        TextDiffInstruction instruction = ParseTextDiffPayload(valueNode);

        if (IsRootPath(path))
        {
            string current = RequireStringValue(root);
            string updated = ApplyTextMutation(current, instruction);
            root = JsonValue.Create(updated)!;
            return;
        }

        PathLocation location = ResolveParent(root, path);

        switch (location)
        {
            case { Parent: JsonObject obj, PropertyName: { } property }:
                if (!obj.TryGetPropertyValue(property, out JsonNode? targetNode) || targetNode is null)
                {
                    throw new InvalidOperationException("Text diff target not found.");
                }

                string currentValue = RequireStringValue(targetNode);
                string updatedValue = ApplyTextMutation(currentValue, instruction);
                obj[property] = JsonValue.Create(updatedValue);
                return;

            case { Parent: JsonArray array, ArrayIndex: int index }:
                EnsureIndexInRange(index, array.Count - 1);
                JsonNode? elementNode = array[index];
                string elementValue = RequireStringValue(elementNode);
                string updatedElement = ApplyTextMutation(elementValue, instruction);
                array[index] = JsonValue.Create(updatedElement);
                return;

            default:
                throw new InvalidOperationException("Text diff target not found.");
        }
    }

    private static void ApplyMove(ref JsonNode root, string path, string? fromPath)
    {
        if (string.IsNullOrEmpty(fromPath))
        {
            throw new InvalidOperationException("Move operation requires an 'f' path.");
        }

        JsonNode nodeToMove = ExtractNode(ref root, fromPath);
        InsertNodeAtPath(ref root, path, nodeToMove, cloneValue: false);
    }

    private static void ApplyCopy(ref JsonNode root, string path, string? fromPath)
    {
        if (string.IsNullOrEmpty(fromPath))
        {
            throw new InvalidOperationException("Copy operation requires an 'f' path.");
        }

        JsonNode sourceNode = ResolveNode(root, fromPath);
        JsonNode nodeToInsert = CloneNode(sourceNode);
        InsertNodeAtPath(ref root, path, nodeToInsert, cloneValue: false);
    }

    private static void ApplyListDiff(ref JsonNode root, string path, JsonNode? valueNode)
    {
        if (valueNode is not JsonObject payload)
        {
            throw new InvalidOperationException("List diff operation requires an object payload.");
        }

        JsonNode target = ResolveNode(root, path);
        if (target is not JsonArray targetArray)
        {
            throw new InvalidOperationException("List diff target must be an array.");
        }

        if (!payload.TryGetPropertyValue(ListDiffMovesPropertyName, out JsonNode? movesNode) || movesNode is null)
        {
            throw new InvalidOperationException("List diff payload must include an 'm' array.");
        }

        if (movesNode is not JsonArray movesArray)
        {
            throw new InvalidOperationException("List diff 'm' must be an array.");
        }

        foreach (JsonNode? moveNode in movesArray)
        {
            if (moveNode is not JsonObject moveObject)
            {
                throw new InvalidOperationException("List diff move entries must be objects.");
            }

            int from = ReadRequiredInt(moveObject, FromPropertyName);
            int to = ReadRequiredInt(moveObject, ListDiffMoveToPropertyName);

            MoveWithinArray(targetArray, from, to);
        }
    }

    private static JsonNode ExtractNode(ref JsonNode root, string path)
    {
        if (IsRootPath(path))
        {
            throw new InvalidOperationException("Moving from the root is not supported.");
        }

        PathLocation location = ResolveParent(root, path);

        switch (location)
        {
            case { Parent: JsonObject obj, PropertyName: { } property }:
                if (!obj.TryGetPropertyValue(property, out JsonNode? value))
                {
                    throw new InvalidOperationException("Move source not found.");
                }

                obj.Remove(property);
                return value ?? JsonValue.Create((object?)null)!;

            case { Parent: JsonArray array, ArrayIndex: int index }:
                EnsureIndexInRange(index, array.Count - 1);
                JsonNode? element = array[index];
                array.RemoveAt(index);
                return element ?? JsonValue.Create((object?)null)!;

            default:
                throw new InvalidOperationException("Move source not found.");
        }
    }

    private static void InsertNodeAtPath(ref JsonNode root, string path, JsonNode value, bool cloneValue)
    {
        JsonNode nodeToInsert = cloneValue ? CloneNode(value) : value;

        if (IsRootPath(path))
        {
            root = nodeToInsert;
            return;
        }

        PathLocation location = ResolveParent(root, path);

        switch (location)
        {
            case { Parent: JsonArray array, ArrayIndex: int index }:
                if ((uint)index > array.Count)
                {
                    throw new InvalidOperationException("Target array index out of range.");
                }

                array.Insert(index, nodeToInsert);
                return;

            case { Parent: JsonObject obj, PropertyName: { } propertyName }:
                if (!obj.TryGetPropertyValue(propertyName, out JsonNode? targetNode) || targetNode is null)
                {
                    obj[propertyName] = nodeToInsert;
                    return;
                }

                if (targetNode is JsonArray targetArray)
                {
                    targetArray.Add(nodeToInsert);
                    return;
                }

                obj[propertyName] = nodeToInsert;
                return;

            default:
                throw new InvalidOperationException("Move target not found.");
        }
    }

    private static JsonNode ResolveNode(JsonNode root, string path)
    {
        if (IsRootPath(path))
        {
            return root;
        }

        var reader = new PathReader(path);
        JsonNode current = root;

        while (reader.TryRead(out ReadOnlySpan<char> segment))
        {
            if (segment.Length == 0 && reader.IsAtEnd && (path.Length == 0 || path[^1] != '/'))
            {
                break;
            }

            current = NavigateToChild(current, segment);
        }

        return current;
    }

    private static void MoveWithinArray(JsonArray array, int from, int to)
    {
        int count = array.Count;
        if (from < 0 || from >= count)
        {
            throw new InvalidOperationException("List diff move source index out of range.");
        }

        if (to < 0 || to > count)
        {
            throw new InvalidOperationException("List diff move target index out of range.");
        }

        if (from == to)
        {
            return;
        }

        JsonNode? element = array[from];
        array.RemoveAt(from);

        int targetIndex = to;
        if (from < to)
        {
            targetIndex--;
        }

        if (targetIndex < 0 || targetIndex > array.Count)
        {
            throw new InvalidOperationException("List diff move target index out of range.");
        }

        array.Insert(targetIndex, element);
    }

    private static TextDiffInstruction ParseTextDiffPayload(JsonNode? valueNode)
    {
        if (valueNode is not JsonObject payload)
        {
            throw new InvalidOperationException("Text diff operation requires an object payload.");
        }

        int start = ReadRequiredInt(payload, TextDiffStartPropertyName);
        int deleteLength = ReadRequiredInt(payload, TextDiffDeleteLengthPropertyName);
        string insertText = ReadRequiredString(payload, TextDiffInsertTextPropertyName);

        if (start < 0)
        {
            throw new InvalidOperationException("Text diff 's' must be non-negative.");
        }

        if (deleteLength < 0)
        {
            throw new InvalidOperationException("Text diff 'dl' must be non-negative.");
        }

        return new TextDiffInstruction(start, deleteLength, insertText);
    }

    private static string ApplyTextMutation(string original, TextDiffInstruction instruction)
    {
        if (instruction.Start > original.Length)
        {
            throw new InvalidOperationException("Text diff start index out of range.");
        }

        if (instruction.DeleteLength > original.Length - instruction.Start)
        {
            throw new InvalidOperationException("Text diff delete length out of range.");
        }

        ReadOnlySpan<char> prefix = original.AsSpan(0, instruction.Start);
        ReadOnlySpan<char> suffix = original.AsSpan(instruction.Start + instruction.DeleteLength);

        return string.Concat(prefix, instruction.InsertText, suffix);
    }

    private static string RequireStringValue(JsonNode? node)
    {
        if (TryGetStringValue(node, out string value))
        {
            return value;
        }

        throw new InvalidOperationException("Text diff target must be a string value.");
    }

    private static bool TryGetStringValue(JsonNode? node, out string value)
    {
        if (node is JsonValue jsonValue && jsonValue.TryGetValue(out string? stringValue) && stringValue is not null)
        {
            value = stringValue;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static int CountSharedStringPrefix(string original, string modified)
    {
        int limit = Math.Min(original.Length, modified.Length);
        int index = 0;

        while (index < limit && original[index] == modified[index])
        {
            index++;
        }

        return index;
    }

    private static int CountSharedStringSuffix(string original, string modified, int prefixLength)
    {
        int originalRemaining = original.Length - prefixLength;
        int modifiedRemaining = modified.Length - prefixLength;
        int limit = Math.Min(originalRemaining, modifiedRemaining);

        int suffix = 0;
        while (suffix < limit && original[original.Length - 1 - suffix] == modified[modified.Length - 1 - suffix])
        {
            suffix++;
        }

        return suffix;
    }

    private readonly struct TextDiffInstruction
    {
        public TextDiffInstruction(int start, int deleteLength, string insertText)
        {
            Start = start;
            DeleteLength = deleteLength;
            InsertText = insertText;
        }

        public int Start { get; }
        public int DeleteLength { get; }
        public string InsertText { get; }
    }

    private static void EnsureIndexInRange(int index, int maxIndex)
    {
        if (index < 0 || index > maxIndex)
        {
            throw new InvalidOperationException("Array index out of range.");
        }
    }

    private static string AppendPath(string basePath, string segment)
    {
        string escaped = EscapeSegment(segment);
        return string.IsNullOrEmpty(basePath)
            ? "/" + escaped
            : $"{basePath}/{escaped}";
    }

    private static string AppendIndex(string basePath, int index)
    {
        string indexSegment = index.ToString(CultureInfo.InvariantCulture);
        return AppendPath(basePath, indexSegment);
    }

    private static string EscapeSegment(string segment)
    {
        if (segment.Length == 0)
        {
            return segment;
        }

        bool needsEscaping = false;
        for (int i = 0; i < segment.Length; i++)
        {
            if (segment[i] is '~' or '/')
            {
                needsEscaping = true;
                break;
            }
        }

        if (!needsEscaping)
        {
            return segment;
        }

        Span<char> buffer = segment.Length <= 64 ? stackalloc char[segment.Length * 2] : new char[segment.Length * 2];
        int outputIndex = 0;
        foreach (char c in segment)
        {
            switch (c)
            {
                case '~':
                    buffer[outputIndex++] = '~';
                    buffer[outputIndex++] = '0';
                    break;
                case '/':
                    buffer[outputIndex++] = '~';
                    buffer[outputIndex++] = '1';
                    break;
                default:
                    buffer[outputIndex++] = c;
                    break;
            }
        }

        return new string(buffer[..outputIndex]);
    }

    private static string DecodeSegment(ReadOnlySpan<char> span)
    {
        if (span.Length == 0)
        {
            return string.Empty;
        }

        int tildeIndex = span.IndexOf('~');
        if (tildeIndex < 0)
        {
            return span.ToString();
        }

        Span<char> buffer = span.Length <= 64 ? stackalloc char[span.Length] : new char[span.Length];
        int outputIndex = 0;
        for (int i = 0; i < span.Length; i++)
        {
            char c = span[i];
            if (c == '~')
            {
                if (i + 1 >= span.Length)
                {
                    throw new InvalidOperationException("Invalid escape sequence in path.");
                }

                char escape = span[++i];
                buffer[outputIndex++] = escape switch
                {
                    '0' => '~',
                    '1' => '/',
                    _ => throw new InvalidOperationException("Invalid escape sequence in path.")
                };
            }
            else
            {
                buffer[outputIndex++] = c;
            }
        }

        return new string(buffer[..outputIndex]);
    }

    private static bool TryParseIndex(ReadOnlySpan<char> span, out int index)
    {
        if (span.Length == 0)
        {
            index = default;
            return false;
        }

        return int.TryParse(span, NumberStyles.None, CultureInfo.InvariantCulture, out index);
    }

    private static bool IsRootPath(string path) => string.IsNullOrEmpty(path) || path == "/";
}
