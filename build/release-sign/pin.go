package main

import (
	"context"
	"errors"
	"fmt"
	"os"
	"strings"

	"golang.org/x/term"
)

func acquireSigningPIN(keyLocation string, prompt func() ([]byte, error)) ([]byte, error) {
	if !strings.HasPrefix(strings.ToLower(keyLocation), "yubikey://") {
		return nil, nil
	}
	if pin, set := os.LookupEnv("NUGET_SIGN_PIN"); set {
		if pin == "" {
			return nil, errors.New("NUGET_SIGN_PIN is empty")
		}
		// Legacy input only: never forward this environment entry to children.
		return []byte(pin), nil
	}
	pin, err := prompt()
	if err != nil {
		clear(pin[:cap(pin)])
		return nil, err
	}
	if len(pin) == 0 {
		clear(pin[:cap(pin)])
		return nil, errors.New("PIN must not be empty")
	}
	return pin, nil
}

func promptSigningPIN(ctx context.Context, keyLocation string) ([]byte, error) {
	fd := int(os.Stdin.Fd())
	if !term.IsTerminal(fd) {
		return nil, errors.New("no terminal for PIN prompt; run release-sign in a local terminal")
	}
	fmt.Fprintf(os.Stderr, "Enter PIN for %s: ", safeKeyLocation(keyLocation))
	pin, err := term.ReadPassword(fd)
	fmt.Fprintln(os.Stderr)
	if ctx.Err() != nil {
		err = ctx.Err()
	}
	return pin, err
}
