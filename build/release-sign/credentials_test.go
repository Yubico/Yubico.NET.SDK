package main

import (
	"errors"
	"os"
	"path/filepath"
	"reflect"
	"strings"
	"testing"
)

func TestTaskSpecificCredentialEnvironments(t *testing.T) {
	for _, name := range []string{"GH_TOKEN", "NUGET_API_KEY", "NUGET_SIGN_PIN", "NUGET_SIGN_PASSWORD", "GOOGLE_APPLICATION_CREDENTIALS", "HTTPS_PROXY", "SSL_CERT_FILE"} {
		t.Setenv(name, "test-value")
	}
	for _, purpose := range []string{"sign", "sign-pin", "verify", "attest"} {
		env := map[string]string{}
		for _, item := range childEnvironment(purpose) {
			name, value, _ := strings.Cut(item, "=")
			env[name] = value
		}
		if env["NUGET_API_KEY"] != "" || env["NUGET_SIGN_PIN"] != "" {
			t.Fatalf("%s inherited unrelated credentials", purpose)
		}
		if (env["GH_TOKEN"] != "") != (purpose == "attest") {
			t.Fatalf("%s: GitHub credentials sent to wrong task", purpose)
		}
		if (env["NUGET_SIGN_PASSWORD"] != "") != (purpose == "sign") || (env["GOOGLE_APPLICATION_CREDENTIALS"] != "") != (purpose == "sign") {
			t.Fatalf("%s: file/KMS credentials sent to wrong task", purpose)
		}
		if env["HTTPS_PROXY"] == "" || env["SSL_CERT_FILE"] == "" {
			t.Fatalf("%s lost proxy/trust settings", purpose)
		}
	}
}

func TestBatchPackageSigningAndFailure(t *testing.T) {
	for _, failure := range []bool{false, true} {
		dir := t.TempDir()
		staging := filepath.Join(dir, "staging")
		if err := os.Mkdir(staging, 0o700); err != nil {
			t.Fatal(err)
		}
		packages := []packageInfo{}
		for _, name := range []string{"A.1.snupkg", "B.1.snupkg"} {
			path := writeZip(t, dir, name, []zipItem{{"test.pdb", []byte("symbols")}})
			packages = append(packages, packageInfo{Path: path, Name: name, Kind: "snupkg"})
		}
		cfg := runConfig{NugetSign: "nuget-sign", signingPIN: []byte("test-pin")}
		runner := &fakeRunner{attachedRun: func(call []string) error {
			if failure {
				return errors.New("PIN verification failed")
			}
			for _, path := range call[len(call)-2:] {
				addSignature(t, path, path)
			}
			return nil
		}}
		_, err := prepareAndSignPackages(t.Context(), runner, cfg, packages, staging)
		if (err != nil) != failure {
			t.Fatalf("batch failure=%v: %v", failure, err)
		}
		if len(runner.attached) != 1 {
			t.Fatalf("expected one batch with no retries, got %d calls", len(runner.attached))
		}
		call := runner.attached[0]
		wantPaths := []string{filepath.Join(staging, packages[0].Name), filepath.Join(staging, packages[1].Name)}
		if !reflect.DeepEqual(call[len(call)-2:], wantPaths) {
			t.Fatal("packages were not batched from staging")
		}
		for _, info := range packages {
			contents, err := archiveContents(info.Path)
			if err != nil {
				t.Fatal(err)
			}
			if _, signed := contents[".signature.p7s"]; signed {
				t.Fatal("modified the attested input")
			}
			if !failure {
				if err := verifyPreservation(info.Path, filepath.Join(staging, info.Name), nil); err != nil {
					t.Fatal(err)
				}
			}
		}
	}
}

func TestOldSignerRejectedBeforeSigning(t *testing.T) {
	bin := filepath.Join(t.TempDir(), "nuget-sign")
	if err := os.WriteFile(bin, []byte("old tool"), 0o700); err != nil {
		t.Fatal(err)
	}
	runner := &fakeRunner{run: func(args []string) ([]byte, error) {
		if args[1] == "--version" {
			return []byte("nuget-sign version 0.1.0"), nil
		}
		return nil, errors.New("unknown command capabilities")
	}}
	if _, err := inspectNugetSign(t.Context(), runner, bin); err == nil || !strings.Contains(err.Error(), "upgrade nuget-sign") {
		t.Fatalf("expected actionable compatibility failure: %v", err)
	}
	if len(runner.attached) != 0 {
		t.Fatal("attempted signing with incompatible tool")
	}
}
