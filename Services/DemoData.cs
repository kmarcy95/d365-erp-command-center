using D365CommandCenter.Models;

namespace D365CommandCenter.Services;

/// <summary>Container for the whole demo dataset (one object persisted to localStorage).</summary>
public class DemoData
{
    public int Version { get; set; } = 6;
    public string CompanyName { get; set; } = "Alamo Foods Co.";
    public DateOnly AsOf { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;

    public BudgetMeta BudgetMeta { get; set; } = new();
    public List<AuditEntry> AuditLog { get; set; } = new();

    public List<GlAccount> GlAccounts { get; set; } = new();
    public List<JournalEntry> JournalEntries { get; set; } = new();
    public List<Vendor> Vendors { get; set; } = new();
    public List<Customer> Customers { get; set; } = new();
    public List<ApInvoice> ApInvoices { get; set; } = new();
    public List<ArInvoice> ArInvoices { get; set; } = new();
    public List<BudgetLine> Budget { get; set; } = new();
    public List<Warehouse> Warehouses { get; set; } = new();
    public List<InventoryItem> Inventory { get; set; } = new();
    public ReservePolicy ReservePolicy { get; set; } = ReservePolicy.Default();
    public List<InventorySnapshot> InventorySnapshots { get; set; } = new();
    public List<PurchaseOrder> PurchaseOrders { get; set; } = new();
    public List<SalesOrder> SalesOrders { get; set; } = new();
    public List<ProjectPhase> Phases { get; set; } = new();
    public List<Milestone> Milestones { get; set; } = new();
    public List<RaidEntry> Raid { get; set; } = new();
    public List<Requirement> Requirements { get; set; } = new();
    public List<TestCase> TestCases { get; set; } = new();
    public List<ProcessFlow> Processes { get; set; } = new();
}
