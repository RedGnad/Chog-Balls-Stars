using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// Minimal reflection runner: executes NUnit [Test] and [TestCase] methods by attribute name.
static class Runner
{
    static int Main(string[] args)
    {
        var assembly = Assembly.LoadFrom(args[0]);
        int passed = 0, failed = 0;
        foreach (var type in assembly.GetTypes())
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            var cases = new List<object[]>();
            foreach (var attribute in method.GetCustomAttributes(true))
            {
                var attributeType = attribute.GetType();
                if (attributeType.FullName == "NUnit.Framework.TestCaseAttribute")
                    cases.Add((object[])attributeType.GetProperty("Arguments").GetValue(attribute));
                else if (attributeType.FullName == "NUnit.Framework.TestAttribute")
                    cases.Add(new object[0]);
            }
            foreach (var arguments in cases)
            {
                var name = type.Name + "." + method.Name + "(" + string.Join(", ", arguments.Select(a => a ?? "null")) + ")";
                try
                {
                    method.Invoke(Activator.CreateInstance(type), arguments);
                    passed++;
                    Console.WriteLine("PASS " + name);
                }
                catch (TargetInvocationException exception)
                {
                    failed++;
                    Console.WriteLine("FAIL " + name + " :: " + exception.InnerException?.Message);
                }
            }
        }
        Console.WriteLine("passed=" + passed + " failed=" + failed);
        return failed == 0 && passed > 0 ? 0 : 1;
    }
}
