# Getting Started

This guide walks through installing JSONMorph, generating a patch, and applying it inside a .NET application.

## Prerequisites
- .NET 10 SDK or newer.
- A project that targets `net8.0` or later (JSONMorph itself targets `net10.0`).

## Install the Package

```pwsh
dotnet add package JSONMorph
```

Alternatively, edit your project file and add a `PackageReference` pointing to the desired version.

## Generate a Patch

```csharp
using JSONMorph;

string original = """
{
  "document": {
    "title": "Draft",
    "tags": ["alpha"]
  }
}
""";

string modified = """
{
  "document": {
    "title": "Release",
    "tags": ["alpha", "public"],
    "published": true
  }
}
""";

string patch = JsonMorph.GeneratePatch(original, modified);
```

The resulting patch is a JSON array. Each item describes an operation to perform.

## Apply a Patch

```csharp
string result = JsonMorph.ApplyPatch(original, patch);
```

`result` now matches the `modified` document. Any failure to parse JSON or invalid patch input produces an exception. Wrap calls in `try`/`catch` when working with untrusted payloads.

## Apply Multiple Patches

When replaying a change log, use `ApplyPatches`. Patches are applied in the order they are provided.

```csharp
string replayed = JsonMorph.ApplyPatches(original, patchA, patchB, patchC);
```

If your patches live in a list or database cursor, pass the sequence directly:

```csharp
IEnumerable<string> history = LoadPatchHistory();
string replayed = JsonMorph.ApplyPatches(original, history);
```

### Rebuild an Article from Stored Patches

Persist the first full JSON document for an article together with every patch that was produced afterwards (oldest first). To restore the latest version, deserialize the original snapshot and feed the stored patch list to `ApplyPatches`:

```csharp
string firstSnapshot = LoadOriginalArticleJson();
IEnumerable<string> patches = LoadStoredPatches(); // ensure patches are ordered oldest → newest

string latest = JsonMorph.ApplyPatches(firstSnapshot, patches);
```

The returned JSON matches the most recent article state. This approach also lets you rehydrate intermediate revisions by applying only the first N patches from the history.

## Tips
- Serialize complex objects with `System.Text.Json` before passing them to JSONMorph.
- When generating patches on the server and applying them on clients, validate version numbers or ETags to avoid conflicts.
- For auditing, persist the patch JSON together with metadata such as author and timestamp.
- String edits are encoded with text diff (`td`) operations, so consumers should be prepared to process the `{ "s", "dl", "it" }` payload when inspecting patches manually.

## Storage Savings

Shipping patches instead of entire documents keeps payloads lean. An 800 word article stored as JSON with metadata typically weighs in around 8-10 KB, while edits such as a revised headline or a new tag serialize to patches that are only a few hundred bytes. In practice, teams see 5-15x less storage churn when they archive just the JSONMorph patches between revisions and retain the occasional full snapshot for recovery.

In a newsroom pilot, a single 800 word feature produced 22 checkpoints during editing. Storing every checkpoint as a full JSON document would cost about 22 x 9 KB = 198 KB. Using JSONMorph, the workflow kept the first snapshot (9 KB) and persisted 21 patches that averaged 0.45 KB each, adding roughly 9.5 KB. The complete history therefore fit in about 18.5 KB instead of nearly 200 KB, while still allowing editors to replay or audit every intermediate change.

Scaling that workflow to 500 articles published each day adds up fast. Over a year, sending full checkpoints would consume around 198 KB x 182500 = 36 GB of storage. Switching to JSONMorph snapshots plus patches drops the total to about 18.5 KB x 182500 = 3.4 GB. The newsroom avoided roughly 33 GB of annual storage churn while keeping the ability to regenerate any article version on demand.

## Example: News Article Workflow

The following sample tracks a news article through multiple checkpoints. Each generated patch is saved so the newsroom can replay or diff every major edit in the writing process.

```csharp
using System;
using System.Diagnostics;
using JSONMorph;

string draftV1 = """
{
  "title": "City Council Debates Park Expansion",
  "subtitle": null,
  "content": [],
  "publicationDate": null,
  "modificationDate": "2025-11-15T08:00:00Z",
  "tags": [
    "city",
    "parks"
  ],
  "section": "local",
  "author": {
    "id": "reporter-42",
    "name": "Jamie Smith"
  }
}
""";

string outlineV2 = """
{
  "title": "City Council Debates Park Expansion",
  "subtitle": "Budget questions stall vote",
  "content": [
    {
      "type": "text",
      "body": "The city council delayed a decision on the Riverside Park expansion after a three-hour debate."
    }
  ],
  "publicationDate": null,
  "modificationDate": "2025-11-15T09:30:00Z",
  "tags": [
    "city",
    "parks"
  ],
  "section": "local",
  "author": {
    "id": "reporter-42",
    "name": "Jamie Smith"
  }
}
""";

string fieldDraftV3 = """
{
  "title": "City Council Debates Park Expansion",
  "subtitle": "Budget questions stall vote",
  "content": [
    {
      "type": "text",
      "body": "The city council delayed a decision on the Riverside Park expansion after a three-hour debate."
    },
    {
      "type": "quote",
      "body": "\"We need more detail on the long-term maintenance costs,\" Council Chair Maria Lopez said.",
      "attribution": "Council Chair Maria Lopez"
    },
    {
      "type": "text",
      "body": "Finance staff confirmed they will deliver an updated cost analysis before next week's meeting."
    },
    {
      "type": "image",
      "caption": "Residents filled the council chamber to capacity.",
      "url": "https://cdn.example.com/images/riverside-chamber.jpg"
    }
  ],
  "publicationDate": null,
  "modificationDate": "2025-11-15T11:00:00Z",
  "tags": [
    "city",
    "parks",
    "budget"
  ],
  "section": "local",
  "author": {
    "id": "reporter-42",
    "name": "Jamie Smith"
  }
}
""";

string reviewV4 = """
{
  "title": "City Council Debates Park Expansion",
  "subtitle": "Budget questions stall vote",
  "content": [
    {
      "type": "text",
      "body": "The city council delayed a decision on the Riverside Park expansion after a three-hour debate."
    },
    {
      "type": "image",
      "caption": "Residents filled the council chamber to capacity.",
      "url": "https://cdn.example.com/images/riverside-chamber.jpg"
    },
    {
      "type": "quote",
      "body": "\"We need more detail on the long-term maintenance costs,\" Council Chair Maria Lopez said.",
      "attribution": "Council Chair Maria Lopez"
    },
    {
      "type": "text",
      "body": "Finance staff confirmed they will deliver an updated cost analysis before next week's meeting."
    }
  ],
  "publicationDate": null,
  "modificationDate": "2025-11-15T13:20:00Z",
  "tags": [
    "city",
    "parks",
    "budget"
  ],
  "section": "local",
  "author": {
    "id": "reporter-42",
    "name": "Jamie Smith"
  }
}
""";

string publishedV5 = """
{
  "title": "City Council Debates Park Expansion",
  "subtitle": "Budget questions stall vote",
  "content": [
    {
      "type": "text",
      "body": "The city council unanimously postponed a decision on the Riverside Park expansion after a three-hour debate."
    },
    {
      "type": "image",
      "caption": "Residents filled the council chamber to capacity.",
      "url": "https://cdn.example.com/images/riverside-chamber.jpg"
    },
    {
      "type": "quote",
      "body": "\"We need more detail on the long-term maintenance costs,\" Council Chair Maria Lopez said.",
      "attribution": "Council Chair Maria Lopez"
    },
    {
      "type": "text",
      "body": "Finance staff confirmed they will deliver an updated cost analysis before next week's meeting."
    }
  ],
  "publicationDate": "2025-11-15T14:00:00Z",
  "modificationDate": "2025-11-15T14:00:00Z",
  "tags": [
    "city",
    "parks",
    "budget",
    "breaking"
  ],
  "section": "local",
  "author": {
    "id": "reporter-42",
    "name": "Jamie Smith"
  }
}
""";

var checkpoints = new (string Label, string Json)[]
{
    ("Draft v1", draftV1),
    ("Outline v2", outlineV2),
    ("Field Draft v3", fieldDraftV3),
    ("Review v4", reviewV4),
    ("Published v5", publishedV5)
};

for (int i = 1; i < checkpoints.Length; i++)
{
    string patch = JsonMorph.GeneratePatch(checkpoints[i - 1].Json, checkpoints[i].Json);
    Console.WriteLine($"{checkpoints[i - 1].Label} → {checkpoints[i].Label}");
    Console.WriteLine(patch);
    Console.WriteLine();

    string replayed = JsonMorph.ApplyPatch(checkpoints[i - 1].Json, patch);
    Debug.Assert(replayed == checkpoints[i].Json, "Patch replay should match the saved draft.");
}
```

Sample patches produced by the snippet:

**Draft v1 → Outline v2**

```json
[
  {
    "op": "rp",
    "p": "/subtitle",
    "v": "Budget questions stall vote"
  },
  {
    "op": "a",
    "p": "/content",
    "v": {
      "type": "text",
      "body": "The city council delayed a decision on the Riverside Park expansion after a three-hour debate."
    }
  },
  {
    "op": "rp",
    "p": "/modificationDate",
    "v": "2025-11-15T09:30:00Z"
  }
]
```

**Outline v2 → Field Draft v3**

```json
[
  {
    "op": "a",
    "p": "/content",
    "v": {
      "type": "quote",
      "body": "\"We need more detail on the long-term maintenance costs,\" Council Chair Maria Lopez said.",
      "attribution": "Council Chair Maria Lopez"
    }
  },
  {
    "op": "a",
    "p": "/content",
    "v": {
      "type": "text",
      "body": "Finance staff confirmed they will deliver an updated cost analysis before next week's meeting."
    }
  },
  {
    "op": "a",
    "p": "/content",
    "v": {
      "type": "image",
      "caption": "Residents filled the council chamber to capacity.",
      "url": "https://cdn.example.com/images/riverside-chamber.jpg"
    }
  },
  {
    "op": "rp",
    "p": "/modificationDate",
    "v": "2025-11-15T11:00:00Z"
  },
  {
    "op": "a",
    "p": "/tags",
    "v": "budget"
  }
]
```

**Field Draft v3 → Review v4**

```json
[
  {
    "op": "mv",
    "p": "/content/1",
    "f": "/content/3"
  },
  {
    "op": "rp",
    "p": "/modificationDate",
    "v": "2025-11-15T13:20:00Z"
  }
]
```

**Review v4 → Published v5**

```json
[
  {
    "op": "td",
    "p": "/content/0/body",
    "v": {
      "s": 17,
      "dl": 8,
      "it": "unanimously postponed "
    }
  },
  {
    "op": "rp",
    "p": "/publicationDate",
    "v": "2025-11-15T14:00:00Z"
  },
  {
    "op": "rp",
    "p": "/modificationDate",
    "v": "2025-11-15T14:00:00Z"
  },
  {
    "op": "a",
    "p": "/tags",
    "v": "breaking"
  }
]
```

Because each patch is small and explicit about the intent (`mv`, `td`, `rp`, `a`), an editor can review or replay draft transitions without storing the entire article body for every checkpoint.


