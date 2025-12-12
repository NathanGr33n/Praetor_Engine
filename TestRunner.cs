using System;
using System.IO;
using PraetorEngine.Core.ECS;

class TestRunner
{
    static void Main(string[] args)
    {
        var logPath = "ecs_test_results.txt";
        using (var writer = new StreamWriter(logPath))
        {
            var oldOut = Console.Out;
            Console.SetOut(writer);
            
            try
            {
                ECSTest.RunAllTests();
                Console.WriteLine("\n✓✓✓ ALL TESTS PASSED ✓✓✓");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗✗✗ TEST FAILED ✗✗✗");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace:\n{ex.StackTrace}");
            }
            
            Console.SetOut(oldOut);
        }
        
        Console.WriteLine($"Test results written to: {Path.GetFullPath(logPath)}");
        Console.WriteLine(File.ReadAllText(logPath));
    }
}
