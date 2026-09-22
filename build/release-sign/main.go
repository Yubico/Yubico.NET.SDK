package main

import (
	"context"
	"errors"
	"flag"
	"fmt"
	"os"
)

const defaultTimestamper = "http://timestamp.digicert.com"

func main() {
	if err := execute(context.Background(), os.Args[1:], osRunner{}); err != nil {
		fmt.Fprintln(os.Stderr, "error:", err)
		os.Exit(1)
	}
}

func execute(ctx context.Context, args []string, runner commandRunner) error {
	if len(args) == 0 || args[0] != "run" {
		return errors.New("usage: release-sign run --component core|nativeshims --working-directory DIR --artifact ZIP [--artifact ZIP...] --manifest FILE --source-digest COMMIT --key LOCATION --certificate PEM --root PEM --timestamp-root PEM --independent-verifier PATH [--timestamper URL] [--clean]")
	}
	fs := flag.NewFlagSet("run", flag.ContinueOnError)
	var artifacts stringList
	cfg := runConfig{}
	fs.StringVar(&cfg.Component, "component", "", "core or nativeshims")
	fs.StringVar(&cfg.WorkingDirectory, "working-directory", "", "owned working directory")
	fs.Var(&artifacts, "artifact", "artifact ZIP (repeatable)")
	fs.StringVar(&cfg.ManifestPath, "manifest", "", "release manifest")
	fs.StringVar(&cfg.KeyLocation, "key", os.Getenv("RELEASE_SIGN_KEY"), "signing key location")
	fs.StringVar(&cfg.CertificatePath, "certificate", os.Getenv("RELEASE_SIGN_CERTIFICATE"), "signer certificate chain")
	fs.StringVar(&cfg.RootPath, "root", os.Getenv("RELEASE_SIGN_ROOT"), "signer trust root")
	fs.StringVar(&cfg.TimestampRootPath, "timestamp-root", os.Getenv("RELEASE_SIGN_TIMESTAMP_ROOT"), "timestamp trust root")
	fs.StringVar(&cfg.IndependentVerifier, "independent-verifier", os.Getenv("RELEASE_SIGN_INDEPENDENT_VERIFIER"), "independent verifier executable")
	fs.StringVar(&cfg.SourceDigest, "source-digest", os.Getenv("RELEASE_SIGN_SOURCE_DIGEST"), "40-character Git commit digest")
	fs.StringVar(&cfg.Timestamper, "timestamper", defaultTimestamper, "RFC 3161 timestamp authority")
	fs.BoolVar(&cfg.Clean, "clean", false, "replace existing signed output")
	if err := fs.Parse(args[1:]); err != nil {
		return err
	}
	if fs.NArg() != 0 {
		return fmt.Errorf("unexpected arguments: %v", fs.Args())
	}
	cfg.Artifacts = artifacts
	return run(ctx, cfg, runner, acquireProductionSigner)
}

type stringList []string

func (s *stringList) String() string { return fmt.Sprint([]string(*s)) }
func (s *stringList) Set(value string) error {
	*s = append(*s, value)
	return nil
}
