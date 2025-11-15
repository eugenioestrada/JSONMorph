using System;
using System.Globalization;
using JSONMorph;

namespace UnitTests;

[TestClass]
public sealed class ApplyTests
{
    private static void AssertThrows<TException>(Action action, string? expectedMessage = null) where TException : Exception
    {
        try
        {
            action();
            Assert.Fail($"Expected exception of type {typeof(TException).Name}.");
        }
        catch (TException ex)
        {
            if (expectedMessage is not null)
            {
                StringAssert.Contains(ex.Message, expectedMessage);
            }
        }
    }

    [TestMethod]
    public void ApplyReplacePatchTest()
    {
        string originalJson = """
        {
          "name": "John",
          "age": 30
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "rp",
            "p": "/name",
            "v": "Jane"
          }
        ]
        """;
        string expectedJson = """
        {
          "name": "Jane",
          "age": 30
        }
        """;

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyTextDiffPatchTest()
    {
        string originalJson = """
        {
          "name": "John",
          "age": 30
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "td",
            "p": "/name",
            "v": {
              "s": 1,
              "dl": 3,
              "it": "ane"
            }
          }
        ]
        """;
        string expectedJson = """
        {
          "name": "Jane",
          "age": 30
        }
        """;

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyAppendPatchTest()
    {
        string originalJson = """
        {
          "fruits": [
            "apple",
            "banana"
          ]
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "a",
            "p": "/fruits",
            "v": "cherry"
          }
        ]
        """;
        string expectedJson = """
        {
          "fruits": [
            "apple",
            "banana",
            "cherry"
          ]
        }
        """;

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyRemovePatchTest()
    {
        string originalJson = """
        {
          "name": "John",
          "age": 30
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "rm",
            "p": "/age"
          }
        ]
        """;
        string expectedJson = """
        {
          "name": "John"
        }
        """;

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyMultiplePatchTest()
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
        string jsonPatch = """
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
        string expectedJson = """
        {
          "name": "Jane",
          "fruits": [
            "apple",
            "banana",
            "cherry"
          ]
        }
        """;

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyRemoveIndexOfArrayPatchTest()
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
        string jsonPatch = """
        [
          {
            "op": "rm",
            "p": "/fruits/1"
          }
        ]
        """;
        string expectedJson = """
        {
          "fruits": [
            "apple",
            "cherry"
          ]
        }
        """;

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyAppendToIndexPatchTest()
    {
        string originalJson = """
        {
          "fruits": [
            "apple",
            "banana"
          ]
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "a",
            "p": "/fruits/1",
            "v": "cherry"
          }
        ]
        """;
        string expectedJson = """
        {
          "fruits": [
            "apple",
            "cherry",
            "banana"
          ]
        }
        """;

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyAppendComplexObjectPatchTest()
    {
        string originalJson = """
        {
          "employees": []
        }
        """;
        string jsonPatch = """
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
        string expectedJson = """
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

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyReplaceComplexObjectPatchTest()
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
        string jsonPatch = """
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
        string expectedJson = """
        {
          "employee": {
            "name": "Robert",
            "age": 36,
            "department": "IT"
          }
        }
        """;

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyReplaceInObjectInArrayPatchTest()
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
        string jsonPatch = """
        [
          {
            "op": "rp",
            "p": "/employees/1/name",
            "v": "Dina"
          }
        ]
        """;
        string expectedJson = """
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

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyReplaceWithEscapedPathTest()
    {
        string originalJson = """
        {
          "complex/name": {
            "~value": "old",
            "other": "unchanged"
          }
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "rp",
               "p": "/complex~1name/~0value",
               "v": "new"
          }
        ]
        """;
        string expectedJson = """
        {
          "complex/name": {
            "~value": "new",
            "other": "unchanged"
          }
        }
        """;

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyMovePatchTest()
    {
        string originalJson = """
        {
          "values": [
            "a",
            "b",
            "c"
          ]
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "mv",
            "p": "/values/0",
            "f": "/values/2"
          }
        ]
        """;
        string expectedJson = """
        {
          "values": [
            "c",
            "a",
            "b"
          ]
        }
        """;

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyCopyPatchTest()
    {
        string originalJson = """
        {
          "person": {
            "name": "Alice"
          }
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "cp",
            "p": "/person/alias",
            "f": "/person/name"
          }
        ]
        """;
        string expectedJson = """
        {
          "person": {
            "name": "Alice",
            "alias": "Alice"
          }
        }
        """;

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyListDiffPatchTest()
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
        string jsonPatch = """
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
        string expectedJson = """
        {
          "values": [
            "three",
            "four",
            "one",
            "two"
          ]
        }
        """;

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyAppendNestedArrayPatchTest()
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
        string jsonPatch = """
        [
          {
            "op": "a",
            "p": "/teams/0/members",
            "v": "Amy"
          }
        ]
        """;
        string expectedJson = """
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

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyRemoveDeepPropertyPatchTest()
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
        string jsonPatch = """
        [
          {
            "op": "rm",
            "p": "/department/metrics/targets"
          }
        ]
        """;
        string expectedJson = """
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

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyReplaceRootPatchTest()
    {
        string originalJson = """
        {
          "status": "draft"
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "rp",
            "p": "/",
            "v": {
              "status": "published",
              "version": 2
            }
          }
        ]
        """;
        string expectedJson = """
        {
          "status": "published",
          "version": 2
        }
        """;

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyTextDiffRootPatchTest()
    {
        string originalJson = "\"hello\"";
        string jsonPatch = """
        [
          {
            "op": "td",
            "p": "/",
            "v": {
              "s": 5,
              "dl": 0,
              "it": " world"
            }
          }
        ]
        """;
        string expectedJson = "\"hello world\"";

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyAddNewPropertyPatchTest()
    {
        string originalJson = """
        {
          "employee": {
            "name": "Chris"
          }
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "a",
            "p": "/employee/role",
            "v": "Manager"
          }
        ]
        """;
        string expectedJson = """
        {
          "employee": {
            "name": "Chris",
            "role": "Manager"
          }
        }
        """;

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyAddRootPatchTest()
    {
        string originalJson = """
        {
          "status": "draft"
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "a",
            "p": "/",
            "v": {
              "status": "published",
              "version": 3
            }
          }
        ]
        """;
        string expectedJson = """
        {
          "status": "published",
          "version": 3
        }
        """;

        string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

        Assert.AreEqual(expectedJson, resultJson);
    }

    [TestMethod]
    public void ApplyRemoveMissingPropertyThrowsTest()
    {
        string originalJson = """
        {
          "name": "John"
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "rm",
            "p": "/age"
          }
        ]
        """;

        AssertThrows<InvalidOperationException>(() => JsonMorph.ApplyPatch(originalJson, jsonPatch), "Remove target not found.");
    }

    [TestMethod]
    public void ApplyAddIndexOutOfRangeThrowsTest()
    {
        string originalJson = """
        {
          "values": [
            1,
            2
          ]
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "a",
            "p": "/values/5",
            "v": 3
          }
        ]
        """;

        AssertThrows<InvalidOperationException>(() => JsonMorph.ApplyPatch(originalJson, jsonPatch), "Add index out of range.");
    }

    [TestMethod]
    public void ApplyUnsupportedOperationThrowsTest()
    {
        string originalJson = """
        {
          "name": "John"
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "copy",
            "p": "/name",
            "v": "Jane"
          }
        ]
        """;

        AssertThrows<InvalidOperationException>(() => JsonMorph.ApplyPatch(originalJson, jsonPatch), "Unsupported operation 'copy'.");
    }

    [TestMethod]
    public void ApplyRemoveRootThrowsTest()
    {
        string originalJson = """
        {
          "value": 1
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "rm",
            "p": "/"
          }
        ]
        """;

        AssertThrows<InvalidOperationException>(() => JsonMorph.ApplyPatch(originalJson, jsonPatch), "Removing the root is not supported.");
    }

    [TestMethod]
    public void ApplyPatchWithNonArrayDocumentThrowsArgumentException()
    {
        string originalJson = """
        {
          "name": "John"
        }
        """;
        string jsonPatch = """
        {
          "op": "rp",
          "p": "/name",
          "v": "Jane"
        }
        """;

        AssertThrows<ArgumentException>(() => JsonMorph.ApplyPatch(originalJson, jsonPatch), "Patch document must be a JSON array.");
    }

    [TestMethod]
    public void ApplyPatchWithNonObjectOperationThrowsInvalidOperationException()
    {
        string originalJson = """
        {
          "name": "John"
        }
        """;
        string jsonPatch = """
        [
          "not an object"
        ]
        """;

        AssertThrows<InvalidOperationException>(() => JsonMorph.ApplyPatch(originalJson, jsonPatch), "Patch operation must be a JSON object.");
    }

    [TestMethod]
    public void ApplyPatchMissingOperationCodeThrowsInvalidOperationException()
    {
        string originalJson = """
        {
          "name": "John"
        }
        """;
        string jsonPatch = """
        [
          {
            "p": "/name",
            "v": "Jane"
          }
        ]
        """;

        AssertThrows<InvalidOperationException>(() => JsonMorph.ApplyPatch(originalJson, jsonPatch), "Patch operation missing 'op'.");
    }

    [TestMethod]
    public void ApplyPatchMissingPathThrowsInvalidOperationException()
    {
        string originalJson = """
        {
          "name": "John"
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "rp",
            "v": "Jane"
          }
        ]
        """;

        AssertThrows<InvalidOperationException>(() => JsonMorph.ApplyPatch(originalJson, jsonPatch), "Patch operation missing 'p'.");
    }

    [TestMethod]
    public void ApplyReplaceWithoutValueThrowsInvalidOperationException()
    {
        string originalJson = """
        {
          "name": "John"
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "rp",
            "p": "/name"
          }
        ]
        """;

        AssertThrows<InvalidOperationException>(() => JsonMorph.ApplyPatch(originalJson, jsonPatch), "Replace operation requires a value.");
    }

    [TestMethod]
    public void ApplyAddWithoutValueThrowsInvalidOperationException()
    {
        string originalJson = """
        {
          "items": []
        }
        """;
        string jsonPatch = """
        [
          {
            "op": "a",
            "p": "/items"
          }
        ]
        """;

        AssertThrows<InvalidOperationException>(() => JsonMorph.ApplyPatch(originalJson, jsonPatch), "Add operation requires a value.");
    }

    [TestMethod]
    public void ApplyPatchFormatsNumbersInvariantly()
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
            string jsonPatch = """
            [
              {
                "op": "rp",
                "p": "/value",
                "v": 2.75
              }
            ]
            """;
            string expectedJson = """
            {
              "value": 2.75
            }
            """;

            string resultJson = JsonMorph.ApplyPatch(originalJson, jsonPatch);

            Assert.AreEqual(expectedJson, resultJson);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
