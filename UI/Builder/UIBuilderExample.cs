using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Peridot.UI;
using Peridot.UI.Builder;

namespace Peridot.UI.Builder
{
    /// <summary>
    /// Example usage of the UI markup parser and builder system
    /// </summary>
    public class UIBuilderExample
    {
        private SpriteFont _font;
        private UIBuilder _builder;
        private UIElement _rootElement;

        public void Initialize(SpriteFont font)
        {
            _font = font;
            _builder = new UIBuilder(font);
            
            // Register event handlers
            RegisterEventHandlers();
            
            // Build UI from markup
            BuildExampleUI();
        }

        private void RegisterEventHandlers()
        {
            // Register button click handlers
            _builder.RegisterEventHandler("ShowHelpDialog", _ => ShowHelpDialog());
            _builder.RegisterEventHandler("ShowSettingsDialog", _ => ShowSettingsDialog());
            _builder.RegisterEventHandler("SubmitUserInput", _ => SubmitUserInput());
            _builder.RegisterEventHandler("HandleNameChanged", text => HandleNameChanged(text));
            _builder.RegisterEventHandler("SubmitForm", _ => SubmitForm());
            _builder.RegisterEventHandler("HandleVolumeChanged", value => HandleVolumeChanged(value));
            _builder.RegisterEventHandler("HandleMusicVolumeChanged", value => HandleMusicVolumeChanged(value));
            _builder.RegisterEventHandler("HandleSfxVolumeChanged", value => HandleSfxVolumeChanged(value));
            _builder.RegisterEventHandler("ExecuteAction1", _ => ExecuteAction("Action 1"));
            _builder.RegisterEventHandler("ExecuteAction2", _ => ExecuteAction("Action 2"));
            _builder.RegisterEventHandler("ExecuteAction3", _ => ExecuteAction("Action 3"));
            _builder.RegisterEventHandler("ShowSuccessToast", _ => ShowToast("Success!", "success"));
            _builder.RegisterEventHandler("ShowWarningToast", _ => ShowToast("Warning!", "warning"));
            _builder.RegisterEventHandler("ShowErrorToast", _ => ShowToast("Error!", "error"));
        }

        private void BuildExampleUI()
        {
            var markup = @"
<canvas bounds=""0,0,1920,1080"" backgroundColor=""#2c3e50"" clipToBounds=""true"">
    
    <!-- Main Application Header -->
    <div bounds=""0,0,1920,80"" spacing=""20"" direction=""horizontal""
         horizontalAlignment=""Center"" 
         verticalAlignment=""Center"" 
         backgroundColor=""#34495e"">
        
        <label bounds=""0,0,200,60"" 
               text=""Peridot UI Framework"" 
               textColor=""#ecf0f1"" 
               backgroundColor=""#3498db"" />
        
        <button bounds=""0,0,120,50"" 
                text=""Help"" 
                textColor=""#ffffff"" 
                backgroundColor=""#27ae60"" 
                hoverColor=""#2ecc71"" 
                onClick=""ShowHelpDialog"" />
                
        <button bounds=""0,0,120,50"" 
                text=""Settings"" 
                textColor=""#ffffff"" 
                backgroundColor=""#e74c3c"" 
                hoverColor=""#c0392b"" 
                onClick=""ShowSettingsDialog"" />
                
    </div>

    <!-- Main Content Area -->
    <div bounds=""50,100,1820,900"" spacing=""15"" 
         horizontalAlignment=""Stretch"" 
         verticalAlignment=""Top"">
        
        <!-- User Input Section -->
        <div bounds=""0,0,1820,120"" spacing=""10"" direction=""horizontal""
             horizontalAlignment=""Left"" 
             verticalAlignment=""Center"" 
             backgroundColor=""#ecf0f1"">
            
            <label bounds=""0,0,150,40"" 
                   text=""Enter your name:"" 
                   textColor=""#2c3e50"" />
            
            <input bounds=""0,0,300,40"" 
                   placeholder=""Type your name here..."" 
                   backgroundColor=""#ffffff"" 
                   textColor=""#2c3e50"" 
                   borderColor=""#bdc3c7"" 
                   focusedBorderColor=""#3498db"" 
                   padding=""8"" 
                   borderWidth=""2"" 
                   maxLength=""50"" 
                   onTextChanged=""HandleNameChanged"" 
                   onEnterPressed=""SubmitForm"" />
            
            <button bounds=""0,0,100,40"" 
                    text=""Submit"" 
                    textColor=""#ffffff"" 
                    backgroundColor=""#27ae60"" 
                    hoverColor=""#2ecc71"" 
                    onClick=""SubmitUserInput"" />
                    
        </div>

        <!-- Controls Demo Section -->
        <canvas bounds=""0,0,1820,400"" backgroundColor=""#ffffff"" clipToBounds=""false"">
            
            <!-- Slider Controls -->
            <div bounds=""50,20,400,350"" spacing=""20"">
                
                <label bounds=""0,0,400,30"" 
                       text=""Volume Controls"" 
                       textColor=""#2c3e50"" 
                       backgroundColor=""#f39c12"" />
                
                <div bounds=""0,0,400,50"" spacing=""10"" direction=""horizontal"" 
                     verticalAlignment=""Center"">
                    <label bounds=""0,0,120,30"" 
                           text=""Master Volume:"" 
                           textColor=""#34495e"" />
                    <slider bounds=""0,0,200,30"" 
                            minValue=""0"" 
                            maxValue=""100"" 
                            initialValue=""75"" 
                            step=""1"" 
                            isHorizontal=""true"" 
                            trackColor=""#bdc3c7"" 
                            fillColor=""#3498db"" 
                            handleColor=""#ffffff"" 
                            onValueChanged=""HandleVolumeChanged"" />
                    <label bounds=""0,0,50,30"" 
                           name=""volumeLabel"" 
                           text=""75%"" 
                           textColor=""#27ae60"" />
                </div>
                
            </div>
            
            <!-- Button Grid Demo -->
            <div bounds=""500,20,600,350"" direction=""grid""
                 columns=""3"" 
                 spacing=""10"" 
                 backgroundColor=""#f8f9fa"">
                
                <button bounds=""0,0,180,50"" 
                        text=""Action 1"" 
                        textColor=""#ffffff"" 
                        backgroundColor=""#007bff"" 
                        hoverColor=""#0056b3"" 
                        onClick=""ExecuteAction1"" />
                
                <button bounds=""0,0,180,50"" 
                        text=""Action 2"" 
                        textColor=""#ffffff"" 
                        backgroundColor=""#28a745"" 
                        hoverColor=""#1e7e34"" 
                        onClick=""ExecuteAction2"" />
                
                <button bounds=""0,0,180,50"" 
                        text=""Action 3"" 
                        textColor=""#ffffff"" 
                        backgroundColor=""#ffc107"" 
                        hoverColor=""#d39e00"" 
                        onClick=""ExecuteAction3"" />
                        
            </div>
            
        </canvas>

        <!-- Toast Demo Buttons -->
        <div bounds=""0,0,1820,60"" spacing=""10"" direction=""horizontal""
             horizontalAlignment=""Center"" 
             verticalAlignment=""Center"" 
             backgroundColor=""#6c757d"">
            
            <button bounds=""0,0,150,40"" 
                    text=""Success Toast"" 
                    textColor=""#ffffff"" 
                    backgroundColor=""#28a745"" 
                    hoverColor=""#1e7e34""
                    onClick=""ShowSuccessToast"" />
            
            <button bounds=""0,0,150,40"" 
                    text=""Warning Toast"" 
                    textColor=""#212529"" 
                    backgroundColor=""#ffc107"" 
                    hoverColor=""#d39e00""
                    onClick=""ShowWarningToast"" />
                   
            <button bounds=""0,0,150,40"" 
                    text=""Error Toast"" 
                    textColor=""#ffffff"" 
                    backgroundColor=""#dc3545"" 
                    hoverColor=""#bd2130""
                    onClick=""ShowErrorToast"" />
                   
        </div>
        
    </div>
    
</canvas>";

            _rootElement = _builder.BuildFromMarkup(markup);
        }

        // Event handler implementations
        private void ShowHelpDialog()
        {
            Console.WriteLine("Help dialog requested");
        }

        private void ShowSettingsDialog()
        {
            Console.WriteLine("Settings dialog requested");
        }

        private void SubmitUserInput()
        {
            Console.WriteLine("User input submitted");
        }

        private void HandleNameChanged(string name)
        {
            Console.WriteLine($"Name changed to: {name}");
        }

        private void SubmitForm()
        {
            Console.WriteLine("Form submitted via Enter key");
        }

        private void HandleVolumeChanged(string value)
        {
            Console.WriteLine($"Master volume changed to: {value}%");
        }

        private void HandleMusicVolumeChanged(string value)
        {
            Console.WriteLine($"Music volume changed to: {value}%");
        }

        private void HandleSfxVolumeChanged(string value)
        {
            Console.WriteLine($"SFX volume changed to: {value}%");
        }

        private void ExecuteAction(string actionName)
        {
            Console.WriteLine($"Executed: {actionName}");
        }

        private void ShowToast(string message, string type)
        {
            Console.WriteLine($"Toast ({type}): {message}");
        }

        public UIElement GetRootElement()
        {
            return _rootElement;
        }
    }

    /// <summary>
    /// Demonstrates parsing markup without building actual UI elements (for debugging/analysis)
    /// </summary>
    public class UIParserExample
    {
        public static void DemoTokenization()
        {
            var markup = @"<button bounds=""0,0,100,50"" text=""Click Me"" onClick=""HandleClick"" />";
            
            var tokenizer = new UITokenizer(markup);
            var tokens = tokenizer.Tokenize();
            
            Console.WriteLine("Tokens:");
            foreach (var token in tokens)
            {
                Console.WriteLine($"  {token}");
            }
        }

        public static void DemoParsing()
        {
            var markup = @"
<canvas bounds=""0,0,800,600"">
    <div direction=""horizontal"" spacing=""10"">
        <label text=""Hello World"" />
        <button text=""Click Me"" onClick=""HandleClick"" />
    </div>
</canvas>";

            var rootNode = UIParser.ParseMarkup(markup);
            
            Console.WriteLine("Parsed tree:");
            PrintNode(rootNode, 0);
        }

        private static void PrintNode(UIElementNode node, int indent)
        {
            var indentStr = new string(' ', indent * 2);
            
            Console.WriteLine($"{indentStr}{node.ElementType}");
            
            foreach (var attr in node.Attributes)
            {
                Console.WriteLine($"{indentStr}  {attr.Key} = \"{attr.Value}\"");
            }
            
            if (!string.IsNullOrEmpty(node.TextContent))
            {
                Console.WriteLine($"{indentStr}  Text: \"{node.TextContent}\"");
            }
            
            foreach (var child in node.Children)
            {
                PrintNode(child, indent + 1);
            }
        }
    }
}