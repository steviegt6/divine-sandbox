using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ReLogic.Content.Sources;
using Terraria;
using Terraria.Initializers;
using Terraria.ModLoader;

namespace DivineSandbox.Features.AssetHotReloading;

internal class HotReloadableMod {
    public string ModName { get; }

    public string ModDir { get; }

    public FileSystemWatcher Watcher { get; }

    public HotReloadableMod(string modDir) {
        ModName = Path.GetFileName(modDir);
        ModDir = modDir;
        Watcher = new FileSystemWatcher(modDir);
    }
}

internal sealed class HotReloadAwareContentSource : ContentSource {
    private readonly List<HotReloadableMod> mods = new();
    private readonly Dictionary<string, HotReloadableMod> modByAssetPath = new();

    public HotReloadAwareContentSource(IEnumerable<HotReloadableMod> mods) {
        foreach (var mod in mods) {
            if (ModLoader.HasMod(mod.ModName))
                this.mods.Add(mod);

            mod.Watcher.NotifyFilter = NotifyFilters.Attributes
                                     | NotifyFilters.Security
                                     | NotifyFilters.Size
                                     | NotifyFilters.CreationTime
                                     | NotifyFilters.DirectoryName
                                     | NotifyFilters.FileName
                                     | NotifyFilters.LastAccess
                                     | NotifyFilters.LastWrite;
            mod.Watcher.IncludeSubdirectories = true;
            mod.Watcher.EnableRaisingEvents = true;
            mod.Watcher.Changed += OnFileChanged(mod);
            mod.Watcher.Created += OnFileCreated(mod);
            mod.Watcher.Deleted += OnFileDeleted(mod);
            mod.Watcher.Renamed += OnFileRenamed(mod);
        }

        assetPaths = Array.Empty<string>();
    }

    public override Stream OpenStream(string fullAssetName) {
        if (modByAssetPath.TryGetValue(NormalizePath(fullAssetName), out var mod))
            return File.OpenRead(Path.Combine(mod.ModDir, fullAssetName));

        throw new FileNotFoundException($"Could not find asset '{fullAssetName}'");
    }

    private FileSystemEventHandler OnFileChanged(HotReloadableMod mod) {
        return (_, args) =>  {
            if (args.FullPath.Contains(".git"))
                return;

            var relativePath = NormalizePath(args.FullPath[mod.ModDir.Length..]);

            assetPaths = assetPaths.Where(x => x != relativePath).ToArray();
            assetPaths = assetPaths.Append(relativePath).ToArray();
            SetAssetNames(assetPaths);

            modByAssetPath[relativePath] = mod;

            OnCommon();
        };
    }

    private FileSystemEventHandler OnFileCreated(HotReloadableMod mod) {
        return (_, args) =>  {
            if (args.FullPath.Contains(".git"))
                return;

            var relativePath = NormalizePath(args.FullPath[mod.ModDir.Length..]);

            assetPaths = assetPaths.Where(x => x != relativePath).ToArray();
            assetPaths = assetPaths.Append(relativePath).ToArray();
            SetAssetNames(assetPaths);

            modByAssetPath[relativePath] = mod;

            OnCommon();
        };
    }

    private FileSystemEventHandler OnFileDeleted(HotReloadableMod mod) {
        return (_, args) =>  {
            if (args.FullPath.Contains(".git"))
                return;

            var relativePath = NormalizePath(args.FullPath[mod.ModDir.Length..]);

            // if (assetPaths.Contains(relativePath)) {
            assetPaths = assetPaths.Where(x => x != relativePath).ToArray();
            SetAssetNames(assetPaths);
            // }

            modByAssetPath.Remove(relativePath);

            OnCommon();
        };
    }

    private RenamedEventHandler OnFileRenamed(HotReloadableMod mod) {
        return (_, args) =>  {
            if (args.FullPath.Contains(".git"))
                return;

            var relativeOldPath = NormalizePath(args.OldFullPath[mod.ModDir.Length..]);
            var relativePath = NormalizePath(args.FullPath[mod.ModDir.Length..]);

            assetPaths = assetPaths.Where(x => x != relativeOldPath).ToArray();
            assetPaths = assetPaths.Where(x => x != relativePath).ToArray();
            assetPaths = assetPaths.Append(relativePath).ToArray();
            SetAssetNames(assetPaths);

            modByAssetPath.Remove(relativeOldPath);
            modByAssetPath[relativePath] = mod;

            OnCommon();
        };
    }

    private static void OnCommon() {
        TriggerResourcePackUpdate();
    }

    private static void TriggerResourcePackUpdate() {
        Main.QueueMainThreadAction(() => {
            Main.AssetSourceController.UseResourcePacks(AssetInitializer.CreateResourcePackList(Main.instance.Services));
        });
    }

    private static string NormalizePath(string path) {
        path = path.StartsWith('\\') || path.StartsWith('/') ? path[1..] : path;
        return path.Replace('\\', '/');
    }
}
