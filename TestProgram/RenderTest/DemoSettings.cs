using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

    public DemoSettings(RenderDemo demo, Vector2i mainWindowSize)
    {
        _demo = demo;

        Scene sc = Scene.CreateEmpty();
        sc.Components.Add(new UpdateHandler());
        sc.Renderers.Add(new UIRenderer());
        sc.CheckCachedComponents();
        sc.Children.Add(this);

        var windowSize = new Vector2i(300, 200);
        _window = Window.Create(new(windowSize, Title: "Render demo settings", Border: OpenTK.Windowing.Common.WindowBorder.Fixed));
        _window.CenterWindow();
        _window.ClientLocation = new Vector2i(_window.ClientLocation.X + mainWindowSize.X/2 + windowSize.X/2 + 30, _window.ClientLocation.Y);
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
            var debugRenderer = (DebugRenderer)demo.Scene!.SceneRenderer!;
            switch(dirSwitch)
            {
                case 0:
                dirLightSwitch.Text = "Directional light: 3 cascades (default)";
                debugRenderer.UseInfiniteShadowMap = false;
                demo.SkyLight.Cascades = null;
                break;

                case 1: 
                dirLightSwitch.Text = "Directional light: map+ 3 cascades";
                debugRenderer.UseInfiniteShadowMap = true;
                demo.SkyLight.Cascades = null;
                break;

                case 2:
                dirLightSwitch.Text = "Directional light: Shadow casting off";
                demo.SkyLight.CastsShadows = false;
                break;

                case 3:
                dirLightSwitch.Text = "Directional light: 1 cascade";
                debugRenderer.UseInfiniteShadowMap = false;
                demo.SkyLight.CastsShadows = true;
                demo.SkyLight.Cascades = [32f];
                break;

                case 4: 
                dirLightSwitch.Text = "Directional light: map+ 1 cascade";
                debugRenderer.UseInfiniteShadowMap = true;
                demo.SkyLight.CastsShadows = true;
                demo.SkyLight.Cascades = [32f];
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
        TextButton megaInstanceSwitch = new TextButton("Many triangles: off");
        root.UIChildren.Add(megaInstanceSwitch);
        megaInstanceSwitch.Style = BasicStyle.DefaultStyle;
        megaInstanceSwitch.OnButtonReleased += _ =>
        {
            if (spheres.InScene)
            {
                spheres.Remove();
                megaInstanceSwitch.Text = "Many triangles: off";
            }
            else
            {
                demo.Children.Add(spheres);
                megaInstanceSwitch.Text = "Many triangles: on";
            }
        };
        SceneObject? manyCubes = null;
        TextButton cubeSwitch = new TextButton("Many draw calls: off");
        root.UIChildren.Add(cubeSwitch);
        cubeSwitch.Style = BasicStyle.DefaultStyle;
        cubeSwitch.OnButtonReleased += _ =>
        {
            if (manyCubes == null)
            {
                manyCubes = new ();
                Random rand = new(3621);
                Material cube = Material.Create(SlopperShader.Create(Asset.GetEngineAsset("shaders/phongShader.sesl")));
                for (int i = 0; i < 10000; i++)
                {
                    float t = i/100f;
                    float x = float.Cos(t);
                    float y = float.Sin(t);
                    x = -float.Abs(x);
                    float d = i/20f + 30;
                    manyCubes.Children.Add(new MeshRenderer
                    {
                        Material = cube,
                        Mesh = DefaultMeshes.Cube,
                        LocalPosition = new Vector3(x,0.5f,y) * d,
                        LocalRotation = Quaternion.FromEulerAngles(rand.NextSingle(), rand.NextSingle(), rand.NextSingle()),
                        LocalScale = new Vector3(d/30)
                    });
                }
            }
            if (manyCubes.InScene)
            {
                manyCubes.Remove();
                cubeSwitch.Text = "Many draw calls: off";
            }
            else
            {
                demo.Children.Add(manyCubes);
                cubeSwitch.Text = "Many draw calls: on";
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
        TextButton recordPerformance = new TextButton("Record and save performance");
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
                recordPerformance.Enabled = true;
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

        recordPerformance.Style = BasicStyle.DefaultStyle;
        recordPerformance.Enabled = _recordedKeyframes is not null;
        root.UIChildren.Add(recordPerformance);
        recordPerformance.OnButtonReleased += _ =>
        {
            if (_recordedKeyframes == null)
            {
                Console.WriteLine("Recorded keyframes were null!");
                return;
            }

            if (demo.Scene?.GetDataContainerEnumerable<Camera>().EnumerateReadonly().FirstOrDefault() is not Camera camera) return;
            if (camera.Children.FirstOfType<FlyThroughPlayer>() is FlyThroughPlayer realPlayer)
                realPlayer.Destroy();

            const int frequency = 60;
            
            realPlayer = new FlyThroughPlayer();
            FrametimeRecorder timeRecorder = new FrametimeRecorder((int)(_recordedKeyframes[_recordedKeyframes.Count-1].Time * frequency));
            realPlayer.Children.Add(timeRecorder);
            realPlayer.Keyframes = _recordedKeyframes;
            realPlayer.Playing = true;
            realPlayer.OverrideAnimationFrequency = frequency;
            camera.Children.Add(realPlayer);
            camera.Children.FirstOfType<NoclipController>()?.Destroy();
            recordPerformance.Enabled = false;
            saveFlythrough.Enabled = false;
            recordFlyThroughButton.Enabled = false;
            cubeSwitch.Enabled = false;
            playFlyThroughButton.Enabled = false;
            megaInstanceSwitch.Enabled = false;
            realPlayer.OnAnimationFinish += () =>
            {
                realPlayer.Destroy();
                camera.Children.Add(new NoclipController());

                recordPerformance.Enabled = true;
                saveFlythrough.Enabled = true;
                recordFlyThroughButton.Enabled = true;
                cubeSwitch.Enabled = true;
                playFlyThroughButton.Enabled = true;
                megaInstanceSwitch.Enabled = true;

                if (Asset.TryGetFile("RenderDemo/Frametimes.csv", out var file, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Write))
                {
                    timeRecorder.SaveFrametimes(file.Value);
                    System.Console.WriteLine("Saved delta times to 'TestProgram/AssetsRenderDemo/Frametimes.csv'");                    
                }
                else
                    Console.WriteLine("No permissions to save frame times :{"); 
            };
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

    private class FrametimeRecorder : SceneObject
    {
        float[] _deltaTimes;
        int _current;
        public FrametimeRecorder(int capacity)
        {
            _deltaTimes = new float[capacity];
        }
        [OnFrameUpdate]
        void OnUpdate(FrameUpdateArgs args)
        {
            if (_current < _deltaTimes.Length)
            _deltaTimes[_current] = args.DeltaTime;
            _current++;
        }

        public void SaveFrametimes(Asset csvFile)
        {
            if (!csvFile.CanWrite) throw new System.Exception("File cannot write!");
            
            using var stream = csvFile.GetStream();
            using var textStream = new StreamWriter(stream, Encoding.UTF8);
            
            textStream.WriteLine("delta time (ms)");
            foreach (var time in _deltaTimes)
                textStream.WriteLine((time*1000).ToString("0.00"));
        }
    }
}