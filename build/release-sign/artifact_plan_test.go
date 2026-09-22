package main

import (
	"archive/zip"
	"context"
	"errors"
	"fmt"
	"os"
	"path/filepath"
	"reflect"
	"strings"
	"testing"
)

func TestExtractArtifactsRejectsUnsafeCollisionAndDuplicatePackage(t *testing.T) {
	tests := []struct {
		name      string
		artifacts [][]zipItem
		want      string
	}{
		{"traversal", [][]zipItem{{{"../a.nupkg", []byte("x")}}}, "unsafe ZIP entry"},
		{"backslash", [][]zipItem{{{`x\a.nupkg`, []byte("x")}}}, "backslashes"},
		{"case collision", [][]zipItem{{{"A.nupkg", []byte("x")}, {"a.NUPKG", []byte("x")}}}, "case-insensitive"},
		{"duplicate package", [][]zipItem{{{"one/A.nupkg", []byte("x")}}, {{"two/A.nupkg", []byte("x")}}}, "duplicate package filename"},
	}
	for _, test := range tests {
		t.Run(test.name, func(t *testing.T) {
			work := t.TempDir()
			var names []string
			for i, entries := range test.artifacts {
				names = append(names, writeZip(t, work, string(rune('a'+i))+".zip", entries))
			}
			_, err := extractArtifacts(work, names, filepath.Join(work, "scratch", "unsigned"))
			if err == nil || !strings.Contains(err.Error(), test.want) {
				t.Fatalf("want %q, got %v", test.want, err)
			}
		})
	}
}

func TestAttestationFailurePrecedesSignerAcquisition(t *testing.T) {
	dir := t.TempDir()
	pkg := filepath.Join(dir, "a.nupkg")
	if err := os.WriteFile(pkg, []byte("package"), 0o600); err != nil {
		t.Fatal(err)
	}
	digest, err := sha256File(pkg)
	if err != nil {
		t.Fatal(err)
	}
	runner := &fakeRunner{output: []byte(`not-json`)}
	called := false
	err = attestPackages(context.Background(), runner, "Yubico/Yubico.NET.SDK", "Yubico/Yubico.NET.SDK/.github/workflows/build.yml", strings.Repeat("a", 40), []extractedPackage{{Path: pkg}}, func() error { called = true; return nil })
	if err == nil || called {
		t.Fatalf("attestation err=%v signer called=%v", err, called)
	}
	runner.err = errors.New("verification failed")
	if err := attestPackages(context.Background(), runner, "Yubico/Yubico.NET.SDK", "Yubico/Yubico.NET.SDK/.github/workflows/build.yml", strings.Repeat("a", 40), []extractedPackage{{Path: pkg}}, func() error { called = true; return nil }); err == nil {
		t.Fatal("accepted failed gh command")
	}
	runner.err = nil
	runner.output = []byte(fmt.Sprintf(`[{"verificationResult":{"statement":{"subject":[{"name":"a.nupkg","digest":{"sha256":%q}}]}}}]`, digest))
	if err := attestPackages(context.Background(), runner, "Yubico/Yubico.NET.SDK", "Yubico/Yubico.NET.SDK/.github/workflows/build.yml", strings.Repeat("a", 40), []extractedPackage{{Path: pkg}}, func() error { called = true; return nil }); err != nil {
		t.Fatal(err)
	}
	if !called {
		t.Fatal("signer callback not called")
	}
	want := []string{"gh", "attestation", "verify", pkg, "--repo", "Yubico/Yubico.NET.SDK", "--signer-workflow", "Yubico/Yubico.NET.SDK/.github/workflows/build.yml", "--predicate-type", "https://slsa.dev/provenance/v1", "--deny-self-hosted-runners", "--source-digest", strings.Repeat("a", 40), "--format", "json"}
	if got := runner.calls[len(runner.calls)-1]; !reflect.DeepEqual(got, want) {
		t.Fatalf("command %v, want %v", got, want)
	}
	runner.output = []byte(`[{"verificationResult":{"statement":{"subject":[{"digest":{"sha256":"ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"}}]}}}]`)
	called = false
	if err := attestPackages(context.Background(), runner, "Yubico/Yubico.NET.SDK", "Yubico/Yubico.NET.SDK/.github/workflows/build.yml", strings.Repeat("a", 40), []extractedPackage{{Path: pkg}}, func() error { called = true; return nil }); err == nil || called {
		t.Fatalf("mismatched artifact digest err=%v signer called=%v", err, called)
	}
	runner.output = []byte(fmt.Sprintf(`[{"verificationResult":{"statement":{"subject":[{"digest":{"sha256":"ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"}}],"predicate":{"verificationResult":{"statement":{"subject":[{"digest":{"sha256":%q}}]}}}}}}]`, digest))
	if err := attestPackages(context.Background(), runner, "Yubico/Yubico.NET.SDK", "Yubico/Yubico.NET.SDK/.github/workflows/build.yml", strings.Repeat("a", 40), []extractedPackage{{Path: pkg}}, func() error { called = true; return nil }); err == nil {
		t.Fatal("accepted digest found only in an arbitrary nested predicate")
	}
}

func TestPlanRejectsUnknownMissingDuplicateAndSymbolMismatch(t *testing.T) {
	m := testCoreManifest()
	tests := []struct {
		name     string
		packages []packageInfo
		want     string
	}{
		{"unknown", []packageInfo{{ID: "Other", Version: "1", Kind: "nupkg"}}, "unknown package ID"},
		{"missing", []packageInfo{{ID: "Yubico.Core", Version: "1", Kind: "nupkg"}, {ID: "Yubico.Core", Version: "1", Kind: "snupkg"}}, "missing package"},
		{"duplicate", []packageInfo{{ID: "Yubico.Core", Version: "1", Kind: "nupkg"}, {ID: "Yubico.Core", Version: "1", Kind: "nupkg"}}, "duplicate"},
		{"symbols", []packageInfo{{ID: "Yubico.Core", Version: "1", Kind: "nupkg"}, {ID: "Yubico.Core", Version: "2", Kind: "snupkg"}, {ID: "Yubico.YubiKey", Version: "1", Kind: "nupkg"}, {ID: "Yubico.YubiKey", Version: "1", Kind: "snupkg"}}, "matching .snupkg"},
	}
	for _, test := range tests {
		t.Run(test.name, func(t *testing.T) {
			_, err := planPackages(m, test.packages)
			if err == nil || !strings.Contains(err.Error(), test.want) {
				t.Fatalf("want %q, got %v", test.want, err)
			}
		})
	}
}

func TestInspectPackageReadsNamespacedNuspec(t *testing.T) {
	dir := t.TempDir()
	filename := writeZip(t, dir, "Yubico.Core.1.2.3.nupkg", []zipItem{{"Yubico.Core.nuspec", []byte(`<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata><id>Yubico.Core</id><version>1.2.3</version></metadata></package>`)}})
	info, err := inspectPackage(extractedPackage{Path: filename, Name: filepath.Base(filename)})
	if err != nil {
		t.Fatal(err)
	}
	if info.ID != "Yubico.Core" || info.Version != "1.2.3" || info.Kind != "nupkg" {
		t.Fatalf("unexpected package info: %#v", info)
	}
}

type fakeRunner struct {
	output []byte
	err    error
	calls  [][]string
}

func (f *fakeRunner) Run(_ context.Context, name string, args ...string) ([]byte, error) {
	f.calls = append(f.calls, append([]string{name}, args...))
	return f.output, f.err
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
		if _, err = x.Write(item.body); err != nil {
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

func testCoreManifest() manifest {
	return manifest{Schema: 1, AttestationRepo: "Yubico/Yubico.NET.SDK", SignerWorkflow: "Yubico/Yubico.NET.SDK/.github/workflows/build.yml", Packages: map[string]packagePolicy{"Yubico.Core": {Symbols: "required", Authenticode: &authenticodePolicy{Include: []string{"lib/a.dll"}, FirstParty: []string{"Yubico.*.dll"}, AlreadySigned: "reject"}}, "Yubico.YubiKey": {Symbols: "required", Authenticode: &authenticodePolicy{Include: []string{"lib/b.dll"}, FirstParty: []string{"Yubico.*.dll"}, AlreadySigned: "reject"}}}}
}
