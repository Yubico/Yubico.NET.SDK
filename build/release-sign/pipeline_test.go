package main

import (
	"context"
	"crypto/rand"
	"crypto/rsa"
	"crypto/x509"
	"crypto/x509/pkix"
	"encoding/json"
	"encoding/pem"
	"errors"
	"math/big"
	"os"
	"path/filepath"
	"reflect"
	"strings"
	"testing"
	"time"
)

func TestNugetSignExecutableMetadata(t *testing.T) {
	bin := t.TempDir()
	if err := os.WriteFile(filepath.Join(bin, "nuget-sign"), []byte("nuget-sign binary"), 0o700); err != nil {
		t.Fatal(err)
	}
	t.Setenv("PATH", bin+string(os.PathListSeparator)+os.Getenv("PATH"))
	runner := &fakeRunner{output: []byte("nuget-sign version 0.1.0\n")}
	metadata, err := inspectNugetSign(context.Background(), runner, "nuget-sign")
	if err != nil {
		t.Fatal(err)
	}
	tool := filepath.Join(bin, "nuget-sign")
	if metadata.Path != tool || metadata.Version != "nuget-sign version 0.1.0" || metadata.SHA256 == "" {
		t.Fatalf("metadata %#v", metadata)
	}
	if call := runner.calls[0]; !reflect.DeepEqual(call, []string{tool, "--version"}) {
		t.Fatalf("call %v", call)
	}
	runner.output = []byte("other version 1.2.3\n")
	if _, err := inspectNugetSign(context.Background(), runner, "nuget-sign"); err == nil {
		t.Fatal("accepted an executable that is not nuget-sign")
	}
}

func TestSigningPINPrompt(t *testing.T) {
	t.Setenv("NUGET_SIGN_PIN", "")
	if err := os.Unsetenv("NUGET_SIGN_PIN"); err != nil {
		t.Fatal(err)
	}
	prompts := 0
	readPIN := func() (string, error) {
		prompts++
		return "test-pin", nil
	}
	pin, err := acquireSigningPIN("yubikey://9c?serial=28188992", readPIN)
	if err != nil || pin != "test-pin" {
		t.Fatalf("PIN prompt failed: %v", err)
	}
	if prompts != 1 {
		t.Fatalf("prompted %d times, want once", prompts)
	}
	if _, set := os.LookupEnv("NUGET_SIGN_PIN"); set {
		t.Fatal("prompted PIN escaped into the process environment")
	}
	if _, err := acquireSigningPIN("yubikey://9c", func() (string, error) { return "", nil }); err == nil {
		t.Fatal("accepted an empty PIN")
	}
	if _, err := acquireSigningPIN("yubikey://9c", func() (string, error) { return "", errors.New("no terminal") }); err == nil {
		t.Fatal("accepted a failed prompt")
	}
	if pin, err := acquireSigningPIN("file:///key.pem", func() (string, error) { t.Fatal("prompted for a file key"); return "", nil }); err != nil || pin != "" {
		t.Fatalf("unexpected PIN for file key: %v", err)
	}
	t.Setenv("NUGET_SIGN_PIN", "provided")
	if pin, err := acquireSigningPIN("yubikey://9c", func() (string, error) { t.Fatal("prompted with PIN already set"); return "", nil }); err != nil || pin != "" {
		t.Fatalf("unexpected prompt with PIN already set: %v", err)
	}
	t.Setenv("NUGET_SIGN_PIN", "")
	if _, err := acquireSigningPIN("yubikey://9c", func() (string, error) { t.Fatal("prompted with an empty PIN in the environment"); return "", nil }); err == nil {
		t.Fatal("accepted an empty PIN from the environment")
	}
}

func TestSigningEnvironmentScope(t *testing.T) {
	if os.Getenv("RELEASE_SIGN_TEST_CHILD") == "1" {
		if os.Getenv("NUGET_SIGN_PIN") != "test-pin" {
			t.Fatal("signing child did not receive the PIN")
		}
		return
	}
	t.Setenv("NUGET_SIGN_PIN", "")
	if err := os.Unsetenv("NUGET_SIGN_PIN"); err != nil {
		t.Fatal(err)
	}
	program, err := os.Executable()
	if err != nil {
		t.Fatal(err)
	}
	if err := (osRunner{}).RunAttached(context.Background(), program, []string{"RELEASE_SIGN_TEST_CHILD=1", "NUGET_SIGN_PIN=test-pin"}, "-test.run=^TestSigningEnvironmentScope$"); err != nil {
		t.Fatal(err)
	}
	if _, set := os.LookupEnv("NUGET_SIGN_PIN"); set {
		t.Fatal("signing child PIN leaked into the parent environment")
	}
}

func TestExactNugetSignAndVerifyCommands(t *testing.T) {
	dir := t.TempDir()
	input := writeZip(t, dir, "input.nupkg", []zipItem{{"a.nuspec", []byte(`<package><metadata><id>A</id><version>1</version></metadata></package>`)}, {"lib/b.dll", []byte("b")}, {"lib/a.dll", []byte("a")}, {"data", []byte("same")}})
	info := packageInfo{Path: input, Name: "A.1.nupkg", Kind: "nupkg"}
	selected := map[string]struct{}{"lib/b.dll": {}, "lib/a.dll": {}}
	output := filepath.Join(dir, "output.nupkg")
	cfg := runConfig{NugetSign: "/tools/nuget-sign", KeyLocation: "yubikey://9c", CertificatePath: "chain.pem", RootPath: "root.pem", TimestampRootPath: "timestamp.pem", Timestamper: "https://timestamp", signingPIN: "test-pin"}
	runner := &fakeRunner{}
	runner.attachedRun = func(call []string) error {
		if call[1] == "sign-assemblies" {
			for _, filename := range call[10:] {
				body, err := os.ReadFile(filename)
				if err != nil {
					return err
				}
				if err := os.WriteFile(filename, append(body, "-signed"...), 0o600); err != nil {
					return err
				}
			}
			return nil
		}
		rebuilt, err := archiveContents(call[len(call)-1])
		if err != nil {
			return err
		}
		if string(rebuilt["lib/a.dll"]) != "a-signed" || string(rebuilt["lib/b.dll"]) != "b-signed" || string(rebuilt["data"]) != "same" {
			t.Errorf("repacked package does not carry the signed assemblies: %q", rebuilt)
		}
		addSignature(t, call[len(call)-1], call[11])
		return nil
	}
	if err := signPackage(context.Background(), runner, cfg, info, output, selected); err != nil {
		t.Fatal(err)
	}
	if len(runner.attached) != 2 {
		t.Fatalf("attached calls %v", runner.attached)
	}
	if len(runner.attachedEnv) != 2 {
		t.Fatalf("received %d signing environments, want two", len(runner.attachedEnv))
	}
	for _, environment := range runner.attachedEnv {
		if !reflect.DeepEqual(environment, []string{"NUGET_SIGN_PIN=test-pin"}) {
			t.Fatal("signer subprocess did not receive the PIN")
		}
	}
	assemblyCall := runner.attached[0]
	wantAssemblyPrefix := []string{"/tools/nuget-sign", "sign-assemblies", "--key", "yubikey://9c", "--certificate", "chain.pem", "--hash-algorithm", "sha256", "--timestamper", "https://timestamp"}
	if !reflect.DeepEqual(assemblyCall[:10], wantAssemblyPrefix) || !strings.HasSuffix(assemblyCall[10], filepath.FromSlash("lib/a.dll")) || !strings.HasSuffix(assemblyCall[11], filepath.FromSlash("lib/b.dll")) {
		t.Fatalf("assembly command %v", assemblyCall)
	}
	packageCall := runner.attached[1]
	if want := []string{"/tools/nuget-sign", "sign", "--key", "yubikey://9c", "--certificate", "chain.pem", "--hash-algorithm", "sha256", "--timestamper", "https://timestamp", "--output", output, packageCall[12]}; !reflect.DeepEqual(packageCall, want) {
		t.Fatalf("package command %v, want %v", packageCall, want)
	}
	fingerprint := strings.Repeat("A", 64)
	if err := verifySignedPackage(context.Background(), runner, cfg, info, output, selected, fingerprint); err != nil {
		t.Fatal(err)
	}
	wantVerify := []string{"/tools/nuget-sign", "verify", "--revocation", "none", "--root", "root.pem", "--timestamp-root", "timestamp.pem", "--certificate-fingerprint", fingerprint, "--assemblies", output}
	if !reflect.DeepEqual(runner.calls[0], wantVerify) {
		t.Fatalf("verify command %v", runner.calls[0])
	}
	if len(runner.calls) != 1 {
		t.Fatalf("verification calls %v", runner.calls)
	}
	symbolInput := writeZip(t, dir, "input.snupkg", []zipItem{{"a.nuspec", []byte("same")}, {"a.pdb", []byte("symbols")}})
	symbolOutput := addSignature(t, symbolInput, filepath.Join(dir, "output.snupkg"))
	runner.calls = nil
	if err := verifySignedPackage(context.Background(), runner, cfg, packageInfo{Path: symbolInput, Kind: "snupkg"}, symbolOutput, nil, fingerprint); err != nil {
		t.Fatal(err)
	}
	wantSymbolVerify := []string{"/tools/nuget-sign", "verify", "--revocation", "none", "--root", "root.pem", "--timestamp-root", "timestamp.pem", "--certificate-fingerprint", fingerprint, symbolOutput}
	if !reflect.DeepEqual(runner.calls, [][]string{wantSymbolVerify}) {
		t.Fatalf("symbol verify commands %v", runner.calls)
	}
}

func TestAtomicOutputReportOrderingMetadataAndNoSecrets(t *testing.T) {
	dir := t.TempDir()
	stage := filepath.Join(dir, "stage")
	if err := os.Mkdir(stage, 0o700); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(filepath.Join(stage, "a.nupkg"), []byte("a"), 0o600); err != nil {
		t.Fatal(err)
	}
	if err := commitOutputs(filepath.Join(dir, "signed"), stage, []string{"a.nupkg", "missing.nupkg"}, []byte(`{}`), false); err == nil {
		t.Fatal("expected atomic failure")
	}
	if _, err := os.Stat(filepath.Join(dir, "signed")); !errors.Is(err, os.ErrNotExist) {
		t.Fatalf("partial output: %v", err)
	}

	r := report{Schema: 1, KeyLocation: safeKeyLocation("yubikey://9c?pin=123456"), Tool: reportTool{Path: "/tools/nuget-sign", Version: "nuget-sign version 0.1.0", SHA256: "abc"}, Packages: []reportPackage{{ID: "Z"}, {ID: "A"}}, Artifacts: []reportArtifact{{Path: "z"}, {Path: "a"}}}
	b, err := marshalReport(r)
	if err != nil {
		t.Fatal(err)
	}
	if strings.Contains(string(b), "123456") || strings.Contains(string(b), "NUGET_SIGN_PIN") {
		t.Fatal("secret leaked")
	}
	var got report
	if err := json.Unmarshal(b, &got); err != nil {
		t.Fatal(err)
	}
	if got.Packages[0].ID != "A" || got.Artifacts[0].Path != "a" || got.Tool.Path != "/tools/nuget-sign" {
		t.Fatalf("report %s", b)
	}
	if err := os.Mkdir(filepath.Join(dir, "signed"), 0o700); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(filepath.Join(dir, "signed", "stale"), []byte("keep"), 0o600); err != nil {
		t.Fatal(err)
	}
	if err := commitOutputs(filepath.Join(dir, "signed"), stage, []string{"a.nupkg"}, b, false); err == nil {
		t.Fatal("overwrote stale output")
	}
}

type fakeRunner struct {
	output      []byte
	err         error
	calls       [][]string
	attached    [][]string
	attachedEnv [][]string
	run         func([]string) ([]byte, error)
	attachedRun func([]string) error
}

func (f *fakeRunner) Run(_ context.Context, name string, args ...string) ([]byte, error) {
	call := append([]string{name}, args...)
	f.calls = append(f.calls, call)
	if f.run != nil {
		return f.run(call)
	}
	return f.output, f.err
}
func (f *fakeRunner) RunAttached(_ context.Context, name string, environment []string, args ...string) error {
	call := append([]string{name}, args...)
	f.attached = append(f.attached, call)
	f.attachedEnv = append(f.attachedEnv, environment)
	if f.attachedRun != nil {
		return f.attachedRun(call)
	}
	return f.err
}

func testCertificateChain(t *testing.T, dir string) (string, string, *x509.Certificate) {
	t.Helper()
	now := time.Now()
	rootKey, _ := rsa.GenerateKey(rand.Reader, 2048)
	rootTemplate := &x509.Certificate{SerialNumber: big.NewInt(1), Subject: pkix.Name{CommonName: "root"}, NotBefore: now.Add(-time.Hour), NotAfter: now.Add(time.Hour), BasicConstraintsValid: true, IsCA: true, KeyUsage: x509.KeyUsageCertSign}
	rootDER, err := x509.CreateCertificate(rand.Reader, rootTemplate, rootTemplate, &rootKey.PublicKey, rootKey)
	if err != nil {
		t.Fatal(err)
	}
	root, _ := x509.ParseCertificate(rootDER)
	leafKey, _ := rsa.GenerateKey(rand.Reader, 2048)
	leafTemplate := &x509.Certificate{SerialNumber: big.NewInt(2), Subject: pkix.Name{CommonName: "leaf"}, NotBefore: now.Add(-time.Hour), NotAfter: now.Add(time.Hour), KeyUsage: x509.KeyUsageDigitalSignature, ExtKeyUsage: []x509.ExtKeyUsage{x509.ExtKeyUsageCodeSigning}}
	leafDER, err := x509.CreateCertificate(rand.Reader, leafTemplate, root, &leafKey.PublicKey, rootKey)
	if err != nil {
		t.Fatal(err)
	}
	leaf, _ := x509.ParseCertificate(leafDER)
	chainPath, rootPath := filepath.Join(dir, "chain.pem"), filepath.Join(dir, "root.pem")
	if err := os.WriteFile(chainPath, pem.EncodeToMemory(&pem.Block{Type: "CERTIFICATE", Bytes: leaf.Raw}), 0o600); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(rootPath, pem.EncodeToMemory(&pem.Block{Type: "CERTIFICATE", Bytes: root.Raw}), 0o600); err != nil {
		t.Fatal(err)
	}
	return chainPath, rootPath, leaf
}
