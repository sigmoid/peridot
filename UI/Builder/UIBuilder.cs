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
        private Texture2D _fallbackTexture;

        public UIElementFactory(SpriteFont defaultFont)
        {
            _defaultFont = defaultFont ?? throw new ArgumentNullException(nameof(defaultFont));
            _elementCreators = new Dictionary<string, Func<UIElementNode, UIElement>>();
            _eventHandlers = new Dictionary<string, Action<string>>();
            
            RegisterDefaultCreators();
        }

        private Texture2D CreateFallbackTexture()
        {
            if (_fallbackTexture == null)
            {
                // Create a simple colored 32x32 texture as fallback
                _fallbackTexture = new Texture2D(Core.GraphicsDevice, 32, 32);
                var colorData = new Color[32 * 32];
                
                // Create a simple pattern: light blue with darker border
                for (int i = 0; i < colorData.Length; i++)
                {
                    int x = i % 32;
                    int y = i / 32;
                    
                    if (x == 0 || x == 31 || y == 0 || y == 31)
                    {
                        colorData[i] = Color.DarkBlue; // Border
                    }
                    else
                    {
                        colorData[i] = Color.LightBlue; // Interior
                    }
                }
                
                _fallbackTexture.SetData(colorData);
            }
            return _fallbackTexture;
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

            // ScrollArea creator
            _elementCreators["scrollarea"] = node =>
            {
                var bounds = AttributeParser.ParseBounds(node.Attributes.GetValueOrDefault("bounds", "0,0,400,300"));
                var scrollbarWidth = AttributeParser.ParseInt(node.Attributes.GetValueOrDefault("scrollbarWidth", "16"));
                
                var scrollArea = new ScrollArea(bounds, scrollbarWidth);
                
                // Configure scrolling behavior
                if (node.Attributes.ContainsKey("autoShowScrollbars"))
                {
                    scrollArea.AutoShowScrollbars = AttributeParser.ParseBool(node.Attributes["autoShowScrollbars"]);
                }
                if (node.Attributes.ContainsKey("alwaysShowVertical"))
                {
                    scrollArea.AlwaysShowVerticalScrollbar = AttributeParser.ParseBool(node.Attributes["alwaysShowVertical"]);
                }
                if (node.Attributes.ContainsKey("alwaysShowHorizontal"))
                {
                    scrollArea.AlwaysShowHorizontalScrollbar = AttributeParser.ParseBool(node.Attributes["alwaysShowHorizontal"]);
                }
                if (node.Attributes.ContainsKey("scrollSpeed"))
                {
                    scrollArea.ScrollSpeed = AttributeParser.ParseFloat(node.Attributes["scrollSpeed"]);
                }
                
                // Wire up event handler
                var onScrollChanged = node.Attributes.GetValueOrDefault("onScrollChanged", "");
                if (!string.IsNullOrEmpty(onScrollChanged) && _eventHandlers.ContainsKey(onScrollChanged))
                {
                    scrollArea.OnScrollChanged += offset => _eventHandlers[onScrollChanged]($"{offset.X},{offset.Y}");
                }
                
                return scrollArea;
            };

            // TextArea creator
            _elementCreators["textarea"] = node =>
            {
                var bounds = AttributeParser.ParseBounds(node.Attributes.GetValueOrDefault("bounds", "0,0,300,200"));
                var wordWrap = AttributeParser.ParseBool(node.Attributes.GetValueOrDefault("wordWrap", "true"));
                var readOnly = AttributeParser.ParseBool(node.Attributes.GetValueOrDefault("readOnly", "false"));
                var backgroundColor = node.Attributes.ContainsKey("backgroundColor") 
                    ? AttributeParser.ParseColor(node.Attributes["backgroundColor"]) 
                    : (Color?)Color.White;
                var textColor = node.Attributes.ContainsKey("textColor") 
                    ? AttributeParser.ParseColor(node.Attributes["textColor"]) 
                    : (Color?)Color.Black;
                var borderColor = node.Attributes.ContainsKey("borderColor") 
                    ? AttributeParser.ParseColor(node.Attributes["borderColor"]) 
                    : (Color?)Color.DarkGray;
                var focusedBorderColor = node.Attributes.ContainsKey("focusedBorderColor") 
                    ? AttributeParser.ParseColor(node.Attributes["focusedBorderColor"]) 
                    : (Color?)Color.CornflowerBlue;
                var padding = AttributeParser.ParseInt(node.Attributes.GetValueOrDefault("padding", "8"));
                var borderWidth = AttributeParser.ParseInt(node.Attributes.GetValueOrDefault("borderWidth", "2"));
                var scrollbarWidth = AttributeParser.ParseInt(node.Attributes.GetValueOrDefault("scrollbarWidth", "16"));
                var scrollbarTrackColor = node.Attributes.ContainsKey("scrollbarTrackColor") 
                    ? AttributeParser.ParseColor(node.Attributes["scrollbarTrackColor"]) 
                    : (Color?)null;
                var scrollbarThumbColor = node.Attributes.ContainsKey("scrollbarThumbColor") 
                    ? AttributeParser.ParseColor(node.Attributes["scrollbarThumbColor"]) 
                    : (Color?)null;
                
                var textArea = new TextArea(bounds, _defaultFont, wordWrap, readOnly, 
                    backgroundColor, textColor, borderColor, focusedBorderColor,
                    padding, borderWidth, scrollbarWidth, scrollbarTrackColor, scrollbarThumbColor);
                
                // Set initial text content
                var initialText = node.Attributes.GetValueOrDefault("text", node.TextContent);
                if (!string.IsNullOrEmpty(initialText))
                {
                    textArea.Text = initialText;
                }
                
                // Set placeholder (if TextArea supports it - would need to be added to TextArea class)
                var placeholder = node.Attributes.GetValueOrDefault("placeholder", "");
                // Note: TextArea doesn't currently have placeholder support, but we could add it
                
                // Set max length
                if (node.Attributes.ContainsKey("maxLength"))
                {
                    textArea.MaxLength = AttributeParser.ParseInt(node.Attributes["maxLength"]);
                }
                
                // Wire up event handlers
                var onTextChanged = node.Attributes.GetValueOrDefault("onTextChanged", "");
                if (!string.IsNullOrEmpty(onTextChanged) && _eventHandlers.ContainsKey(onTextChanged))
                {
                    textArea.OnTextChanged += text => _eventHandlers[onTextChanged](text);
                }
                
                var onFocusGained = node.Attributes.GetValueOrDefault("onFocusGained", "");
                if (!string.IsNullOrEmpty(onFocusGained) && _eventHandlers.ContainsKey(onFocusGained))
                {
                    textArea.OnFocusGained += () => _eventHandlers[onFocusGained]("");
                }
                
                var onFocusLost = node.Attributes.GetValueOrDefault("onFocusLost", "");
                if (!string.IsNullOrEmpty(onFocusLost) && _eventHandlers.ContainsKey(onFocusLost))
                {
                    textArea.OnFocusLost += () => _eventHandlers[onFocusLost]("");
                }
                
                return textArea;
            };

            // ImageButton creator
            _elementCreators["imagebutton"] = node =>
            {
                var bounds = AttributeParser.ParseBounds(node.Attributes.GetValueOrDefault("bounds", "0,0,64,64"));
                var texturePath = node.Attributes.GetValueOrDefault("texture", "");
                
                // Load texture - handle gracefully if not found
                Texture2D texture = null;
                try
                {
                    if (!string.IsNullOrEmpty(texturePath))
                    {
                        texture = Core.Content.Load<Texture2D>(texturePath);
                    }
                }
                catch (Exception)
                {
                    // Use a fallback if texture loading fails
                    texture = CreateFallbackTexture();
                }
                
                if (texture == null)
                {
                    texture = CreateFallbackTexture();
                }
                
                // Parse source rectangle if specified
                Rectangle? sourceRectangle = null;
                if (node.Attributes.ContainsKey("sourceRect"))
                {
                    sourceRectangle = AttributeParser.ParseBounds(node.Attributes["sourceRect"]);
                }
                
                // Parse colors
                var tintColor = node.Attributes.ContainsKey("tintColor") 
                    ? AttributeParser.ParseColor(node.Attributes["tintColor"]) 
                    : Color.White;
                var hoverTintColor = node.Attributes.ContainsKey("hoverTintColor") 
                    ? AttributeParser.ParseColor(node.Attributes["hoverTintColor"]) 
                    : Color.Lerp(tintColor, Color.White, 0.2f);
                var pressedTintColor = node.Attributes.ContainsKey("pressedTintColor") 
                    ? AttributeParser.ParseColor(node.Attributes["pressedTintColor"]) 
                    : Color.Lerp(tintColor, Color.Black, 0.2f);
                var disabledTintColor = node.Attributes.ContainsKey("disabledTintColor") 
                    ? AttributeParser.ParseColor(node.Attributes["disabledTintColor"]) 
                    : Color.Lerp(tintColor, Color.Gray, 0.5f);
                
                // Parse background options
                var drawBackground = AttributeParser.ParseBool(node.Attributes.GetValueOrDefault("drawBackground", "false"));
                var backgroundColor = node.Attributes.ContainsKey("backgroundColor") 
                    ? AttributeParser.ParseColor(node.Attributes["backgroundColor"]) 
                    : Color.Transparent;
                var hoverBackgroundColor = node.Attributes.ContainsKey("hoverBackgroundColor") 
                    ? AttributeParser.ParseColor(node.Attributes["hoverBackgroundColor"]) 
                    : Color.LightGray;
                var pressedBackgroundColor = node.Attributes.ContainsKey("pressedBackgroundColor") 
                    ? AttributeParser.ParseColor(node.Attributes["pressedBackgroundColor"]) 
                    : Color.Gray;
                
                // Get click handler
                Action onClick = () => { };
                var onClickHandler = node.Attributes.GetValueOrDefault("onClick", "");
                if (!string.IsNullOrEmpty(onClickHandler) && _eventHandlers.ContainsKey(onClickHandler))
                {
                    onClick = () => _eventHandlers[onClickHandler]("");
                }
                
                var imageButton = new ImageButton(bounds, texture, onClick, sourceRectangle,
                    tintColor, hoverTintColor, pressedTintColor, disabledTintColor,
                    drawBackground, backgroundColor, hoverBackgroundColor, pressedBackgroundColor);
                
                // Set additional properties
                if (node.Attributes.ContainsKey("enabled"))
                {
                    imageButton.IsEnabled = AttributeParser.ParseBool(node.Attributes["enabled"]);
                }
                
                if (node.Attributes.ContainsKey("scaleToFit"))
                {
                    imageButton.ScaleToFit = AttributeParser.ParseBool(node.Attributes["scaleToFit"]);
                }
                
                return imageButton;
            };

            // Image creator
            _elementCreators["image"] = node =>
            {
                var bounds = AttributeParser.ParseBounds(node.Attributes.GetValueOrDefault("bounds", "0,0,64,64"));
                var texturePath = node.Attributes.GetValueOrDefault("texture", "");
                
                // Load texture - handle gracefully if not found
                Texture2D texture = null;
                try
                {
                    if (!string.IsNullOrEmpty(texturePath))
                    {
                        texture = Core.Content.Load<Texture2D>(texturePath);
                    }
                }
                catch (Exception)
                {
                    // Use a fallback if texture loading fails
                    texture = CreateFallbackTexture();
                }
                
                if (texture == null)
                {
                    texture = CreateFallbackTexture();
                }
                
                // Parse tint color
                var tintColor = node.Attributes.ContainsKey("tintColor") 
                    ? AttributeParser.ParseColor(node.Attributes["tintColor"]) 
                    : Color.White;
                
                var image = new UIImage(texture, bounds, tintColor);
                
                return image;
            };

            // Modal creator
            _elementCreators["modal"] = node =>
            {
                // Modal requires screen bounds and modal bounds
                var screenBounds = node.Attributes.ContainsKey("screenBounds") 
                    ? AttributeParser.ParseBounds(node.Attributes["screenBounds"]) 
                    : new Rectangle(0, 0, 1200, 800); // Default screen size
                var modalBounds = AttributeParser.ParseBounds(node.Attributes.GetValueOrDefault("bounds", "200,150,400,300"));
                var title = node.Attributes.GetValueOrDefault("title", node.TextContent);
                
                // Parse colors
                var overlayColor = node.Attributes.ContainsKey("overlayColor") 
                    ? AttributeParser.ParseColor(node.Attributes["overlayColor"]) 
                    : Color.Black * 0.5f; // Semi-transparent by default
                var backgroundColor = node.Attributes.ContainsKey("backgroundColor") 
                    ? AttributeParser.ParseColor(node.Attributes["backgroundColor"]) 
                    : Color.White;
                var titleBarColor = node.Attributes.ContainsKey("titleBarColor") 
                    ? AttributeParser.ParseColor(node.Attributes["titleBarColor"]) 
                    : Color.DarkBlue;
                
                // Parse behavior options
                var isClosable = AttributeParser.ParseBool(node.Attributes.GetValueOrDefault("closable", "true"));
                var closeOnOverlayClick = AttributeParser.ParseBool(node.Attributes.GetValueOrDefault("closeOnOverlayClick", "true"));
                var visible = AttributeParser.ParseBool(node.Attributes.GetValueOrDefault("visible", "false"));
                var centerOnScreen = AttributeParser.ParseBool(node.Attributes.GetValueOrDefault("centerOnScreen", "false"));
                
                // Get close handler
                Action onClose = null;
                var onCloseHandler = node.Attributes.GetValueOrDefault("onClose", "");
                if (!string.IsNullOrEmpty(onCloseHandler) && _eventHandlers.ContainsKey(onCloseHandler))
                {
                    onClose = () => _eventHandlers[onCloseHandler]("");
                }
                
                var modal = new Modal(screenBounds, modalBounds, title, _defaultFont, _defaultFont,
                    overlayColor, backgroundColor, titleBarColor, onClose, isClosable, closeOnOverlayClick);
                
                // Set initial visibility
                modal.SetVisibility(visible);
                
                // Center if requested
                if (centerOnScreen)
                {
                    modal.CenterOnScreen();
                }
                
                return modal;
            };

            // Toast creator
            _elementCreators["toast"] = node =>
            {
                // Toast requires screen bounds for positioning
                var screenBounds = node.Attributes.ContainsKey("screenBounds") 
                    ? AttributeParser.ParseBounds(node.Attributes["screenBounds"]) 
                    : new Rectangle(0, 0, 1200, 800); // Default screen size
                var message = node.Attributes.GetValueOrDefault("message", node.TextContent);
                
                // Parse toast type
                var toastType = ParseToastType(node.Attributes.GetValueOrDefault("type", "Info"));
                
                // Parse duration and timing
                var duration = AttributeParser.ParseFloat(node.Attributes.GetValueOrDefault("duration", "3.0"));
                
                // Parse position
                var position = ParseToastPosition(node.Attributes.GetValueOrDefault("position", "BottomRight"));
                
                // Parse offset (optional)
                Vector2? offset = null;
                if (node.Attributes.ContainsKey("offset"))
                {
                    var offsetStr = node.Attributes["offset"];
                    if (offsetStr.Contains(','))
                    {
                        var parts = offsetStr.Split(',');
                        if (parts.Length == 2 && 
                            float.TryParse(parts[0].Trim(), out float x) && 
                            float.TryParse(parts[1].Trim(), out float y))
                        {
                            offset = new Vector2(x, y);
                        }
                    }
                }
                
                // Parse visibility and auto-show behavior
                var visible = AttributeParser.ParseBool(node.Attributes.GetValueOrDefault("visible", "false"));
                var autoShow = AttributeParser.ParseBool(node.Attributes.GetValueOrDefault("autoShow", "false"));
                
                var toast = Toast.Show(message, _defaultFont, screenBounds, toastType, duration, position, offset);
                
                // Set initial visibility
                if (!visible && !autoShow)
                {
                    toast.SetVisibility(false);
                    toast.Hide(); // Set to finished state
                }
                
                // Wire up event handler for when toast finishes
                var onFinished = node.Attributes.GetValueOrDefault("onFinished", "");
                if (!string.IsNullOrEmpty(onFinished) && _eventHandlers.ContainsKey(onFinished))
                {
                    toast.OnToastFinished += () => _eventHandlers[onFinished]("");
                }
                
                return toast;
            };

            // Grid creator
            _elementCreators["grid"] = node =>
            {
                var bounds = AttributeParser.ParseBounds(node.Attributes.GetValueOrDefault("bounds", "0,0,300,300"));
                var spacing = AttributeParser.ParseInt(node.Attributes.GetValueOrDefault("spacing", "5"));
                
                // Support both backgroundColor and background-color attribute naming
                var backgroundColor = node.Attributes.ContainsKey("backgroundColor") 
                    ? AttributeParser.ParseColor(node.Attributes["backgroundColor"]) 
                    : node.Attributes.ContainsKey("background-color")
                        ? AttributeParser.ParseColor(node.Attributes["background-color"])
                        : (Color?)null;
                
                return CreateGridLayout(node, bounds, spacing, backgroundColor);
            };

            // Draggable Window creator
            _elementCreators["window"] = node =>
            {
                var bounds = AttributeParser.ParseBounds(node.Attributes.GetValueOrDefault("bounds", "100,100,400,300"));
                var title = node.Attributes.GetValueOrDefault("title", node.TextContent);

                // Optional style attributes
                Color? backgroundColor = node.Attributes.ContainsKey("backgroundColor")
                    ? AttributeParser.ParseColor(node.Attributes["backgroundColor"]) : (Color?)null;
                Color? titleBarColor = node.Attributes.ContainsKey("titleBarColor")
                    ? AttributeParser.ParseColor(node.Attributes["titleBarColor"]) : (Color?)null;
                Color? titleTextColor = node.Attributes.ContainsKey("titleTextColor")
                    ? AttributeParser.ParseColor(node.Attributes["titleTextColor"]) : (Color?)null;
                Color? borderColor = node.Attributes.ContainsKey("borderColor")
                    ? AttributeParser.ParseColor(node.Attributes["borderColor"]) : (Color?)null;
                var borderThickness = AttributeParser.ParseInt(node.Attributes.GetValueOrDefault("borderThickness", "2"));

                var window = new DraggableWindow(bounds, title, _defaultFont,
                    backgroundColor, titleBarColor, titleTextColor, borderColor, borderThickness);

                return window;
            };
        }

        private Toast.ToastType ParseToastType(string type)
        {
            return type.ToLower() switch
            {
                "info" => Toast.ToastType.Info,
                "success" => Toast.ToastType.Success,
                "warning" => Toast.ToastType.Warning,
                "error" => Toast.ToastType.Error,
                _ => Toast.ToastType.Info
            };
        }

        private Toast.ToastPosition ParseToastPosition(string position)
        {
            return position.ToLower() switch
            {
                "topleft" => Toast.ToastPosition.TopLeft,
                "topcenter" => Toast.ToastPosition.TopCenter,
                "topright" => Toast.ToastPosition.TopRight,
                "middleleft" => Toast.ToastPosition.MiddleLeft,
                "middlecenter" => Toast.ToastPosition.MiddleCenter,
                "middleright" => Toast.ToastPosition.MiddleRight,
                "bottomleft" => Toast.ToastPosition.BottomLeft,
                "bottomcenter" => Toast.ToastPosition.BottomCenter,
                "bottomright" => Toast.ToastPosition.BottomRight,
                _ => Toast.ToastPosition.BottomRight
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
            var columns = AttributeParser.ParseInt(node.Attributes.GetValueOrDefault("columns", "3"));
            var rows = AttributeParser.ParseInt(node.Attributes.GetValueOrDefault("rows", "3"));
            var horizontalSpacing = AttributeParser.ParseInt(node.Attributes.GetValueOrDefault("horizontal-spacing", spacing.ToString()));
            var verticalSpacing = AttributeParser.ParseInt(node.Attributes.GetValueOrDefault("vertical-spacing", spacing.ToString()));
            
            return new GridLayoutGroup(bounds, columns, rows, horizontalSpacing, verticalSpacing, backgroundColor);
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
                case GridLayoutGroup gridLayout:
                    gridLayout.AddChild(child);
                    break;
                case ScrollArea scrollArea:
                    scrollArea.AddChild(child);
                    break;
                case Modal modal:
                    modal.AddContentElement(child);
                    break;
                case DraggableWindow window:
                    window.AddChild(child);
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