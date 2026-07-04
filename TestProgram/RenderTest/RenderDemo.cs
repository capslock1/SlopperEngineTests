using System;
using OpenTK.Mathematics;
using SlopperEngine.Core;
using SlopperEngine.Graphics;
using SlopperEngine.Graphics.DefaultResources;
using SlopperEngine.Rendering;
using SlopperEngine.Rendering.Lighting;
using SlopperEngine.SceneObjects;
using SlopperEngine.Windowing;

namespace TestProgram.RenderTest;

public class RenderDemo : SceneObject, IDemo
{
    public readonly DirectionalLight SkyLight;
    Window _displayWindow;
    SceneObject3D _randomObjectHolder;
    double _fpscapBeforeDemo;
    DemoSettings _settings;

    public static Scene CreateDemoScene()
    {
        Scene scene = Scene.CreateDefault();
        scene.Children.Add(new RenderDemo(scene));
        return scene;
    }

    private RenderDemo(Scene scene)
    {
        var displaySize = new Vector2i(1200, 800);

        _displayWindow = Window.Create(new(displaySize, Title:"Render demo"));
        _displayWindow.CenterWindow();
        _displayWindow.Scene = scene;
        _displayWindow.WindowTexture = scene.SceneRenderer?.GetOutputTexture();
        _displayWindow.Closing += a => scene.Destroy();
        scene.SceneRenderer?.Resize(2*displaySize);

        // set fps cap infinite to test it properly
        _fpscapBeforeDemo = MainContext.Instance.UpdateFrequency;
        MainContext.Instance.UpdateFrequency = 0;
        scene.OnDestroy += () => 
        {
            MainContext.Instance.UpdateFrequency = _fpscapBeforeDemo;
            _settings!.Destroy();
        };

        Camera cam = new();
        scene.Children.Add(cam);
        cam.Children.Add(new NoclipController());
        cam.Projection = Matrix4.CreatePerspectiveFieldOfView(1, 1.5f, 0.2f, 512f);

        Random rand = new(6767);
        Material mat = Material.Create(SlopperShader.Create(Asset.GetEngineAsset("shaders/phongShader.sesl")));
        _randomObjectHolder = new();
        scene.Children.Add(_randomObjectHolder);
        for(int i = 0; i<100; i++)
        {
            MeshRenderer rend = new()
            {
                Mesh = ((uint)rand.Next() % 3) switch
                {
                   1u => DefaultMeshes.Plane,
                   2u => DefaultMeshes.Sphere,
                   _ => DefaultMeshes.Cube, 
                },
                LocalPosition = new(rand.NextSingle()*20 - 10, rand.NextSingle()*20 - 10, rand.NextSingle()*20 - 10),
                LocalRotation = Quaternion.FromEulerAngles(rand.NextSingle(), rand.NextSingle(), rand.NextSingle()),
                Material = mat
            };
            _randomObjectHolder.Children.Add(rend);
        }
        scene.Children.Add(new MeshRenderer() // floor
        {
            LocalPosition = new(0,-15,0),
            LocalScale = new(150),
            LocalRotation = Quaternion.FromAxisAngle(new(1,0,0), float.Pi*1.5f),
            Material = mat,
            Mesh = DefaultMeshes.Plane,
        });
        scene.Children.Add(new PointLight()
        {
           LocalPosition = new(12,12,12),
           Color = new(1,3,10),
           Radius = 40,
           Sharpness = 1.5f 
        });
        scene.Children.Add(new PointLight()
        {
           LocalPosition = new(-12,-12,-12),
           Color = new(10,3,1),
           Radius = 40,
           Sharpness = 1.5f 
        });
        scene.Children.Add(SkyLight = new DirectionalLight()
        {
           LocalRotation = Quaternion.FromAxisAngle(new(1,0,0), float.Pi*1.5f),
           Color = new(0.1f, 0.5f, 0.1f),
           CastsShadows = true,
        });

        _settings = new(this, displaySize);
    }

    // Can't do this right now because resetting the rotation doesn't seem to work, and having it rotate anyway messes up the performance recording
    // [OnFrameUpdate]
    // void FrameUpdate(FrameUpdateArgs args)
    // {
    //     _randomObjectHolder.LocalRotation *= Quaternion.FromAxisAngle(new(0,1,1), args.DeltaTime * 0.02f);
    // }

    [OnInputUpdate]
    void InputUpdate(InputUpdateArgs args)
    {
        if(args.KeyboardState.IsKeyPressed(OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape))
            Kill();
    }

    public void Kill() => _displayWindow.Close();

    static string? IDemo.GetDescription() => "Demo for testing new render features for the engine. \nPress 'Esc' to close.";
}