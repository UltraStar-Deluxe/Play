public class ValueInputDialogValidationResult
{
    public EValueInputDialogValidationResultSeverity Severity { get; private set; }
    public Translation Message { get; private set; }

    private ValueInputDialogValidationResult(EValueInputDialogValidationResultSeverity severity, Translation message)
    {
        Severity = severity;
        Message = message;
    }

    public static ValueInputDialogValidationResult CreateValidResult()
    {
        return new ValueInputDialogValidationResult(EValueInputDialogValidationResultSeverity.None, Translation.Empty);
    }

    public static ValueInputDialogValidationResult CreateWarningResult(Translation warningMessage)
    {
        return new ValueInputDialogValidationResult(EValueInputDialogValidationResultSeverity.Warning, warningMessage);
    }

    public static ValueInputDialogValidationResult CreateErrorResult(Translation errorMessage)
    {
        return new ValueInputDialogValidationResult(EValueInputDialogValidationResultSeverity.Error, errorMessage);
    }
}
