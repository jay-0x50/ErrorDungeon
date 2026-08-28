#if UNITY_EDITOR
using System;
using System.IO;
using System.Threading.Tasks;
#if UNITY_MCP_READY
using static com.IvanMurzak.McpPlugin.Common.Consts.MCP.Server;
using com.IvanMurzak.Unity.MCP;
using com.IvanMurzak.Unity.MCP.Editor;
#endif
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ErrorDungeon.Editor
{
#if UNITY_MCP_READY
    [InitializeOnLoad]
    internal static class ErrorDungeonMcpBatchBootstrap
    {
        static ErrorDungeonMcpBatchBootstrap()
        {
            if (Array.Exists(Environment.GetCommandLineArgs(), value => value == "-errorDungeonSetupMcp"))
                EditorApplication.delayCall += ErrorDungeonBuild.SetupUnityMcpBatch;
        }
    }
#endif

    public static class ErrorDungeonBuild
    {
        private const string ScenePath = "Assets/ErrorDungeon/Scenes/Main.unity";

        [MenuItem("Error Dungeon/Build/Linux Client")]
        public static void BuildLinuxClient() => Build("Builds/LinuxClient/ErrorDungeon.x86_64", false);

        [MenuItem("Error Dungeon/Build/Linux Dedicated Server")]
        public static void BuildLinuxServer() => Build("Builds/LinuxServer/ErrorDungeonServer.x86_64", true);

        [MenuItem("Error Dungeon/Build/WebGL Website")]
        public static void BuildWebGL()
        {
            EnsureScene();
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.template = "PROJECT:ErrorDungeon";
            Directory.CreateDirectory("Builds/WebGL");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/WebGL",
                target = BuildTarget.WebGL,
                options = BuildOptions.CleanBuildCache
            });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new BuildFailedException($"Build failed: {report.summary.result}");
        }

        public static void BuildLinuxClientBatch() => BuildLinuxClient();
        public static void BuildLinuxServerBatch() => BuildLinuxServer();
        public static void BuildWebGLBatch() => BuildWebGL();

        public static void VerifyGroundCollisionBatch()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0f, -1f, 0f);
            floor.transform.localScale = new Vector3(115f, 2f, 115f);
            floor.isStatic = true;

            var actor = new GameObject("GroundCollisionProbe");
            actor.transform.position = new Vector3(0f, 1.35f, -30f);
            var controller = actor.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.38f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.minMoveDistance = 0f;
            Physics.SyncTransforms();

            var velocity = 0f;
            const float deltaTime = 1f / 60f;
            for (var i = 0; i < 240; i++)
            {
                if (controller.isGrounded && velocity < 0f) velocity = -2f;
                velocity = Mathf.Max(velocity - 28f * deltaTime, -42f);
                controller.Move(Vector3.up * (velocity * deltaTime));
            }

            var finalY = actor.transform.position.y;
            UnityEngine.Object.DestroyImmediate(actor);
            UnityEngine.Object.DestroyImmediate(floor);
            if (finalY < -0.1f || finalY > 0.3f)
                throw new BuildFailedException($"Ground collision verification failed. finalY={finalY:0.000}");
            Debug.Log($"GROUND_COLLISION_OK finalY={finalY:0.000}");
        }

#if UNITY_MCP_READY
        public static async void SetupUnityMcpBatch()
        {
            try
            {
                UnityMcpPluginEditor.ConnectionMode = ConnectionMode.Custom;
                UnityMcpPluginEditor.LocalHost = UnityMcpPlugin.UnityConnectionConfig.DefaultHost;
                UnityMcpPluginEditor.TransportMethod = TransportMethod.stdio;
                UnityMcpPluginEditor.KeepConnected = true;
                UnityMcpPluginEditor.KeepServerRunning = false;
                UnityMcpPluginEditor.Instance.Save(false);

                var deadline = DateTime.UtcNow.AddMinutes(3);
                while (!McpServerManager.IsBinaryReadyToStart() && DateTime.UtcNow < deadline)
                {
                    await McpServerManager.DownloadServerBinaryIfNeeded(unattended: true);
                    if (!McpServerManager.IsBinaryReadyToStart()) await Task.Delay(500);
                }

                if (!McpServerManager.IsBinaryReadyToStart())
                    throw new BuildFailedException("Unity MCP server binary download timed out.");

                Debug.Log($"UNITY_MCP_READY binary={McpServerManager.ExecutableFullPath} port={UnityMcpPluginEditor.Port}");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
#endif

        private static void Build(string output, bool server)
        {
            EnsureScene();
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.CleanBuildCache,
                subtarget = server ? (int)StandaloneBuildSubtarget.Server : (int)StandaloneBuildSubtarget.Player
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new BuildFailedException($"Build failed: {report.summary.result}");
        }

        private static void EnsureScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            if (File.Exists(ScenePath)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
#endif
