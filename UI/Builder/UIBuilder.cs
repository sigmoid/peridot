using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot.UI;

namespace Peridot.UI.Builder
{
    /// <summary>
    /// Factory for creating UI elements from parsed markup
    /// </summary>
    public class UIElementFactory
    {
        private readonly SpriteFont _defaultFont;
        private readonly Dictionary<string, Func<UIElementNode, UIElement>> _elementCreators;
        private readonly Dictionary<string, Action<string>> _eventHandlers;

        public UIElementFactory(SpriteFont defaultFont)
        {
            _defaultFont = defaultFont ?? throw new ArgumentNullException(nameof(defaultFont));
            _elementCreators = new Dictionary<string, Func<UIElementNode, UIElement>>();
            _eventHandlers = new Dictionary<string, Action<string>>();
            
            RegisterDefaultCreators();
        }

        public void RegisterEventHandler(string name, Action<string> handler)
        {
            _eventHandlers[name] = handler;
        }

        public void RegisterElementCreator(string elementType, Func<UIElementNode, UIElement> creator)
        {
            _elementCreators[elementType] = creator;
        }

        public UIElement CreateElement(UIElementNode node)
        {
            if (_elementCreators.TryGetValue(node.ElementType, out var creator))
            {
                var element = creator(node);
                
                // Set common properties
                SetCommonProperties(element, node);
                
                // Create and add children
                foreach (var childNode in node.Children)
                {
                    var childElement = CreateElement(childNode);
                    if (childElement != null)
                    {
                        AddChildToElement(element, childElement);
                    }
                }
                
                return element;
            }

            throw new NotSupportedException($"Unsupported element type: {node.ElementType}");
        }

        private void RegisterDefaultCreators()
        {
            // Canvas creator
            _elementCreators["canvas"] = node =>
            {
                var bounds = AttributeParser.ParseBounds(node.Attributes.GetValueOrDefault("bounds", "0,0,800,600"));
                var backgroundColor = node.Attributes.ContainsKey("backgroundColor") 
                    ? AttributeParser.ParseColor(node.Attributes["backgroundColor"]) 
                    : (Color?)null;
                var clipToBounds = AttributeParser.ParseBool(node.Attributes.GetValueOrDefault("clipToBounds", "false"));
                
                return new Canvas(bounds, backgroundColor, clipToBounds);
            };

            // Div (Layout Group) creator
            _elementCreators["div"] = node =>
            {
                var bounds = AttributeParser.ParseBounds(node.Attributes.GetValueOrDefault("bounds", "0,0,100,100"));
                var spacing = AttributeParser.ParseInt(node.Attributes.GetValueOrDefault("spacing", "0"));
                var direction = node.Attributes.GetValueOrDefault("direction", "vertical");
                var backgroundColor = node.Attributes.ContainsKey("backgroundColor") 
                    ? AttributeParser.ParseColor(node.Attributes["backgroundColor"]) 
                    : (Color?)null;

                return direction.ToLower() switch
                {
                    "horizontal" => CreateHorizontalLayout(node, bounds, spacing, backgroundColor),
                    "grid" => CreateGridLayout(node, bounds, spacing, backgroundColor),
                    _ => CreateVerticalLayout(node, bounds, spacing, backgroundColor) // default
                };
            };

            // Label creator
            _elementCreators["label"] = node =>
            {
                var bounds = AttributeParser.ParseBounds(node.Attributes.GetValueOrDefault("bounds", "0,0,100,30"));
                var text = node.Attributes.GetValueOrDefault("text", node.TextContent);
                var textColor = AttributeParser.ParseColor(node.Attributes.GetValueOrDefault("textColor", "#000000"));
                var backgroundColor = node.Attributes.ContainsKey("backgroundColor") 
                    ? AttributeParser.ParseColor(node.Attributes["backgroundColor"]) 
                    : (Color?)null;
                
                return new Label(bounds, text, _defaultFont, textColor, backgroundColor);
            };

            // Button creator
            _elementCreators["button"] = node =>
            {
                var bounds = AttributeParser.ParseBounds(node.Attributes.GetValueOrDefault("bounds", "0,0,100,30"));
                var text = node.Attributes.GetValueOrDefault("text", node.TextContent);
                var textColor = AttributeParser.ParseColor(node.Attributes.GetValueOrDefault("textColor", "#FFFFFF"));
                var backgroundColor = AttributeParser.ParseColor(node.Attributes.GetValueOrDefault("backgroundColor", "#808080"));
                var hoverColor = AttributeParser.ParseColor(node.Attributes.GetValueOrDefault("hoverColor", "#A0A0A0"));
                var onClick = node.Attributes.GetValueOrDefault("onClick", "");
                
                Action clickAction = () => { };
                if (!string.IsNullOrEmpty(onClick) && _eventHandlers.ContainsKey(onClick))
                {
                    clickAction = () => _eventHandlers[onClick]("");
                }
                
                return new Button(bounds, text, _defaultFont, backgroundColor, hoverColor, textColor, clickAction);
            };

            // Input creator
            _elementCreators["input"] = node =>
            {
                var bounds = AttributeParser.ParseBounds(node.Attributes.GetValueOrDefault("bounds", "0,0,200,30"));
                var placeholder = node.Attributes.GetValueOrDefault("placeholder", "");
                var backgroundColor = AttributeParser.ParseColor(node.Attributes.GetValueOrDefault("backgroundColor", "#FFFFFF"));
                var textColor = AttributeParser.ParseColor(node.Attributes.GetValueOrDefault("textColor", "#000000"));
                var borderColor = AttributeParser.ParseColor(node.Attributes.GetValueOrDefault("borderColor", "#808080"));
                var focusedBorderColor = AttributeParser.ParseColor(node.Attributes.GetValueOrDefault("focusedBorderColor", "#0080FF"));
                var padding = AttributeParser.ParseInt(node.Attributes.GetValueOrDefault("padding", "4"));
                var borderWidth = AttributeParser.ParseInt(node.Attributes.GetValueOrDefault("borderWidth", "1"));
                
                var input = new TextInput(bounds, _defaultFont, placeholder, backgroundColor, textColor, 
                    borderColor, focusedBorderColor, padding, borderWidth);
                
                // Set max length if specified
                if (node.Attributes.ContainsKey("maxLength"))
                {
                    input.MaxLength = AttributeParser.ParseInt(node.Attributes["maxLength"]);
                }
                
                // Wire up event handlers
                var onTextChanged = node.Attributes.GetValueOrDefault("onTextChanged", "");
                if (!string.IsNullOrEmpty(onTextChanged) && _eventHandlers.ContainsKey(onTextChanged))
                {
                    input.OnTextChanged += text => _eventHandlers[onTextChanged](text);
                }
                
                var onEnterPressed = node.Attributes.GetValueOrDefault("onEnterPressed", "");
                if (!string.IsNullOrEmpty(onEnterPressed) && _eventHandlers.ContainsKey(onEnterPressed))
                {
                    input.OnEnterPressed += text => _eventHandlers[onEnterPressed](text);
                }
                
                return input;
            };

            // Slider creator
            _elementCreators["slider"] = node =>
            {
                var bounds = AttributeParser.ParseBounds(node.Attributes.GetValueOrDefault("bounds", "0,0,200,30"));
                var minValue = AttributeParser.ParseFloat(node.Attributes.GetValueOrDefault("minValue", "0"));
                var maxValue = AttributeParser.ParseFloat(node.Attributes.GetValueOrDefault("maxValue", "100"));
                var initialValue = AttributeParser.ParseFloat(node.Attributes.GetValueOrDefault("initialValue", "0"));
                var step = AttributeParser.ParseFloat(node.Attributes.GetValueOrDefault("step", "0"));
                var isHorizontal = AttributeParser.ParseBool(node.Attributes.GetValueOrDefault("isHorizontal", "true"));
                
                var trackColor = node.Attributes.ContainsKey("trackColor") 
                    ? AttributeParser.ParseColor(node.Attributes["trackColor"]) 
                    : (Color?)null;
                var fillColor = node.Attributes.ContainsKey("fillColor") 
                    ? AttributeParser.ParseColor(node.Attributes["fillColor"]) 
                    : (Color?)null;
                var handleColor = node.Attributes.ContainsKey("handleColor") 
                    ? AttributeParser.ParseColor(node.Attributes["handleColor"]) 
                    : (Color?)null;
                var handleHoverColor = node.Attributes.ContainsKey("handleHoverColor") 
                    ? AttributeParser.ParseColor(node.Attributes["handleHoverColor"]) 
                    : (Color?)null;
                var handlePressedColor = node.Attributes.ContainsKey("handlePressedColor") 
                    ? AttributeParser.ParseColor(node.Attributes["handlePressedColor"]) 
                    : (Color?)null;
                
                var trackHeight = AttributeParser.ParseInt(node.Attributes.GetValueOrDefault("trackHeight", "6"));
                var handleSize = AttributeParser.ParseInt(node.Attributes.GetValueOrDefault("handleSize", "20"));
                
                var slider = new Slider(bounds, minValue, maxValue, initialValue, step, isHorizontal,
                    trackColor, fillColor, handleColor, handleHoverColor, handlePressedColor,
                    trackHeight, handleSize);
                
                // Wire up event handler
                var onValueChanged = node.Attributes.GetValueOrDefault("onValueChanged", "");
                if (!string.IsNullOrEmpty(onValueChanged) && _eventHandlers.ContainsKey(onValueChanged))
                {
                    slider.OnValueChanged += value => _eventHandlers[onValueChanged](value.ToString());
                }
                
                return slider;
            };
        }

        private VerticalLayoutGroup CreateVerticalLayout(UIElementNode node, Rectangle bounds, int spacing, Color? backgroundColor)
        {
            var horizontalAlignment = ParseHorizontalAlignmentForVertical(node.Attributes.GetValueOrDefault("horizontalAlignment", "Left"));
            var verticalAlignment = ParseVerticalAlignmentForVertical(node.Attributes.GetValueOrDefault("verticalAlignment", "Top"));
            
            return new VerticalLayoutGroup(bounds, spacing, horizontalAlignment, verticalAlignment, backgroundColor);
        }

        private HorizontalLayoutGroup CreateHorizontalLayout(UIElementNode node, Rectangle bounds, int spacing, Color? backgroundColor)
        {
            var horizontalAlignment = ParseHorizontalAlignmentForHorizontal(node.Attributes.GetValueOrDefault("horizontalAlignment", "Left"));
            var verticalAlignment = ParseVerticalAlignmentForHorizontal(node.Attributes.GetValueOrDefault("verticalAlignment", "Center"));
            
            return new HorizontalLayoutGroup(bounds, spacing, horizontalAlignment, verticalAlignment, backgroundColor);
        }

        private UIElement CreateGridLayout(UIElementNode node, Rectangle bounds, int spacing, Color? backgroundColor)
        {
            // For now, create a canvas as we don't have a GridLayoutGroup implemented
            // This could be extended when GridLayoutGroup is implemented
            var canvas = new Canvas(bounds, backgroundColor);
            
            var columns = AttributeParser.ParseInt(node.Attributes.GetValueOrDefault("columns", "3"));
            
            // TODO: Implement grid layout logic
            // For now, just return a canvas
            return canvas;
        }

        private VerticalLayoutGroup.HorizontalAlignment ParseHorizontalAlignmentForVertical(string alignment)
        {
            return alignment.ToLower() switch
            {
                "left" => VerticalLayoutGroup.HorizontalAlignment.Left,
                "center" => VerticalLayoutGroup.HorizontalAlignment.Center,
                "right" => VerticalLayoutGroup.HorizontalAlignment.Right,
                "stretch" => VerticalLayoutGroup.HorizontalAlignment.Stretch,
                _ => VerticalLayoutGroup.HorizontalAlignment.Left
            };
        }

        private VerticalLayoutGroup.VerticalAlignment ParseVerticalAlignmentForVertical(string alignment)
        {
            return alignment.ToLower() switch
            {
                "top" => VerticalLayoutGroup.VerticalAlignment.Top,
                "center" => VerticalLayoutGroup.VerticalAlignment.Center,
                "bottom" => VerticalLayoutGroup.VerticalAlignment.Bottom,
                _ => VerticalLayoutGroup.VerticalAlignment.Top
            };
        }

        private HorizontalLayoutGroup.HorizontalAlignment ParseHorizontalAlignmentForHorizontal(string alignment)
        {
            return alignment.ToLower() switch
            {
                "left" => HorizontalLayoutGroup.HorizontalAlignment.Left,
                "center" => HorizontalLayoutGroup.HorizontalAlignment.Center,
                "right" => HorizontalLayoutGroup.HorizontalAlignment.Right,
                _ => HorizontalLayoutGroup.HorizontalAlignment.Left
            };
        }

        private HorizontalLayoutGroup.VerticalAlignment ParseVerticalAlignmentForHorizontal(string alignment)
        {
            return alignment.ToLower() switch
            {
                "top" => HorizontalLayoutGroup.VerticalAlignment.Top,
                "center" => HorizontalLayoutGroup.VerticalAlignment.Center,
                "bottom" => HorizontalLayoutGroup.VerticalAlignment.Bottom,
                _ => HorizontalLayoutGroup.VerticalAlignment.Center
            };
        }

        private void SetCommonProperties(UIElement element, UIElementNode node)
        {
            // Set name/ID for later reference
            if (node.Attributes.ContainsKey("name") || node.Attributes.ContainsKey("id"))
            {
                var name = node.Attributes.GetValueOrDefault("name", node.Attributes.GetValueOrDefault("id", ""));
                element.Name = name;
            }
            
            // Set visibility
            if (node.Attributes.ContainsKey("visible"))
            {
                var visible = AttributeParser.ParseBool(node.Attributes["visible"]);
                element.SetVisibility(visible);
            }
        }

        private void AddChildToElement(UIElement parent, UIElement child)
        {
            switch (parent)
            {
                case Canvas canvas:
                    canvas.AddChild(child);
                    break;
                case VerticalLayoutGroup verticalLayout:
                    verticalLayout.AddChild(child);
                    break;
                case HorizontalLayoutGroup horizontalLayout:
                    horizontalLayout.AddChild(child);
                    break;
                // Add other container types as needed
                default:
                    // Element doesn't support children
                    break;
            }
        }
    }

    /// <summary>
    /// Main builder class that orchestrates parsing and element creation
    /// </summary>
    public class UIBuilder
    {
        private readonly UIElementFactory _factory;

        public UIBuilder(SpriteFont defaultFont)
        {
            _factory = new UIElementFactory(defaultFont);
        }

        public void RegisterEventHandler(string name, Action<string> handler)
        {
            _factory.RegisterEventHandler(name, handler);
        }

        public void RegisterElementCreator(string elementType, Func<UIElementNode, UIElement> creator)
        {
            _factory.RegisterElementCreator(elementType, creator);
        }

        public UIElement BuildFromMarkup(string markup)
        {
            try
            {
                var rootNode = UIParser.ParseMarkup(markup);
                
                // If the root has only one child, return that child
                if (rootNode.Children.Count == 1)
                {
                    return _factory.CreateElement(rootNode.Children[0]);
                }
                
                // If multiple root elements, wrap in a canvas
                var canvas = new Canvas(new Rectangle(0, 0, 800, 600));
                foreach (var childNode in rootNode.Children)
                {
                    var element = _factory.CreateElement(childNode);
                    if (element != null)
                    {
                        canvas.AddChild(element);
                    }
                }
                
                return canvas;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to build UI from markup: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Convenience method to build UI with event handlers
        /// </summary>
        public UIElement BuildFromMarkup(string markup, Dictionary<string, Action<string>> eventHandlers)
        {
            // Register event handlers
            foreach (var handler in eventHandlers)
            {
                RegisterEventHandler(handler.Key, handler.Value);
            }
            
            return BuildFromMarkup(markup);
        }
    }
}