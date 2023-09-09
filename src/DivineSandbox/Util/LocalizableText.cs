using System;
using System.Diagnostics;
using Terraria.Localization;

namespace DivineSandbox.Util;

public enum LocalizableTextType {
    Literal,
    Key,
    LocalizedText,
}

public readonly struct LocalizableText {
    private readonly object rawValue;

    public LocalizableTextType TextType { get; }

    public object[] Args { get; }

    private LocalizableText(object value, LocalizableTextType textType, params object[] args) {
        rawValue = value;
        TextType = textType;
        Args = args;
    }

    public override string ToString() {
        var value = TextType switch {
            LocalizableTextType.Literal => string.Format(Expect<string>(rawValue), Args),
            LocalizableTextType.Key => Language.GetTextValue(Expect<string>(rawValue), Args),
            LocalizableTextType.LocalizedText => Expect<LocalizedText>(rawValue).Format(Args),
            _ => throw new ArgumentOutOfRangeException(nameof(TextType)),
        };
        return MaskWhenNotYetLoaded(rawValue, TextType, value);
    }

    private static string MaskWhenNotYetLoaded(object rawValue, LocalizableTextType textType, string value) {
        const string not_yet_loaded_msg = "Localization not yet loaded...";

        switch (textType) {
            case LocalizableTextType.Literal: {
                return value;
            }

            case LocalizableTextType.Key: {
                var str = Expect<string>(rawValue);
                return string.IsNullOrWhiteSpace(value) || str == value ? not_yet_loaded_msg : value;
            }

            case LocalizableTextType.LocalizedText: {
                var txt = Expect<LocalizedText>(rawValue);
                return string.IsNullOrWhiteSpace(value) || txt.Key == value ? not_yet_loaded_msg : value;
            }

            default:
                throw new ArgumentOutOfRangeException(nameof(textType), textType, null);
        }
    }

    [StackTraceHidden]
    private static T Expect<T>(object value) {
        if (value is T t)
            return t;

        throw new InvalidCastException($"Expected {typeof(T).Name} but got {value.GetType().Name}");
    }

    public static LocalizableText FromLiteral(string text, params object[] args) => new(text, LocalizableTextType.Literal, args);

    public static LocalizableText FromKey(string key, params object[] args) => new(key, LocalizableTextType.Key, args);

    public static LocalizableText FromLocalizedText(LocalizedText text, params object[] args) => new(text, LocalizableTextType.LocalizedText, args);
}
