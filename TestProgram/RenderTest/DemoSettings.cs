using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using SlopperEngine.Core;
using SlopperEngine.Core.SceneComponents;
using SlopperEngine.Graphics;
using SlopperEngine.Graphics.DefaultResources;
using SlopperEngine.Rendering;
using SlopperEngine.SceneObjects;
using SlopperEngine.UI.Base;
using SlopperEngine.UI.Interaction;
using SlopperEngine.UI.Layout;
using SlopperEngine.UI.Style;
using SlopperEngine.UI.Text;
using SlopperEngine.Windowing;

namespace TestProgram.RenderTest;

/// <summary>
/// Settings window for the demo.
/// </summary>
public class DemoSettings : SceneObject
{
    RenderDemo? _demo;
    Window _window;
    TextBox _frametimeDisplay;
    double _frameTimeAcc;
    int _framesHad = 1;
    List<Keyframe>? _recordedKeyframes;
    public DemoSettings(RenderDemo demo)
    {
        _demo = demo;

        Scene sc = Scene.CreateEmpty();
        sc.Components.Add(new UpdateHandler());
        sc.Renderers.Add(new UIRenderer());
        sc.CheckCachedComponents();
        sc.Children.Add(this);

        var windowSize = new Vector2i(300, 200);
        _window = Window.Create(new(windowSize, Title: "Render demo settings"));
        _window.CenterWindow();
        _window.Scene = sc;
        _window.WindowTexture = sc.SceneRenderer?.GetOutputTexture();
        _window.Closing += a => _demo?.Kill();
        sc.SceneRenderer!.Resize(windowSize);

        UIElement root = new();
        sc.Children.Add(root);
        root.Layout.Value = new LinearArrangedLayout
        {
            IsLayoutHorizontal = false,
            StartAtMax = true,
            ChildAlignment = Alignment.Middle
        };
        root.UIChildren.Add(_frametimeDisplay = new("FPS: -"){Scale = 1});
        TextButton dirLightSwitch = new TextButton("Directional light: 3 cascades (default)");
        root.UIChildren.Add(dirLightSwitch);
        int dirSwitch = 0;
        dirLightSwitch.Style = BasicStyle.DefaultStyle;
        dirLightSwitch.OnButtonReleased += _ =>
        {
            dirSwitch++;
            _framesHad = 1;
            _frameTimeAcc = 0;
            switch(dirSwitch)
            {
                case 0:
                dirLightSwitch.Text = "Directional light: 3 cascades (default)";
                demo.SkyLight.Cascades = null;
                break;

                case 1:
                dirLightSwitch.Text = "Directional light: Shadow casting off";
                demo.SkyLight.CastsShadows = false;
                break;

                case 2: 
                (demo.Scene!.SceneRenderer as DebugRenderer)!.UseInfiniteShadowMap = true;
                dirLightSwitch.Text = "Directional light: infinite map";
                demo.SkyLight.CastsShadows = true;
                demo.SkyLight.Cascades = [32f];
                break;

                case 3:
                (demo.Scene!.SceneRenderer as DebugRenderer)!.UseInfiniteShadowMap = false;
                dirLightSwitch.Text = "Directional light: 1 cascade";
                dirSwitch = -1;
                break;
            }
        };
        Material mat = Material.Create(SlopperShader.Create(Asset.GetFile("shaders/InstancedPhong.sesl")));
        // mat.Uniforms[mat.GetUniformIndexFromName("randomRange")].Value = 100f;
        MeshRenderer spheres = new MeshRenderer
        {
            Material = mat,
            Mesh = DefaultMeshes.Sphere,
            InstanceCount = 30000,
            LocalPosition = new Vector3(20,0,-100)
        };
        TextButton megaInstanceSwitch = new TextButton("Sphere spam: off");
        root.UIChildren.Add(megaInstanceSwitch);
        megaInstanceSwitch.Style = BasicStyle.DefaultStyle;
        megaInstanceSwitch.OnButtonReleased += _ =>
        {
            if (spheres.InScene)
            {
                spheres.Remove();
                megaInstanceSwitch.Text = "Sphere spam: off";
            }
            else
            {
                demo.Children.Add(spheres);
                megaInstanceSwitch.Text = "Sphere spam: on";
            }
        };

        if (Asset.TryGetFile("RenderDemo/Flythrough.csv", out var asset))
            _recordedKeyframes = Keyframe.LoadFromCsv(asset.Value);

        TextButton playFlyThroughButton = new TextButton("Play camera flythrough");
        playFlyThroughButton.Style = BasicStyle.DefaultStyle;
        playFlyThroughButton.Enabled = _recordedKeyframes is not null;
        root.UIChildren.Add(playFlyThroughButton);
        playFlyThroughButton.OnButtonReleased += _ =>
        {
            if (demo.Scene?.GetDataContainerEnumerable<Camera>().EnumerateReadonly().FirstOrDefault() is not Camera camera) return;
            if (camera.Children.FirstOfType<FlyThroughPlayer>() is FlyThroughPlayer realPlayer)
            {
                realPlayer.Destroy();
                playFlyThroughButton.Text = "Play camera flythrough";
                camera.Children.Add(new NoclipController());
            }
            else
            {
                if (_recordedKeyframes is null)
                {
                    Console.WriteLine("Haven't recorded anything to play back yet");
                    return;
                }
                realPlayer = new FlyThroughPlayer();
                realPlayer.Keyframes = _recordedKeyframes;
                realPlayer.Playing = true;
                playFlyThroughButton.Text = "Stop playing flythrough";
                camera.Children.Add(realPlayer);
                camera.Children.FirstOfType<NoclipController>()?.Destroy();
            }
        };
        TextButton saveFlythrough = new TextButton("Save flythrough");
        TextButton recordFlyThroughButton = new TextButton("Record camera flythrough");
        recordFlyThroughButton.Style = BasicStyle.DefaultStyle;
        root.UIChildren.Add(recordFlyThroughButton);
        recordFlyThroughButton.OnButtonReleased += _ =>
        {
            if (demo.Scene?.GetDataContainerEnumerable<Camera>().EnumerateReadonly().FirstOrDefault() is not Camera camera) return;
            if (camera.Children.FirstOfType<FlyThroughRecorder>() is FlyThroughRecorder realRecorder)
            {
                realRecorder.StopRecording();
                _recordedKeyframes = realRecorder.Keyframes;
                playFlyThroughButton.Enabled = true;
                saveFlythrough.Enabled = true;
                realRecorder.Destroy();
                recordFlyThroughButton.Text = "Record camera flythrough";
            }
            else
            {
                realRecorder = new FlyThroughRecorder();
                realRecorder.StartRecording();
                recordFlyThroughButton.Text = "Stop recording flythrough";
                camera.Children.Add(realRecorder);
            }
        };

        saveFlythrough.Style = BasicStyle.DefaultStyle;
        saveFlythrough.Enabled = _recordedKeyframes is not null;
        root.UIChildren.Add(saveFlythrough);
        saveFlythrough.OnButtonReleased += _ =>
        {
            if (_recordedKeyframes == null)
            {
                Console.WriteLine("Recorded keyframes were null!");
                return;
            }
            
            if (Asset.TryGetFile("RenderDemo/Flythrough.csv", out var file, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Write))
                Keyframe.SaveToCsv(file.Value, _recordedKeyframes);
            else
                Console.WriteLine("No permissions to save flythrough :{"); 
        };
    }

    [OnFrameUpdate]
    void OnUpdate(FrameUpdateArgs args)
    {
        _frameTimeAcc += args.DeltaTime;
        _framesHad++;
        _frametimeDisplay.Text = $"Average Frametime: {_frameTimeAcc * 1000f / _framesHad:0.000}ms\nafter {_framesHad} frames";
    }

    protected override void OnDestroyed()
    {
        _demo = null;
        _window.Close();
    }
}