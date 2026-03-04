namespace Domain.Enums;
public enum ExpenseCategory : short
{
    // Operations
    SparePartsPurchase = 1,
    ToolsAndEquipment = 2,
    MachineMaintenance = 3,
    WorkshopSupplies = 4,

    // Labor
    StaffSalaries = 5,
    OvertimePayments = 6,
    ContractorMechanics = 7,

    // Facility
    RentLease = 8,
    Electricity = 9,
    Water = 10,
    Internet = 11,

    // Vehicle
    TestDriveFuel = 12,
    CompanyVehicleMaintenance = 13,
    VehicleInsurance = 14,

    // Inventory
    InventoryPurchase = 15,
    InventoryLossOrDamage = 16,

    // Marketing
    Advertising = 17,
    SocialMediaPromotions = 18,
    DiscountsAndOffers = 19,

    // Technology
    SoftwareSubscriptions = 20,
    HostingServerCosts = 21,
    POSBillingSystems = 22,

    // Administrative
    OfficeSupplies = 23,
    PrintingStationery = 24,
    LicensesPermits = 25,

    // Financial
    BankCharges = 26,
    LoanPayments = 27,
    Taxes = 28,

    // Misc
    MiscExpense = 29,
    EmergencyExpense = 30,
    Other = 99
}
