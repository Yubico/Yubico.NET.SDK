# Coordinator clean-unit sampling

2026-11-04; Astra. Investigator nominated27units; whole-range comparison conservatively removed5edited
overlaps and1runner-metadata nomination, leaving**21eligible semantic units** at this checkpoint.
Required max(3,ceil(21/10))=3; selected**OTP-CL001,009,016**, spanning API/narrative/internal-test.
Exact original nominated ranges/hash/eligibility are in sampling-population.json; final acceptance must
revalidate selected bodies and denominator after any further content/format changes.

| Unit | Bounded independent check | Result |
| --- | --- | --- |
| CL001 Slot/operation mapping | Read complete Slot.cs and ConfigSlot.cs; all10slot/operation mappings pair correctly and unknown combination throws; XML describes mapping rather than device support. | PASS; no hardware-capability claim inferred from switch. |
| CL009 package narrative | Read entire PACKAGE_README; purpose matches local API/configuration families, package references Core, linked v2 module/repository paths correct. Alpha/banner/install convention unchanged. | PASS; no installed-feed availability or all-device qualification asserted. |
| CL016 reflected-buffer zeroing | Read complete test class and private reflection helpers; arrays filledA5, original buffers captured before Dispose and every byte asserted0; compare local Dispose implementation. | PASS; unlike weaker disposed-access tests, this actually observes the3private arrays, not all heap copies or independent GetConfig results. |

No material miss; expansion unnecessary. Samples are distinct clean semantic units, not independent
whole-module review. Product/source interpretations outside these bounded units remain investigator/
reviewer coverage and deferred findings, not additional counted samples.
