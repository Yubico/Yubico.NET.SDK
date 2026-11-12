# Final docs-inclusive package check

`build-pack-final` completed the ordinary clean Release solution build and ten pack commands with
zero warnings. The output directory contains20 packages because earlier versions were retained;
the exact requested version `2.0.0-alpha.2` produced the ten packages below.

Astra opened every package as ZIP data without loading its assemblies: archive integrity passed,
nuspec ID/version matched, packaged README bytes matched the current module `PACKAGE_README.md`,
and `lib/net10.0` contained the expected DLL and parseable XML documentation with the correct assembly
name and nonempty member set. All ten package bytes were copied and hash-verified under
`/home/rupicola/Code/y/.core-docs-audit-runtime/core-001/final-packages/` before further artifact cleanup.

Each filename is `Yubico.YubiKit.<Module>.2.0.0-alpha.2.nupkg`.

| Module | Bytes | XML members | SHA-256 |
| --- | ---: | ---: | --- |
| Core | 440230 | 1688 | `f77c0a72fac8ce1289c1d75f8b8104b81fcf993d221ae926622c91d562e37837` |
| Ctap2 | 162788 | 779 | `f73a279c22863854712c0a74f5d94866a8299c4de2d17f9868c70ce4e9a491c8` |
| Management | 27546 | 38 | `3fe89fabb5b242135ea567f5d092eeabbe94d9c7cef9e61da3fc069b08004bc3` |
| Oath | 48515 | 128 | `5a9bcd824ff07714e350e0a0a6a198d8cf8eac10c30136f0fb321e0075d0ad94` |
| OpenPgp | 80066 | 331 | `2685f74c6b23b676b1dae1751b719115611be4c58cd0c68adea099403193d606` |
| Piv | 88753 | 340 | `6dedb1a9b6570528fc56b2882e1a4cb86a578ea1fdf8797f5dbd6ae24ad3dc4a` |
| SecurityDomain | 36624 | 69 | `93253292b5e120e2b804ad1dc41fbd0387e4260fe251526a882abe6de7129b69` |
| WebAuthn | 107269 | 429 | `957c9eae9b8d6a2130b15fc0451fcd2d9ea4b5459b278ef19e14adfc34005f6b` |
| YubiHsm | 34460 | 78 | `da802a246470a83f4cb2e1339d16635e27d9c2c15a443e208a3aa11d65667a5d` |
| YubiOtp | 43401 | 129 | `e3d336e6523896b642e43005545067c79f8109d7cd2408323fe0d2166c1a215d` |

This is local packaging/documentation evidence, not a package-registry publication, signing claim,
provider/native conformance result or device test.
