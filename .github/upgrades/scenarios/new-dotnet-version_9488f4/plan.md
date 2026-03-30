# .NET 10.0 Upgrade Plan

## Table of Contents

- [Executive Summary](#executive-summary)
- [Migration Strategy](#migration-strategy)
- [Detailed Dependency Analysis](#detailed-dependency-analysis)
- [Project-by-Project Plans](#project-by-project-plans)
- [Risk Management](#risk-management)
- [Testing & Validation Strategy](#testing-validation-strategy)
- [Complexity & Effort Assessment](#complexity-effort-assessment)
- [Source Control Strategy](#source-control-strategy)
- [Success Criteria](#success-criteria)

---

## Executive Summary

### Overview

This plan details the migration of **TinyShop** solution from **.NET 9.0** to **.NET 10.0 (LTS)**.

**Scope:**
- **Projects to Upgrade:** 5 of 6 projects (TinyShop.AppHost already on net10.0)
- **Total Issues:** 26 (5 Mandatory, 21 Potential)
- **NuGet Packages to Update:** 10 packages
- **Code Changes Required:** ~11+ lines of code
- **Affected Files:** 9 files

**Projects:**
1. DataEntities (ClassLibrary) — net9.0 → net10.0
2. TinyShop.ServiceDefaults (ClassLibrary) — net9.0 → net10.0
3. Products (AspNetCore) — net9.0 → net10.0
4. Store (AspNetCore/Blazor) — net9.0 → net10.0 ⚠️ **Has API breaking changes**
5. BenchmarkSuite1 (Console) — net9.0 → net10.0
6. TinyShop.AppHost (AppHost) — ✅ Already net10.0

**Classification: SIMPLE UPGRADE**
- Small solution (≤5 projects need upgrade)
- Shallow dependency graph
- Single version jump (9.0 → 10.0)
- Low risk (only 1 project has API compatibility issues)

**Estimated Effort:** 1-2 hours

**Risk Level:** Low

---

## Migration Strategy

### Selected Approach: **All-at-Once Strategy**

Given the simple structure of this solution, we'll upgrade all projects in a single coordinated effort.

**Why This Approach:**
- ✅ Small solution with clear dependency hierarchy
- ✅ All projects moving from same version (net9.0 → net10.0)
- ✅ Minimal cross-project complexity
- ✅ Only one project (Store) has code-level changes
- ✅ Clean dependency graph with no circular references

**Upgrade Sequence:**

```
Phase 1: Foundation Libraries (Parallel)
├─ DataEntities
└─ TinyShop.ServiceDefaults

Phase 2: Application Projects (After Phase 1)
├─ Products (depends on: DataEntities, ServiceDefaults)
└─ Store (depends on: DataEntities, ServiceDefaults) ⚠️ Code changes needed

Phase 3: Support Projects (After Phase 2)
└─ BenchmarkSuite1 (depends on: Products)

TinyShop.AppHost: No action needed (already net10.0)
```

**Execution Steps:**

1. **Validate Prerequisites**
   - Verify .NET 10.0 SDK installed
   - Check global.json compatibility
   - Run baseline tests

2. **Update Foundation Libraries**
   - Update DataEntities TFM to net10.0
   - Update TinyShop.ServiceDefaults TFM to net10.0
   - Update NuGet packages in both projects
   - Build and verify

3. **Update Application Projects**
   - Update Products TFM to net10.0 + packages
   - Update Store TFM to net10.0 + packages
   - **Fix API breaking changes in Store** (TimeSpan, Uri, HttpContent)
   - Build and verify

4. **Update Support Projects**
   - Update BenchmarkSuite1 TFM to net10.0 + packages
   - Build and verify

5. **Final Validation**
   - Full solution build
   - Run all tests
   - Verify runtime behavior

**Rollback Strategy:**
- Git branch `upgrade-to-NET10` allows easy revert via `git checkout main`
- Each phase independently testable

---

## Detailed Dependency Analysis

### Project Dependency Graph

```
DataEntities (net9.0)
  ├─ No project dependencies
  └─ Depended on by: Products, Store

TinyShop.ServiceDefaults (net9.0)
  ├─ No project dependencies
  └─ Depended on by: Products, Store

Products (net9.0)
  ├─ Depends on: DataEntities, TinyShop.ServiceDefaults
  └─ Depended on by: BenchmarkSuite1

Store (net9.0) — Blazor WebAssembly/Server
  ├─ Depends on: DataEntities, TinyShop.ServiceDefaults
  └─ Depended on by: None

BenchmarkSuite1 (net9.0)
  ├─ Depends on: Products
  └─ Depended on by: None

TinyShop.AppHost (net10.0) ✅
  ├─ Already on target framework
  └─ No changes required
```

### Dependency Levels (Bottom-Up)

**Level 0 (Leaf Libraries):**
- DataEntities
- TinyShop.ServiceDefaults

**Level 1 (Dependent Applications):**
- Products
- Store

**Level 2 (Consumer Applications):**
- BenchmarkSuite1

**Level N/A (Already Upgraded):**
- TinyShop.AppHost

### NuGet Package Dependencies

**Critical Package Updates:**

| Package | Current | Target | Affected Projects |
|---------|---------|--------|-------------------|
| Microsoft.EntityFrameworkCore | 9.0.6 | 10.0.3 | Products |
| Microsoft.EntityFrameworkCore.Design | 9.0.6 | 10.0.3 | Products |
| Microsoft.Extensions.Http.Resilience | 9.6.0 | 10.3.0 | ServiceDefaults |
| Microsoft.Extensions.ServiceDiscovery | 9.3.1 | 10.3.0 | ServiceDefaults |
| Microsoft.VisualStudio.Web.CodeGeneration.Design | 9.0.0 | 10.0.2 | Products |
| OpenTelemetry.Instrumentation.AspNetCore | 1.12.0 | 1.15.0 | ServiceDefaults |
| OpenTelemetry.Instrumentation.Http | 1.12.0 | 1.15.0 | ServiceDefaults |
| System.Formats.Asn1 | 9.0.6 | 10.0.3 | Products |
| System.Text.Json | 9.0.6 | 10.0.3 | Products |
| BenchmarkDotNet | 0.14.0 | 0.14.0 | BenchmarkSuite1 (compatible) |

**Update Order:**
1. Update packages in Level 0 projects first (ServiceDefaults)
2. Update packages in Level 1 projects (Products)
3. Update packages in Level 2 projects (BenchmarkSuite1)

---

## Project-by-Project Plans

[To be filled]

---

## Risk Management

### Critical Issues Requiring Code Changes

#### 🔴 HIGH PRIORITY: Store Project (Blazor)

**1. Breaking Change: TimeSpan.FromMinutes(Int64)**
- **Location:** `Store\Services\ProductService.cs`, line 31
- **Issue:** Method signature changed from `FromMinutes(Int64)` to `FromMinutes(Double)`
- **Current Code:** `entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);`
- **Resolution:** Change literal `5` to `5.0` or cast to double
- **Risk:** Build breaks if not addressed
- **Reference:** [Breaking Changes in .NET](https://go.microsoft.com/fwlink/?linkid=2262679)

**2. Behavioral Change: System.Uri Constructor**
- **Locations:** 
  - `Store\Program.cs`, line 12
  - `Store\obj\...\About_razor.g.cs`, line 140
- **Issue:** Uri constructor and ToString() behavior changes in .NET 10
- **Current Code:** `c.BaseAddress = new(url);` and `new Uri(NavigationManager.Uri)`
- **Resolution:** Review and validate Uri handling still produces expected results
- **Risk:** Runtime behavior changes (Low—likely transparent)

**3. Behavioral Change: HttpContent.ReadFromJsonAsync**
- **Locations:** `Store\Services\ProductService.cs`, lines 41, 58, 72
- **Issue:** Serialization behavior improvements in .NET 10
- **Current Code:** `await response.Content.ReadFromJsonAsync<Product>()`
- **Resolution:** No code change required—verify through testing
- **Risk:** Serialization edge cases (Low)

**4. Behavioral Change: UseExceptionHandler Middleware**
- **Location:** `Store\Program.cs`, line 29
- **Issue:** `createScopeForErrors` parameter behavior updated
- **Current Code:** `app.UseExceptionHandler("/Error", createScopeForErrors: true);`
- **Resolution:** No code change required—validate error handling
- **Risk:** Error scoping behavior (Low)

### Medium-Risk Items

**NuGet Package Version Jumps:**
- Most packages: Minor version updates (low risk)
- OpenTelemetry: 1.12.0 → 1.15.0 (review telemetry configuration)
- Microsoft.Extensions.*: 9.x → 10.x (framework-aligned, expected)

### Low-Risk Items

**Target Framework Updates:**
- All projects moving net9.0 → net10.0 (standard upgrade path)
- No multi-targeting required
- No .NET Framework dependencies

### Mitigation Strategies

| Risk | Mitigation |
|------|------------|
| Breaking API changes | Fix TimeSpan.FromMinutes before build |
| Behavioral changes | Comprehensive test suite execution |
| Package compatibility | Update packages in dependency order |
| Build failures | Validate each phase before proceeding |
| Runtime regressions | Manual smoke testing of Blazor app |

### Rollback Plan

1. **Pre-upgrade:** All changes committed to `main` branch
2. **Upgrade branch:** `upgrade-to-NET10` (isolated)
3. **Quick rollback:** `git checkout main`
4. **Selective rollback:** Cherry-pick working changes if needed

---

## Testing & Validation Strategy

[To be filled]

---

## Complexity & Effort Assessment

[To be filled]

---

## Source Control Strategy

[To be filled]

---

## Success Criteria

[To be filled]
