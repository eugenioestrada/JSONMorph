using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using JSONMorph;

namespace UnitTests;

[TestClass]
public sealed class GenerateTests
{
    [TestMethod]
    public void GenerateReplacePatchTest()
    {
        string originalJson = """
        {
          "name": "John",
          "age": 30
        }
        """;
        string modifiedJson = """
        {
          "name": "Jane",
          "age": 30
        }
        """;
        string expectedPatch = """
        [
          {
            "op": "rp",
            "p": "/name",
            "v": "Jane"
          }
        ]
        """;

        string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

        Assert.AreEqual(expectedPatch, resultPatch);
    }

    [TestMethod]
    public void GenerateAppendPatchTest()
    {
        string originalJson = """
        {
          "fruits": [
            "apple",
            "banana"
          ]
        }
        """;
        string modifiedJson = """
        {
          "fruits": [
            "apple",
            "banana",
            "cherry"
          ]
        }
        """;
        string expectedPatch = """
        [
          {
            "op": "a",
            "p": "/fruits",
            "v": "cherry"
          }
        ]
        """;

        string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

        Assert.AreEqual(expectedPatch, resultPatch);
    }

    [TestMethod]
    public void GenerateRemovePatchTest()
    {
        string originalJson = """
        {
          "name": "John",
          "age": 30
        }
        """;
        string modifiedJson = """
        {
          "name": "John"
        }
        """;
        string expectedPatch = """
        [
          {
            "op": "rm",
            "p": "/age"
          }
        ]
        """;

        string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

        Assert.AreEqual(expectedPatch, resultPatch);
    }

    [TestMethod]
    public void GenerateMultiplePatchTest()
    {
        string originalJson = """
        {
          "name": "John",
          "age": 30,
          "fruits": [
            "apple",
            "banana"
          ]
        }
        """;
        string modifiedJson = """
        {
          "name": "Jane",
          "fruits": [
            "apple",
            "banana",
            "cherry"
          ]
        }
        """;
        string expectedPatch = """
        [
          {
            "op": "rp",
            "p": "/name",
            "v": "Jane"
          },
          {
            "op": "a",
            "p": "/fruits",
            "v": "cherry"
          },
          {
            "op": "rm",
            "p": "/age"
          }
        ]
        """;

        string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

        Assert.AreEqual(expectedPatch, resultPatch);
    }

    [TestMethod]
    public void GenerateRemoveIndexOfArrayPatchTest()
    {
        string originalJson = """
        {
          "fruits": [
            "apple",
            "banana",
            "cherry"
          ]
        }
        """;
        string modifiedJson = """
        {
          "fruits": [
            "apple",
            "cherry"
          ]
        }
        """;
        string expectedPatch = """
        [
          {
            "op": "rm",
            "p": "/fruits/1"
          }
        ]
        """;

        string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

        Assert.AreEqual(expectedPatch, resultPatch);
    }

    [TestMethod]
    public void GenerateAppendToIndexPatchTest()
    {
        string originalJson = """
        {
          "fruits": [
            "apple",
            "banana"
          ]
        }
        """;
        string modifiedJson = """
        {
          "fruits": [
            "apple",
            "cherry",
            "banana"
          ]
        }
        """;
        string expectedPatch = """
        [
          {
            "op": "a",
            "p": "/fruits/1",
            "v": "cherry"
          }
        ]
        """;

        string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

        Assert.AreEqual(expectedPatch, resultPatch);
    }

    [TestMethod]
    public void GenerateAppendComplexObjectPatchTest()
    {
        string originalJson = """
        {
          "employees": []
        }
        """;
        string modifiedJson = """
        {
          "employees": [
            {
              "name": "Alice",
              "age": 28,
              "department": "HR"
            }
          ]
        }
        """;
        string expectedPatch = """
        [
          {
            "op": "a",
            "p": "/employees",
            "v": {
              "name": "Alice",
              "age": 28,
              "department": "HR"
            }
          }
        ]
        """;

        string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

        Assert.AreEqual(expectedPatch, resultPatch);
    }

    [TestMethod]
    public void GenerateReplaceComplexObjectPatchTest()
    {
        string originalJson = """
        {
          "employee": {
            "name": "Bob",
            "age": 35,
            "department": "IT"
          }
        }
        """;
        string modifiedJson = """
        {
          "employee": {
            "name": "Robert",
            "age": 36,
            "department": "IT"
          }
        }
        """;
        string expectedPatch = """
        [
          {
            "op": "rp",
            "p": "/employee",
            "v": {
              "name": "Robert",
              "age": 36,
              "department": "IT"
            }
          }
        ]
        """;

        string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

        Assert.AreEqual(expectedPatch, resultPatch);
    }

    [TestMethod]
    public void GenerateReplaceInObjectInArrayPatchTest()
    {
        string originalJson = """
        {
          "employees": [
            {
              "name": "Charlie",
              "age": 40
            },
            {
              "name": "Diana",
              "age": 32
            }
          ]
        }
        """;
        string modifiedJson = """
        {
          "employees": [
            {
              "name": "Charlie",
              "age": 40
            },
            {
              "name": "Dina",
              "age": 32
            }
          ]
        }
        """;
        string expectedPatch = """
        [
          {
            "op": "rp",
            "p": "/employees/1/name",
            "v": "Dina"
          }
        ]
        """;

        string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

        Assert.AreEqual(expectedPatch, resultPatch);
    }

    [TestMethod]
    public void GenerateReplaceWithEscapedPathTest()
    {
        string originalJson = """
        {
          "complex/name": "old",
          "~section": {
            "value": 1
          }
        }
        """;
        string modifiedJson = """
        {
          "complex/name": "new",
          "~section": {
            "value": 1
          }
        }
        """;
        string expectedPatch = """
        [
          {
            "op": "rp",
            "p": "/complex~1name",
            "v": "new"
          }
        ]
        """;

        string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

        Assert.AreEqual(expectedPatch, resultPatch);
    }

    [TestMethod]
    public void GenerateAppendNestedArrayPatchTest()
    {
        string originalJson = """
        {
          "teams": [
            {
              "name": "Alpha",
              "members": [
                "Ann",
                "Abe"
              ]
            }
          ]
        }
        """;
        string modifiedJson = """
        {
          "teams": [
            {
              "name": "Alpha",
              "members": [
                "Ann",
                "Abe",
                "Amy"
              ]
            }
          ]
        }
        """;
        string expectedPatch = """
        [
          {
            "op": "a",
            "p": "/teams/0/members",
            "v": "Amy"
          }
        ]
        """;

        string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

        Assert.AreEqual(expectedPatch, resultPatch);
    }

    [TestMethod]
    public void GenerateRemoveDeepPropertyPatchTest()
    {
        string originalJson = """
        {
          "department": {
            "name": "Sales",
            "metrics": {
              "targets": {
                "q1": 100
              },
              "achieved": {
                "q1": 90
              }
            }
          }
        }
        """;
        string modifiedJson = """
        {
          "department": {
            "name": "Sales",
            "metrics": {
              "achieved": {
                "q1": 90
              }
            }
          }
        }
        """;
        string expectedPatch = """
        [
          {
            "op": "rm",
            "p": "/department/metrics/targets"
          }
        ]
        """;

        string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

        Assert.AreEqual(expectedPatch, resultPatch);
    }

    [TestMethod]
    public void GenerateAddNewPropertyPatchTest()
    {
        string originalJson = """
        {
          "employee": {
            "name": "Chris"
          }
        }
        """;
        string modifiedJson = """
        {
          "employee": {
            "name": "Chris",
            "role": "Manager"
          }
        }
        """;
        string expectedPatch = """
        [
          {
            "op": "a",
            "p": "/employee/role",
            "v": "Manager"
          }
        ]
        """;

        string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

        Assert.AreEqual(expectedPatch, resultPatch);
    }

    [TestMethod]
    public void GenerateRootReplacePrimitiveToObjectPatchTest()
    {
        string originalJson = "42";
        string modifiedJson = """
        {
          "value": 42
        }
        """;
        string expectedPatch = """
        [
          {
            "op": "rp",
            "p": "",
            "v": {
              "value": 42
            }
          }
        ]
        """;

        string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

        Assert.AreEqual(expectedPatch, resultPatch);
    }

    [TestMethod]
    public void GenerateArrayReorderPatchTest()
    {
        string originalJson = """
        {
          "fruits": [
            "apple",
            "banana",
            "cherry"
          ]
        }
        """;
        string modifiedJson = """
        {
          "fruits": [
            "cherry",
            "apple",
            "banana"
          ]
        }
        """;
        string expectedPatch = """
        [
          {
            "op": "mv",
            "p": "/fruits/0",
            "f": "/fruits/2"
          }
        ]
        """;

        string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

        Assert.AreEqual(expectedPatch, resultPatch);
    }

    [TestMethod]
    public void GenerateArrayMultiMovePatchProducesListDiff()
    {
        string originalJson = """
        {
          "values": [
            "one",
            "two",
            "three",
            "four"
          ]
        }
        """;
        string modifiedJson = """
        {
          "values": [
            "three",
            "four",
            "one",
            "two"
          ]
        }
        """;
        string expectedPatch = """
        [
          {
            "op": "ld",
            "p": "/values",
            "v": {
              "m": [
                {
                  "f": 2,
                  "t": 0
                },
                {
                  "f": 3,
                  "t": 1
                }
              ]
            }
          }
        ]
        """;

        string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

        Assert.AreEqual(expectedPatch, resultPatch);
    }

    [TestMethod]
    public void GeneratePatchMixedOperationsTest()
    {
        string originalJson = """
        {
          "employee": {
            "name": "Chris",
            "skills": [
              "csharp",
              "sql"
            ],
            "details": {
              "title": "Engineer",
              "location": "NYC"
            }
          },
          "active": true,
          "tags": [
            "backend",
            "api"
          ]
        }
        """;
        string modifiedJson = """
        {
          "employee": {
            "name": "Christian",
            "skills": [
              "csharp",
              "azure",
              "sql"
            ],
            "details": {
              "title": "Senior Engineer",
              "location": "Remote"
            },
            "level": 3
          },
          "active": true,
          "tags": [
            "backend"
          ]
        }
        """;
        string expectedPatch = """
        [
          {
            "op": "rp",
            "p": "/employee",
            "v": {
              "name": "Christian",
              "skills": [
                "csharp",
                "azure",
                "sql"
              ],
              "details": {
                "title": "Senior Engineer",
                "location": "Remote"
              },
              "level": 3
            }
          },
          {
            "op": "rm",
            "p": "/tags/1"
          }
        ]
        """;

        string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

        Assert.AreEqual(expectedPatch, resultPatch);
    }

  [TestMethod]
    public void GeneratePatchObjectDiffEmitsTargetedOperationsWhenCheaper()
    {
        string sharedBio = new string('x', 256);

        var originalDocument = new
        {
            employee = new
            {
                name = "Chris",
                meta = new
                {
                    location = "NYC",
                    department = "Sales",
                    bio = sharedBio
                }
            }
        };

        var modifiedDocument = new
        {
            employee = new
            {
                name = "Chris",
                meta = new
                {
                    location = "Remote",
                    department = "Sales",
                    bio = sharedBio,
                    region = "NA"
                }
            }
        };

        string originalJson = JsonSerializer.Serialize(originalDocument);
        string modifiedJson = JsonSerializer.Serialize(modifiedDocument);

        string patchJson = JsonMorph.GeneratePatch(originalJson, modifiedJson);

    JsonNode? patchNode = JsonNode.Parse(patchJson);
    Assert.IsNotNull(patchNode, "Generated patch should parse to JSON.");
    Assert.IsTrue(patchNode is JsonArray, "Patch root should be an array.");

    var operations = (JsonArray)patchNode;
    Assert.AreEqual(2, operations.Count, "Expected exactly two targeted operations.");

    bool locationUpdated = false;
    bool regionAdded = false;

    foreach (JsonNode? operationNode in operations)
    {
      Assert.IsNotNull(operationNode);
      JsonObject operation = operationNode.AsObject();
      string opCode = operation["op"]!.GetValue<string>();
      string path = operation["p"]!.GetValue<string>();

      if (path == "/employee/meta/region")
      {
        Assert.AreEqual("a", opCode, "Region change should be an add operation.");
        Assert.AreEqual("NA", operation["v"]!.GetValue<string>());
        regionAdded = true;
      }
      else
      {
        Assert.AreEqual("/employee/meta/location", path, "Unexpected path mutated in meta object.");
        Assert.IsTrue(opCode is "td" or "rp", "Location update should be emitted as td or rp.");
        string updatedValue = opCode == "td"
          ? operation["v"]!.AsObject()["it"]!.GetValue<string>()
          : operation["v"]!.GetValue<string>();
        Assert.AreEqual("Remote", updatedValue, "Location should be updated to Remote.");
        locationUpdated = true;
      }
    }

    Assert.IsTrue(locationUpdated, "Location mutation was not emitted.");
    Assert.IsTrue(regionAdded, "Region addition was not emitted.");
  }

  [TestMethod]
  public void GeneratePatchLargeObjectPrefersSingleReplace()
  {
    Dictionary<string, bool> originalSettings = Enumerable.Range(0, 40)
      .ToDictionary(i => $"feature{i:D2}", _ => false);
    Dictionary<string, bool> modifiedSettings = originalSettings.Keys
      .ToDictionary(key => key, _ => true);

    var originalDocument = new
    {
      metadata = new { owner = "ops" },
      settings = originalSettings
    };

    var modifiedDocument = new
    {
      metadata = new { owner = "ops" },
      settings = modifiedSettings
    };

    string originalJson = JsonSerializer.Serialize(originalDocument);
    string modifiedJson = JsonSerializer.Serialize(modifiedDocument);

    string patchJson = JsonMorph.GeneratePatch(originalJson, modifiedJson);

    JsonNode? patchNode = JsonNode.Parse(patchJson);
    Assert.IsNotNull(patchNode, "Generated patch should parse to JSON.");
    JsonArray operations = patchNode.AsArray();
    Assert.AreEqual(1, operations.Count, "Expected a single replace operation.");

    JsonObject operation = operations[0]!.AsObject();
    Assert.AreEqual("rp", operation["op"]!.GetValue<string>(), "Patch should use replace.");
    Assert.AreEqual("/settings", operation["p"]!.GetValue<string>(), "Replace should target the settings object.");

    JsonNode? valueNode = operation["v"];
    Assert.IsNotNull(valueNode, "Replace operation must include a value payload.");

    JsonNode expectedSettings = JsonNode.Parse(JsonSerializer.Serialize(modifiedSettings))!;
    Assert.IsTrue(JsonNode.DeepEquals(expectedSettings, valueNode), "Replace payload should match the modified settings.");
  }

  [TestMethod]
  public void GeneratePatchLargeArrayReorderFallsBackToElementReplacements()
  {
    const int length = 260;
    int[] originalValues = Enumerable.Range(0, length).ToArray();
    int[] rotatedValues = originalValues.Select(i => (i + 1) % length).ToArray();

    string originalJson = JsonSerializer.Serialize(new { values = originalValues });
    string modifiedJson = JsonSerializer.Serialize(new { values = rotatedValues });

    string patchJson = JsonMorph.GeneratePatch(originalJson, modifiedJson);

    JsonNode? patchNode = JsonNode.Parse(patchJson);
    Assert.IsNotNull(patchNode, "Generated patch should parse to JSON.");
    JsonArray operations = patchNode.AsArray();

    Assert.AreEqual(length, operations.Count, "Expected a replacement per array element when exceeding reorder threshold.");

    foreach (JsonNode? operationNode in operations)
    {
      Assert.IsNotNull(operationNode);
      JsonObject operation = operationNode.AsObject();
      Assert.AreEqual("rp", operation["op"]!.GetValue<string>(), "Large array reorder should emit replace operations.");
      string path = operation["p"]!.GetValue<string>();
      StringAssert.StartsWith(path, "/values/", "Array element replace should target values path.");
    }
  }

  [TestMethod]
  public void GeneratePatchPrefersTextDiffForLocalizedStringChanges()
  {
    string prefix = new string('a', 80);
    string suffix = new string('b', 80);
    string originalBody = prefix + "mid" + suffix;
    string modifiedBody = prefix + "mighty" + suffix;

    string originalJson = JsonSerializer.Serialize(new { article = originalBody });
    string modifiedJson = JsonSerializer.Serialize(new { article = modifiedBody });

    string patchJson = JsonMorph.GeneratePatch(originalJson, modifiedJson);

    JsonNode? patchNode = JsonNode.Parse(patchJson);
    Assert.IsNotNull(patchNode, "Generated patch should parse to JSON.");
    JsonArray operations = patchNode.AsArray();
    Assert.AreEqual(1, operations.Count, "Expected a single text diff operation.");

    JsonObject operation = operations[0]!.AsObject();
    Assert.AreEqual("td", operation["op"]!.GetValue<string>(), "Localized string mutation should use text diff.");
    Assert.AreEqual("/article", operation["p"]!.GetValue<string>());

    JsonObject payload = operation["v"]!.AsObject();
    Assert.AreEqual(prefix.Length + 2, payload["s"]!.GetValue<int>(), "Text diff should account for the matching 'mi' prefix.");
    Assert.AreEqual(1, payload["dl"]!.GetValue<int>(), "Only the differing character should be removed.");
    Assert.AreEqual("ghty", payload["it"]!.GetValue<string>(), "Inserted text should represent the localized change.");
  }

    [TestMethod]
    public void GeneratePatchRoundTripProducesModifiedDocument()
    {
        string originalJson = """
        {
          "project": {
            "name": "Atlas",
            "owners": [
              "alice",
              "bob"
            ],
            "metadata": {
              "status": "draft",
              "version": 1,
              "tags": [
                "alpha",
                "internal"
              ]
            }
          },
          "published": false
        }
        """;
        string modifiedJson = """
        {
          "project": {
            "name": "Atlas",
            "owners": [
              "alice",
              "bob",
              "charlie"
            ],
            "metadata": {
              "status": "released",
              "version": 2,
              "tags": [
                "alpha",
                "public"
              ],
              "notes": "Launched"
            }
          },
          "published": true
        }
        """;

        string patchJson = JsonMorph.GeneratePatch(originalJson, modifiedJson);
        string resultJson = JsonMorph.ApplyPatch(originalJson, patchJson);

        AssertJsonEquivalent(modifiedJson, resultJson);

        JsonNode? patchNode = JsonNode.Parse(patchJson);
        Assert.IsNotNull(patchNode);
        Assert.IsTrue(patchNode is JsonArray { Count: > 0 });
    }

    [TestMethod]
    public void GeneratePatchFormatsNumbersInvariantly()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

            string originalJson = """
            {
              "value": 1.5
            }
            """;
            string modifiedJson = """
            {
              "value": 2.75
            }
            """;
            string expectedPatch = """
            [
              {
                "op": "rp",
                "p": "/value",
                "v": 2.75
              }
            ]
            """;

            string resultPatch = JsonMorph.GeneratePatch(originalJson, modifiedJson);

            Assert.AreEqual(expectedPatch, resultPatch);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    private static void AssertJsonEquivalent(string expectedJson, string actualJson)
    {
        JsonNode? expectedNode = JsonNode.Parse(expectedJson);
        JsonNode? actualNode = JsonNode.Parse(actualJson);

        Assert.IsNotNull(expectedNode, "Expected JSON could not be parsed.");
        Assert.IsNotNull(actualNode, "Actual JSON could not be parsed.");
        Assert.IsTrue(JsonNode.DeepEquals(expectedNode, actualNode), "JSON documents differ.");
    }
}
