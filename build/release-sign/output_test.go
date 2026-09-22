package main

import (
	"encoding/json"
	"os"
	"path/filepath"
	"strings"
	"testing"
	"time"
)

func TestAtomicOutputFailureLeavesNoPackages(t *testing.T) {
	dir := t.TempDir()
	staging := filepath.Join(dir, "stage")
	if err := os.Mkdir(staging, 0o700); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(filepath.Join(staging, "a.nupkg"), []byte("a"), 0o600); err != nil {
		t.Fatal(err)
	}
	err := commitOutputs(filepath.Join(dir, "signed"), staging, []string{"a.nupkg", "missing.nupkg"}, []byte(`{}`), false)
	if err == nil {
		t.Fatal("expected failure")
	}
	entries, readErr := os.ReadDir(filepath.Join(dir, "signed", "packages"))
	if readErr == nil && len(entries) > 0 {
		t.Fatalf("partial outputs: %v", entries)
	}
}

func TestCommitOutputsRejectsPackagesPathThatIsAFileWithoutClean(t *testing.T) {
	dir := t.TempDir()
	signed := filepath.Join(dir, "signed")
	if err := os.Mkdir(signed, 0o700); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(filepath.Join(signed, "packages"), []byte("not a directory"), 0o600); err != nil {
		t.Fatal(err)
	}
	stage := filepath.Join(dir, "stage")
	if err := os.Mkdir(stage, 0o700); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(filepath.Join(stage, "a.nupkg"), []byte("a"), 0o600); err != nil {
		t.Fatal(err)
	}
	if err := commitOutputs(signed, stage, []string{"a.nupkg"}, []byte(`{}`), false); err == nil {
		t.Fatal("accepted signed/packages file")
	}
	contents, err := os.ReadFile(filepath.Join(signed, "packages"))
	if err != nil || string(contents) != "not a directory" {
		t.Fatalf("prior output changed: %q, %v", contents, err)
	}
}

func TestReportSortedAndContainsNoKeySecret(t *testing.T) {
	r := report{Schema: 1, Component: "core", SourceDigest: strings.Repeat("a", 40), SignerWorkflow: "workflow", StartedUTC: time.Unix(1, 0).UTC(), CompletedUTC: time.Unix(2, 0).UTC(), Manifest: reportManifest{Path: "manifest.json", SHA256: "abc"}, KeyLocation: safeKeyLocation("yubikey://9c?serial=12&pin=123456"), Packages: []reportPackage{{ID: "Z"}, {ID: "A"}}, Artifacts: []reportArtifact{{Path: "z.zip", SHA256: "z"}, {Path: "a.zip", SHA256: "a"}}}
	b, err := marshalReport(r)
	if err != nil {
		t.Fatal(err)
	}
	if strings.Contains(string(b), "NUGET_SIGN_PIN") || strings.Contains(string(b), "123456") {
		t.Fatal("report leaked a secret")
	}
	if strings.Contains(string(b), "attestationVerified") {
		t.Fatal("report contains redundant per-package attestation state")
	}
	var got report
	if err := json.Unmarshal(b, &got); err != nil {
		t.Fatal(err)
	}
	if got.Packages[0].ID != "A" || got.Artifacts[0].Path != "a.zip" {
		t.Fatalf("report not sorted: %s", b)
	}
}
