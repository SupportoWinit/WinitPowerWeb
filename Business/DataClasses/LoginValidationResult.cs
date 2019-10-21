using System.Collections.Generic;

namespace Business.DataClasses
{
    public class LoginValidationResult
    {
        public bool isValid { get; set; }

        public bool privacyModuleActive { get; set; }

        public string validationError { get; set; }

        public List<object> formErrors { get; private set; }

        public bool missingAdminUser { get; set; }

        public bool isWinitUser { get; set; }

        public bool fired { get; set; }

        public bool completeSecretQuestion { get; set; }

        public bool isDoubleLoginAllowed { get; set; }

        public LoginValidationResult()
        {
            formErrors = new List<object>();
        }

        public void AddFormErrors(string field, string message)
        {
            formErrors.Add(new { field = field, message = message });
        }
    }
}
