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
        TextButton Record = new TextButton("Record camera flythrough");
        Record.Style = BasicStyle.DefaultStyle;
        root.UIChildren.Add(Record);
        Record.OnButtonReleased += _ =>
        {
            if (demo.Scene?.GetDataContainerEnumerable<Camera>().EnumerateReadonly().FirstOrDefault() is not Camera camera) return;
            if (camera.Children.FirstOfType<FlyThroughRecorder>() is FlyThroughRecorder RealRecorder)
            {
                RealRecorder.StopRecording();
                _recordedKeyframes = RealRecorder.Keyframes;
                RealRecorder.Destroy();
                Record.Text = "Record camera flythrough";
            }
            else
            {
                RealRecorder = new FlyThroughRecorder();
                RealRecorder.StartRecording();
                Record.Text = "Stop recording flythrough";
                camera.Children.Add(RealRecorder);
            }
        };
        TextButton Play = new TextButton("Play camera flythrough");
        Play.Style = BasicStyle.DefaultStyle;
        root.UIChildren.Add(Play);
        Play.OnButtonReleased += _ =>
        {
            if (demo.Scene?.GetDataContainerEnumerable<Camera>().EnumerateReadonly().FirstOrDefault() is not Camera camera) return;
            if (camera.Children.FirstOfType<FlyThroughPlayer>() is FlyThroughPlayer RealPlayer)
            {
                RealPlayer.Destroy();
                Play.Text = "Play camera flythrough";
                camera.Children.Add(new NoclipController());
            }
            else
            {
                if (_recordedKeyframes is null)
                {
                    Console.WriteLine("Haven't recorded anything to play back yet");
                    return;
                }
                RealPlayer = new FlyThroughPlayer();
                RealPlayer.Keyframes = _recordedKeyframes;
                RealPlayer.Playing = true;
                Play.Text = "Stop playback";
                camera.Children.Add(RealPlayer);
                camera.Children.FirstOfType<NoclipController>()?.Destroy();
            }
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