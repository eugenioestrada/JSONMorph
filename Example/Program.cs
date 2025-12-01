// See https://aka.ms/new-console-template for more information
using JSONMorph;

Console.WriteLine("Hello, World!");

string terms = """
{
}
""";

Console.WriteLine("Original Terms:");
Console.WriteLine(terms);

string patch1 = JsonMorph.GeneratePatch(terms, """
{
    "Section": [ "slug1" ]
}
""");

Console.WriteLine("Generated Patch 1:");
Console.WriteLine(patch1);

string patchedTerms1 = JsonMorph.ApplyPatch(terms, patch1);

Console.WriteLine("Patched Terms 1:");
Console.WriteLine(patchedTerms1);

string patch2 = JsonMorph.GeneratePatch(patchedTerms1, """
{
    "Section": [ "slug2" ]
}
""");

Console.WriteLine("Generated Patch 2:");
Console.WriteLine(patch2);

string patchedTerms2 = JsonMorph.ApplyPatches(terms, patch1, patch2);
Console.WriteLine("Patched Terms 2:");

Console.WriteLine(patchedTerms2);

Console.ReadKey();