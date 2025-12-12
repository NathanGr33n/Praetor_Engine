using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

namespace PraetorEngine.Core.Rendering
{
    /// <summary>
    /// Test suite for rendering components.
    /// </summary>
    public static class RenderingTest
    {
        public static void RunAllTests(GraphicsDevice graphicsDevice)
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rendering_test_results.txt");
            using (var writer = new StreamWriter(logPath))
            {
                var originalOut = Console.Out;
                Console.SetOut(writer);

                try
                {
                    Console.WriteLine("=== Starting Rendering Tests ===\n");

                    TestCamera2D(graphicsDevice);
                    TestSmoothCamera(graphicsDevice);

                    Console.WriteLine("\n=== All Rendering Tests Passed ===");
                }
                finally
                {
                    Console.SetOut(originalOut);
                }
            }

            Console.WriteLine($"Rendering tests completed successfully. Results written to: {logPath}");
        }

        private static void TestCamera2D(GraphicsDevice graphicsDevice)
        {
            Console.WriteLine("Test: Camera2D");

            var viewport = graphicsDevice.Viewport;
            var camera = new Camera2D(viewport);

            // Test initial state
            Assert(camera.Position == Vector2.Zero, "Initial position should be zero");
            Assert(camera.Zoom == 1.0f, "Initial zoom should be 1.0");
            Assert(camera.Rotation == 0f, "Initial rotation should be 0");

            // Test position
            camera.Position = new Vector2(100, 200);
            Assert(camera.Position.X == 100 && camera.Position.Y == 200, "Position should update correctly");

            // Test zoom clamping
            camera.MinZoom = 0.5f;
            camera.MaxZoom = 2.0f;
            camera.Zoom = 0.1f; // Below min
            Assert(camera.Zoom == 0.5f, "Zoom should clamp to minimum");
            camera.Zoom = 5.0f; // Above max
            Assert(camera.Zoom == 2.0f, "Zoom should clamp to maximum");

            // Test movement
            camera.Position = Vector2.Zero;
            camera.Move(new Vector2(50, 50));
            Assert(camera.Position.X == 50 && camera.Position.Y == 50, "Move should update position");

            // Test LookAt
            camera.LookAt(new Vector2(1000, 500));
            Assert(camera.Position.X == 1000 && camera.Position.Y == 500, "LookAt should set position");

            // Test coordinate conversion
            camera.Reset();
            camera.Zoom = 1.0f;
            var worldPos = new Vector2(100, 100);
            var screenPos = camera.WorldToScreen(worldPos);
            var backToWorld = camera.ScreenToWorld(screenPos);
            Assert(Math.Abs(worldPos.X - backToWorld.X) < 0.01f && 
                   Math.Abs(worldPos.Y - backToWorld.Y) < 0.01f, 
                   "Coordinate conversion should be reversible");

            // Test visibility
            camera.Reset();
            camera.Position = new Vector2(400, 300);
            Assert(camera.IsVisible(new Vector2(400, 300)), "Center should be visible");

            // Test reset
            camera.Position = new Vector2(999, 999);
            camera.Zoom = 3.0f;
            camera.Reset();
            Assert(camera.Position == Vector2.Zero && camera.Zoom == 1.0f, "Reset should restore defaults");

            Console.WriteLine("  ✓ Camera2D works correctly\n");
        }

        private static void TestSmoothCamera(GraphicsDevice graphicsDevice)
        {
            Console.WriteLine("Test: SmoothCamera2D");

            var viewport = graphicsDevice.Viewport;
            var camera = new Camera2D(viewport);
            var smoothCamera = new SmoothCamera2D(camera);

            // Test initial state
            Assert(smoothCamera.Camera == camera, "Should reference correct camera");

            // Test smooth speed
            smoothCamera.SmoothSpeed = 10.0f;
            Assert(smoothCamera.SmoothSpeed == 10.0f, "Smooth speed should update");
            
            smoothCamera.SmoothSpeed = -1.0f; // Invalid
            Assert(smoothCamera.SmoothSpeed >= 0.1f, "Smooth speed should have minimum value");

            // Test target setting
            smoothCamera.SetTargetPosition(new Vector2(100, 100));
            smoothCamera.SetTargetZoom(2.0f);
            
            // Simulate a few update frames
            for (int i = 0; i < 10; i++)
            {
                smoothCamera.Update(1.0f / 60.0f); // 60 FPS
            }

            // Camera should have moved toward target (but maybe not reached due to smooth interpolation)
            Assert(camera.Position != Vector2.Zero, "Camera should have moved toward target");

            Console.WriteLine("  ✓ SmoothCamera2D works correctly\n");
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }
    }
}
