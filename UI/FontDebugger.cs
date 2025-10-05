namespace Peridot.UI;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>
/// Utility class for diagnosing spritefont rendering issues including kerning problems.
/// </summary>
public static class FontDebugger
{
    /// <summary>
    /// Analyzes a SpriteFont for potential rendering issues and returns diagnostic information.
    /// </summary>
    public static FontDiagnostics AnalyzeFont(SpriteFont font)
    {
        var diagnostics = new FontDiagnostics();
        
        if (font == null)
        {
            diagnostics.Errors.Add("Font is null");
            return diagnostics;
        }

        // Basic font information
        diagnostics.LineSpacing = font.LineSpacing;
        diagnostics.Spacing = font.Spacing;
        diagnostics.DefaultCharacter = font.DefaultCharacter;
        diagnostics.CharacterCount = font.Characters.Count;
        diagnostics.AvailableCharacters = font.Characters.ToList();

        // Test common problematic characters
        var testCharacters = new char[] { 'i', 'l', 'I', 'j', 'W', 'M', 'f', 'r', 'V', 'A', 'T', ' ' };
        foreach (char c in testCharacters)
        {
            if (font.Characters.Contains(c))
            {
                var measurement = font.MeasureString(c.ToString());
                diagnostics.CharacterWidths[c] = measurement.X;
            }
            else
            {
                diagnostics.MissingCharacters.Add(c);
            }
        }

        // Test kerning pairs that commonly cause issues
        var kerningTestPairs = new string[]
        {
            "AV", "Ta", "To", "Tr", "Tu", "Tw", "Ty", "Ya", "Ye", "Yo", "Yu",
            "PA", "VA", "WA", "Wa", "We", "Wi", "Wo", "Wu", "FA", "LT", "LY",
            "ff", "fi", "fl", "ffi", "ffl"
        };

        foreach (string pair in kerningTestPairs)
        {
            if (pair.All(c => font.Characters.Contains(c)))
            {
                var pairWidth = font.MeasureString(pair).X;
                var individualWidths = pair.Sum(c => font.MeasureString(c.ToString()).X);
                var kerningOffset = pairWidth - individualWidths;
                
                if (Math.Abs(kerningOffset) > 0.1f) // Only report if there's significant kerning
                {
                    diagnostics.KerningPairs[pair] = kerningOffset;
                }
            }
        }

        // Check for potential spacing issues
        CheckSpacingIssues(font, diagnostics);

        // Test Unicode character handling
        TestUnicodeHandling(font, diagnostics);

        return diagnostics;
    }

    private static void CheckSpacingIssues(SpriteFont font, FontDiagnostics diagnostics)
    {
        // Check if space character has reasonable width
        if (font.Characters.Contains(' '))
        {
            var spaceWidth = font.MeasureString(" ").X;
            var averageCharWidth = font.Characters.Where(c => c != ' ')
                .Take(10)
                .Average(c => font.MeasureString(c.ToString()).X);

            if (spaceWidth > averageCharWidth * 2)
            {
                diagnostics.Warnings.Add($"Space character appears too wide: {spaceWidth}px vs average {averageCharWidth:F1}px");
            }
            else if (spaceWidth < averageCharWidth * 0.1f)
            {
                diagnostics.Warnings.Add($"Space character appears too narrow: {spaceWidth}px vs average {averageCharWidth:F1}px");
            }
        }

        // Check for characters with zero or negative width
        foreach (char c in font.Characters)
        {
            var width = font.MeasureString(c.ToString()).X;
            if (width <= 0)
            {
                diagnostics.Warnings.Add($"Character '{c}' has zero or negative width: {width}");
            }
        }
    }

    private static void TestUnicodeHandling(SpriteFont font, FontDiagnostics diagnostics)
    {
        var problematicUnicodeChars = new char[]
        {
            '\u2013', '\u2014', // En dash, Em dash
            '\u2018', '\u2019', // Left/right single quotation mark
            '\u201C', '\u201D', // Left/right double quotation mark
            '\u2022', // Bullet point
            '\u2026', // Horizontal ellipsis
        };

        foreach (char c in problematicUnicodeChars)
        {
            if (!font.Characters.Contains(c))
            {
                diagnostics.MissingUnicodeCharacters.Add(c);
            }
        }

        if (diagnostics.MissingUnicodeCharacters.Count > 0 && font.DefaultCharacter == null)
        {
            diagnostics.Warnings.Add("Font is missing common Unicode characters and has no default character defined");
        }
    }

    /// <summary>
    /// Renders a comprehensive font test pattern to help visualize rendering issues.
    /// </summary>
    public static void DrawFontTest(SpriteBatch spriteBatch, SpriteFont font, Vector2 position, Color color, float layerDepth = 0f)
    {
        if (font == null || spriteBatch == null)
            return;

        var startPosition = position;
        var lineHeight = font.LineSpacing;
        var currentY = position.Y;

        // Test 1: Basic alphabet
        var alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ\nabcdefghijklmnopqrstuvwxyz";
        spriteBatch.DrawString(font, $"Font Test - Alphabet:", new Vector2(position.X, currentY), color, 0, Vector2.Zero, 1f, SpriteEffects.None, layerDepth);
        currentY += lineHeight;
        spriteBatch.DrawString(font, alphabet, new Vector2(position.X, currentY), color, 0, Vector2.Zero, 1f, SpriteEffects.None, layerDepth);
        currentY += lineHeight * 3;

        // Test 2: Numbers and symbols
        var numbersSymbols = "0123456789 !@#$%^&*()_+-=[]{}|;':\",./<>?";
        spriteBatch.DrawString(font, "Numbers & Symbols:", new Vector2(position.X, currentY), color, 0, Vector2.Zero, 1f, SpriteEffects.None, layerDepth);
        currentY += lineHeight;
        spriteBatch.DrawString(font, numbersSymbols, new Vector2(position.X, currentY), color, 0, Vector2.Zero, 1f, SpriteEffects.None, layerDepth);
        currentY += lineHeight * 2;

        // Test 3: Kerning pairs
        var kerningTest = "AV Ta To Tr Tu Tw Ty Ya Ye Yo Yu\nPA VA WA Wa We Wi Wo Wu FA LT LY\nffi ffl ff fi fl";
        spriteBatch.DrawString(font, "Kerning Test Pairs:", new Vector2(position.X, currentY), color, 0, Vector2.Zero, 1f, SpriteEffects.None, layerDepth);
        currentY += lineHeight;
        spriteBatch.DrawString(font, kerningTest, new Vector2(position.X, currentY), color, 0, Vector2.Zero, 1f, SpriteEffects.None, layerDepth);
        currentY += lineHeight * 4;

        // Test 4: Spacing test
        var spacingTest = "i i i i i | l l l l l | W W W W W | . . . . .";
        spriteBatch.DrawString(font, "Spacing Test:", new Vector2(position.X, currentY), color, 0, Vector2.Zero, 1f, SpriteEffects.None, layerDepth);
        currentY += lineHeight;
        spriteBatch.DrawString(font, spacingTest, new Vector2(position.X, currentY), color, 0, Vector2.Zero, 1f, SpriteEffects.None, layerDepth);
        currentY += lineHeight * 2;

        // Test 5: Size comparison
        var sizeTest = "The quick brown fox jumps over the lazy dog.";
        spriteBatch.DrawString(font, "Size Test - Normal:", new Vector2(position.X, currentY), color, 0, Vector2.Zero, 1f, SpriteEffects.None, layerDepth);
        currentY += lineHeight;
        spriteBatch.DrawString(font, sizeTest, new Vector2(position.X, currentY), color, 0, Vector2.Zero, 1f, SpriteEffects.None, layerDepth);
        currentY += lineHeight;
        spriteBatch.DrawString(font, "Size Test - 0.5x Scale:", new Vector2(position.X, currentY), color, 0, Vector2.Zero, 0.5f, SpriteEffects.None, layerDepth);
        currentY += lineHeight * 0.5f;
        spriteBatch.DrawString(font, sizeTest, new Vector2(position.X, currentY), color, 0, Vector2.Zero, 0.5f, SpriteEffects.None, layerDepth);
        currentY += lineHeight * 0.5f;
        spriteBatch.DrawString(font, "Size Test - 2x Scale:", new Vector2(position.X, currentY), color, 0, Vector2.Zero, 1f, SpriteEffects.None, layerDepth);
        currentY += lineHeight;
        spriteBatch.DrawString(font, sizeTest, new Vector2(position.X, currentY), color, 0, Vector2.Zero, 2f, SpriteEffects.None, layerDepth);
    }

    /// <summary>
    /// Creates a diagnostic report string from font analysis.
    /// </summary>
    public static string GenerateReport(FontDiagnostics diagnostics)
    {
        if (diagnostics == null)
            return "No diagnostics available";

        var report = new System.Text.StringBuilder();
        
        report.AppendLine("=== FONT DIAGNOSTICS REPORT ===");
        report.AppendLine();

        // Basic info
        report.AppendLine("BASIC INFORMATION:");
        report.AppendLine($"  Line Spacing: {diagnostics.LineSpacing}");
        report.AppendLine($"  Character Spacing: {diagnostics.Spacing}");
        report.AppendLine($"  Default Character: {(diagnostics.DefaultCharacter?.ToString() ?? "None")}");
        report.AppendLine($"  Character Count: {diagnostics.CharacterCount}");
        report.AppendLine();

        // Errors
        if (diagnostics.Errors.Count > 0)
        {
            report.AppendLine("ERRORS:");
            foreach (var error in diagnostics.Errors)
            {
                report.AppendLine($"  ❌ {error}");
            }
            report.AppendLine();
        }

        // Warnings
        if (diagnostics.Warnings.Count > 0)
        {
            report.AppendLine("WARNINGS:");
            foreach (var warning in diagnostics.Warnings)
            {
                report.AppendLine($"  ⚠️  {warning}");
            }
            report.AppendLine();
        }

        // Missing characters
        if (diagnostics.MissingCharacters.Count > 0)
        {
            report.AppendLine("MISSING TEST CHARACTERS:");
            report.AppendLine($"  {string.Join(", ", diagnostics.MissingCharacters.Select(c => $"'{c}'"))}");
            report.AppendLine();
        }

        // Missing Unicode characters
        if (diagnostics.MissingUnicodeCharacters.Count > 0)
        {
            report.AppendLine("MISSING UNICODE CHARACTERS:");
            foreach (var c in diagnostics.MissingUnicodeCharacters)
            {
                report.AppendLine($"  U+{((int)c):X4} ({c})");
            }
            report.AppendLine();
        }

        // Kerning pairs
        if (diagnostics.KerningPairs.Count > 0)
        {
            report.AppendLine("KERNING PAIRS:");
            foreach (var kvp in diagnostics.KerningPairs.OrderBy(x => x.Key))
            {
                var direction = kvp.Value > 0 ? "wider" : "narrower";
                report.AppendLine($"  '{kvp.Key}': {kvp.Value:F2}px {direction}");
            }
            report.AppendLine();
        }

        // Character widths for key characters
        if (diagnostics.CharacterWidths.Count > 0)
        {
            report.AppendLine("KEY CHARACTER WIDTHS:");
            foreach (var kvp in diagnostics.CharacterWidths.OrderBy(x => x.Key))
            {
                report.AppendLine($"  '{kvp.Key}': {kvp.Value:F2}px");
            }
        }

        return report.ToString();
    }

    /// <summary>
    /// Container for font diagnostic information.
    /// </summary>
    public class FontDiagnostics
    {
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<char> MissingCharacters { get; set; } = new List<char>();
        public List<char> MissingUnicodeCharacters { get; set; } = new List<char>();
        public Dictionary<string, float> KerningPairs { get; set; } = new Dictionary<string, float>();
        public Dictionary<char, float> CharacterWidths { get; set; } = new Dictionary<char, float>();
        public List<char> AvailableCharacters { get; set; } = new List<char>();
        
        public int LineSpacing { get; set; }
        public float Spacing { get; set; }
        public char? DefaultCharacter { get; set; }
        public int CharacterCount { get; set; }
    }
}