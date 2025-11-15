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

| Member | Attribute | Type | Description |
| ------ | --------- | ---- | ----------- |
| `op` | Operation | string | Must be "rp" to indicate a replace operation. |
| `p` | Path | string | JSON Pointer path to the value to replace. |
| `v` | Value | any | Replacement value serialized as JSON. |

```json
{
  "op": "rp",
  "p": "/employee/title",
  "v": "Lead Engineer"
}
```

### Add

| Member | Attribute | Type | Description |
| ------ | --------- | ---- | ----------- |
| `op` | Operation | string | Must be "a" to indicate an add operation. |
| `p` | Path | string | JSON Pointer path where the value should be added. |
| `v` | Value | any | Value to insert. Arrays append when `p` refers to the array itself. |

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

| Member | Attribute | Type | Description |
| ------ | --------- | ---- | ----------- |
| `op` | Operation | string | Must be "rm" to indicate a remove operation. |
| `p` | Path | string | JSON Pointer path to the value that will be removed. Root paths are not allowed. |

```json
{
  "op": "rm",
  "p": "/employees/0"
}
```

### Text Diff

| Member | Attribute | Type | Description |
| ------ | --------- | ---- | ----------- |
| `op` | Operation | string | Must be "td" to indicate a text diff. |
| `p` | Path | string | JSON Pointer path to the string that will be edited. |
| `v.s` | Value start | number | Start index (zero-based characters) where the change begins. |
| `v.dl` | Value delete length | number | Number of characters to delete. |
| `v.it` | Value insert text | string | Text to insert at the specified location. |

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

| Member | Attribute | Type | Description |
| ------ | --------- | ---- | ----------- |
| `op` | Operation | string | Must be "mv" to indicate a move. |
| `p` | Path | string | Destination JSON Pointer path. |
| `f` | From | string | Source JSON Pointer path. |

```json
{
  "op": "mv",
  "p": "/items/0",
  "f": "/items/3"
}
```

Move transfers the value located at pointer `f` to pointer `p` without cloning. The source location is removed as part of the move. Root moves are not supported.

## Copy

| Member | Attribute | Type | Description |
| ------ | --------- | ---- | ----------- |
| `op` | Operation | string | Must be "cp" to indicate a copy. |
| `p` | Path | string | Destination JSON Pointer path. |
| `f` | From | string | Source JSON Pointer path to clone. |

```json
{
  "op": "cp",
  "p": "/items/-",
  "f": "/items/0"
}
```

Copy clones the value located at pointer `f` and inserts the clone at pointer `p`. The source value remains intact.

## List Diff

| Member | Attribute | Type | Description |
| ------ | --------- | ---- | ----------- |
| `op` | Operation | string | Must be "ld" to indicate a list diff. |
| `p` | Path | string | JSON Pointer path to the target array. |
| `v.m` | Value moves | array | Collection of move objects, each with `f` (from index) and `t` (to index). |

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


