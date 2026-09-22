// Command release-sign-relic-verify is an independent check on the
// Authenticode signatures inside a signed NuGet package.
//
// It exists to be a second opinion. The release pipeline signs assemblies with
// github.com/Yubico/nuget-sign and verifies the result with that same library;
// this program verifies the same assemblies with an unrelated implementation,
// github.com/sassoftware/relic, so that a bug in one is not confirmed by
// itself. It says nothing about the NuGet package signature — the caller
// checks that.
//
// See README.md for when this program should be deleted.
package main

import (
	"archive/zip"
	"crypto/sha256"
	"crypto/x509"
	"encoding/hex"
	"errors"
	"flag"
	"fmt"
	"io"
	"os"
	"strings"
	"time"
)

func main() {
	if err := run(os.Args[1:], os.Stdout, os.Stderr); err != nil {
		// flag.ContinueOnError has already reported usage errors.
		if !errors.Is(err, flag.ErrHelp) {
			fmt.Fprintln(os.Stderr, "release-sign-relic-verify:", err)
		}
		os.Exit(1)
	}
}

// repeatedFlag collects a flag that may be given more than once, in order.
type repeatedFlag []string

func (f *repeatedFlag) String() string { return strings.Join(*f, ",") }

func (f *repeatedFlag) Set(value string) error {
	*f = append(*f, value)
	return nil
}

func run(args []string, stdout, stderr io.Writer) error {
	flags := flag.NewFlagSet("release-sign-relic-verify", flag.ContinueOnError)
	flags.SetOutput(stderr)
	packagePath := flags.String("package", "", "signed NuGet package to read assemblies from")
	rootPath := flags.String("root", "", "PEM file of roots the code-signing certificate must chain to")
	timestampRootPath := flags.String("timestamp-root", "", "PEM file of roots the timestamp authority must chain to")
	signerFingerprint := flags.String("signer-fingerprint", "", "SHA-256 of the expected signing certificate, as 64 hexadecimal digits")
	var assemblies repeatedFlag
	flags.Var(&assemblies, "assembly", "ZIP entry path of an assembly to verify; repeat for each one")
	if err := flags.Parse(args); err != nil {
		return err
	}
	if flags.NArg() != 0 {
		return fmt.Errorf("unexpected argument %q", flags.Arg(0))
	}

	var missing []string
	for _, required := range []struct {
		name  string
		value string
	}{
		{"--package", *packagePath},
		{"--root", *rootPath},
		{"--timestamp-root", *timestampRootPath},
		{"--signer-fingerprint", *signerFingerprint},
	} {
		if required.value == "" {
			missing = append(missing, required.name)
		}
	}
	if len(assemblies) == 0 {
		missing = append(missing, "--assembly")
	}
	if len(missing) != 0 {
		return fmt.Errorf("%s %s required", strings.Join(missing, ", "), plural(len(missing), "is", "are"))
	}

	// A malformed fingerprint is a mistake in the invocation, not a verdict
	// about the package, so it is caught before anything is opened.
	expectedSigner, err := parseFingerprint(*signerFingerprint)
	if err != nil {
		return fmt.Errorf("--signer-fingerprint: %w", err)
	}

	roots, err := certificatePool(*rootPath)
	if err != nil {
		return err
	}
	timestampRoots, err := certificatePool(*timestampRootPath)
	if err != nil {
		return err
	}

	archive, err := zip.OpenReader(*packagePath)
	if err != nil {
		return err
	}
	defer archive.Close()
	selected, err := selectEntries(&archive.Reader, assemblies)
	if err != nil {
		return err
	}

	// Every requested assembly is checked even after one fails, so a single
	// run tells the operator everything that is wrong with the package.
	failures := 0
	for _, entry := range selected {
		result, err := verifyAssembly(entry.contents, roots, timestampRoots, expectedSigner)
		if err != nil {
			failures++
			fmt.Fprintf(stderr, "FAIL %s: %v\n", entry.name, err)
			continue
		}
		fmt.Fprintln(stdout, passLine(entry.name, result))
	}
	if failures != 0 {
		return fmt.Errorf("%d of %d %s failed verification", failures, len(selected), plural(len(selected), "assembly", "assemblies"))
	}
	return nil
}

func passLine(name string, result assemblyResult) string {
	return fmt.Sprintf("PASS %s image=%s timestamp=%s signer=%s tsa=%s subject=%s",
		name,
		result.imageHash,
		result.timestampedAt.UTC().Format(time.RFC3339),
		fingerprint(result.signer),
		fingerprint(result.timestampAuthority),
		result.signer.Subject,
	)
}

// fingerprint identifies a certificate by the SHA-256 of its DER encoding,
// which is what an operator can compare against the certificate they expect.
func fingerprint(certificate *x509.Certificate) string {
	digest := sha256.Sum256(certificate.Raw)
	return strings.ToUpper(hex.EncodeToString(digest[:]))
}

func plural(count int, one, many string) string {
	if count == 1 {
		return one
	}
	return many
}
