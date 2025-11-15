# Patch Format

JSONMorph emits a compact JSON representation that mirrors the spirit of RFC 6902 JSON Patch but with shorter operation codes. This document explains the schema.

## Operation Codes

| Code | Name     | Meaning |
| ---- | -------- | ------- |
| `rp` | replace  | Replace the item located at pointer `p` with value `v`. Requires the `v` member. |
| `td` | textDiff | Apply an in-place modification to a string value. Payload must include `s`, `dl`, and `it`. |
| `a`  | add      | Insert or set value `v` at pointer `p`. When `p` targets an array index equal to the array length, the value is appended. |
| `rm` | remove   | Remove the item located at pointer `p`. Cannot include `v`. |
| `mv` | move     | Move the value located at pointer `f` to pointer `p`. Requires the `f` member. |
| `cp` | copy     | Copy the value located at pointer `f` and insert the clone at pointer `p`. |
| `ld` | listDiff | Apply one or more in-array moves described by the `m` payload, containing compact move entries `f` and `t`. |

Unknown operation codes cause `InvalidOperationException` during application.

## Path Syntax

Paths use the JSON Pointer standard:
- Must start with `/` for non-root paths.
- Object property names appear as segments. Special characters are escaped using `~0` for tilde (`~`) and `~1` for slash (`/`).
- Array indices are zero-based decimal numbers.

Examples:
- `/name` targets the `name` property.
- `/employees/1/name` targets the `name` property of the second element in the `employees` array.
- `/complex~1name/~0value` resolves to `complex/name` property and then the `~value` property under it.

The empty path (`""`) or root (`"/"`) refers to the root document.

## Operation Shapes

### Replace

```json
{
  "op": "rp",
  "p": "/employee/title",
  "v": "Lead Engineer"
}
```

### Add

```json
{
  "op": "a",
  "p": "/employees/2",
  "v": {
    "name": "Quinn",
    "department": "Engineering"
  }
}
```

If the path points to an existing property containing an array, the value is appended to that array.

### Remove

```json
{
  "op": "rm",
  "p": "/employees/0"
}
```

### Text Diff

```json
{
  "op": "td",
  "p": "/name",
  "v": {
    "s": 2,
    "dl": 1,
    "it": "n"
  }
}
```

Text diff operations operate only on string values. `s` is the zero-based character index, `dl` is the number of characters to remove, and `it` is the replacement segment (which can be empty for deletions). If the indices are invalid or the target value is not a string, the patch application fails.

## Validation Rules

JSONMorph performs strict validation:
- Every operation must be a JSON object.
- The `op` and `p` members must exist and have a string value.
- `add` and `replace` operations demand a `v` member.
- `remove` operations reject the root path.
- Array indices must fall within bounds.
- `mv` and `cp` operations require a valid `f` pointer that resolves to a value.
- `ld` payloads must be objects with an `m` array containing objects with `f` and `t` indices.

Violations throw `InvalidOperationException` while parsing or applying operations.

## Move

```json
{
  "op": "mv",
  "p": "/items/0",
  "f": "/items/3"
}
```

Move transfers the value located at pointer `f` to pointer `p` without cloning. The source location is removed as part of the move. Root moves are not supported.

## Copy

```json
{
  "op": "cp",
  "p": "/items/-",
  "f": "/items/0"
}
```

Copy clones the value located at pointer `f` and inserts the clone at pointer `p`. The source value remains intact.

## List Diff

```json
{
  "op": "ld",
  "p": "/values",
  "v": {
    "m": [
      { "f": 2, "t": 0 },
      { "f": 3, "t": 1 }
    ]
  }
}
```

List diff operations target arrays and apply a sequence of moves within a single payload. Each move removes the element currently at index `f` and reinserts it so it will appear at index `t` after the move is processed. `GeneratePatch` compares the serialized size of emitting individual `mv` operations versus an aggregated `ld` and chooses whichever is smaller.


