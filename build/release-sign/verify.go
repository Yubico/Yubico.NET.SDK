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
	if err := validateNugetSignEvidence(verificationOutput, fingerprint, names); err != nil {
		return fmt.Errorf("nuget-sign verify: %w", err)
	}
	if err := verifyPreservation(info.Path, output, selected, info.Kind == "snupkg"); err != nil {
		return err
	}
	return nil
}

func validateNugetSignEvidence(output []byte, fingerprint string, assemblies []string) error {
	text := string(output)
	if !strings.Contains(strings.ToLower(text), "valid author signature") {
		return fmt.Errorf("missing valid author signature evidence")
	}
	if !strings.Contains(text, fingerprint) {
		return fmt.Errorf("missing certificate fingerprint evidence")
	}
	if len(assemblies) != 0 && !strings.Contains(strings.ToLower(text), "assemblies") {
		return fmt.Errorf("missing assemblies summary")
	}
	for _, name := range assemblies {
		if !strings.Contains(text, name) {
			return fmt.Errorf("missing assembly evidence for %s", name)
		}
	}
	return nil
}
