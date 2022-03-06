public class ValueInputDialogValidationResult
{
    public EValueInputDialogValidationResultSeverity Severity { get; private set; }
    public string Message { get; private set; }

    private ValueInputDialogValidationResult(EValueInputDialogValidationResultSeverity severity, string message)
    {
        Severity = severity;
        Message = message;
    }

    public static ValueInputDialogValidationResult CreateValidResult()
    {
        return new ValueInputDialogValidationResult(EValueInputDialogValidationResultSeverity.None, "");
    }

    public static ValueInputDialogValidationResult CreateWarningResult(string warningMessage)
    {
        return new ValueInputDialogValidationResult(EValueInputDialogValidationResultSeverity.Warning, warningMessage);
    }

    public static ValueInputDialogValidationResult CreateErrorResult(string errorMessage)
    {
        return new ValueInputDialogValidationResult(EValueInputDialogValidationResultSeverity.Error, errorMessage);
    }
}
