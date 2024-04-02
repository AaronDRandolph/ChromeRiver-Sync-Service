namespace ChromeRiverService.Classes.HelperClasses
{
    public static class Codes {

        // People code come from the IAM database, and are different than those onPrem. 
        public enum People
        {
            ITDepartment = 20,
            WorkEmail = 2008,
            SupspendedEmployee = 7001,
            TerminatedEmployee = 7002,
            OnLeaveEmployee = 7003,
            RevokedEmployee = 7004,
            ActiveEmployee = 7005,
        }
        public enum HttpResponses
        {
            AllUpsertedSuccessfully = 200,
            SomeUpsertedSuccessfully = 207,
        }
    }
}