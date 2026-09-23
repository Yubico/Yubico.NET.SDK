package main

import (
	"context"
	"fmt"
	"sort"
	"strings"
)

func verifySignedPackage(ctx context.Context, runner commandRunner, cfg runConfig, info packageInfo, output string, selected map[string]struct{}, fingerprint string) error {
	names := make([]string, 0, len(selected))
	for name := range selected {
		names = append(names, name)
	}
	sort.Strings(names)
	args := []string{"verify", "--revocation", "none", "--root", cfg.RootPath, "--timestamp-root", cfg.TimestampRootPath, "--certificate-fingerprint", fingerprint}
	if info.Kind == "nupkg" {
		args = append(args, "--assemblies")
	}
	args = append(args, output)
	verificationOutput, err := runner.Run(ctx, cfg.NugetSign, args...)
	if err != nil {
		return fmt.Errorf("nuget-sign verify: %w", err)
	}
	if err := requireVerifiedAssemblies(verificationOutput, names); err != nil {
		return fmt.Errorf("nuget-sign verify: %w", err)
	}
	if err := verifyPreservation(info.Path, output, selected); err != nil {
		return err
	}
	return nil
}

// The verifier can exit successfully while reporting an unsigned assembly.
// Require a successful result for each assembly selected by the manifest.
func requireVerifiedAssemblies(output []byte, assemblies []string) error {
	lines := strings.Split(string(output), "\n")
	for _, name := range assemblies {
		verified := false
		for _, line := range lines {
			status, found := strings.CutPrefix(strings.TrimSpace(line), name)
			if found && strings.HasPrefix(status, "  ") && strings.Contains(status, ", digest matches") {
				verified = true
				break
			}
		}
		if !verified {
			return fmt.Errorf("missing successful assembly verification for %s", name)
		}
	}
	return nil
}
