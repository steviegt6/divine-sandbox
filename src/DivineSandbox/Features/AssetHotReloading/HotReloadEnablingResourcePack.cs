using System.IO;
using System.Linq;
using AomojiResourcePacks.API;
using JetBrains.Annotations;
using ReLogic.Content.Sources;
using Terraria;
using Terraria.ModLoader;

namespace DivineSandbox.Features.AssetHotReloading;

/// <summary>
///     A modded resource pack which enables asset hot reloading.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class HotReloadEnablingResourcePack : ModResourcePack {
    public override string RootPath => "Assets/ResourcePacks/AssetHotReloading";

    public override bool ForceEnabled => true;

    public override IContentSource MakeContentSource() {
        var modSourcesDir = Path.Combine(Program.SavePathShared, "ModSources");
        return new HotReloadAwareContentSource(new DirectoryInfo(modSourcesDir).EnumerateDirectories().Select(x => new HotReloadableMod(x.FullName)));
    }
}
