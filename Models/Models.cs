namespace D365CommandCenter.Models;

// ============================================================================
// Domain model for the Alamo Foods Co. D365 ERP Command Center demo.
// All money is decimal; dates are DateOnly. Records are immutable-ish with
// init/set so the UI can edit and persist them to localStorage.
// ============================================================================

public enum RagStatus { OnTrack, AtRisk, OffTrack, Complete }

// ---------- Finance: General Ledger ----------

public enum AccountType { Asset, Liability, Equity, Revenue, Expense }

public class GlAccount
{
    public string Number { get; set; } = "";
    public string Name { get; set; } = "";
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }          // debit-positive for Asset/Expense, credit-positive otherwise
}

public class JournalEntry
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string AccountNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Department { get; set; } = "";
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}

// ---------- Finance: AP / AR ----------

public class Vendor
{
    public string Number { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Terms { get; set; } = "";       // e.g. "Net 30"
}

public class Customer
{
    public string Number { get; set; } = "";
    public string Name { get; set; } = "";
    public string Segment { get; set; } = "";     // Retail, Foodservice, Distributor, Export
    public string Terms { get; set; } = "";
}

public enum InvoiceStatus { Open, Paid, Overdue }

public class ApInvoice
{
    public string Number { get; set; } = "";
    public string VendorNumber { get; set; } = "";
    public string VendorName { get; set; } = "";
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public InvoiceStatus Status { get; set; }
    public string ApprovalStatus { get; set; } = ""; // Approved, Pending, In Review
    public int AgeDays { get; set; }                 // 0 if paid; else days outstanding
    public string AgeBucket { get; set; } = "";      // Current, 1-30, 31-60, 61-90, 90+
}

public class ArInvoice
{
    public string Number { get; set; } = "";
    public string CustomerNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Segment { get; set; } = "";
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public InvoiceStatus Status { get; set; }
    public string CollectionStatus { get; set; } = ""; // Current, Reminded, In Collections
    public int AgeDays { get; set; }
    public string AgeBucket { get; set; } = "";
}

// ---------- Finance: Budget ----------

/// <summary>Governance metadata for the working budget version (lifecycle + approvals).</summary>
public class BudgetMeta
{
    public string Version { get; set; } = "FY26 Original";
    public string Status { get; set; } = "Approved";   // Draft, Submitted, Approved, Locked
    public string Owner { get; set; } = "";
    public string Approver { get; set; } = "";
    public DateOnly? SubmittedDate { get; set; }
    public DateOnly? ApprovedDate { get; set; }
    public string Scenario { get; set; } = "";          // e.g. "Annual operating plan"
    public List<string> Versions { get; set; } = new(); // selectable version labels
}

public class BudgetLine
{
    public string Department { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Budget { get; set; }
    public decimal Actual { get; set; }
    public decimal Variance => Budget - Actual;
    public decimal VariancePct => Budget == 0 ? 0 : Math.Round((Budget - Actual) / Budget * 100, 1);

    /// <summary>The posted transactions that make up <see cref="Actual"/> (sums to Actual).</summary>
    public List<BudgetTxn> Transactions { get; set; } = new();
}

/// <summary>An individual posting that contributes to a budget line's actual spend.</summary>
public class BudgetTxn
{
    public DateOnly Date { get; set; }
    public string Reference { get; set; } = "";   // INV-/PAY-/EXP-/SUB-/UTL-/CAP-####
    public string Type { get; set; } = "";         // Vendor invoice, Payroll run, Expense report, …
    public string Payee { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
}

// ---------- Supply Chain ----------

public class Warehouse
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Location { get; set; } = "";
}

public enum StockState { Stockout, Critical, Low, Healthy, Excess }

public class InventoryItem
{
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";       // Raw Material, WIP, Finished Good, Packaging
    public string WarehouseCode { get; set; } = "";
    public int OnHand { get; set; }
    public int ReorderPoint { get; set; }
    public decimal UnitCost { get; set; }
    public decimal UnitPrice { get; set; }

    // Planning inputs (seeded)
    public double AvgDailyUsage { get; set; }        // units consumed/shipped per day
    public int LeadTimeDays { get; set; }            // replenishment lead time
    public int SafetyStock { get; set; }             // buffer below which stock is critical
    public DateOnly LastCount { get; set; }          // last physical/cycle count

    public string AbcClass { get; set; } = "";       // assigned by the page (value ranking)

    public decimal Value => OnHand * UnitCost;        // on-hand at cost
    public decimal RetailValue => OnHand * UnitPrice;
    public decimal Margin => UnitPrice - UnitCost;
    public decimal MarginPct => UnitPrice == 0 ? 0 : Math.Round(Margin / UnitPrice * 100, 1);
    public bool BelowReorder => OnHand < ReorderPoint;

    /// <summary>How many days the current on-hand will last at average usage.</summary>
    public double DaysOfSupply => AvgDailyUsage <= 0 ? (OnHand > 0 ? 999 : 0) : Math.Round(OnHand / AvgDailyUsage, 1);
    /// <summary>Annualised consumption at cost — basis for turns and ABC ranking.</summary>
    public decimal AnnualUsageValue => Math.Round((decimal)(AvgDailyUsage * 365) * UnitCost, 2);
    /// <summary>Approximate inventory turns (annual usage value ÷ on-hand value).</summary>
    public double Turns => Value <= 0 ? 0 : Math.Round((double)(AnnualUsageValue / Value), 1);
    /// <summary>Suggested replenishment to bring stock back to reorder + safety.</summary>
    public int SuggestedReorderQty => Math.Max(0, ReorderPoint + SafetyStock - OnHand);

    public StockState State =>
        OnHand <= 0 ? StockState.Stockout :
        OnHand < SafetyStock ? StockState.Critical :
        OnHand < ReorderPoint ? StockState.Low :
        DaysOfSupply > 120 ? StockState.Excess :
        StockState.Healthy;
}

public enum PoStatus { Draft, Submitted, Approved, Received }

public class PurchaseOrder
{
    public string Number { get; set; } = "";
    public string VendorNumber { get; set; } = "";
    public string VendorName { get; set; } = "";
    public DateOnly OrderDate { get; set; }
    public DateOnly ExpectedDate { get; set; }
    public decimal Total { get; set; }
    public PoStatus Status { get; set; }
    public string ApprovalStatus { get; set; } = "";
    public string Buyer { get; set; } = "";
}

public enum SoStatus { Open, Picked, Shipped, Invoiced }

public class SalesLine
{
    public string Product { get; set; } = "";
    public string Category { get; set; } = "";
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Math.Round(Qty * UnitPrice, 2);
}

public class SalesOrder
{
    public string Number { get; set; } = "";
    public string CustomerNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Segment { get; set; } = "";
    public DateOnly OrderDate { get; set; }
    public DateOnly ShipDate { get; set; }
    public decimal Total { get; set; }
    public SoStatus Status { get; set; }
    public List<SalesLine> Lines { get; set; } = new();
}

// ---------- Implementation delivery ----------

public class ProjectPhase
{
    public int Id { get; set; }
    public string Name { get; set; } = "";          // Initiate, Implement, Prepare, Operate
    public DateOnly Start { get; set; }
    public DateOnly End { get; set; }
    public int PercentComplete { get; set; }
    public RagStatus Rag { get; set; }
}

public class Milestone
{
    public int Id { get; set; }
    public int PhaseId { get; set; }
    public string PhaseName { get; set; } = "";
    public string Name { get; set; } = "";
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = "";        // Not Started, In Progress, Complete, At Risk
    public string Owner { get; set; } = "";
}

public enum RaidType { Risk, Assumption, Issue, Dependency }

public class RaidEntry
{
    public string Id { get; set; } = "";
    public RaidType Type { get; set; }
    public string Title { get; set; } = "";
    public int Probability { get; set; }            // 1-5
    public int Impact { get; set; }                 // 1-5
    public int Score => Probability * Impact;
    public string Severity => Score >= 15 ? "High" : Score >= 8 ? "Medium" : "Low";
    public string Owner { get; set; } = "";
    public string Status { get; set; } = "";        // Open, Mitigating, Closed
    public string Response { get; set; } = "";
}

public enum FitGap { Standard, Configuration, Customization, ISV }

public class Requirement
{
    public string Code { get; set; } = "";          // e.g. FIN-012
    public string Area { get; set; } = "";          // Finance, Procurement, Inventory, Sales, Reporting
    public string Description { get; set; } = "";
    public string Priority { get; set; } = "";      // Must, Should, Could
    public string ProcessArea { get; set; } = "";   // P2P, O2C, R2R
    public FitGap FitGap { get; set; }
    public int EffortDays { get; set; }
    public string TestStatus { get; set; } = "";    // Not Run, Pass, Fail
}

public enum TestResult { NotRun, Pass, Fail }

public class TestCase
{
    public string Code { get; set; } = "";          // TC-101
    public string RequirementCode { get; set; } = "";
    public string Title { get; set; } = "";
    public string ProcessArea { get; set; } = "";
    public TestResult Result { get; set; }
    public DateOnly? LastRun { get; set; }
    public string Tester { get; set; } = "";
}

public class ProcessStep
{
    public int Order { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string System { get; set; } = "";        // D365 module touched
}

public class ProcessFlow
{
    public string Code { get; set; } = "";          // P2P, O2C, R2R
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<ProcessStep> Steps { get; set; } = new();
}

// ---------- Governance: audit / activity trail ----------

public class AuditEntry
{
    public DateTime Timestamp { get; set; }          // UTC
    public string User { get; set; } = "";
    public string Role { get; set; } = "";
    public string Module { get; set; } = "";         // Budget, GL, AP, Security, System…
    public string Action { get; set; } = "";
    public string Detail { get; set; } = "";
}
