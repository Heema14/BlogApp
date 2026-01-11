using System;
using System.ComponentModel.DataAnnotations;

namespace SyncSyntax.Validation
{
    public class MinAgeAttribute : ValidationAttribute
    {
        private readonly int _minAge;

        public MinAgeAttribute(int minAge)
        {
            _minAge = minAge;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return new ValidationResult(ErrorMessage ?? "Date of birth is required.");

            if (value is not DateTime dob)
                return new ValidationResult("Invalid date of birth.");

            var today = DateTime.Today;

            if (dob > today)
                return new ValidationResult("Date of birth cannot be in the future.");

            var age = today.Year - dob.Year;
            if (dob.Date > today.AddYears(-age)) age--;

            return age >= _minAge
                ? ValidationResult.Success
                : new ValidationResult(ErrorMessage ?? $"You must be at least {_minAge} years old.");
        }
    }
}
