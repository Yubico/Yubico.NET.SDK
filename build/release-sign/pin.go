package main

import (
	"errors"
	"fmt"
	"os"
	"strings"

	"golang.org/x/term"
)

func acquireSigningPIN(keyLocation string, prompt func() (string, error)) (string, error) {
	if !strings.HasPrefix(strings.ToLower(keyLocation), "yubikey://") {
		return "", nil
	}
	if pin, set := os.LookupEnv("NUGET_SIGN_PIN"); set {
		if pin == "" {
			return "", errors.New("NUGET_SIGN_PIN is empty")
		}
		return "", nil
	}
	pin, err := prompt()
	if err != nil {
		return "", err
	}
	if pin == "" {
		return "", errors.New("PIN must not be empty")
	}
	return pin, nil
}

func promptSigningPIN(keyLocation string) (string, error) {
	fd := int(os.Stdin.Fd())
	if !term.IsTerminal(fd) {
		return "", errors.New("no terminal for PIN prompt; run release-sign in a local terminal or set NUGET_SIGN_PIN")
	}
	fmt.Fprintf(os.Stderr, "Enter PIN for %s: ", safeKeyLocation(keyLocation))
	pin, err := term.ReadPassword(fd)
	fmt.Fprintln(os.Stderr)
	return string(pin), err
}
