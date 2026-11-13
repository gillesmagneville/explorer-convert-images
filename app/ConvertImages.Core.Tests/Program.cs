// SPDX-License-Identifier: GPL-3.0-only

using System.Reflection;

namespace ConvertImages.Core.Tests;

/// <summary>Marks a static method without parameters as a test.</summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class TestAttribute : Attribute;

/// <summary>Thrown by a failed check.</summary>
internal sealed class TestFailure(string message) : Exception(message);

/// <summary>Thrown by a test that cannot run here (reported, not a failure).</summary>
internal sealed class TestSkipped(string reason) : Exception(reason);

internal static class Check
{
    public static void True(bool condition, string what)
    {
        if (!condition)
        {
            throw new TestFailure(what);
        }
    }

    public static void Equal<T>(T expected, T actual, string? what = null)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new TestFailure($"{what ?? "values differ"}\n      expected: {Show(expected)}\n      actual:   {Show(actual)}");
        }
    }

    public static void Sequence<T>(IEnumerable<T> expected, IEnumerable<T> actual, string? what = null)
    {
        var (e, a) = (expected.ToList(), actual.ToList());
        if (!e.SequenceEqual(a))
        {
            throw new TestFailure(
                $"{what ?? "sequences differ"}\n      expected: [{string.Join(", ", e.Select(Show))}]\n      actual:   [{string.Join(", ", a.Select(Show))}]");
        }
    }

    public static TException Throws<TException>(Action action, string what)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException e)
        {
            return e;
        }
        throw new TestFailure($"{what}: no {typeof(TException).Name} was thrown");
    }

    private static string Show<T>(T value) => value switch
    {
        null => "null",
        string s => "\"" + s.Replace("\n", "\\n").Replace("\r", "\\r") + "\"",
        _ => value.ToString() ?? "",
    };
}

/// <summary>Where things are in the repository.</summary>
internal static class Repo
{
    private static readonly Lazy<string> LazyRoot = new(() =>
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "VERSION")) && Directory.Exists(Path.Combine(dir.FullName, "po")))
            {
                return dir.FullName;
            }
        }
        throw new TestFailure("repository root not found (no VERSION file and po/ folder above the test program)");
    });

    public static string Root => LazyRoot.Value;

    public static string File_(params string[] parts) => Path.Combine([Root, .. parts]);
}

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--write-pot")
        {
            var path = args.Length > 1 ? args[1] : PotFile.DefaultPath;
            File.WriteAllText(path, PotFile.Generate());
            Console.WriteLine($"written: {path}");
            return 0;
        }

        var tests = typeof(Program).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(method => method.GetCustomAttribute<TestAttribute>() is not null)
            .OrderBy(method => method.DeclaringType!.Name, StringComparer.Ordinal)
            .ThenBy(method => method.Name, StringComparer.Ordinal)
            .ToList();

        var (passed, failed, skipped) = (0, 0, 0);
        foreach (var test in tests)
        {
            var name = $"{test.DeclaringType!.Name}.{test.Name}";
            try
            {
                test.Invoke(null, null);
                passed++;
                Console.WriteLine($"ok    {name}");
            }
            catch (TargetInvocationException e) when (e.InnerException is TestSkipped skip)
            {
                skipped++;
                Console.WriteLine($"skip  {name}: {skip.Message}");
            }
            catch (TargetInvocationException e)
            {
                failed++;
                var cause = e.InnerException!;
                Console.WriteLine($"FAIL  {name}\n      {(cause is TestFailure ? cause.Message : cause.ToString())}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"{passed} passed, {failed} failed, {skipped} skipped");
        return failed == 0 ? 0 : 1;
    }
}
