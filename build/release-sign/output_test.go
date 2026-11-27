package main

import (
	"bytes"
	"context"
	"errors"
	"os"
	"path/filepath"
	"testing"
)

func TestOutputPublicationCancellation(t *testing.T) {
	for _, phase := range []string{"before-copy", "during-copy", "before-publication", "success"} {
		t.Run(phase, func(t *testing.T) {
			dir := t.TempDir()
			stage := filepath.Join(dir, "stage")
			signed := filepath.Join(dir, "signed")
			for _, path := range []string{stage, signed} {
				if err := os.Mkdir(path, 0o700); err != nil {
					t.Fatal(err)
				}
			}
			contents := bytes.Repeat([]byte("signed package"), 100000)
			if err := os.WriteFile(filepath.Join(stage, "a.nupkg"), contents, 0o600); err != nil {
				t.Fatal(err)
			}
			oldOutput := filepath.Join(signed, "existing-output")
			if err := os.WriteFile(oldOutput, []byte("keep"), 0o600); err != nil {
				t.Fatal(err)
			}
			ctx, cancel := context.WithCancel(t.Context())
			defer cancel()
			// Cancel at observable filesystem milestones without timing races or
			// relying on a particular number of context checks or copy buffers.
			observed := false
			cancelContext := outputCancellationContext{Context: ctx, check: func() {
				trigger := phase == "before-copy"
				pattern := filepath.Join(dir, ".release-sign-output-*", "packages", "a.nupkg")
				if phase == "before-publication" {
					pattern = filepath.Join(dir, ".release-sign-output-*", "report.json")
				}
				if phase == "during-copy" || phase == "before-publication" {
					matches, err := filepath.Glob(pattern)
					if err != nil {
						t.Fatal(err)
					}
					for _, path := range matches {
						info, err := os.Stat(path)
						if err != nil {
							t.Fatal(err)
						}
						if info.Size() > 0 && (phase != "during-copy" || info.Size() < int64(len(contents))) {
							trigger = true
						}
					}
				}
				if trigger {
					observed = true
					cancel()
				}
			}}
			err := commitOutputs(cancelContext, signed, stage, []string{"a.nupkg"}, []byte("{}\n"), true)
			if phase == "success" {
				if err != nil {
					t.Fatal(err)
				}
				got, err := os.ReadFile(filepath.Join(signed, "packages", "a.nupkg"))
				if err != nil || !bytes.Equal(got, contents) {
					t.Fatalf("published package mismatch: %v", err)
				}
			} else {
				if !observed || !errors.Is(err, context.Canceled) {
					t.Fatalf("cancellation milestone observed=%v, error=%v", observed, err)
				}
				got, err := os.ReadFile(oldOutput)
				if err != nil || string(got) != "keep" {
					t.Fatalf("cancellation replaced existing output: %q, %v", got, err)
				}
				entries, err := os.ReadDir(signed)
				if err != nil || len(entries) != 1 {
					t.Fatalf("cancellation published partial output: %v, %v", entries, err)
				}
			}
			leftovers, err := filepath.Glob(filepath.Join(dir, ".release-sign-output-*"))
			if err != nil || len(leftovers) != 0 {
				t.Fatalf("temporary output not cleaned up: %v, %v", leftovers, err)
			}
		})
	}
}

type outputCancellationContext struct {
	context.Context
	check func()
}

func (c outputCancellationContext) Err() error {
	c.check()
	return c.Context.Err()
}
