namespace ExpenseTracker.Application.Common.Models
{
    public class Result
    {
        protected Result(bool succeeded, IEnumerable<string> errors)
        {
            Succeeded = succeeded;
            Errors = errors.ToArray();
        }

        public bool Succeeded { get; }
        public string[] Errors { get; }
        public string? FirstError => Errors.FirstOrDefault();

        public static Result Success() => new(true, Array.Empty<string>());
        public static Result Failure(IEnumerable<string> errors) => new(false, errors);
        public static Result Failure(string error) => new(false, [error]);
    }

    public class Result<T> : Result
    {
        private Result(bool succeeded, T? value, IEnumerable<string> errors)
            : base(succeeded, errors)
        {
            Value = value;
        }

        public T? Value { get; }

        public static Result<T> Success(T value) => new(true, value, Array.Empty<string>());
        public new static Result<T> Failure(IEnumerable<string> errors) => new(false, default, errors);
        public new static Result<T> Failure(string error) => new(false, default, [error]);
    }
}
