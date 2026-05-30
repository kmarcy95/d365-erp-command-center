namespace D365CommandCenter.Models;

// Phase 2 populates the full Alamo Foods Co. domain model here
// (GL, AP, AR, budget, procurement, inventory, sales, and the
// implementation entities: phases, milestones, RAID, requirements,
// fit-gap, test cases, status reports, process flows).
//
// A starter enum is defined now so the Models namespace is valid for
// the foundation build and to lock the RAG vocabulary used across pages.

public enum RagStatus
{
    OnTrack,
    AtRisk,
    OffTrack,
    Complete
}
