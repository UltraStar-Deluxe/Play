using System;
using UnityEngine;
using UnityEngine.UIElements;

public class BaseFieldWithTextFieldControl<T>
{
    private readonly BaseField<T> baseField;
    private readonly TextField textField;
    private readonly Func<T, string> valueToTextFunction;
    private readonly Func<string, T> textToValueFunction;
    
    private int valueToTextFrameCount;
    private int textToValueFrameCount;
    
    public BaseFieldWithTextFieldControl(
        BaseField<T> baseField, 
        TextField textField,
        Func<T,string> valueToTextFunction,
        Func<string,T> textToValueFunction)
    {
        this.baseField = baseField;
        this.textField = textField;
        this.valueToTextFunction = valueToTextFunction;
        this.textToValueFunction = textToValueFunction;
        
        // Set TextField when BaseField changes.
        baseField.RegisterValueChangedCallback(evt => UpdateTextField(evt.newValue));
        
        // Set BaseField when TextField changes.
        textField.RegisterValueChangedCallback(evt => UpdateBaseField(evt.newValue));
    }

    public void UpdateBaseField(string newText, bool force = false)
    {
        if (textToValueFunction == null
            || textToValueFrameCount == Time.frameCount)
        {
            return;
        }
        textToValueFrameCount = Time.frameCount;

        try
        {
            T newValue = textToValueFunction(newText);
            if (force
                || !Equals(baseField.value, newValue))
            {
                baseField.value = newValue;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError("Failed to set the TextField to the new value.");
            try
            {
                string oldText = valueToTextFunction(baseField.value);
                if (valueToTextFunction != null)
                {
                    textField.value = oldText;
                }
            }
            catch (Exception ex2)
            {
                Debug.LogException(ex);
                Debug.LogError("Failed to reset the TextField to the old value.");
            }
        }
    }
    
    public void UpdateTextField(T newValue, bool force = false)
    {
        if (valueToTextFunction == null
            || valueToTextFrameCount == Time.frameCount)
        {
            return;
        }
        valueToTextFrameCount = Time.frameCount;
        
        string newText = valueToTextFunction(newValue);
        if (force
            || !Equals(textField.value, newText))
        {
            textField.value = newText;
        }
    }
}
