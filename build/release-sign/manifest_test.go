package main

import (
	"os"
	"path/filepath"
	"reflect"
	"strings"
	"testing"
)

func TestManifestStrictJSONAndComponentScope(t *testing.T) {
	valid := `{"schema":1,"attestationRepo":"Yubico/Yubico.NET.SDK","signerWorkflow":"Yubico/Yubico.NET.SDK/.github/workflows/build.yml","packages":{"Yubico.Core":{"symbols":"required","authenticode":{"include":["lib/net472/Yubico.Core.dll"],"firstParty":["Yubico.*.dll"],"alreadySigned":"reject"}},"Yubico.YubiKey":{"symbols":"required","authenticode":{"include":["lib/net472/Yubico.YubiKey.dll"],"firstParty":["Yubico.*.dll"],"alreadySigned":"reject"}}}}`
	tests := []struct {
		name, body, component, want string
	}{
		{"valid", valid, "core", ""},
		{"unknown field", strings.Replace(valid, `"schema":1`, `"schema":1,"extra":true`, 1), "core", "unknown field"},
		{"wrong schema", strings.Replace(valid, `"schema":1`, `"schema":2`, 1), "core", "schema must be 1"},
		{"missing workflow", strings.Replace(valid, `,"signerWorkflow":"Yubico/Yubico.NET.SDK/.github/workflows/build.yml"`, ``, 1), "core", "signerWorkflow"},
		{"wrong workflow", strings.Replace(valid, `build.yml`, `other.yml`, 1), "core", "signerWorkflow"},
		{"unknown package", strings.Replace(valid, `"Yubico.Core"`, `"Other"`, 1), "core", "unknown package ID"},
		{"wrong component", strings.Replace(valid, `build.yml`, `build-nativeshims.yml`, 1), "nativeshims", "not valid for component"},
		{"bad symbols", strings.Replace(valid, `"required"`, `"sometimes"`, 1), "core", "symbols"},
	}
	for _, test := range tests {
		t.Run(test.name, func(t *testing.T) {
			name := filepath.Join(t.TempDir(), "manifest.json")
			if err := os.WriteFile(name, []byte(test.body), 0o600); err != nil {
				t.Fatal(err)
			}
			_, _, err := loadManifest(name, test.component)
			if test.want == "" && err != nil {
				t.Fatal(err)
			}
			if test.want != "" && (err == nil || !strings.Contains(err.Error(), test.want)) {
				t.Fatalf("want %q, got %v", test.want, err)
			}
		})
	}
}

func TestCommittedManifestsContainExactReleaseScope(t *testing.T) {
	core, _, err := loadManifest("manifests/core.json", "core")
	if err != nil {
		t.Fatal(err)
	}
	for id, want := range map[string][]string{
		"Yubico.Core":    {"lib/net472/Yubico.Core.dll", "lib/netstandard2.0/Yubico.Core.dll", "lib/netstandard2.1/Yubico.Core.dll"},
		"Yubico.YubiKey": {"lib/net472/Yubico.YubiKey.dll", "lib/netstandard2.0/Yubico.YubiKey.dll", "lib/netstandard2.1/Yubico.YubiKey.dll"},
	} {
		if got := core.Packages[id].Authenticode.Include; !reflect.DeepEqual(got, want) {
			t.Fatalf("%s includes %v, want %v", id, got, want)
		}
	}
	native, _, err := loadManifest("manifests/nativeshims.json", "nativeshims")
	if err != nil {
		t.Fatal(err)
	}
	want := []string{"runtimes/win-arm64/native/Yubico.NativeShims.dll", "runtimes/win-x64/native/Yubico.NativeShims.dll", "runtimes/win-x86/native/Yubico.NativeShims.dll"}
	if got := native.Packages["Yubico.NativeShims"].Authenticode.Include; !reflect.DeepEqual(got, want) {
		t.Fatalf("NativeShims includes %v, want %v", got, want)
	}
}
