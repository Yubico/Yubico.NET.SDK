package main

import (
	"archive/zip"
	"bytes"
	"context"
	"encoding/binary"
	"fmt"
	"os"
	"path/filepath"
	"reflect"
	"strings"
	"testing"
)

func TestPolicyIsStrictAndFailClosed(t *testing.T) {
	core, _, err := loadManifest("manifests/core.json", "core")
	if err != nil {
		t.Fatal(err)
	}
	want := map[string][]string{
		"Yubico.Core":    {"lib/net472/Yubico.Core.dll", "lib/netstandard2.0/Yubico.Core.dll", "lib/netstandard2.1/Yubico.Core.dll"},
		"Yubico.YubiKey": {"lib/net472/Yubico.YubiKey.dll", "lib/netstandard2.0/Yubico.YubiKey.dll", "lib/netstandard2.1/Yubico.YubiKey.dll"},
	}
	for id, entries := range want {
		if !reflect.DeepEqual(core.Packages[id].Authenticode.Include, entries) {
			t.Fatalf("%s includes %v", id, core.Packages[id].Authenticode.Include)
		}
	}
	native, _, err := loadManifest("manifests/nativeshims.json", "nativeshims")
	if err != nil {
		t.Fatal(err)
	}
	if got := native.Packages["Yubico.NativeShims"].Authenticode.Include; len(got) != 3 {
		t.Fatalf("NativeShims includes %v", got)
	}

	valid := `{"schema":1,"attestationRepo":"Yubico/Yubico.NET.SDK","signerWorkflow":"Yubico/Yubico.NET.SDK/.github/workflows/build-nativeshims.yml","packages":{"Yubico.NativeShims":{"symbols":"absent","authenticode":{"include":["lib/Yubico.NativeShims.dll"],"firstParty":["Yubico.*.dll"]}}}}`
	manifestPath := filepath.Join(t.TempDir(), "manifest.json")
	if err := os.WriteFile(manifestPath, []byte(strings.Replace(valid, `"schema":1`, `"schema":1,"extra":true`, 1)), 0o600); err != nil {
		t.Fatal(err)
	}
	if _, _, err := loadManifest(manifestPath, "nativeshims"); err == nil || !strings.Contains(err.Error(), "unknown field") {
		t.Fatalf("strict manifest error = %v", err)
	}

	policy := authenticodePolicy{Include: []string{"lib/Yubico.Core.dll"}, FirstParty: []string{"Yubico.*.dll"}}
	for _, test := range []struct {
		name    string
		entries []string
		want    string
	}{
		{"missing", []string{"data.txt"}, "unmatched include"},
		{"case mismatch", []string{"lib/yubico.core.dll"}, "unmatched include"},
		{"unselected first party", []string{"lib/Yubico.Core.dll", "lib/Yubico.Other.dll"}, "not selected"},
	} {
		t.Run(test.name, func(t *testing.T) {
			_, err := selectEntries(test.entries, policy)
			if err == nil || !strings.Contains(err.Error(), test.want) {
				t.Fatalf("want %q, got %v", test.want, err)
			}
		})
	}
}

func TestArtifactSafetyPackagePlanNamespacedNuspecAndAttestation(t *testing.T) {
	for _, test := range []struct {
		name string
		zips [][]zipItem
		want string
	}{
		{"traversal", [][]zipItem{{{"../a.nupkg", []byte("x")}}}, "unsafe ZIP entry"},
		{"backslash", [][]zipItem{{{`x\a.nupkg`, []byte("x")}}}, "backslashes"},
		{"collision", [][]zipItem{{{"A.nupkg", []byte("x")}, {"a.NUPKG", []byte("x")}}}, "case-insensitive"},
		{"duplicate", [][]zipItem{{{"one/A.nupkg", []byte("x")}}, {{"two/A.nupkg", []byte("x")}}}, "duplicate package filename"},
	} {
		t.Run(test.name, func(t *testing.T) {
			dir := t.TempDir()
			var artifacts []string
			for i, entries := range test.zips {
				artifacts = append(artifacts, writeZip(t, dir, fmt.Sprintf("%d.zip", i), entries))
			}
			_, err := extractArtifacts(dir, artifacts, filepath.Join(dir, "unsigned"))
			if err == nil || !strings.Contains(err.Error(), test.want) {
				t.Fatalf("want %q, got %v", test.want, err)
			}
		})
	}

	pkg := writeZip(t, t.TempDir(), "Yubico.Core.1.2.3.nupkg", []zipItem{{"Yubico.Core.nuspec", []byte(`<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata><id>Yubico.Core</id><version>1.2.3</version></metadata></package>`)}})
	info, err := inspectPackage(extractedPackage{Path: pkg, Name: filepath.Base(pkg)})
	if err != nil || info.ID != "Yubico.Core" || info.Version != "1.2.3" {
		t.Fatalf("info=%#v err=%v", info, err)
	}
	for _, test := range []struct {
		name, entry, want string
	}{
		{"collision", "yubico.core.NUSPEC", "duplicate ZIP name"},
		{"traversal", "../extra.txt", "unsafe ZIP entry"},
	} {
		t.Run("package "+test.name, func(t *testing.T) {
			path := writeZip(t, t.TempDir(), "test.nupkg", []zipItem{
				{"Yubico.Core.nuspec", []byte(`<package><metadata><id>Yubico.Core</id><version>1.2.3</version></metadata></package>`)},
				{test.entry, []byte("x")},
			})
			if _, err := inspectPackage(extractedPackage{Path: path, Name: filepath.Base(path)}); err == nil || !strings.Contains(err.Error(), test.want) {
				t.Fatalf("want %q, got %v", test.want, err)
			}
		})
	}
	m := testCoreManifest()
	for _, test := range []struct {
		name string
		pkgs []packageInfo
		want string
	}{
		{"unknown", []packageInfo{{ID: "Other", Kind: "nupkg"}}, "unknown package"},
		{"missing", []packageInfo{{ID: "Yubico.Core", Version: "1", Kind: "nupkg"}, {ID: "Yubico.Core", Version: "1", Kind: "snupkg"}}, "missing package"},
		{"duplicate", []packageInfo{{ID: "Yubico.Core", Kind: "nupkg"}, {ID: "Yubico.Core", Kind: "nupkg"}}, "duplicate"},
	} {
		t.Run("plan "+test.name, func(t *testing.T) {
			if _, err := planPackages(m, test.pkgs); err == nil || !strings.Contains(err.Error(), test.want) {
				t.Fatalf("want %q, got %v", test.want, err)
			}
		})
	}

	dir := t.TempDir()
	attestedPackage := filepath.Join(dir, "a.nupkg")
	if err := os.WriteFile(attestedPackage, []byte("package"), 0o600); err != nil {
		t.Fatal(err)
	}
	digest, _ := sha256File(attestedPackage)
	source := strings.Repeat("a", 40)
	runner := &fakeRunner{output: []byte(fmt.Sprintf(`[{"verificationResult":{"statement":{"subject":[{"digest":{"sha256":%q}}]}}}]`, digest))}
	if err := attestPackages(context.Background(), runner, "Yubico/Yubico.NET.SDK", "workflow.yml", source, []extractedPackage{{Path: attestedPackage}}); err != nil {
		t.Fatal(err)
	}
	want := []string{"gh", "attestation", "verify", attestedPackage, "--repo", "Yubico/Yubico.NET.SDK", "--signer-workflow", "workflow.yml", "--predicate-type", "https://slsa.dev/provenance/v1", "--deny-self-hosted-runners", "--source-digest", source, "--format", "json"}
	if !reflect.DeepEqual(runner.calls[0], want) {
		t.Fatalf("command %v", runner.calls[0])
	}
	runner.output = []byte(`[{"verificationResult":{"statement":{"subject":[{"digest":{"sha256":"bad"}}]}}}]`)
	if err := attestPackages(context.Background(), runner, "repo", "workflow", source, []extractedPackage{{Path: attestedPackage}}); err == nil {
		t.Fatal("accepted mismatched digest")
	}
	if len(runner.attached) != 0 {
		t.Fatalf("attached signing ran: %v", runner.attached)
	}

	work := t.TempDir()
	bin := filepath.Join(work, "bin")
	if err := os.Mkdir(bin, 0o700); err != nil {
		t.Fatal(err)
	}
	for _, name := range []string{"gh", "nuget-sign"} {
		if err := os.WriteFile(filepath.Join(bin, name), []byte("#!/bin/sh\nexit 0\n"), 0o700); err != nil {
			t.Fatal(err)
		}
	}
	t.Setenv("PATH", bin+string(os.PathListSeparator)+os.Getenv("PATH"))
	chain, root, _ := testCertificateChain(t, work)
	manifestPath := filepath.Join(work, "manifest.json")
	manifest := `{"schema":1,"attestationRepo":"Yubico/Yubico.NET.SDK","signerWorkflow":"Yubico/Yubico.NET.SDK/.github/workflows/build-nativeshims.yml","packages":{"Yubico.NativeShims":{"symbols":"absent","authenticode":{"include":["runtimes/win-x64/native/Yubico.NativeShims.dll"],"firstParty":["Yubico.NativeShims.dll"]}}}}`
	if err := os.WriteFile(manifestPath, []byte(manifest), 0o600); err != nil {
		t.Fatal(err)
	}
	artifact := writeZip(t, work, "artifact.zip", []zipItem{{"package.nupkg", []byte("not inspected before attestation")}})
	pipelineRunner := &fakeRunner{run: func(call []string) ([]byte, error) {
		if len(call) == 2 && call[1] == "--version" {
			if filepath.Base(call[0]) == "nuget-sign" {
				return []byte("nuget-sign version 0.1.0\n"), nil
			}
		}
		return []byte(`[{"verificationResult":{"statement":{"subject":[{"digest":{"sha256":"wrong"}}]}}}]`), nil
	}}
	cfg := runConfig{Component: "nativeshims", WorkingDirectory: work, Artifacts: []string{artifact}, ManifestPath: manifestPath, KeyLocation: "yubikey://9c", CertificatePath: chain, RootPath: root, TimestampRootPath: root, NugetSign: "nuget-sign", SourceDigest: source, Timestamper: defaultTimestamper}
	if err := run(context.Background(), cfg, pipelineRunner); err == nil {
		t.Fatal("pipeline accepted invalid attestation")
	}
	if len(pipelineRunner.attached) != 0 {
		t.Fatalf("pipeline signed before attestation passed: %v", pipelineRunner.attached)
	}
}

func TestRewritePreservationAndPEMutationGuard(t *testing.T) {
	dir := t.TempDir()
	before := minimalPE()
	after := fakeSignedPE(t, before)
	if err := verifyAuthenticodeOnlyMutation(before, after); err != nil {
		t.Fatal(err)
	}
	mutated := append([]byte(nil), after...)
	mutated[len(before)-1] ^= 1
	if err := verifyAuthenticodeOnlyMutation(before, mutated); err == nil {
		t.Fatal("accepted executable mutation")
	}
	nupkg := writeZip(t, dir, "in.nupkg", []zipItem{{"lib/a.dll", before}, {"data.bin", bytes.Repeat([]byte("raw"), 20)}})
	rebuilt := filepath.Join(dir, "rebuilt.nupkg")
	if err := rewritePackage(nupkg, rebuilt, map[string]struct{}{"lib/a.dll": {}}, func(string, []byte) ([]byte, error) { return after, nil }); err != nil {
		t.Fatal(err)
	}
	signed := addSignature(t, rebuilt, filepath.Join(dir, "signed.nupkg"))
	if err := verifyPreservation(nupkg, signed, map[string]struct{}{"lib/a.dll": {}}); err != nil {
		t.Fatal(err)
	}
	snupkg := writeZip(t, dir, "in.snupkg", []zipItem{{"x.nuspec", []byte("same")}, {"symbols.pdb", []byte("symbols")}})
	if err := verifyPreservation(snupkg, addSignature(t, snupkg, filepath.Join(dir, "signed.snupkg")), nil); err != nil {
		t.Fatal(err)
	}
	modifiedSymbols := writeZip(t, dir, "modified.snupkg", []zipItem{{"x.nuspec", []byte("same")}, {"symbols.pdb", []byte("changed")}})
	if err := verifyPreservation(snupkg, addSignature(t, modifiedSymbols, filepath.Join(dir, "signed-modified.snupkg")), nil); err == nil {
		t.Fatal("accepted changed symbol package entry")
	}
}

type zipItem struct {
	name string
	body []byte
}

func writeZip(t *testing.T, dir, name string, entries []zipItem) string {
	t.Helper()
	p := filepath.Join(dir, name)
	f, err := os.Create(p)
	if err != nil {
		t.Fatal(err)
	}
	w := zip.NewWriter(f)
	for _, item := range entries {
		x, err := w.Create(item.name)
		if err != nil {
			t.Fatal(err)
		}
		if _, err := x.Write(item.body); err != nil {
			t.Fatal(err)
		}
	}
	if err := w.Close(); err != nil {
		t.Fatal(err)
	}
	if err := f.Close(); err != nil {
		t.Fatal(err)
	}
	return p
}

func addSignature(t *testing.T, input, output string) string {
	t.Helper()
	contents, err := archiveContents(input)
	if err != nil {
		t.Fatal(err)
	}
	items := make([]zipItem, 0, len(contents)+1)
	for name, body := range contents {
		items = append(items, zipItem{name, body})
	}
	items = append(items, zipItem{".signature.p7s", []byte("signature")})
	return writeZip(t, filepath.Dir(output), filepath.Base(output), items)
}

func minimalPE() []byte {
	b := make([]byte, 512)
	copy(b, "MZ")
	binary.LittleEndian.PutUint32(b[0x3c:], 0x80)
	copy(b[0x80:], "PE\x00\x00")
	binary.LittleEndian.PutUint16(b[0x84+16:], 224)
	opt := b[0x98:]
	binary.LittleEndian.PutUint16(opt, 0x10b)
	binary.LittleEndian.PutUint32(opt[92:], 16)
	return b
}

func fakeSignedPE(t *testing.T, before []byte) []byte {
	t.Helper()
	layout, err := parsePESigningLayout(before)
	if err != nil {
		t.Fatal(err)
	}
	after := append(append([]byte(nil), before...), make([]byte, 8)...)
	binary.LittleEndian.PutUint32(after[layout.checksum:], 1)
	binary.LittleEndian.PutUint32(after[layout.security:], uint32(len(before)))
	binary.LittleEndian.PutUint32(after[layout.security+4:], 8)
	return after
}

func testCoreManifest() manifest {
	return manifest{Schema: 1, Packages: map[string]packagePolicy{
		"Yubico.Core":    {Symbols: "required", Authenticode: &authenticodePolicy{Include: []string{"lib/a.dll"}, FirstParty: []string{"Yubico.*.dll"}}},
		"Yubico.YubiKey": {Symbols: "required", Authenticode: &authenticodePolicy{Include: []string{"lib/b.dll"}, FirstParty: []string{"Yubico.*.dll"}}},
	}}
}
