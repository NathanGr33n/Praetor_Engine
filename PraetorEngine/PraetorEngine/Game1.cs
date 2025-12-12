using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PraetorEngine
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // Run all tests and demos
            try
            {
                // Phase 1 tests and demo
                Core.ECS.ECSTest.RunAllTests();
                Core.Memory.MemoryTest.RunAllTests();
                Core.Rendering.RenderingTest.RunAllTests(GraphicsDevice);
                Phase1Demo.RunDemo(GraphicsDevice);
                
                // Phase 2 demo
                Phase2Demo.RunDemo(GraphicsDevice);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test/Demo Failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Exit();
            }

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
