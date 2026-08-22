namespace ProjectDelta.Data
{
    public interface IDataValidator
    {
        DataValidationReport Validate(DataRepository repository);
    }
}
