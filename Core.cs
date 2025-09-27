using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using peridot.Physics;
using Peridot.Graphics;
using Peridot.Testing;

namespace Peridot;

public class Core : Game
{
    internal static Core s_instance;

    public static Core Instance => s_instance;

    public static GraphicsDeviceManager Graphics { get; private set; }

    public static new GraphicsDevice GraphicsDevice { get; private set; }

    public static SpriteBatch SpriteBatch { get; private set; }
    public static SpriteBatch UIBatch { get; private set; }

    public static new ContentManager Content { get; private set; }

    public static TextureAtlas TextureAtlas { get; private set; }

    public static AudioLibrary AudioLibrary { get; private set; } = new AudioLibrary();

    public static Scene CurrentScene { get; set; } = new Scene();

    public static IInputManager InputManager { get; set; } = new InputManager();

    public static Camera2D Camera { get; private set; }

    public static TestRecorder TestRecorder { get; private set; }

    public static UISystem UISystem { get; private set; } = new UISystem();
    public static PhysicsSystem Physics { get; private set; }
    public static Vector2 Gravity { get; set; }

	public static int ScreenWidth => Graphics.PreferredBackBufferWidth;
    public static int ScreenHeight => Graphics.PreferredBackBufferHeight;

    TestRunner _testRunner;

    private static SpriteFont _defaultFont;

    public static SpriteFont DefaultFont => _defaultFont;
    
    public static DeveloperConsole DeveloperConsole { get; private set; } = new DeveloperConsole();

    public Core(string title, int width, int height, bool fullScreen, string defaultFont)
    {

        if (s_instance != null)
        {
            throw new InvalidOperationException($"Only a single Core instance can be created");
        }

        s_instance = this;

        Graphics = new GraphicsDeviceManager(this);

        Graphics.PreferredBackBufferWidth = width;
        Graphics.PreferredBackBufferHeight = height;
        Graphics.IsFullScreen = fullScreen;

        Graphics.ApplyChanges();

        Window.Title = title;

        Content = base.Content;

        Content.RootDirectory = "Content";

        IsMouseVisible = true;

        TestRecorder = new TestRecorder();
        _testRunner = new TestRunner();

        _defaultFont = Content.Load<SpriteFont>(defaultFont);
    }

    protected override void Initialize()
    {
        GraphicsDevice = base.GraphicsDevice;

        SpriteBatch = new SpriteBatch(GraphicsDevice);
        UIBatch = new SpriteBatch(GraphicsDevice);

        Camera = new Camera2D(GraphicsDevice.Viewport);

        Physics = new PhysicsSystem(Gravity);
        
        DeveloperConsole.Initialize();
        UISystem.AddElement(DeveloperConsole.GetRootElement());

        base.Initialize();
    }

    protected override void LoadContent()
    {
        AudioLibrary.Load(Content, "Content/audio/dictionary.txt");

        // Try to load texture atlas from different possible locations
        if (System.IO.File.Exists("Content/images/atlas.xml"))
        {
            TextureAtlas = TextureAtlas.FromFile(Content, "images/atlas.xml");
        }
        else if (System.IO.File.Exists("Content/images/characters_sheet_definition.xml"))
        {
            TextureAtlas = TextureAtlas.FromFile(Content, "images/characters_sheet_definition.xml");
        }
        else
        {
            Logger.Error("No texture atlas found. AnimatedSprite components will not work properly.");
        }
    }

    private KeyboardState _previousKeyboardState;
    private bool _isRecording;

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();

        Physics.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        InputManager.Update(gameTime);

        CurrentScene.Update(gameTime);

        _testRunner.Update(gameTime);

        UISystem.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

        // Test Recording Controls
        if (keyboardState.IsKeyDown(Keys.F5) && _previousKeyboardState.IsKeyUp(Keys.F5) && !_isRecording)
        {
            // create a test name 
            string testName = $"Test_{DateTime.Now:yyyyMMdd_HHmmss}";
            TestRecorder.BeginRecordingTest(testName, gameTime);
            _isRecording = true;
        }
        else if (keyboardState.IsKeyDown(Keys.F5) && _previousKeyboardState.IsKeyUp(Keys.F5) && _isRecording)
        {
            // Stop recording a test
            TestRecorder.EndRecordingTest(gameTime);
            _isRecording = false;
        }
        else if (keyboardState.IsKeyDown(Keys.F7) && _previousKeyboardState.IsKeyUp(Keys.F7))
        {
            // Capture assertion during recording (F7 = Capture Player Position)
            if (_isRecording)
            {
                TestRecorder.CaptureAssertion(gameTime);
            }
        }

        // Test Playback Controls
        else if (keyboardState.IsKeyDown(Keys.F8) && _previousKeyboardState.IsKeyUp(Keys.F8))
        {
            _testRunner.EnqueueAllTests();
        }
        else if (keyboardState.IsKeyDown(Keys.F9) && _previousKeyboardState.IsKeyUp(Keys.F9))
        {
            // Run single test (last recorded or available)
        }
        else if (keyboardState.IsKeyDown(Keys.F10) && _previousKeyboardState.IsKeyUp(Keys.F10))
        {
            // Stop current test playback
        }

        // TODO: Implement test playback
        //TestRecorder.Update(gameTime);

        if(keyboardState.IsKeyDown(Keys.OemTilde) && _previousKeyboardState.IsKeyUp(Keys.OemTilde))
        {
            DeveloperConsole.GetRootElement().SetVisibility(!DeveloperConsole.GetRootElement().IsVisible());
        }   

        base.Update(gameTime);

        _previousKeyboardState = keyboardState;
    }

    protected override void Draw(GameTime gameTime)
    {
        CurrentScene.DrawOffscreen();

        GraphicsDevice.Clear(Color.SkyBlue);

        SpriteBatch.Begin(transformMatrix: Camera.GetViewMatrix(), sortMode: SpriteSortMode.FrontToBack, samplerState: SamplerState.PointClamp);
        CurrentScene.Draw(SpriteBatch);
        SpriteBatch.End();

        UIBatch.Begin(samplerState: SamplerState.PointClamp, sortMode: SpriteSortMode.Deferred);
        UISystem.Draw(UIBatch); 
        UIBatch.End();

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CurrentScene.Dispose();
            SpriteBatch?.Dispose();
        }
        base.Dispose(disposing);
    }
}