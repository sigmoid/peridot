using System;
using Peridot.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Peridot.UI
{
    /// <summary>
    /// Simple test to demonstrate the recursive search functionality
    /// </summary>
    public class SearchTest
    {
        public static void RunSearchTest(SpriteFont font)
        {
            Console.WriteLine("=== UI Element Search Test ===");
            
            // Create a test hierarchy manually
            var rootCanvas = new Canvas(new Rectangle(0, 0, 800, 600));
            rootCanvas.Name = "rootCanvas";
            
            var mainLayout = new VerticalLayoutGroup(new Rectangle(50, 50, 700, 500), 10);
            mainLayout.Name = "mainLayout";
            
            var titleLabel = new Label(new Rectangle(0, 0, 300, 50), "Test Title", font, Color.White);
            titleLabel.Name = "titleLabel";
            
            var buttonContainer = new HorizontalLayoutGroup(new Rectangle(0, 0, 600, 100), 15);
            buttonContainer.Name = "buttonContainer";
            
            var button1 = new Button(new Rectangle(0, 0, 100, 40), "Button 1", font, Color.Blue, Color.LightBlue, Color.White, () => Console.WriteLine("Button 1 clicked"));
            button1.Name = "button1";
            
            var button2 = new Button(new Rectangle(0, 0, 100, 40), "Button 2", font, Color.Green, Color.LightGreen, Color.White, () => Console.WriteLine("Button 2 clicked"));
            button2.Name = "button2";
            
            var nestedCanvas = new Canvas(new Rectangle(0, 0, 400, 200));
            nestedCanvas.Name = "nestedCanvas";
            
            var nestedLabel = new Label(new Rectangle(10, 10, 200, 30), "Nested Label", font, Color.Yellow);
            nestedLabel.Name = "nestedLabel";
        
        // Build the hierarchy
        nestedCanvas.AddChild(nestedLabel);
        buttonContainer.AddChild(button1);
        buttonContainer.AddChild(button2);
        
        mainLayout.AddChild(titleLabel);
        mainLayout.AddChild(buttonContainer);
        mainLayout.AddChild(nestedCanvas);
        
        rootCanvas.AddChild(mainLayout);
        
        // Test the search functionality
        Console.WriteLine("Testing Canvas.FindChildByName:");
        TestFindSingle(rootCanvas, "titleLabel");        // Should find (recursive through LayoutGroup)
        TestFindSingle(rootCanvas, "button1");           // Should find (recursive through multiple levels)
        TestFindSingle(rootCanvas, "nestedLabel");       // Should find (recursive through Canvas and LayoutGroup)
        TestFindSingle(rootCanvas, "nonExistent");       // Should not find
        
        Console.WriteLine("\nTesting LayoutGroup.FindChildByName:");
        TestFindSingle(mainLayout, "titleLabel");        // Should find (direct child)
        TestFindSingle(mainLayout, "button1");           // Should find (recursive through HorizontalLayoutGroup)
        TestFindSingle(mainLayout, "nestedLabel");       // Should find (recursive through Canvas)
        TestFindSingle(mainLayout, "rootCanvas");        // Should not find (parent, not child)
        
        Console.WriteLine("\nTesting nested container search:");
        TestFindSingle(buttonContainer, "button1");      // Should find (direct child)
        TestFindSingle(buttonContainer, "nestedLabel");  // Should not find (not a descendant)
        TestFindSingle(nestedCanvas, "nestedLabel");     // Should find (direct child)
        
        Console.WriteLine("\nTesting FindAllChildrenByName (multiple matches):");
        
        // Add another element with the same name to test finding all
        var anotherButton1 = new Button(new Rectangle(0, 0, 100, 40), "Another Button 1", font, Color.Red, Color.Pink, Color.White, () => Console.WriteLine("Another Button 1 clicked"));
        anotherButton1.Name = "button1";  // Same name as first button
        nestedCanvas.AddChild(anotherButton1);        var allButton1s = rootCanvas.FindAllChildrenByName("button1");
        Console.WriteLine($"Found {allButton1s.Count} elements named 'button1'");
        for (int i = 0; i < allButton1s.Count; i++)
        {
            Console.WriteLine($"  [{i}] Type: {allButton1s[i].GetType().Name}");
        }
        
        Console.WriteLine("=== Test Complete ===");
    }
    
    private static void TestFindSingle(UIElement container, string name)
    {
        UIElement found = null;
        
        if (container is Canvas canvas)
        {
            found = canvas.FindChildByName(name);
        }
        else if (container is LayoutGroup layoutGroup)
        {
            found = layoutGroup.FindChildByName(name);
        }
        
        Console.WriteLine($"  Searching for '{name}' in {container.GetType().Name} '{container.Name}': {(found != null ? "FOUND" : "NOT FOUND")}");
        if (found != null)
        {
            Console.WriteLine($"    Found element type: {found.GetType().Name}");
        }
    }
    }
}