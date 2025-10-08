﻿using System;
using BepInEx.Configuration;

namespace Hide_and_PEAK.Configuration;

public class Option
{
    public enum OptionType { Bool, Int, String, InputAction, Colour, Float }
    
    public string Label {get; set;}
    public OptionType Type {get; set;}
    public ConfigEntry<bool> BoolEntry {get; set;}
    public ConfigEntry<int> IntEntry {get; set;}
    public ConfigEntry<string> StringEntry { get; set; }
    public int MinInt  {get; set;}
    public int MaxInt  {get; set;}
    public int Step {get; set;}
    public ConfigEntry<float> FloatEntry {get; set;}
    public float MinFloat {get; set;}
    public float MaxFloat {get; set;}
    public float StepFloat {get; set;}
    public Func<bool> IsDisabled { get; set; } = () => false;
    public Func<string> DisplayValue { get; set; } = () => "";

    private Option(string label, OptionType type)
    {
        Label = label;
        Type = type;
    }

    public static Option Bool(string label, ConfigEntry<bool> entry, Func<bool>? isDisabled = null)
    {
        return new Option(label, OptionType.Bool)
        {
            BoolEntry = entry,
            IsDisabled = isDisabled ?? (() => false),
            DisplayValue = () => entry.Value ? "ON" : "OFF"
        };
    }
    
    public static Option Colour(string label, ConfigEntry<int> rEntry, ConfigEntry<int> gEntry, ConfigEntry<int> bEntry, Func<bool>? isDisabled = null)
    {
        return new Option(label, OptionType.Colour)
        {
            IntEntry = rEntry, 
            DisplayValue = () => $"R:{rEntry.Value} G:{gEntry.Value} B:{bEntry.Value}",
            IsDisabled = isDisabled ?? (() => false),
            
        };
    }

    public static Option InputAction(string label, ConfigEntry<string> entry, Func<bool>? isDisabled = null)
    {
        return new Option(label, OptionType.InputAction)
        {
            StringEntry = entry,
            IsDisabled = isDisabled ?? (() => false),
            DisplayValue = () =>
            {
                string[] strings = entry.Value.Split("/");
                return strings.Length > 1 ? strings[^1].ToUpper() : entry.Value;
            }
        };
    }

    public static Option Int(string label, ConfigEntry<int> entry, int min, int max, int step = 1, Func<bool>? isDisabled = null)
    {
        return new Option(label, OptionType.Int)
        {
            IntEntry = entry,
            MinInt = min,
            MaxInt = max,
            Step = step,
            IsDisabled = isDisabled ?? (() => false),
            DisplayValue = () => entry.Value.ToString()
        };
    }
    
    public static Option Float(string label, ConfigEntry<float> entry, float min, float max, float step = 0.1f, Func<bool>? isDisabled = null)
    {
        return new Option(label, OptionType.Float)
        {
            FloatEntry = entry,
            MinFloat = min,
            MaxFloat = max,
            StepFloat = step,
            IsDisabled = isDisabled ?? (() => false),
            DisplayValue = () => entry.Value.ToString()
        };
    }
}