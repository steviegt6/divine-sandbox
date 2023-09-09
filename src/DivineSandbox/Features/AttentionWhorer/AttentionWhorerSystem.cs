using System.Reflection;
using JetBrains.Annotations;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;

namespace DivineSandbox.Features.AttentionWhorer;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class AttentionWhorerSystem : ModSystem {
    private ILHook? uiModItemOnInitializeHook;

    public override void Load() {
        base.Load();

        var uiModItemType = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.UIModItem")!;
        var onInitializeMethod = uiModItemType.GetMethod("OnInitialize", BindingFlags.Public | BindingFlags.Instance)!;
        uiModItemOnInitializeHook = new ILHook(onInitializeMethod, EditUiModItemPresentationToBeObnoxiouslyAttentionGrabbing);
    }

    public override void Unload() {
        base.Unload();

        uiModItemOnInitializeHook?.Dispose();
    }

    private static void EditUiModItemPresentationToBeObnoxiouslyAttentionGrabbing(ILContext il) {
        var c = new ILCursor(il);

        var uiModItemType = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.UIModItem")!;
        var modIconField = uiModItemType.GetField("_modIcon", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var modNameProperty = uiModItemType.GetProperty("ModName", BindingFlags.Public | BindingFlags.Instance)!;
        c.GotoNext(MoveType.After, x => x.MatchLdfld(modIconField));
        c.Emit(OpCodes.Callvirt, modNameProperty);
        c.EmitDelegate((UIImage modIconImage, string modName) => {
            if (modName == "DivineSandbox")
                return new UIImage(TextureAssets.MagicPixel);

            return modIconImage;
        });
        c.Emit(OpCodes.Stfld, modIconField);
    }
}
