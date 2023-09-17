using System.Linq;
using DivineSandbox.Util;
using DivineSandbox.Util.Reflection;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace DivineSandbox.Features.MenuIdDebugger;

/// <summary>
///     Displays the current menu ID in the top-left corner of the screen, as
///     well as the UI state if applicable.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class MenuIdDebuggingSystem : ModSystem {
    private const string menu_id_key = "Mods.DivineSandbox.UI.MenuId";
    private const string menu_id_with_known_name_key = "Mods.DivineSandbox.UI.MenuIdWithKnownName";
    private const string menu_ui_info_key = "Mods.DivineSandbox.UI.MenuUiInfo";

    private ILHook? terrariaOverhaulDrawOverlayEdit;

    public override void Load() {
        base.Load();

        On_Main.DrawVersionNumber += DrawMenuId;

        if (!ModLoader.TryGetMod("TerrariaOverhaul", out var terrariaOverhaul))
            return;

        if (!terrariaOverhaul.Code.TryGetType("TerrariaOverhaul.Common.MainMenuOverlays.MainMenuOverlaySystem", out var mainMenuOverlaySystem)) {
            Mod.Logger.Warn("Failed to find type: TerrariaOverhaul.Common.MainMenuOverlays.MainMenuOverlaySystem");
            return;
        }

        if (!mainMenuOverlaySystem.TryGetMethod("DrawOverlay", null, out var drawOverlayMethod)) {
            Mod.Logger.Warn("Failed to find method: TerrariaOverhaul.Common.MainMenuOverlays.MainMenuOverlaySystem::DrawOverlay");
            return;
        }

        terrariaOverhaulDrawOverlayEdit = new ILHook(drawOverlayMethod, DrawOverlayEdit);
    }

    public override void Unload() {
        base.Unload();

        terrariaOverhaulDrawOverlayEdit?.Dispose();
    }

    private static void DrawMenuId(On_Main.orig_DrawVersionNumber orig, Color menuColor, float upBump) {
        orig(menuColor, upBump);

        var matches = typeof(MenuID).GetFields().Where(x => x is { IsLiteral: true, IsStatic: true } && x.GetValue(null) is int value && value == Main.menuMode).ToList();
        var menuIdText = LocalizableText.FromKey(matches.Count != 1 ? menu_id_key : menu_id_with_known_name_key, Main.menuMode, matches[0].Name);
        var menuUiInfoText = LocalizableText.FromKey(menu_ui_info_key, Main.MenuUI.CurrentState?.GetType().FullName ?? "<null>", Main.MenuUI.IsVisible);

        var origin = FontAssets.MouseText.Value.MeasureString(menuIdText.ToString());
        origin.X *= 0.5f;

        for (var i = 0; i < 5; i++) {
            var color = Color.Black;

            if (i == 4) {
                color = menuColor;
                color.R = (byte)((255 + color.R) / 2);
                color.G = (byte)((255 + color.G) / 2);
                color.B = (byte)((255 + color.B) / 2);
            }

            color.A = (byte)(color.A * 0.3f);

            var xOffset = 0;
            var yOffset = 0;

            switch (i) {
                case 0:
                    xOffset = -2;
                    break;

                case 1:
                    xOffset = 2;
                    break;

                case 2:
                    yOffset = -2;
                    break;

                case 3:
                    yOffset = 2;
                    break;
            }

            Main.spriteBatch.DrawString(FontAssets.MouseText.Value, menuIdText.ToString(), new Vector2(origin.X + xOffset + 10f, origin.Y + yOffset + 8f), color, 0f, origin, 1f, SpriteEffects.None, 0f);
            Main.spriteBatch.DrawString(FontAssets.MouseText.Value, menuUiInfoText.ToString(), new Vector2(origin.X + xOffset + 10f, origin.Y * 2 + yOffset + 8f), color, 0f, origin, 1f, SpriteEffects.None, 0f);
        }
    }

    private static void DrawOverlayEdit(ILContext il) {
        var c = new ILCursor(il);

        c.GotoNext(MoveType.After, x => x.MatchLdcR4(16), x => x.MatchLdcR4(16));
        c.Emit(OpCodes.Ldc_R4, 4f);
        c.Emit(OpCodes.Mul);
    }
}
