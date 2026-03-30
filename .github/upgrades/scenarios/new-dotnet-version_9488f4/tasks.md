# TinyShop .NET 10.0 Upgrade Tasks

## Overview

This document tracks the execution of the TinyShop solution upgrade from .NET 9.0 to .NET 10.0. All projects will be upgraded simultaneously in a single atomic operation, followed by verification.

**Progress**: 0/2 tasks complete (0%) ![0%](https://progress-bar.xyz/0)

---

## Tasks

### [▶] TASK-001: Verify prerequisites
**References**: Plan §Migration Strategy - Execution Steps

- [▶] (1) Verify .NET 10.0 SDK installed per Plan §Executive Summary
- [ ] (2) .NET 10.0 SDK is available (**Verify**)
- [ ] (3) Check global.json compatibility (if present in repository)
- [ ] (4) global.json compatible with .NET 10.0 or updated (**Verify**)

---

### [ ] TASK-002: Atomic framework and package upgrade for all projects
**References**: Plan §Migration Strategy - Execution Steps, Plan §Detailed Dependency Analysis - NuGet Package Dependencies, Plan §Risk Management - Critical Issues

- [ ] (1) Update TargetFramework to net10.0 in all 5 projects: DataEntities, TinyShop.ServiceDefaults, Products, Store, BenchmarkSuite1 per Plan §Migration Strategy - Upgrade Sequence
- [ ] (2) All project TargetFramework properties updated to net10.0 (**Verify**)
- [ ] (3) Update package references per Plan §Detailed Dependency Analysis - NuGet Package Dependencies (10 packages: Microsoft.EntityFrameworkCore 9.0.6→10.0.3, Microsoft.EntityFrameworkCore.Design 9.0.6→10.0.3, Microsoft.Extensions.Http.Resilience 9.6.0→10.3.0, Microsoft.Extensions.ServiceDiscovery 9.3.1→10.3.0, Microsoft.VisualStudio.Web.CodeGeneration.Design 9.0.0→10.0.2, OpenTelemetry.Instrumentation.AspNetCore 1.12.0→1.15.0, OpenTelemetry.Instrumentation.Http 1.12.0→1.15.0, System.Formats.Asn1 9.0.6→10.0.3, System.Text.Json 9.0.6→10.0.3)
- [ ] (4) All package references updated (**Verify**)
- [ ] (5) Restore all dependencies
- [ ] (6) All dependencies restored successfully (**Verify**)
- [ ] (7) Build solution and fix all compilation errors per Plan §Risk Management - Critical Issues (focus on Store project: fix TimeSpan.FromMinutes(5) to TimeSpan.FromMinutes(5.0) at Store\Services\ProductService.cs line 31)
- [ ] (8) Solution builds with 0 errors (**Verify**)
- [ ] (9) Commit changes with message: "TASK-002: Complete .NET 10.0 atomic upgrade for all projects"

---
