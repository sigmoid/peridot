# UI Markup Parser & Builder System

This system provides a complete solution for parsing HTML-like UI markup and converting it into actual UI elements. It consists of three main components:

## Components

### 1. UITokenizer
Breaks down the markup text into tokens (tags, attributes, values, text content, etc.)

### 2. UIParser
Parses the tokens into a symbolic tree representation (`UIElementNode`) that represents the UI structure

### 3. UIBuilder & UIElementFactory
Converts the symbolic representation into actual UI elements

## Usage

### Basic Usage

```csharp
// Initialize with a font
var builder = new UIBuilder(myFont);

// Define your UI markup
var markup = @"
<canvas bounds=""0,0,800,600"" backgroundColor=""#2c3e50"">
    <div spacing=""10"">
        <label text=""Hello World"" textColor=""#ffffff"" />
        <button text=""Click Me"" onClick=""HandleClick"" />
    </div>
</canvas>";

// Register event handlers
builder.RegisterEventHandler("HandleClick", _ => Console.WriteLine("Button clicked!"));

// Build the UI
UIElement rootElement = builder.BuildFromMarkup(markup);
```

### Advanced Usage with Multiple Event Handlers

```csharp
var eventHandlers = new Dictionary<string, Action<string>>
{
    {"HandleClick", _ => DoSomething()},
    {"HandleTextChanged", text => ProcessText(text)},
    {"HandleVolumeChanged", value => SetVolume(float.Parse(value))}
};

UIElement ui = builder.BuildFromMarkup(markup, eventHandlers);
```

## Symbolic Representation

The parser creates a tree of `UIElementNode` objects:

```csharp
public class UIElementNode
{
    public string ElementType { get; set; }              // "button", "label", etc.
    public Dictionary<string, string> Attributes { get; set; }  // All attributes
    public List<UIElementNode> Children { get; set; }    // Child elements
    public string TextContent { get; set; }              // Text between tags
    public bool IsSelfClosing { get; set; }              // Self-closing tag?
    public int LineNumber { get; set; }                  // For error reporting
    public int ColumnNumber { get; set; }
}
```

## Supported Elements

### Canvas
```xml
<canvas bounds="0,0,800,600" backgroundColor="#2c3e50" clipToBounds="true">
    <!-- children -->
</canvas>
```

### Layout Containers (div)
```xml
<!-- Vertical layout (default) -->
<div bounds="0,0,400,300" spacing="10" horizontalAlignment="Center">
    <!-- children -->
</div>

<!-- Horizontal layout -->
<div direction="horizontal" spacing="15" verticalAlignment="Center">
    <!-- children -->
</div>

<!-- Grid layout -->
<div direction="grid" columns="3" spacing="10">
    <!-- children -->
</div>
```

### Labels
```xml
<label bounds="0,0,200,30" text="Hello World" textColor="#ffffff" backgroundColor="#3498db" />
```

### Buttons
```xml
<button bounds="0,0,120,40" 
        text="Click Me" 
        textColor="#ffffff" 
        backgroundColor="#27ae60" 
        hoverColor="#2ecc71" 
        onClick="HandleClick" />
```

### Text Inputs
```xml
<input bounds="0,0,300,40" 
       placeholder="Enter text..." 
       backgroundColor="#ffffff" 
       textColor="#000000" 
       borderColor="#bdc3c7" 
       focusedBorderColor="#3498db" 
       maxLength="50" 
       onTextChanged="HandleTextChanged" 
       onEnterPressed="HandleSubmit" />
```

### Sliders
```xml
<slider bounds="0,0,200,30" 
        minValue="0" 
        maxValue="100" 
        initialValue="50" 
        step="1" 
        isHorizontal="true" 
        trackColor="#bdc3c7" 
        fillColor="#3498db" 
        handleColor="#ffffff" 
        onValueChanged="HandleValueChanged" />
```

## Attribute Parsing Utilities

The system includes utilities for parsing common attribute types:

```csharp
// Parse rectangles from "x,y,width,height" format
Rectangle bounds = AttributeParser.ParseBounds("10,20,300,200");

// Parse colors from hex strings
Color color = AttributeParser.ParseColor("#3498db");
Color colorWithAlpha = AttributeParser.ParseColor("#80ff0000"); // Semi-transparent red

// Parse booleans
bool flag = AttributeParser.ParseBool("true");

// Parse numbers with defaults
int spacing = AttributeParser.ParseInt("15", 0);
float value = AttributeParser.ParseFloat("3.14", 0.0f);
```

## Error Handling

The parser provides detailed error information including line and column numbers:

```csharp
try 
{
    var ui = builder.BuildFromMarkup(markup);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Parse error: {ex.Message}");
    // Example: "Expected closing tag for 'div' at 15:8"
}
```

## Extending the System

### Custom Elements

```csharp
// Register a custom element creator
builder.RegisterElementCreator("mycustom", node => 
{
    // Create and return your custom UI element
    var bounds = AttributeParser.ParseBounds(node.Attributes.GetValueOrDefault("bounds", "0,0,100,100"));
    return new MyCustomElement(bounds);
});
```

### Custom Event Handlers

```csharp
// Register event handlers that receive string parameters
builder.RegisterEventHandler("MyHandler", parameter => 
{
    Console.WriteLine($"Handler called with: {parameter}");
});
```

## Token Analysis (for debugging)

You can also use the tokenizer and parser independently for analysis:

```csharp
// Tokenize markup
var tokenizer = new UITokenizer(markup);
var tokens = tokenizer.Tokenize();

foreach (var token in tokens)
{
    Console.WriteLine($"{token.Type}: '{token.Value}' at {token.LineNumber}:{token.ColumnNumber}");
}

// Parse into symbolic representation
var rootNode = UIParser.ParseMarkup(markup);
// Inspect the tree structure...
```

## Benefits

1. **Declarative UI**: Define UIs in markup rather than code
2. **Separation of Concerns**: UI layout separate from logic
3. **Easy Iteration**: Quick UI changes without recompiling
4. **Tooling Friendly**: Markup can be generated by tools
5. **Version Control Friendly**: Text-based format diffs well
6. **Designer Friendly**: Familiar HTML-like syntax
7. **Type Safety**: Compile-time checking of event handler names
8. **Error Reporting**: Clear error messages with line/column info

## Performance Notes

- The parser creates an intermediate tree representation, which uses some memory
- For very large UIs, consider parsing sections on-demand
- Event handler resolution happens at build time, so runtime performance is good
- The tokenizer is a single-pass algorithm and quite efficient