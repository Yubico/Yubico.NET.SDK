package main

import (
	"bytes"
	"encoding/hex"
	"strings"
	"testing"
)

type commandFixture struct {
	*signingFixture
	packagePath       string
	rootPath          string
	timestampRootPath string
	fingerprint       string
}

func newCommandFixture(t *testing.T, entries ...packageEntry) commandFixture {
	t.Helper()
	return commandFixtureFor(t, newSigningFixture(t), entries...)
}

func commandFixtureFor(t *testing.T, fixture *signingFixture, entries ...packageEntry) commandFixture {
	t.Helper()
	directory := t.TempDir()
	return commandFixture{
		signingFixture:    fixture,
		packagePath:       writeNupkg(t, entries...),
		rootPath:          writePEM(t, directory, "root.pem", fixture.codeRoot.certificate),
		timestampRootPath: writePEM(t, directory, "timestamp-root.pem", fixture.timestampRoot.certificate),
		fingerprint:       hex.EncodeToString(fixture.signerFingerprint()),
	}
}

func (f commandFixture) args(assemblies ...string) []string {
	args := []string{
		"--package", f.packagePath,
		"--root", f.rootPath,
		"--timestamp-root", f.timestampRootPath,
		"--signer-fingerprint", f.fingerprint,
	}
	for _, assembly := range assemblies {
		args = append(args, "--assembly", assembly)
	}
	return args
}

func TestRunPrintsOnePassLinePerAssembly(t *testing.T) {
	fixture := newSigningFixture(t)
	signed := fixture.sign(t, minimalPE())
	command := commandFixtureFor(t, fixture,
		packageEntry{name: "lib/net47/Yubico.Core.dll", contents: signed},
		packageEntry{name: "lib/net47/Yubico.YubiKey.dll", contents: signed},
		packageEntry{name: "Yubico.Core.nuspec", contents: []byte("<package />")},
	)

	var stdout, stderr bytes.Buffer
	err := run(command.args("lib/net47/Yubico.Core.dll", "lib/net47/Yubico.YubiKey.dll"), &stdout, &stderr)
	if err != nil {
		t.Fatalf("run failed: %v\nstderr:\n%s", err, stderr.String())
	}

	lines := strings.Split(strings.TrimSuffix(stdout.String(), "\n"), "\n")
	if len(lines) != 2 {
		t.Fatalf("got %d output lines, want 2:\n%s", len(lines), stdout.String())
	}
	for index, want := range []string{"lib/net47/Yubico.Core.dll", "lib/net47/Yubico.YubiKey.dll"} {
		if !strings.HasPrefix(lines[index], "PASS "+want+" ") {
			t.Fatalf("line %d is %q, want a PASS line for %s", index, lines[index], want)
		}
		if !strings.Contains(lines[index], "image=SHA-256") {
			t.Fatalf("line %d does not report the image digest: %q", index, lines[index])
		}
		if !strings.Contains(lines[index], "signer="+strings.ToUpper(command.fingerprint)) {
			t.Fatalf("line %d does not report the pinned signer: %q", index, lines[index])
		}
	}
	if stderr.Len() != 0 {
		t.Fatalf("unexpected stderr: %s", stderr.String())
	}
}

func TestRunFailsWhenAnAssemblyFailsVerification(t *testing.T) {
	fixture := newSigningFixture(t)
	good := fixture.sign(t, minimalPE())
	bad := append([]byte(nil), good...)
	bad[511] ^= 0x01
	command := commandFixtureFor(t, fixture,
		packageEntry{name: "good.dll", contents: good},
		packageEntry{name: "bad.dll", contents: bad},
	)

	var stdout, stderr bytes.Buffer
	err := run(command.args("good.dll", "bad.dll"), &stdout, &stderr)
	if err == nil {
		t.Fatal("run succeeded with a modified assembly")
	}
	if !strings.Contains(stdout.String(), "PASS good.dll ") {
		t.Fatalf("the sound assembly was not reported: %q", stdout.String())
	}
	if !strings.Contains(stderr.String(), "FAIL bad.dll") {
		t.Fatalf("the modified assembly was not reported: %q", stderr.String())
	}
}

func TestRunRequiresEveryFlag(t *testing.T) {
	fixture := newCommandFixture(t, packageEntry{name: "lib/a.dll", contents: []byte("a")})
	full := fixture.args("lib/a.dll")

	// full is --package, --root, --timestamp-root, --signer-fingerprint, then
	// --assembly, two arguments each.
	without := func(pair int) []string {
		return append(append([]string{}, full[:pair*2]...), full[pair*2+2:]...)
	}
	for _, test := range []struct {
		name string
		args []string
		want string
	}{
		{"no package", without(0), "--package"},
		{"no root", without(1), "--root"},
		{"no timestamp root", without(2), "--timestamp-root"},
		{"no signer fingerprint", without(3), "--signer-fingerprint"},
		{"no assembly", full[:8], "--assembly"},
		{"nothing", nil, "--package"},
	} {
		t.Run(test.name, func(t *testing.T) {
			var stdout, stderr bytes.Buffer
			err := run(test.args, &stdout, &stderr)
			if err == nil || !strings.Contains(err.Error(), test.want) {
				t.Fatalf("run(%v) = %v, want a %s requirement", test.args, err, test.want)
			}
			if stdout.Len() != 0 {
				t.Fatalf("unexpected stdout: %s", stdout.String())
			}
		})
	}
}

// A malformed fingerprint is rejected before the package is opened: it is a
// mistake in the invocation, not a verdict about the package.
func TestRunRejectsMalformedSignerFingerprint(t *testing.T) {
	for name, given := range map[string]string{
		"empty":     "",
		"too short": strings.Repeat("ab", 31),
		"too long":  strings.Repeat("ab", 33),
		"not hex":   strings.Repeat("zz", 32),
	} {
		t.Run(name, func(t *testing.T) {
			fixture := newCommandFixture(t, packageEntry{name: "lib/a.dll", contents: []byte("a")})
			fixture.fingerprint = given
			var stdout, stderr bytes.Buffer
			err := run(fixture.args("lib/a.dll"), &stdout, &stderr)
			if err == nil || !strings.Contains(err.Error(), "--signer-fingerprint") {
				t.Fatalf("run with a %s fingerprint = %v", name, err)
			}
		})
	}
}

func TestRunAcceptsAFormattedSignerFingerprint(t *testing.T) {
	fixture := newSigningFixture(t)
	signed := fixture.sign(t, minimalPE())
	command := commandFixtureFor(t, fixture, packageEntry{name: "lib/a.dll", contents: signed})

	// The form an operator gets from certmgr or `openssl x509 -fingerprint`.
	var spaced []string
	for index := 0; index < len(command.fingerprint); index += 2 {
		spaced = append(spaced, command.fingerprint[index:index+2])
	}
	command.fingerprint = strings.ToUpper(strings.Join(spaced, ":"))

	var stdout, stderr bytes.Buffer
	if err := run(command.args("lib/a.dll"), &stdout, &stderr); err != nil {
		t.Fatalf("run failed: %v\nstderr:\n%s", err, stderr.String())
	}
	if !strings.HasPrefix(stdout.String(), "PASS lib/a.dll ") {
		t.Fatalf("unexpected output: %q", stdout.String())
	}
}

func TestRunRejectsWrongSignerFingerprint(t *testing.T) {
	fixture := newSigningFixture(t)
	signed := fixture.sign(t, minimalPE())
	command := commandFixtureFor(t, fixture, packageEntry{name: "lib/a.dll", contents: signed})
	command.fingerprint = strings.Repeat("ab", 32)

	var stdout, stderr bytes.Buffer
	err := run(command.args("lib/a.dll"), &stdout, &stderr)
	if err == nil {
		t.Fatal("run accepted an assembly signed by a different certificate")
	}
	if !strings.Contains(stderr.String(), "FAIL lib/a.dll") {
		t.Fatalf("the unexpected signer was not reported: %q", stderr.String())
	}
	if stdout.Len() != 0 {
		t.Fatalf("unexpected stdout: %s", stdout.String())
	}
}

func TestRunRejectsUnknownFlag(t *testing.T) {
	fixture := newCommandFixture(t, packageEntry{name: "lib/a.dll", contents: []byte("a")})
	var stdout, stderr bytes.Buffer
	if err := run(append(fixture.args("lib/a.dll"), "--skip-timestamp"), &stdout, &stderr); err == nil {
		t.Fatal("run accepted an unknown flag")
	}
}

func TestRunRejectsMissingPackage(t *testing.T) {
	fixture := newCommandFixture(t, packageEntry{name: "lib/a.dll", contents: []byte("a")})
	fixture.packagePath += ".absent"
	var stdout, stderr bytes.Buffer
	if err := run(fixture.args("lib/a.dll"), &stdout, &stderr); err == nil {
		t.Fatal("run accepted a package path that does not exist")
	}
}
