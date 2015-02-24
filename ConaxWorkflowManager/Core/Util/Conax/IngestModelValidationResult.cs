namespace MPS.MPP.Auxiliary.ConaxWorkflowManager.Core.Util.Conax
{
    public class IngestModelValidationResult
    {
        public bool IsValid;
        public string Message;

        public IngestModelValidationResult(bool valid, string message)
        {
            IsValid = valid;
            Message = message;
        }
    }
}
