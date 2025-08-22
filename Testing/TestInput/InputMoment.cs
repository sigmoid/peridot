namespace Peridot.Testing.Input;
   
[System.Serializable]
public class InputMoment
{
    public string ButtonName { get; set; }
    public string KeyName { get; set; }
    public double Timestamp { get; set; }
    public InputActionType ActionType { get; set; }

    public InputMoment()
    {
    }

    public InputMoment(InputButton button, double timestamp, InputActionType actionType)
    {
        ButtonName = button?.Name ?? "Unknown";
        KeyName = button?.Key.ToString() ?? "Unknown";
        Timestamp = timestamp;
        ActionType = actionType;
    }
}

public enum InputActionType
{
    Pressed,
    Released,
}