namespace ChromeRiverService.Classes.HelperClasses
{
    public static class Codes {

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

        public enum ResultType
        {
            OneUpserted,
            AllUpsertsComplete,
            UncategorizedError,
            ManuallyCreatedPeopleAreNotUpdated,
            InvalidEntity,
            InvalidAllocation
        }
    }
}