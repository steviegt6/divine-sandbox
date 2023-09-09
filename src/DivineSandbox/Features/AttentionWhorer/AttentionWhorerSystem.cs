using System;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace DivineSandbox.Features.AttentionWhorer;

/// <summary>
///     Makes edits to the Mod List UI to obnoxiously bring attention to our mod
///     through silly graphical changes.
///     <br />
///     Animates the mod icon, rainbowifies the mod name, and animates the mod
///     name to appear three-dimensional.
/// </summary>
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
internal sealed class AttentionWhorerSystem : ModSystem {
    private sealed class UiAnimatedImageAlwaysHovering : UIElement {
        private readonly Asset<Texture2D> texture;
        private readonly int countY;
        private int tickCounter;
        private int frameCounter;

        public int FrameCount { get; set; } = 1;

        public int TicksPerFrame { get; set; } = 5;

        private int DrawHeight => (int)Height.Pixels;

        private int DrawWidth => (int)Width.Pixels;

        public UiAnimatedImageAlwaysHovering(Asset<Texture2D> texture, int width, int height, int countY) {
            this.texture = texture;
            this.countY = countY;
            Width.Pixels = width;
            Height.Pixels = height;
        }

        private Rectangle FrameToRect(int frame) {
            var horizIndex = frame / countY;
            var vertIndex = frame % countY;
            return new Rectangle(horizIndex * DrawWidth, vertIndex * DrawHeight, DrawWidth, DrawHeight);
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            if (++tickCounter < TicksPerFrame)
                return;

            tickCounter = 0;
            if (++frameCounter < FrameCount)
                return;

            frameCounter = 0;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch) {
            var dims = GetDimensions();
            var frame = frameCounter % FrameCount;
            spriteBatch.Draw(texture.Value, dims.ToRectangle(), FrameToRect(frame), Color.White);
        }
    }

    private sealed class UiTextBabiesFirstYoutubeVideo : UIText {
        public UiTextBabiesFirstYoutubeVideo(string text, float textScale = 1, bool large = false) : base(text, textScale, large) { }

        protected override void DrawSelf(SpriteBatch spriteBatch) {
            var realText = Text;
            var realTextOriginX = TextOriginX;

            SetText(RainbowifyText(realText));
            var textSize = FontAssets.MouseText.Value.MeasureString(realText);
            TextOriginX = textSize.X * 0.5f;
            base.DrawSelf(spriteBatch);

            TextOriginX = realTextOriginX;
            SetText(realText);
        }

        private static string RainbowifyText(string text) {
            var sb = new StringBuilder(text.Length * 12);

            var frame = Main.GlobalTimeWrappedHourly * 0.1f;

            for (var i = 0; i < text.Length; i++) {
                var c = text[i];
                var color = Main.hslToRgb((frame + i * 0.1f) % 1f, 1f, 0.5f);
                sb.Append($"[c/{color.R:X2}{color.G:X2}{color.B:X2}:{c}]");
            }

            return sb.ToString();
        }
    }

    private ILHook? uiModItemOnInitializeHook;

    public override void Load() {
        base.Load();

        var uiModItemType = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.UI.UIModItem")!;
        var onInitializeMethod = uiModItemType.GetMethod("OnInitialize", BindingFlags.Public | BindingFlags.Instance)!;
        uiModItemOnInitializeHook = new ILHook(onInitializeMethod, EditUiModItemPresentationToBeObnoxiouslyAttentionGrabbing);
        
        IL_UIText.DrawSelf += CreateVector2Scale;
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
        c.GotoNext(MoveType.Before, x => x.MatchStfld(modIconField));
        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Callvirt, modNameProperty.GetMethod!);
        c.EmitDelegate((UIImage modIconImage, string modName) => {
            if (modName == "DivineSandbox") {
                return new UiAnimatedImageAlwaysHovering(ModContent.Request<Texture2D>("DivineSandbox/Assets/Images/FramedIcon"), 80, 80, 31) {
                    Left = { Percent = 0f },
                    Top = { Percent = 0f },
                    Width = { Pixels = 80 },
                    Height = { Pixels = 80 },
                    TicksPerFrame = 4,
                    FrameCount = 32,
                };
            }

            return (UIElement)modIconImage;
        });

        var modNameField = uiModItemType.GetField("_modName", BindingFlags.NonPublic | BindingFlags.Instance)!;
        c.GotoNext(MoveType.Before, x => x.MatchStfld(modNameField));
        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Callvirt, modNameProperty.GetMethod!);
        c.EmitDelegate((UIText uiModName, string modName) => {
            if (modName == "DivineSandbox") {
                return new UiTextBabiesFirstYoutubeVideo(uiModName.Text) {
                    Left = uiModName.Left,
                    Top = { Pixels = 5f },
                };
            }

            return (UIElement)uiModName;
        });
    }
    
    private static void CreateVector2Scale(ILContext il) {
        var c = new ILCursor(il);

        var positionIndex = 0;
        c.GotoNext(x => x.MatchLdflda<UIText>("_textSize"));
        c.GotoPrev(x => x.MatchLdloca(out positionIndex));

        c.GotoNext(x => x.MatchNewobj<Vector2>());
        c.GotoNext(MoveType.After, x => x.MatchCall(out _));
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((Vector2 origin, UIText text) => {
            if (text is UiTextBabiesFirstYoutubeVideo)
                return new Vector2(origin.X + text.TextOriginX, origin.Y);
            
            return origin;
        });

        var vector2Float32Ctor = typeof(Vector2).GetConstructor(new[] { typeof(float) })!;
        c.GotoNext(MoveType.After, x => x.MatchCall(vector2Float32Ctor));
        var index = c.Index;
        var vector2Index = 0;
        var floatIndex = 0;
        c.GotoPrev(x => x.MatchLdloc(out floatIndex));
        c.GotoPrev(x => x.MatchLdloca(out vector2Index));
        c.Index = index;

        c.Emit(OpCodes.Ldloc_S, (byte)floatIndex);
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((float scale, UIText text) => {
            if (text is UiTextBabiesFirstYoutubeVideo)
                return new Vector2(scale * MathF.Sin(Main.GlobalTimeWrappedHourly * 3f), scale);

            return new Vector2(scale);
        });
        c.Emit(OpCodes.Stloc, vector2Index);

        c.Emit(OpCodes.Ldloc_S, (byte)positionIndex);
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((Vector2 position, UIText text) => {
            if (text is UiTextBabiesFirstYoutubeVideo)
                return new Vector2(position.X + text.TextOriginX, position.Y);

            return position;
        });
        c.Emit(OpCodes.Stloc_S, (byte)positionIndex);
    }
}
