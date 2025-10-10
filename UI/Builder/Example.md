# HTML-Style UI Layout Example

This document demonstrates a comprehensive HTML-like syntax for defining UI layouts using Peridot UI elements. The syntax follows standard HTML rules with self-closing tags, proper nesting, and attribute specification.

## Basic Layout Structure

```html
<canvas bounds="0,0,1920,1080" backgroundColor="#2c3e50" clipToBounds="true">
    
    <!-- Main Application Header -->
    <div bounds="0,0,1920,80" spacing="20" direction="horizontal"
         horizontalAlignment="Center" 
         verticalAlignment="Center" 
         backgroundColor="#34495e">
        
        <label bounds="0,0,200,60" 
               text="Peridot UI Framework" 
               textColor="#ecf0f1" 
               backgroundColor="#3498db" />
        
        <button bounds="0,0,120,50" 
                text="Help" 
                textColor="#ffffff" 
                backgroundColor="#27ae60" 
                hoverColor="#2ecc71" 
                onClick="ShowHelpDialog" />
                
        <button bounds="0,0,120,50" 
                text="Settings" 
                textColor="#ffffff" 
                backgroundColor="#e74c3c" 
                hoverColor="#c0392b" 
                onClick="ShowSettingsDialog" />
                
    </div>

    <!-- Main Content Area -->
    <div bounds="50,100,1820,900" spacing="15" 
         horizontalAlignment="Stretch" 
         verticalAlignment="Top">
        
        <!-- User Input Section -->
        <div bounds="0,0,1820,120" spacing="10" direction="horizontal"
             horizontalAlignment="Left" 
             verticalAlignment="Center" 
             backgroundColor="#ecf0f1">
            
            <label bounds="0,0,150,40" 
                   text="Enter your name:" 
                   textColor="#2c3e50" />
            
            <input bounds="0,0,300,40" 
                   placeholder="Type your name here..." 
                   backgroundColor="#ffffff" 
                   textColor="#2c3e50" 
                   borderColor="#bdc3c7" 
                   focusedBorderColor="#3498db" 
                   padding="8" 
                   borderWidth="2" 
                   maxLength="50" 
                   onTextChanged="HandleNameChanged" 
                   onEnterPressed="SubmitForm" />
            
            <button bounds="0,0,100,40" 
                    text="Submit" 
                    textColor="#ffffff" 
                    backgroundColor="#27ae60" 
                    hoverColor="#2ecc71" 
                    onClick="SubmitUserInput" />
                    
        </div>

        <!-- Controls Demo Section -->
        <canvas bounds="0,0,1820,400" backgroundColor="#ffffff" clipToBounds="false">
            
            <!-- Slider Controls -->
            <div bounds="50,20,400,350" spacing="20">
                
                <label bounds="0,0,400,30" 
                       text="Volume Controls" 
                       textColor="#2c3e50" 
                       backgroundColor="#f39c12" />
                
                <div bounds="0,0,400,50" spacing="10" direction="horizontal" 
                     verticalAlignment="Center">
                    <label bounds="0,0,120,30" 
                           text="Master Volume:" 
                           textColor="#34495e" />
                    <slider bounds="0,0,200,30" 
                            minValue="0" 
                            maxValue="100" 
                            initialValue="75" 
                            step="1" 
                            isHorizontal="true" 
                            trackColor="#bdc3c7" 
                            fillColor="#3498db" 
                            handleColor="#ffffff" 
                            handleHoverColor="#ecf0f1" 
                            handlePressedColor="#bdc3c7" 
                            trackHeight="6" 
                            handleSize="20" 
                            onValueChanged="HandleVolumeChanged" />
                    <label bounds="0,0,50,30" 
                           name="volumeLabel" 
                           text="75%" 
                           textColor="#27ae60" />
                </div>
                
                <div bounds="0,0,400,50" spacing="10" direction="horizontal" 
                     verticalAlignment="Center">
                    <label bounds="0,0,120,30" 
                           text="Music Volume:" 
                           textColor="#34495e" />
                    <slider bounds="0,0,200,30" 
                            minValue="0" 
                            maxValue="100" 
                            initialValue="60" 
                            step="5" 
                            isHorizontal="true" 
                            trackColor="#bdc3c7" 
                            fillColor="#e74c3c" 
                            handleColor="#ffffff" 
                            onValueChanged="HandleMusicVolumeChanged" />
                    <label bounds="0,0,50,30" 
                           name="musicVolumeLabel" 
                           text="60%" 
                           textColor="#e74c3c" />
                </div>
                
                <div bounds="0,0,400,50" spacing="10" direction="horizontal" 
                     verticalAlignment="Center">
                    <label bounds="0,0,120,30" 
                           text="SFX Volume:" 
                           textColor="#34495e" />
                    <slider bounds="0,0,200,30" 
                            minValue="0" 
                            maxValue="100" 
                            initialValue="85" 
                            step="1" 
                            isHorizontal="true" 
                            trackColor="#bdc3c7" 
                            fillColor="#f39c12" 
                            handleColor="#ffffff" 
                            onValueChanged="HandleSfxVolumeChanged" />
                    <label bounds="0,0,50,30" 
                           name="sfxVolumeLabel" 
                           text="85%" 
                           textColor="#f39c12" />
                </div>
                
            </div>
            
            <!-- Button Grid Demo -->
            <div bounds="500,20,600,350" direction="grid"
                 columns="3" 
                 spacing="10" 
                 backgroundColor="#f8f9fa">
                
                <button bounds="0,0,180,50" 
                        text="Action 1" 
                        textColor="#ffffff" 
                        backgroundColor="#007bff" 
                        hoverColor="#0056b3" 
                        onClick="ExecuteAction1" />
                
                <button bounds="0,0,180,50" 
                        text="Action 2" 
                        textColor="#ffffff" 
                        backgroundColor="#28a745" 
                        hoverColor="#1e7e34" 
                        onClick="ExecuteAction2" />
                
                <button bounds="0,0,180,50" 
                        text="Action 3" 
                        textColor="#ffffff" 
                        backgroundColor="#ffc107" 
                        hoverColor="#d39e00" 
                        onClick="ExecuteAction3" />
                
                <button bounds="0,0,180,50" 
                        text="Danger Zone" 
                        textColor="#ffffff" 
                        backgroundColor="#dc3545" 
                        hoverColor="#bd2130" 
                        onClick="ShowDangerDialog" />
                
                <button bounds="0,0,180,50" 
                        text="Info" 
                        textColor="#ffffff" 
                        backgroundColor="#17a2b8" 
                        hoverColor="#117a8b" 
                        onClick="ShowInfoDialog" />
                
                <button bounds="0,0,180,50" 
                        text="Disabled" 
                        textColor="#6c757d" 
                        backgroundColor="#e9ecef" 
                        hoverColor="#e9ecef" 
                        enabled="false" />
                        
            </div>
            
        </canvas>

        <!-- Status and Notifications Area -->
        <div bounds="0,0,1820,100" spacing="20" direction="horizontal"
             horizontalAlignment="Left" 
             verticalAlignment="Top" 
             backgroundColor="#f8f9fa">
            
            <!-- Status Display -->
            <div bounds="0,0,400,100" spacing="5">
                <label bounds="0,0,400,25" 
                       text="System Status" 
                       textColor="#495057" 
                       backgroundColor="#dee2e6" />
                <label bounds="0,0,400,25" 
                       name="statusLabel" 
                       text="Ready" 
                       textColor="#28a745" />
                <label bounds="0,0,400,25" 
                       name="connectionLabel" 
                       text="Connected to server" 
                       textColor="#17a2b8" />
            </div>
            
            <!-- Action Buttons for Notifications -->
            <div bounds="0,0,300,100" spacing="8">
                
                <button bounds="0,0,280,20" 
                        text="Show Success Toast" 
                        textColor="#ffffff" 
                        backgroundColor="#28a745" 
                        hoverColor="#1e7e34" 
                        onClick="ShowSuccessToast" />
                
                <button bounds="0,0,280,20" 
                        text="Show Warning Toast" 
                        textColor="#212529" 
                        backgroundColor="#ffc107" 
                        hoverColor="#d39e00" 
                        onClick="ShowWarningToast" />
                
                <button bounds="0,0,280,20" 
                        text="Show Error Toast" 
                        textColor="#ffffff" 
                        backgroundColor="#dc3545" 
                        hoverColor="#bd2130" 
                        onClick="ShowErrorToast" />
                        
            </div>
            
            <!-- Modal Dialog Triggers -->
            <div bounds="0,0,300,100" spacing="8">
                
                <button bounds="0,0,280,20" 
                        text="Show Confirmation Modal" 
                        textColor="#ffffff" 
                        backgroundColor="#6f42c1" 
                        hoverColor="#59359a" 
                        onClick="ShowConfirmationModal" />
                
                <button bounds="0,0,280,20" 
                        text="Show Message Modal" 
                        textColor="#ffffff" 
                        backgroundColor="#fd7e14" 
                        hoverColor="#e55a00" 
                        onClick="ShowMessageModal" />
                
                <button bounds="0,0,280,20" 
                        text="Show Custom Modal" 
                        textColor="#ffffff" 
                        backgroundColor="#20c997" 
                        hoverColor="#17a085" 
                        onClick="ShowCustomModal" />
                        
            </div>
            
        </div>

        <!-- Footer -->
        <div bounds="0,0,1820,60" spacing="10" direction="horizontal"
             horizontalAlignment="Center" 
             verticalAlignment="Center" 
             backgroundColor="#6c757d">
            
            <label bounds="0,0,300,30" 
                   text="Peridot UI Framework v1.0" 
                   textColor="#ffffff" />
            
            <label bounds="0,0,200,30" 
                   text="© 2025 Your Company" 
                   textColor="#adb5bd" />
                   
        </div>
        
    </div>
    
</canvas>
```

## Element Property Reference

### canvas
- `bounds`: Rectangle (x,y,width,height)
- `backgroundColor`: Color (hex: #RRGGBB or #AARRGGBB)
- `clipToBounds`: Boolean (true/false)

### div (Layout Container)
- `bounds`: Rectangle (x,y,width,height)
- `spacing`: Integer (pixels between children)
- `direction`: String (vertical|horizontal|grid) - **Default: "vertical"**
- `backgroundColor`: Color (optional)
- **For vertical and horizontal layouts:**
  - `horizontalAlignment`: Left|Center|Right|Stretch (for vertical) or Left|Center|Right (for horizontal)
  - `verticalAlignment`: Top|Center|Bottom (for vertical) or Top|Center|Bottom (for horizontal)
- **For grid layout:**
  - `columns`: Integer (number of columns)

### label
- `bounds`: Rectangle (x,y,width,height)
- `text`: String (display text)
- `textColor`: Color (hex format)
- `backgroundColor`: Color (optional, transparent by default)
- `name`: String (optional identifier for code references)

### button
- `bounds`: Rectangle (x,y,width,height)
- `text`: String (button text)
- `textColor`: Color (text color)
- `backgroundColor`: Color (default background)
- `hoverColor`: Color (hover state background)
- `onClick`: String (method name to call)
- `enabled`: Boolean (default true)

### input
- `bounds`: Rectangle (x,y,width,height)
- `placeholder`: String (placeholder text)
- `backgroundColor`: Color (background color)
- `textColor`: Color (text color)
- `borderColor`: Color (border color)
- `focusedBorderColor`: Color (focused border color)
- `padding`: Integer (internal padding in pixels)
- `borderWidth`: Integer (border thickness)
- `maxLength`: Integer (maximum character count)
- `onTextChanged`: String (method name for text change events)
- `onEnterPressed`: String (method name for enter key events)

### slider
- `bounds`: Rectangle (x,y,width,height)
- `minValue`: Float (minimum value)
- `maxValue`: Float (maximum value)
- `initialValue`: Float (starting value)
- `step`: Float (increment step, 0 for continuous)
- `isHorizontal`: Boolean (orientation)
- `trackColor`: Color (track background color)
- `fillColor`: Color (filled portion color)
- `handleColor`: Color (slider handle color)
- `handleHoverColor`: Color (handle hover state)
- `handlePressedColor`: Color (handle pressed state)
- `trackHeight`: Integer (track thickness)
- `handleSize`: Integer (handle size)
- `onValueChanged`: String (method name for value change events)
- `bounds`: Rectangle (x,y,width,height)
- `minValue`: Float (minimum value)
- `maxValue`: Float (maximum value)
- `initialValue`: Float (starting value)
- `step`: Float (increment step, 0 for continuous)
- `isHorizontal`: Boolean (orientation)
- `trackColor`: Color (track background color)
- `fillColor`: Color (filled portion color)
- `handleColor`: Color (slider handle color)
- `handleHoverColor`: Color (handle hover state)
- `handlePressedColor`: Color (handle pressed state)
- `trackHeight`: Integer (track thickness)
- `handleSize`: Integer (handle size)
- `onValueChanged`: String (method name for value change events)

## HTML Rules Followed

1. **Self-closing tags**: Elements without children use `/>` syntax
2. **Proper nesting**: Child elements are properly nested within parent containers
3. **Attribute format**: All attributes use `name="value"` format
4. **Case sensitivity**: Element names are lowercase following HTML conventions
5. **Boolean attributes**: Use `"true"` or `"false"` string values
6. **Color format**: Hex colors with # prefix (#RRGGBB or #AARRGGBB)
7. **Rectangle format**: "x,y,width,height" comma-separated values
8. **Event handlers**: Method names as strings for onClick, onTextChanged, etc.

## Usage Notes

- All bounds are in absolute screen coordinates
- Colors support both RGB (#RRGGBB) and ARGB (#AARRGGBB) hex formats
- Layout groups automatically position their children based on their type and alignment settings
- Event handler strings should correspond to actual method names in the code
- The `name` attribute can be used to reference specific elements from code
- Boolean attributes should always be explicitly set as "true" or "false"