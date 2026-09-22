package main

import (
	"bytes"
	"context"
	"crypto"
	"crypto/x509"
	"errors"
	"fmt"
	"sort"

	"github.com/Yubico/nuget-sign/pkg/assembly"
	"github.com/Yubico/nuget-sign/pkg/nuget"
)

func verifySignedPackage(ctx context.Context, info packageInfo, output string, selected map[string]struct{}, expected map[string][]byte, roots, timestampRoots *x509.CertPool, signer *x509.Certificate) error {
	file, size, err := openFile(output)
	if err != nil {
		return err
	}
	defer file.Close()
	pkg, err := nuget.Open(file, size)
	if err != nil {
		return err
	}
	verification, err := pkg.Verify(ctx, nuget.VerifyOptions{Roots: roots, TimestampRoots: timestampRoots})
	if err != nil {
		return fmt.Errorf("verify NuGet signature: %w", err)
	}
	if verification.Type != nuget.AuthorSignature || verification.TimestampedAt.IsZero() {
		return errors.New("NuGet package must have a timestamped author signature")
	}
	if verification.Hash != crypto.SHA256 {
		return fmt.Errorf("NuGet package signature hash is %s, want SHA-256", verification.Hash)
	}
	if verification.Repository != nil {
		return errors.New("NuGet package unexpectedly has a repository countersignature")
	}
	if len(verification.Warnings) != 0 {
		return fmt.Errorf("NuGet package verification returned warnings: %v", verification.Warnings)
	}
	if !verification.Certificate.Equal(signer) {
		return errors.New("NuGet signer differs from selected signing certificate")
	}
	if err := verifyPreservation(info.Path, output, selected, info.Kind == "snupkg"); err != nil {
		return err
	}
	if info.Kind == "snupkg" {
		return nil
	}
	names := make([]string, 0, len(selected))
	for name := range selected {
		names = append(names, name)
	}
	sort.Strings(names)
	for _, name := range names {
		contents, err := zipEntry(output, name)
		if err != nil {
			return err
		}
		file, err := assembly.Open(contents)
		if err != nil {
			return err
		}
		verified, err := file.Verify(ctx, assembly.VerifyOptions{Roots: roots, TimestampRoots: timestampRoots})
		if err != nil {
			return fmt.Errorf("verify Authenticode %s: %w", name, err)
		}
		if verified.Hash != crypto.SHA256 || !verified.Matches() || verified.TimestampedAt.IsZero() {
			return fmt.Errorf("Authenticode %s must have a matching SHA-256 digest and timestamp", name)
		}
		if !verified.Certificate.Equal(signer) {
			return fmt.Errorf("Authenticode signer differs for %s", name)
		}
		if !bytes.Equal(verified.ActualDigest, expected[name]) {
			return fmt.Errorf("selected assembly image digest changed: %s", name)
		}
	}
	return nil
}
