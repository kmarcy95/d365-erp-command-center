using D365CommandCenter.Models;

namespace D365CommandCenter.Services;

/// <summary>
/// Builds a deterministic, internally-consistent demo dataset for Alamo Foods Co.
/// Subledgers are generated first, then GL control accounts are derived from them
/// (AP/AR/Inventory tie to their subledgers) and a retained-earnings plug keeps the
/// trial balance balanced — the kind of consistency a real ERP analyst expects.
/// </summary>
public static class SeedData
{
    private static readonly DateOnly AsOf = new(2026, 5, 29);
    private static readonly Random Rng = new(42);

    private static T Pick<T>(IReadOnlyList<T> items) => items[Rng.Next(items.Count)];
    private static decimal Money(int min, int max) => Math.Round((decimal)(Rng.Next(min, max) + Rng.NextDouble()), 2);

    public static DemoData Build()
    {
        var d = new DemoData { AsOf = AsOf };

        BuildWarehouses(d);
        BuildVendors(d);
        BuildCustomers(d);
        BuildInventory(d);
        BuildApInvoices(d);
        BuildArInvoices(d);
        BuildPurchaseOrders(d);
        BuildSalesOrders(d);
        BuildBudget(d);
        BuildGl(d);
        BuildJournal(d);
        BuildImplementation(d);
        BuildProcesses(d);
        BuildGovernance(d);
        return d;
    }

    // ---------------- Governance: budget lifecycle + audit trail ----------------

    private static void BuildGovernance(DemoData d)
    {
        d.BudgetMeta = new BudgetMeta
        {
            Version = "FY26 Original",
            Status = "Approved",
            Scenario = "Annual operating plan",
            Owner = "Keith Marcy, Financial Controller",
            Approver = "D. Alvarez, VP Finance",
            SubmittedDate = new DateOnly(2025, 11, 18),
            ApprovedDate = new DateOnly(2025, 12, 5),
            Versions = new List<string> { "FY26 Original", "FY26 Q1 Revision", "FY26 Forecast" }
        };

        // A plausible recent activity trail. Timestamps are UTC, walking back from AsOf.
        var baseTs = d.AsOf.ToDateTime(new TimeOnly(16, 0)).ToUniversalTime();
        AuditEntry E(double hoursAgo, string user, string role, string module, string action, string detail)
            => new() { Timestamp = baseTs.AddHours(-hoursAgo), User = user, Role = role, Module = module, Action = action, Detail = detail };

        d.AuditLog = new List<AuditEntry>
        {
            E(0.4, "Keith Marcy", "Financial Controller", "Budget", "Viewed report", "Opened Budget vs Actual (FY26 Original)"),
            E(2.1, "Keith Marcy", "Financial Controller", "Budget", "Exported data", "Downloaded budget-vs-actual.csv"),
            E(6.5, "S. Whitfield", "FP&A Analyst", "Budget", "Edited line", "Marketing · Trade Promotions actual updated to period close"),
            E(20, "System", "Integration", "GL", "Data sync", "Posted GL period May FY26 from D365 F&O (1,284 entries)"),
            E(21, "System", "Integration", "AP", "Data sync", "Imported 64 vendor invoices via Data Management Framework"),
            E(28, "D. Alvarez", "VP Finance", "Budget", "Approved", "Approved FY26 Original operating plan"),
            E(30, "Keith Marcy", "Financial Controller", "Budget", "Submitted", "Submitted FY26 Original for approval"),
            E(52, "M. Okafor", "Security Admin", "Security", "Role change", "Granted 'Budget contributor' to S. Whitfield"),
            E(73, "System", "Integration", "Inventory", "Data sync", "Refreshed on-hand from warehouse SAT/DAL/HOU"),
            E(96, "Keith Marcy", "Financial Controller", "GL", "Closed period", "Soft-closed April FY26"),
        };
    }

    // ---------------- Supply chain master data ----------------

    private static void BuildWarehouses(DemoData d)
    {
        d.Warehouses.Add(new Warehouse { Code = "SAT", Name = "San Antonio Plant", Location = "San Antonio, TX" });
        d.Warehouses.Add(new Warehouse { Code = "DAL", Name = "Dallas DC", Location = "Dallas, TX" });
        d.Warehouses.Add(new Warehouse { Code = "HOU", Name = "Houston DC", Location = "Houston, TX" });
    }

    private static readonly string[] VendorNames =
    {
        "Lone Star Grain Mills", "Hill Country Dairy", "Rio Grande Packaging", "Gulf Coast Logistics",
        "Pecan Valley Sweeteners", "Alamo Industrial Supply", "Texas Cold Storage", "Brazos Oils & Fats",
        "Coastal Bend Seasonings", "Permian Equipment Co.", "Bluebonnet Print & Label", "Frontier Pallets",
        "Trinity Sanitation Services", "Big Bend Maintenance", "Llano Yeast & Cultures", "Guadalupe Water Systems",
        "El Camino Freight", "Mesa Verde Produce", "Cibolo Steel Fabrication", "Pioneer Energy Partners",
        "Westar Insurance Brokers", "Comal Staffing Group", "Nueces Chemicals", "Red River Transport",
        "Sabine Recycling", "Caldwell Tooling", "Medina Flour Co.", "San Marcos Software LLC" };

    private static readonly string[] VendorCategories =
    {
        "Raw Materials", "Packaging", "Logistics", "Utilities", "MRO", "Professional Services" };

    private static readonly string[] Terms = { "Net 15", "Net 30", "Net 45", "Net 60" };

    private static void BuildVendors(DemoData d)
    {
        for (int i = 0; i < VendorNames.Length; i++)
            d.Vendors.Add(new Vendor
            {
                Number = $"V{1000 + i}",
                Name = VendorNames[i],
                Category = VendorCategories[i % VendorCategories.Length],
                Terms = Pick(Terms)
            });
    }

    private static readonly string[] CustomerNames =
    {
        "H-E-B Grocery", "Lone Star Foodservice", "Alamo City Markets", "Gulf Coast Distributors",
        "Texas Star Restaurants", "Border Town Wholesale", "Capital Grocers", "Coastal Cafes Group",
        "Hill Country Markets", "Pecos Provisions", "Rio Foods Export", "Mesa Dining Co.",
        "Frontier Convenience", "Bluebonnet Bakeries", "Trinity Catering", "Brazos Bistros",
        "Comal Cafeterias", "Nueces Nutrition", "Sabine Supermarkets", "Permian Pantry",
        "Cibolo Cafes", "Guadalupe Grocers", "Medina Meals", "Llano Larder",
        "Caldwell Cuisine", "Red River Retail", "El Paso Provisions", "San Marcos Snacks",
        "Westar Wholesale", "Pioneer Pantries" };

    private static readonly string[] Segments = { "Retail", "Foodservice", "Distributor", "Export" };

    private static void BuildCustomers(DemoData d)
    {
        for (int i = 0; i < CustomerNames.Length; i++)
            d.Customers.Add(new Customer
            {
                Number = $"C{2000 + i}",
                Name = CustomerNames[i],
                Segment = Segments[i % Segments.Length],
                Terms = Pick(Terms)
            });
    }

    private static readonly string[] ItemCategories = { "Raw Material", "Packaging", "WIP", "Finished Good" };
    private static readonly string[] ItemNouns =
    {
        "Corn Tortilla", "Flour Tortilla", "Salsa Roja", "Queso Blanco", "Refried Beans", "Tamale Mix",
        "Chili Seasoning", "Masa Harina", "Pinto Beans", "Jalapeño Relish", "Taco Shell", "Enchilada Sauce",
        "Guacamole Cup", "Tortilla Chips", "Breakfast Burrito", "Carne Picada", "Pico de Gallo", "Hot Sauce" };

    private static void BuildInventory(DemoData d)
    {
        int n = 0;
        foreach (var wh in d.Warehouses)
        {
            for (int i = 0; i < 30; i++)
            {
                var noun = ItemNouns[n % ItemNouns.Length];
                var cat = ItemCategories[n % ItemCategories.Length];
                var cost = Money(2, 24);

                // Planning inputs, then derive a coherent reorder point = lead-time demand + safety.
                double usage = Math.Round(Rng.Next(2, 60) + Rng.NextDouble(), 1);
                int lead = Rng.Next(3, 22);
                int safety = (int)Math.Round(usage * Rng.Next(2, 8));
                int reorder = (int)Math.Round(usage * lead) + safety;
                // Spread on-hand so we get stockouts, lows, healthy and excess across the catalog.
                int onHand = (int)Math.Round(reorder * (0.0 + Rng.NextDouble() * 4.2));

                d.Inventory.Add(new InventoryItem
                {
                    Sku = $"FG{4000 + n}",
                    Name = $"{noun} {(n % 3 == 0 ? "Family" : n % 3 == 1 ? "Case" : "Single")}",
                    Category = cat,
                    WarehouseCode = wh.Code,
                    OnHand = onHand,
                    ReorderPoint = reorder,
                    UnitCost = cost,
                    UnitPrice = Math.Round(cost * (decimal)(1.45 + Rng.NextDouble() * 0.5), 2),
                    AvgDailyUsage = usage,
                    LeadTimeDays = lead,
                    SafetyStock = safety,
                    LastCount = AsOf.AddDays(-Rng.Next(1, 95))
                });
                n++;
            }
        }
    }

    private static string Bucket(int ageDays) =>
        ageDays <= 0 ? "Current" : ageDays <= 30 ? "1-30" : ageDays <= 60 ? "31-60" : ageDays <= 90 ? "61-90" : "90+";

    private static void BuildApInvoices(DemoData d)
    {
        for (int i = 0; i < 110; i++)
        {
            var v = Pick(d.Vendors);
            var inv = new ApInvoice
            {
                Number = $"AP-{50000 + i}",
                VendorNumber = v.Number,
                VendorName = v.Name,
                InvoiceDate = AsOf.AddDays(-Rng.Next(5, 140)),
                Amount = Money(800, 90000),
                ApprovalStatus = Pick(new[] { "Approved", "Approved", "Approved", "Pending", "In Review" })
            };
            inv.DueDate = inv.InvoiceDate.AddDays(30);
            bool paid = Rng.NextDouble() < 0.45;
            if (paid)
            {
                inv.Status = InvoiceStatus.Paid; inv.AgeDays = 0; inv.AgeBucket = "Current";
            }
            else
            {
                inv.AgeDays = Math.Max(0, AsOf.DayNumber - inv.DueDate.DayNumber);
                inv.Status = inv.AgeDays > 0 ? InvoiceStatus.Overdue : InvoiceStatus.Open;
                inv.AgeBucket = Bucket(inv.AgeDays);
            }
            d.ApInvoices.Add(inv);
        }
    }

    private static void BuildArInvoices(DemoData d)
    {
        for (int i = 0; i < 115; i++)
        {
            var c = Pick(d.Customers);
            var inv = new ArInvoice
            {
                Number = $"AR-{70000 + i}",
                CustomerNumber = c.Number,
                CustomerName = c.Name,
                Segment = c.Segment,
                InvoiceDate = AsOf.AddDays(-Rng.Next(5, 150)),
                Amount = Money(1200, 120000)
            };
            inv.DueDate = inv.InvoiceDate.AddDays(30);
            bool paid = Rng.NextDouble() < 0.5;
            if (paid)
            {
                inv.Status = InvoiceStatus.Paid; inv.AgeDays = 0; inv.AgeBucket = "Current";
                inv.CollectionStatus = "Current";
            }
            else
            {
                inv.AgeDays = Math.Max(0, AsOf.DayNumber - inv.DueDate.DayNumber);
                inv.Status = inv.AgeDays > 0 ? InvoiceStatus.Overdue : InvoiceStatus.Open;
                inv.AgeBucket = Bucket(inv.AgeDays);
                inv.CollectionStatus = inv.AgeDays > 60 ? "In Collections" : inv.AgeDays > 0 ? "Reminded" : "Current";
            }
            d.ArInvoices.Add(inv);
        }
    }

    private static void BuildPurchaseOrders(DemoData d)
    {
        for (int i = 0; i < 55; i++)
        {
            var v = Pick(d.Vendors);
            var status = Pick(new[] { PoStatus.Draft, PoStatus.Submitted, PoStatus.Approved, PoStatus.Approved, PoStatus.Received, PoStatus.Received });
            var od = AsOf.AddDays(-Rng.Next(1, 90));
            d.PurchaseOrders.Add(new PurchaseOrder
            {
                Number = $"PO-{90000 + i}",
                VendorNumber = v.Number,
                VendorName = v.Name,
                OrderDate = od,
                ExpectedDate = od.AddDays(Rng.Next(7, 35)),
                Total = Money(2000, 140000),
                Status = status,
                ApprovalStatus = status == PoStatus.Draft ? "Pending" : "Approved",
                Buyer = Pick(new[] { "M. Reyna", "T. Okafor", "S. Patel", "J. Daniels" })
            });
        }
    }

    // Alamo Foods Co. finished-goods catalog: (product, category, low-unit-price, high-unit-price).
    private static readonly (string Product, string Category, int Low, int High)[] SalesCatalog =
    {
        ("Mesquite BBQ Sauce 18oz",      "Sauces & Marinades", 18, 34),
        ("Jalapeño Brine 1gal",          "Sauces & Marinades", 22, 40),
        ("Chipotle Adobo 12oz",          "Sauces & Marinades", 16, 28),
        ("Corn Tortilla Flour 50lb",     "Flour & Grains",     28, 46),
        ("Stone-Ground Masa 25lb",       "Flour & Grains",     24, 38),
        ("Hill Country Wheat Flour 50lb","Flour & Grains",     26, 42),
        ("Pecan Praline Spread 16oz",    "Sweet Goods",        20, 36),
        ("Bluebonnet Honey 24oz",        "Sweet Goods",        24, 44),
        ("Texas Pepper Blend 8oz",       "Seasonings",         12, 22),
        ("Smoked Paprika Rub 10oz",      "Seasonings",         14, 26),
        ("Refried Pinto Beans #10 can",  "Canned Goods",       18, 30),
        ("Roasted Salsa Verde #10 can",  "Canned Goods",       20, 34),
    };

    private static void BuildSalesOrders(DemoData d)
    {
        for (int i = 0; i < 60; i++)
        {
            var c = Pick(d.Customers);
            var status = Pick(new[] { SoStatus.Open, SoStatus.Picked, SoStatus.Shipped, SoStatus.Shipped, SoStatus.Invoiced, SoStatus.Invoiced });
            var od = AsOf.AddDays(-Rng.Next(1, 75));

            var order = new SalesOrder
            {
                Number = $"SO-{80000 + i}",
                CustomerNumber = c.Number,
                CustomerName = c.Name,
                Segment = c.Segment,
                OrderDate = od,
                ShipDate = od.AddDays(Rng.Next(2, 14)),
                Status = status
            };

            var lineCount = Rng.Next(2, 6);                 // 2–5 distinct lines
            var used = new HashSet<int>();
            for (int j = 0; j < lineCount; j++)
            {
                int idx;
                do { idx = Rng.Next(SalesCatalog.Length); } while (!used.Add(idx));
                var sku = SalesCatalog[idx];
                order.Lines.Add(new SalesLine
                {
                    Product = sku.Product,
                    Category = sku.Category,
                    Qty = Rng.Next(20, 600),
                    UnitPrice = Money(sku.Low, sku.High)
                });
            }
            order.Total = order.Lines.Sum(l => l.LineTotal);

            d.SalesOrders.Add(order);
        }
    }

    private static readonly string[] Departments =
    { "Production", "Procurement", "Logistics", "Sales & Marketing", "Quality", "Finance", "IT", "HR" };

    private static void BuildBudget(DemoData d)
    {
        var categories = new[] { "Salaries", "Materials", "Equipment", "Travel", "Software", "Utilities" };
        foreach (var dept in Departments)
            foreach (var cat in categories)
            {
                if (Rng.NextDouble() < 0.25) continue; // not every dept has every category
                var budget = Money(40000, 900000);
                var actual = Math.Round(budget * (decimal)(0.78 + Rng.NextDouble() * 0.4), 2);
                var line = new BudgetLine
                {
                    Department = dept,
                    Category = cat,
                    Budget = budget,
                    Actual = actual,
                    Transactions = BuildBudgetTxns(dept, cat, actual)
                };
                d.Budget.Add(line);
            }
    }

    // Category-specific transaction "shapes" so a drill-down reads like real subledger detail.
    private static readonly Dictionary<string, (string Type, string RefPrefix, string Desc, string[] Payees)> TxnShape = new()
    {
        ["Salaries"]  = ("Payroll run", "PAY", "Bi-weekly payroll & benefits", new[] { "Payroll — Hourly", "Payroll — Salaried", "Benefits true-up", "Overtime & shift premium" }),
        ["Materials"] = ("Vendor invoice", "INV", "Raw material purchase", new[] { "Lone Star Grain Mills", "Hill Country Dairy", "Pecan Valley Sweeteners", "Brazos Oils & Fats", "Coastal Bend Seasonings", "Medina Flour Co." }),
        ["Equipment"] = ("Capital purchase", "CAP", "Equipment & tooling", new[] { "Permian Equipment Co.", "Cibolo Steel Fabrication", "Caldwell Tooling", "Big Bend Maintenance" }),
        ["Travel"]    = ("Expense report", "EXP", "Travel & entertainment", new[] { "Expense report — K. Marcy", "Expense report — S. Patel", "Expense report — T. Okafor", "Expense report — M. Reyna" }),
        ["Software"]  = ("Subscription", "SUB", "SaaS / license renewal", new[] { "San Marcos Software LLC", "Microsoft 365", "Adobe Creative Cloud", "Dynamics 365 licensing" }),
        ["Utilities"] = ("Utility bill", "UTL", "Monthly utilities", new[] { "Pioneer Energy Partners", "Guadalupe Water Systems", "Trinity Sanitation Services" }),
    };

    private static List<BudgetTxn> BuildBudgetTxns(string dept, string cat, decimal actual)
    {
        var shape = TxnShape.TryGetValue(cat, out var s) ? s
            : (Type: "Journal posting", RefPrefix: "JE", Desc: "Posted expense", Payees: new[] { "General ledger" });

        int n = Rng.Next(4, 9);
        // Random positive weights → amounts that sum exactly to actual (last absorbs rounding).
        var weights = Enumerable.Range(0, n).Select(_ => Rng.NextDouble() + 0.15).ToArray();
        double wsum = weights.Sum();

        var txns = new List<BudgetTxn>();
        decimal running = 0m;
        for (int i = 0; i < n; i++)
        {
            decimal amt = i == n - 1
                ? Math.Round(actual - running, 2)
                : Math.Round(actual * (decimal)(weights[i] / wsum), 2);
            running += amt;
            var payee = shape.Payees[Rng.Next(shape.Payees.Length)];
            txns.Add(new BudgetTxn
            {
                Date = AsOf.AddDays(-Rng.Next(2, 150)),
                Reference = $"{shape.RefPrefix}-{Rng.Next(10000, 99999)}",
                Type = shape.Type,
                Payee = payee,
                Description = $"{shape.Desc} · {dept}",
                Amount = amt
            });
        }
        return txns.OrderByDescending(t => t.Amount).ToList();
    }

    // ---------------- GL derived from subledgers ----------------

    private static void BuildGl(DemoData d)
    {
        decimal openAp = d.ApInvoices.Where(x => x.Status != InvoiceStatus.Paid).Sum(x => x.Amount);
        decimal openAr = d.ArInvoices.Where(x => x.Status != InvoiceStatus.Paid).Sum(x => x.Amount);
        decimal inventoryValue = d.Inventory.Sum(x => x.Value);

        decimal cash = 3_120_540m;
        decimal fixedAssets = 8_415_000m;
        decimal accrued = 940_000m;
        decimal longTermDebt = 5_200_000m;
        decimal revenue = 52_480_000m;
        decimal cogs = 36_240_000m;
        decimal opex = 9_870_000m;
        decimal payroll = 4_180_000m;
        decimal depreciation = 1_260_000m;
        decimal commonStock = 2_000_000m;

        decimal debits = cash + openAr + inventoryValue + fixedAssets + cogs + opex + payroll + depreciation;
        decimal creditsExclRe = openAp + accrued + longTermDebt + commonStock + revenue;
        decimal retainedEarnings = debits - creditsExclRe; // plug to balance

        void Add(string num, string name, AccountType t, decimal bal) =>
            d.GlAccounts.Add(new GlAccount { Number = num, Name = name, Type = t, Balance = Math.Round(bal, 2) });

        Add("1000", "Cash & Cash Equivalents", AccountType.Asset, cash);
        Add("1100", "Accounts Receivable", AccountType.Asset, openAr);
        Add("1200", "Inventory", AccountType.Asset, inventoryValue);
        Add("1500", "Property, Plant & Equipment (net)", AccountType.Asset, fixedAssets);
        Add("2000", "Accounts Payable", AccountType.Liability, openAp);
        Add("2100", "Accrued Liabilities", AccountType.Liability, accrued);
        Add("2500", "Long-Term Debt", AccountType.Liability, longTermDebt);
        Add("3000", "Common Stock", AccountType.Equity, commonStock);
        Add("3100", "Retained Earnings", AccountType.Equity, retainedEarnings);
        Add("4000", "Sales Revenue", AccountType.Revenue, revenue);
        Add("5000", "Cost of Goods Sold", AccountType.Expense, cogs);
        Add("6000", "Operating Expenses", AccountType.Expense, opex);
        Add("6500", "Payroll Expense", AccountType.Expense, payroll);
        Add("6900", "Depreciation Expense", AccountType.Expense, depreciation);
    }

    private static void BuildJournal(DemoData d)
    {
        var samples = new[]
        {
            ("4000","Sales Revenue","Sales & Marketing","Daily sales summary"),
            ("1100","Accounts Receivable","Finance","Customer invoicing"),
            ("5000","Cost of Goods Sold","Production","Production cost posting"),
            ("1200","Inventory","Logistics","Goods receipt"),
            ("2000","Accounts Payable","Procurement","Vendor invoice"),
            ("6500","Payroll Expense","HR","Bi-weekly payroll"),
            ("6000","Operating Expenses","Finance","Utilities & overhead"),
            ("1000","Cash & Cash Equivalents","Finance","Customer payment received"),
        };
        for (int i = 0; i < 60; i++)
        {
            var s = samples[i % samples.Length];
            var amt = Money(2500, 180000);
            bool debit = i % 2 == 0;
            d.JournalEntries.Add(new JournalEntry
            {
                Id = i + 1,
                Date = AsOf.AddDays(-Rng.Next(0, 120)),
                AccountNumber = s.Item1,
                AccountName = s.Item2,
                Department = s.Item3,
                Description = s.Item4,
                Debit = debit ? amt : 0,
                Credit = debit ? 0 : amt
            });
        }
    }

    // ---------------- Implementation delivery ----------------

    private static void BuildImplementation(DemoData d)
    {
        d.Phases.Add(new ProjectPhase { Id = 1, Name = "Initiate", Start = new(2025, 9, 1), End = new(2025, 10, 31), PercentComplete = 100, Rag = RagStatus.Complete });
        d.Phases.Add(new ProjectPhase { Id = 2, Name = "Implement", Start = new(2025, 11, 1), End = new(2026, 4, 30), PercentComplete = 100, Rag = RagStatus.Complete });
        d.Phases.Add(new ProjectPhase { Id = 3, Name = "Prepare", Start = new(2026, 5, 1), End = new(2026, 7, 15), PercentComplete = 62, Rag = RagStatus.AtRisk });
        d.Phases.Add(new ProjectPhase { Id = 4, Name = "Operate", Start = new(2026, 7, 16), End = new(2026, 9, 30), PercentComplete = 0, Rag = RagStatus.OnTrack });

        // (phase, name, due, status, owner, pct, health, deliverables)
        var ms = new (int phase, string name, string due, string status, string owner, int pct, string health, string[] deliv)[]
        {
            (1,"Project charter approved","2025-09-20","Complete","K. Marcy",100,"Signed off on schedule.",
                new[]{"Project charter","Governance model","Stakeholder register"}),
            (1,"Solution blueprint signed off","2025-10-25","Complete","K. Marcy",100,"Approved by steering committee.",
                new[]{"Solution blueprint","Scope statement","High-level architecture"}),
            (2,"Finance module configured","2025-12-15","Complete","S. Patel",100,"GL, AP, AR, Budget configured & unit-tested.",
                new[]{"Chart of accounts","Financial dimensions","Posting profiles","Budget control rules"}),
            (2,"Procurement & Inventory configured","2026-01-31","Complete","T. Okafor",100,"P2P and warehouse setup complete.",
                new[]{"Vendor groups","Item model groups","Warehouse layout","Approval workflows"}),
            (2,"Data migration dry run 1","2026-02-20","Complete","M. Reyna",100,"First load at 92% accuracy; gaps logged.",
                new[]{"Migration templates","Dry-run 1 reconciliation","Data-quality report"}),
            (2,"Integrations build complete","2026-03-25","Complete","J. Daniels",100,"EDI, bank, and planning interfaces built.",
                new[]{"EDI 850/810 maps","Bank file interface","Forecast import"}),
            (2,"SIT (System Integration Test) passed","2026-04-28","Complete","K. Marcy",100,"End-to-end SIT signed off.",
                new[]{"SIT test results","Defect log","Integration sign-off"}),
            (3,"UAT round 1 complete","2026-05-23","Complete","K. Marcy",100,"Round 1 passed; 9 defects retested.",
                new[]{"UAT round 1 results","Defect triage log"}),
            (3,"UAT round 2 complete","2026-06-13","In Progress","K. Marcy",55,"On track; SME availability is the watch item.",
                new[]{"UAT round 2 scripts","Business sign-off"}),
            (3,"Cutover plan approved","2026-06-20","In Progress","S. Patel",40,"Draft in review with infrastructure team.",
                new[]{"Cutover runbook","Rollback plan","Cutover schedule"}),
            (3,"Data migration final load","2026-07-05","Not Started","M. Reyna",0,"Awaiting dry-run 2 acceptance.",
                new[]{"Final load reconciliation","Opening balance sign-off"}),
            (3,"Go-live readiness review","2026-07-12","At Risk","K. Marcy",20,"At risk: master-data quality below threshold.",
                new[]{"Go/no-go checklist","Readiness scorecard"}),
            (4,"Go-live","2026-07-16","Not Started","K. Marcy",0,"Dependent on readiness review.",
                new[]{"Production cutover","Hypercare kickoff"}),
            (4,"Hypercare exit","2026-09-15","Not Started","K. Marcy",0,"Planned 8-week hypercare window.",
                new[]{"Hypercare exit report","Support handover","Benefits baseline"}),
        };
        int id = 1;
        foreach (var m in ms)
            d.Milestones.Add(new Milestone
            {
                Id = id++, PhaseId = m.phase, PhaseName = d.Phases.First(p => p.Id == m.phase).Name,
                Name = m.name, DueDate = DateOnly.Parse(m.due), Status = m.status, Owner = m.owner,
                PercentComplete = m.pct, Health = m.health, Deliverables = m.deliv.ToList()
            });

        // (type, title, prob, impact, owner, status, response, raisedDaysAgo, targetDate, lastUpdate)
        var raid = new (RaidType t, string title, int p, int im, string owner, string status, string resp, int raisedAgo, string? target, string update)[]
        {
            (RaidType.Risk,"Master data quality below 95% threshold for go-live",4,5,"M. Reyna","Mitigating","Daily data-cleansing sprints; dual validation",48,"2026-07-08","Quality at 92.4%; trending up ~0.5%/week."),
            (RaidType.Risk,"Key SME availability limited during UAT round 2",3,4,"K. Marcy","Open","Backfill with power users; reschedule sessions",21,"2026-06-10","Two SMEs on PTO; power-user backfill identified."),
            (RaidType.Risk,"EDI integration with H-E-B not fully tested",3,5,"J. Daniels","Mitigating","Prioritize EDI test scripts; vendor war-room",35,"2026-06-30","War-room scheduled; 6 of 10 scripts passing."),
            (RaidType.Risk,"Warehouse barcode scanners firmware incompatibility",2,4,"T. Okafor","Open","Pilot on 5 devices; order replacements if needed",30,"2026-06-25","Pilot underway on 5 units at SAT."),
            (RaidType.Issue,"Tax configuration incorrect for export segment",4,4,"S. Patel","Open","Engage tax SME; reconfigure sales tax groups",14,"2026-06-12","Tax SME engaged; reconfiguration in progress."),
            (RaidType.Issue,"Performance lag on inventory aging report",2,3,"J. Daniels","Mitigating","Add index; move to batch report",26,"2026-06-15","Index added; moving to batch in next sprint."),
            (RaidType.Issue,"Duplicate vendor records found in migration",3,2,"M. Reyna","Closed","Dedup rules applied; 312 merged",60,null,"Closed — 312 duplicates merged, rules in place."),
            (RaidType.Assumption,"Legacy system available read-only through hypercare",2,3,"K. Marcy","Open","Confirmed with infra team",40,"2026-09-15","Confirmed with infrastructure team."),
            (RaidType.Assumption,"Business freezes non-critical changes during cutover",3,4,"K. Marcy","Open","Change freeze memo issued",18,"2026-07-14","Change-freeze memo issued to department heads."),
            (RaidType.Dependency,"Bank file format sign-off from treasury",3,4,"S. Patel","Open","Treasury reviewing test files",22,"2026-06-20","Treasury reviewing test files; response expected this week."),
            (RaidType.Dependency,"Network upgrade at Dallas DC before go-live",2,5,"T. Okafor","Mitigating","IT scheduled for 2026-07-01",33,"2026-07-01","IT change scheduled and approved."),
            (RaidType.Risk,"Insufficient hypercare staffing for peak season",3,3,"K. Marcy","Open","Draft hypercare roster; on-call rotation",12,"2026-07-10","Roster drafted; on-call rotation pending HR approval."),
        };
        int rid = 1;
        foreach (var r in raid)
        {
            var raised = AsOf.AddDays(-r.raisedAgo);
            d.Raid.Add(new RaidEntry
            {
                Id = $"R-{rid++:000}", Type = r.t, Title = r.title, Probability = r.p, Impact = r.im,
                Owner = r.owner, Status = r.status, Response = r.resp,
                Raised = raised,
                TargetDate = r.target is null ? null : DateOnly.Parse(r.target),
                LastUpdate = r.update,
                AgeDays = r.raisedAgo
            });
        }

        var areas = new[] { "Finance", "Procurement", "Inventory", "Sales", "Reporting" };
        var procAreas = new[] { "R2R", "P2P", "P2P", "O2C", "R2R" };
        var fits = new[] { FitGap.Standard, FitGap.Standard, FitGap.Configuration, FitGap.Configuration, FitGap.Customization, FitGap.ISV };
        var prios = new[] { "Must", "Must", "Should", "Could" };
        var reqOwners = new[] { "K. Marcy", "S. Patel", "T. Okafor", "M. Reyna", "J. Daniels" };
        var reqStatuses = new[] { "Approved", "Approved", "Approved", "In Review", "Draft" };
        var reqText = new[]
        {
            "Post journal entries with financial dimensions", "Automate AP three-way match",
            "Customer credit limit enforcement", "Multi-warehouse on-hand visibility",
            "Lot/batch traceability for finished goods", "EDI 850/810 with major retailers",
            "Budget control with workflow approval", "Vendor self-service portal",
            "Real-time inventory reorder alerts", "Consolidated month-end close checklist",
            "Sales rebate accruals by segment", "Landed cost on imported raw materials",
            "Quality hold on received goods", "Export documentation generation",
            "Daily flash sales dashboard", "Bank reconciliation automation",
            "Production order backflush costing", "Customer aging & dunning letters",
            "Demand forecast import from planning tool", "Role-based segregation of duties",
            "Fixed asset depreciation schedules", "Intercompany eliminations",
            "Mobile warehouse picking", "Promotional pricing by customer group",
            "Returns / RMA processing", "Multi-currency for export sales",
            "Approval hierarchy for purchase orders", "Shelf-life / expiry management",
            "Commission calculation for sales reps", "Audit trail on master data changes",
        };
        var testStatuses = new[] { "Pass", "Pass", "Pass", "Fail", "Not Run" };
        for (int i = 0; i < reqText.Length; i++)
        {
            int a = i % areas.Length;
            var fit = Pick(fits);
            d.Requirements.Add(new Requirement
            {
                Code = $"{areas[a][..3].ToUpper()}-{i + 1:000}",
                Area = areas[a],
                Description = reqText[i],
                Priority = Pick(prios),
                ProcessArea = procAreas[a],
                FitGap = fit,
                EffortDays = Rng.Next(1, 25),
                TestStatus = Pick(testStatuses),
                Owner = Pick(reqOwners),
                Status = Pick(reqStatuses),
                Rationale = FitGapRationale(fit)
            });
        }

        var testers = new[] { "K. Marcy", "S. Patel", "T. Okafor", "M. Reyna" };
        int tc = 101;
        int defectNo = 201;
        foreach (var r in d.Requirements)
        {
            int cases = Rng.Next(1, 3);
            for (int j = 0; j < cases; j++)
            {
                var res = r.TestStatus switch
                {
                    "Pass" => TestResult.Pass,
                    "Fail" => Rng.NextDouble() < 0.5 ? TestResult.Fail : TestResult.Pass,
                    _ => TestResult.NotRun
                };
                var tester = Pick(testers);
                DateOnly? lastRun = res == TestResult.NotRun ? null : AsOf.AddDays(-Rng.Next(1, 30));

                // Run history: earlier attempts trend toward the final result.
                var runs = new List<TestRun>();
                if (res != TestResult.NotRun && lastRun is DateOnly lr)
                {
                    int attempts = res == TestResult.Pass ? Rng.Next(1, 3) : Rng.Next(2, 4);
                    for (int k = attempts - 1; k >= 0; k--)
                    {
                        // earliest attempts may fail; the final attempt matches the case result
                        var runRes = k == 0 ? res : (Rng.NextDouble() < 0.5 ? TestResult.Fail : TestResult.Pass);
                        runs.Add(new TestRun { Date = lr.AddDays(-k * 4), Result = runRes, Tester = tester });
                    }
                }

                d.TestCases.Add(new TestCase
                {
                    Code = $"TC-{tc++}",
                    RequirementCode = r.Code,
                    Title = $"Verify: {r.Description}",
                    ProcessArea = r.ProcessArea,
                    Result = res,
                    LastRun = lastRun,
                    Tester = tester,
                    Steps = BuildTestSteps(r.Description),
                    Runs = runs,
                    Defect = res == TestResult.Fail ? $"DEF-{defectNo++}" : null
                });
            }
        }
    }

    private static string FitGapRationale(FitGap fit) => fit switch
    {
        FitGap.Standard => "Met by standard D365 F&O functionality; configuration only.",
        FitGap.Configuration => "Achievable through parameters/setup without code.",
        FitGap.Customization => "Requires an X++ extension; assessed vs. process change.",
        FitGap.ISV => "Best served by a certified ISV add-on to avoid custom code.",
        _ => ""
    };

    private static List<string> BuildTestSteps(string desc) => new()
    {
        "Sign in with the relevant security role and open the module.",
        $"Set up preconditions for: {desc.ToLower()}.",
        "Execute the transaction with valid and boundary inputs.",
        "Verify postings, workflow state, and reporting reflect the expected result."
    };

    private static void BuildProcesses(DemoData d)
    {
        d.Processes.Add(new ProcessFlow
        {
            Code = "P2P", Name = "Procure to Pay", Owner = "T. Okafor, Procurement Lead",
            Description = "From purchase requisition through vendor payment.",
            Steps = new()
            {
                new() { Order = 1, Name = "Create requisition", Role = "Requester", System = "Procurement", CycleTimeHrs = 2,
                    Description = "Requester raises a purchase requisition with item, quantity, and need-by date.",
                    Control = "Budget availability check", RequirementCodes = new[]{ "PRO-027" } },
                new() { Order = 2, Name = "Approve requisition", Role = "Manager", System = "Workflow", CycleTimeHrs = 8,
                    Description = "Requisition routes through the approval hierarchy based on amount and category.",
                    Control = "Approval hierarchy / SoD", RequirementCodes = new[]{ "PRO-027", "FIN-021" } },
                new() { Order = 3, Name = "Issue purchase order", Role = "Buyer", System = "Procurement", CycleTimeHrs = 4,
                    Description = "Buyer converts the approved requisition into a purchase order to the vendor.",
                    Control = "Vendor on approved list", RequirementCodes = Array.Empty<string>() },
                new() { Order = 4, Name = "Receive goods", Role = "Warehouse", System = "Inventory", CycleTimeHrs = 48,
                    Description = "Warehouse records the product receipt and posts goods into on-hand inventory.",
                    Control = "Quality hold on receipt", RequirementCodes = new[]{ "INV-013", "PRO-022" } },
                new() { Order = 5, Name = "Match invoice (3-way)", Role = "AP Clerk", System = "Accounts Payable", CycleTimeHrs = 6,
                    Description = "Vendor invoice is matched to the PO and receipt; discrepancies are flagged.",
                    Control = "Three-way match tolerance", RequirementCodes = new[]{ "PRO-002" } },
                new() { Order = 6, Name = "Pay vendor", Role = "Treasury", System = "Cash & Bank", CycleTimeHrs = 24,
                    Description = "Approved invoices are settled in the payment run per the vendor's terms.",
                    Control = "Payment approval & bank file sign-off", RequirementCodes = new[]{ "FIN-016" } },
            }
        });
        d.Processes.Add(new ProcessFlow
        {
            Code = "O2C", Name = "Order to Cash", Owner = "S. Patel, O2C Lead",
            Description = "From sales order through customer collection.",
            Steps = new()
            {
                new() { Order = 1, Name = "Capture sales order", Role = "CSR", System = "Sales", CycleTimeHrs = 1,
                    Description = "Customer service captures the sales order with products, pricing, and ship date.",
                    Control = "Promotional pricing rules", RequirementCodes = new[]{ "SAL-024" } },
                new() { Order = 2, Name = "Check credit & availability", Role = "Finance", System = "Accounts Receivable", CycleTimeHrs = 3,
                    Description = "Order is checked against the customer credit limit and stock availability.",
                    Control = "Credit limit enforcement", RequirementCodes = new[]{ "INV-003", "INV-009" } },
                new() { Order = 3, Name = "Pick & pack", Role = "Warehouse", System = "Inventory", CycleTimeHrs = 12,
                    Description = "Warehouse picks, packs, and stages the order for shipment.",
                    Control = "Lot/batch traceability", RequirementCodes = new[]{ "INV-008", "SAL-019" } },
                new() { Order = 4, Name = "Ship", Role = "Logistics", System = "Sales", CycleTimeHrs = 8,
                    Description = "Goods are shipped and the packing slip / export docs are generated.",
                    Control = "Export documentation", RequirementCodes = new[]{ "REP-015" } },
                new() { Order = 5, Name = "Invoice customer", Role = "AR Clerk", System = "Accounts Receivable", CycleTimeHrs = 2,
                    Description = "Customer invoice is posted from the shipment and sent to the customer.",
                    Control = "Multi-currency for export", RequirementCodes = new[]{ "SAL-026" } },
                new() { Order = 6, Name = "Collect payment", Role = "Collections", System = "Cash & Bank", CycleTimeHrs = 72,
                    Description = "Payment is received and applied; overdue accounts enter dunning.",
                    Control = "Aging & dunning workflow", RequirementCodes = new[]{ "SAL-004" } },
            }
        });
        d.Processes.Add(new ProcessFlow
        {
            Code = "R2R", Name = "Record to Report", Owner = "K. Marcy, Financial Controller",
            Description = "From transaction capture through financial reporting.",
            Steps = new()
            {
                new() { Order = 1, Name = "Capture transactions", Role = "All", System = "Sub-ledgers", CycleTimeHrs = 1,
                    Description = "Sub-ledger transactions are recorded across AP, AR, inventory, and payroll.",
                    Control = "Financial dimensions required", RequirementCodes = new[]{ "FIN-001" } },
                new() { Order = 2, Name = "Post to general ledger", Role = "Accountant", System = "General Ledger", CycleTimeHrs = 2,
                    Description = "Sub-ledger activity posts to GL control accounts with dimensions.",
                    Control = "Posting profile validation", RequirementCodes = new[]{ "FIN-001" } },
                new() { Order = 3, Name = "Reconcile accounts", Role = "Accountant", System = "Cash & Bank", CycleTimeHrs = 16,
                    Description = "Bank and balance-sheet accounts are reconciled to source.",
                    Control = "Bank reconciliation", RequirementCodes = new[]{ "FIN-016" } },
                new() { Order = 4, Name = "Period-end close", Role = "Controller", System = "General Ledger", CycleTimeHrs = 24,
                    Description = "Close checklist is executed and the period is locked.",
                    Control = "Month-end close checklist", RequirementCodes = new[]{ "REP-010" } },
                new() { Order = 5, Name = "Consolidate", Role = "Controller", System = "General Ledger", CycleTimeHrs = 8,
                    Description = "Intercompany balances are eliminated and entities consolidated.",
                    Control = "Intercompany eliminations", RequirementCodes = new[]{ "FIN-022" } },
                new() { Order = 6, Name = "Financial reporting", Role = "FP&A", System = "Management Reporter", CycleTimeHrs = 6,
                    Description = "Statements and the flash dashboard are produced for leadership.",
                    Control = "Reporting review & sign-off", RequirementCodes = new[]{ "REP-005" } },
            }
        });
    }
}
