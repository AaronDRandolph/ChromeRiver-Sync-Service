namespace ChromeRiverService.Classes.Helpers
{
    public static class Codes
    {

        // People code come from the IAM database, and are different than those onPrem. 
        public enum EmploymentStatus
        {
            Supspended = 7001,
            Terminated = 7002,
            OnLeave = 7003,
            Revoked = 7004,
            Active = 7005,
        }

        public enum Department
        {
            AccountingAndFinance = 3,
            IT = 20,
            RegionalInitiatives = 28,
            WorkForceInitiatives = 32,
        }

        public enum ContactType
        {
            WorkEmail = 2008,
        }

        public enum HttpResponses
        {
            AllUpsertedSuccessfully = 200,
            SomeUpsertedSuccessfully = 207,
        }
    }
}