package main

import (
	"context"
	"fmt"
)

// verifySignedPackage asks nuget-sign to verify the package signature and, for
// a .nupkg, every assembly inside, all pinned to the expected signer. It then
// checks that everything the manifest did not select is byte-for-byte the
// attested build output.
func verifySignedPackage(ctx context.Context, runner commandRunner, cfg runConfig, info packageInfo, output string, selected map[string]struct{}, fingerprint string) error {
	args := []string{"verify", "--revocation", "none", "--root", cfg.RootPath, "--timestamp-root", cfg.TimestampRootPath, "--certificate-fingerprint", fingerprint}
	if info.Kind == "nupkg" {
		args = append(args, "--assemblies")
	}
	args = append(args, output)
	if _, err := runner.Run(ctx, cfg.NugetSign, args...); err != nil {
		return fmt.Errorf("nuget-sign verify: %w", err)
	}
	return verifyPreservation(info.Path, output, selected)
}
