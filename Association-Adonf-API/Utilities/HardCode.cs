namespace AssociationAdonfAPI.Utilities
{
    public static class HardCode
    {
        public static Guid STATUS_COMPLETED_ID => Guid.Parse("6662dfc1-9c40-4d78-806f-34cd22e07023");
        public static Guid STATUS_IN_PROGRESS_ID => Guid.Parse("2281c955-b3e1-49dc-be62-6a7912bb46b3");
        public static Guid STATUS_WAITING_ID => Guid.Parse("17c07323-d5b4-4568-b773-de3487ff30b1");


        public static Guid PRIORITY_PRIMARY_ID => Guid.Parse("6662dfc1-9c40-4d78-806f-34cd22e07023");
        public static Guid PRIORITY_SECONDARY_ID => Guid.Parse("2281c955-b3e1-49dc-be62-6a7912bb46b3");
        public static Guid PRIORITY_TERTIARY_ID => Guid.Parse("17c07323-d5b4-4568-b773-de3487ff30b1");
    }
}
